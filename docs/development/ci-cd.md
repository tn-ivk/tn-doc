# CI/CD

## Обзор

Проект TN_Doc использует GitHub Actions для автоматизации процессов сборки, тестирования и упаковки приложения. CI/CD инфраструктура состоит из двух основных workflow файлов, которые выполняют различные задачи в зависимости от типа изменений в репозитории.

### Основные компоненты

- **GitHub Actions** — платформа для автоматизации CI/CD процессов
- **.NET SDK 8.0** — используется для сборки и тестирования
- **Ubuntu latest** — операционная система для всех runner'ов
- **NuGet** — управление зависимостями (включая приватные источники)
- **Git Submodules** — управление зависимостями от приватных репозиториев

## Workflows

### tests-on-push.yml

**Назначение**: Автоматический запуск тестов при каждом push в develop-ветки для обеспечения качества кода.

**Файл**: `.github/workflows/tests-on-push.yml`

**Триггер**:
```yaml
on:
  push:
    branches:
      - 'develop*'
```

Workflow запускается при push в любую ветку, начинающуюся с `develop` (например: `develop`, `developWork`, `develop-feature-x`).

**Переменные окружения**:
- `DOTNET_SDK_VERSION: '8.0'` — версия .NET SDK
- `DOTNET_CLI_TELEMETRY_OPTOUT: 'true'` — отключение телеметрии
- `DOTNET_NOLOGO: 'true'` — отключение логотипа .NET

**Jobs**:

#### 1. build

**Назначение**: Компиляция проекта и создание артефактов для последующих job'ов.

**Основные шаги**:

1. **Configure submodule URLs** — настройка URL субмодулей для доступа к приватным репозиториям через GitHub:
   ```bash
   git config --global url."https://x-access-token:${TOKEN}@github.com/tn-ivk/tn-docgeneral.git".insteadOf "https://git.tncpa.ru/orpovy/ivk/tn.docgeneral.git"
   ```
   Маппинг субмодулей:
   - `tn.docgeneral` → `github.com/tn-ivk/tn-docgeneral.git`
   - `tn_toolsfastreport` → `github.com/tn-ivk/tn-tools.git`
   - `winprutil` → `github.com/tn-ivk/winprutil.git`
   - `tn.utils` → `github.com/tn-ivk/tn-utils.git` (вложенный субмодуль)

2. **Checkout repository** — клонирование репозитория с рекурсивной загрузкой субмодулей:
   ```yaml
   uses: actions/checkout@v4
   with:
     submodules: recursive
     fetch-depth: 0
     token: ${{ secrets.GH_SUBMODULES_TOKEN }}
   ```

3. **Setup .NET SDK** — установка .NET SDK 8.0 с настройкой GitHub Packages:
   ```yaml
   uses: actions/setup-dotnet@v4
   with:
     dotnet-version: ${{ env.DOTNET_SDK_VERSION }}
     source-url: https://nuget.pkg.github.com/tn-ivk/index.json
   ```

4. **Cache NuGet packages** — кэширование NuGet пакетов для ускорения сборки:
   ```yaml
   uses: actions/cache@v4
   with:
     path: ~/.nuget/packages
     key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj') }}
   ```

5. **Add FastReport NuGet source** — добавление приватного источника FastReport:
   ```bash
   dotnet nuget add source "https://nuget.xn--90aia9aifhdb2cxbdg.xn--p1ai/api/v3/index.json" \
     --name fr_nuget \
     --username "${{ secrets.FR_NUGET_USERNAME }}" \
     --password "${{ secrets.FR_NUGET_PASSWORD }}" \
     --store-password-in-clear-text
   ```

6. **Create directories** — создание необходимых директорий:
   ```bash
   mkdir -p TN_Doc/Dll
   ```

7. **Build solution** — сборка всего решения в Release конфигурации:
   ```bash
   dotnet build -c Release
   ```

8. **Upload build artifacts** — сохранение артефактов сборки (retention: 1 день):
   ```yaml
   uses: actions/upload-artifact@v4
   with:
     name: build-output
     path: |
       **/bin/Release/
       **/obj/Release/
     retention-days: 1
   ```

#### 2. unit-test

**Назначение**: Выполнение модульных тестов для проверки корректности контроллеров, сервисов и конфигураций.

**Зависимости**: `needs: build`

**Основные шаги**:

1-6. Аналогичны job `build` (настройка окружения)

7. **Download build artifacts** — загрузка артефактов из job `build`:
   ```yaml
   uses: actions/download-artifact@v4
   with:
     name: build-output
   ```

8. **Run unit tests** — запуск unit-тестов с фильтрацией по namespace:
   ```bash
   dotnet test Tests/Tests.Unit/Tests.Unit.csproj -c Release --no-build \
     --filter "Namespace~Tests.Controllers|Namespace~Tests.Services|Namespace~Tests.Configs" \
     --verbosity normal \
     --logger "trx;LogFileName=unit-test-results.trx" \
     --logger "console;verbosity=detailed"
   ```

   Фильтр включает тесты из:
   - `Tests.Controllers` — тесты контроллеров
   - `Tests.Services` — тесты сервисов
   - `Tests.Configs` — тесты конфигураций

9. **Upload unit test results** — сохранение результатов тестов (retention: 7 дней):
   ```yaml
   uses: actions/upload-artifact@v4
   if: always()
   with:
     name: test-results-unit
     path: "**/TestResults/*.trx"
     retention-days: 7
   ```

#### 3. integration-test

**Назначение**: Выполнение интеграционных тестов для проверки работы модулей документов и общих компонентов.

**Зависимости**: `needs: build`

**Основные шаги**:

1-8. Аналогичны job `unit-test`

9. **Run integration tests** — запуск интеграционных тестов с фильтрацией:
   ```bash
   dotnet test Tests/Tests.Integration/Tests.Integration.csproj -c Release --no-build \
     --filter "Namespace~Tests.Libraries.Integration|Namespace~Tests.Libraries.KMH|Namespace~Tests.Libraries.Common|Namespace~Tests.Libraries.Core" \
     --verbosity normal \
     --logger "trx;LogFileName=integration-test-results.trx" \
     --logger "console;verbosity=detailed"
   ```

   Фильтр включает тесты из:
   - `Tests.Libraries.Integration` — интеграционные тесты
   - `Tests.Libraries.KMH` — тесты модулей КМХ (~168 тестов)
   - `Tests.Libraries.Common` — тесты общих компонентов
   - `Tests.Libraries.Core` — тесты ядра

10. **Upload integration test results** — сохранение результатов (retention: 7 дней)

#### 4. test-summary

**Назначение**: Агрегация и отображение результатов всех тестов в GitHub Actions Summary.

**Зависимости**: `needs: [unit-test, integration-test]`

**Условие**: `if: always()` — выполняется всегда, даже если предыдущие job'ы упали

**Основные шаги**:

1. **Download all test results** — загрузка всех артефактов с результатами тестов:
   ```yaml
   uses: actions/download-artifact@v4
   with:
     pattern: test-results-*
     merge-multiple: true
   ```

2. **Test Results Summary** — формирование сводки результатов:
   ```bash
   echo "## Test Results Summary" >> $GITHUB_STEP_SUMMARY

   if [ "${{ needs.unit-test.result }}" == "success" ]; then
     echo "- Unit Tests: **Passed**" >> $GITHUB_STEP_SUMMARY
   else
     echo "- Unit Tests: **Failed**" >> $GITHUB_STEP_SUMMARY
   fi

   if [ "${{ needs.integration-test.result }}" == "success" ]; then
     echo "- Integration Tests: **Passed**" >> $GITHUB_STEP_SUMMARY
   else
     echo "- Integration Tests: **Failed**" >> $GITHUB_STEP_SUMMARY
   fi
   ```

---

### build-and-package.yml

**Назначение**: Полная сборка, тестирование, упаковка в .deb пакеты и публикация релиза при создании git тега.

**Файл**: `.github/workflows/build-and-package.yml`

**Триггер**:
```yaml
on:
  push:
    tags:
      - '*'
```

Workflow запускается при создании любого git тега (например: `v1.3.8`, `1.4.0-rc1`).

**Переменные окружения**:
- `DOTNET_SDK_VERSION: '8.0'`
- `DOTNET_RT_VERSION: '8.0.13'` — версия .NET Runtime для упаковки
- `PROJECT_NAME: 'TN_Doc'`
- `DOTNET_CLI_TELEMETRY_OPTOUT: 'true'`
- `DOTNET_NOLOGO: 'true'`

**Jobs**:

#### 1. build

**Назначение**: Сборка и публикация приложения для Linux.

**Основные шаги**:

1-6. Аналогичны `tests-on-push.yml` (настройка окружения, субмодули, NuGet)

7. **Create directories** — создание директорий для публикации:
   ```bash
   mkdir -p ./publish TN_Doc/Dll
   ```

8. **List NuGet sources (debug)** — отладочный вывод списка NuGet источников:
   ```bash
   dotnet nuget list source
   ```

9. **Build solution** — сборка в Release конфигурации:
   ```bash
   dotnet build -c Release
   ```

10. **Publish application** — публикация приложения для Linux:
    ```bash
    dotnet publish TN_Doc/TN_Doc.csproj -c Release -r linux-x64 --self-contained false -o ./publish
    ```
    Параметры:
    - `-r linux-x64` — целевая платформа
    - `--self-contained false` — без включения .NET Runtime (требует установленный runtime на целевой системе)
    - `-o ./publish` — выходная директория

11. **Upload build artifacts** — сохранение артефактов (retention: 7 дней):
    ```yaml
    uses: actions/upload-artifact@v4
    with:
      name: build-artifacts
      path: ./publish/
      retention-days: 7
    ```

#### 2. test

**Назначение**: Запуск unit-тестов для проверки качества перед упаковкой.

**Основные шаги**:

1-6. Настройка окружения (аналогично предыдущим job'ам)

7. **Build solution** — сборка для тестирования:
   ```bash
   dotnet build -c Release
   ```

8. **Run unit tests** — выполнение unit-тестов:
   ```bash
   dotnet test Tests/Tests.Unit/Tests.Unit.csproj -c Release --no-build \
     --filter "Namespace~Tests.Controllers|Namespace~Tests.Services|Namespace~Tests.Configs" \
     --verbosity normal \
     --logger "trx;LogFileName=unit-test-results.trx"
   ```

9. **Upload test results** — сохранение результатов (retention: 7 дней)

#### 3. integration-test

**Назначение**: Запуск интеграционных тестов перед упаковкой.

**Основные шаги**:

Аналогичны job `test`, но с запуском интеграционных тестов:
```bash
dotnet test Tests/Tests.Integration/Tests.Integration.csproj -c Release --no-build \
  --filter "Namespace~Tests.Libraries.Integration|Namespace~Tests.Libraries.KMH|Namespace~Tests.Libraries.Common|Namespace~Tests.Libraries.Core" \
  --verbosity normal \
  --logger "trx;LogFileName=integration-test-results.trx"
```

#### 4. extract-version

**Назначение**: Извлечение версии из `.csproj` файла и формирование полной версии для пакетов.

**Зависимости**: Нет (выполняется параллельно)

**Outputs** (выходные переменные для других job'ов):
- `version` — версия из `.csproj` или git тега
- `full_version` — полная версия с ревизией
- `revision` — ревизия (build number + commit SHA)
- `build_number` — номер сборки из GitHub Actions

**Основные шаги**:

1. **Checkout repository** — клонирование репозитория

2. **Extract version from csproj** — извлечение и формирование версии:
   ```bash
   CSPROJ_FILE="TN_Doc/TN_Doc.csproj"

   # Извлечение версии из .csproj
   VERSION=$(grep -oP '<Version>\K[^<]+' $CSPROJ_FILE || echo "1.0.0")

   # Если запущено по тегу, используем версию из тега
   if [[ "$GITHUB_REF" == refs/tags/* ]]; then
     TAG_NAME="${GITHUB_REF#refs/tags/}"
     TAG_CLEAN=$(echo "$TAG_NAME" | sed 's/^v//')
     VERSION=$TAG_CLEAN
   fi

   # Build number из run number
   BUILD_NUMBER=${{ github.run_number }}

   # Формирование ревизии
   SHORT_SHA=$(echo "$GITHUB_SHA" | cut -c1-8)
   REVISION="b${BUILD_NUMBER}-${SHORT_SHA}"
   FULL_VERSION="${VERSION}-${REVISION}"

   # Записываем в outputs
   echo "VERSION=$VERSION" >> $GITHUB_OUTPUT
   echo "FULL_VERSION=$FULL_VERSION" >> $GITHUB_OUTPUT
   echo "REVISION=$REVISION" >> $GITHUB_OUTPUT
   echo "BUILD_NUMBER=$BUILD_NUMBER" >> $GITHUB_OUTPUT
   ```

   Пример: версия `1.3.8`, build `42`, commit `abc12345` → `1.3.8-b42-abc12345`

#### 5. package-full

**Назначение**: Создание полного .deb пакета с включенным .NET Runtime 8.0.13 и шрифтами.

**Статус**: `if: ${{ false }}` — **ОТКЛЮЧЕН**

**Зависимости**: `needs: [build, test, integration-test, extract-version]`

**Описание**:
- Создает self-contained пакет `tn.doc-full-{version}_amd64.deb`
- Включает .NET Runtime из `distrib/aspnetcore-runtime-8.0.13-linux-x64.tar.gz`
- Включает системные шрифты из директории `fonts/`
- Устанавливает все зависимости автономно

**Структура пакета**:
```
deb/
├── DEBIAN/
│   ├── control           # Метаданные пакета
│   ├── preinst          # Pre-installation скрипт
│   ├── postinst         # Post-installation скрипт
│   ├── prerm            # Pre-removal скрипт
│   └── postrm           # Post-removal скрипт
├── opt/TN_Doc/          # Приложение
├── var/log/TN_Doc/      # Логи
├── etc/systemd/system/  # Systemd service
├── usr/share/fonts/     # Шрифты
├── usr/local/bin/       # Утилиты
└── tmp/TN_Doc/          # .NET Runtime архив
```

**Ключевые особенности**:
- Зависимости: `systemd` (не требует `dotnet-runtime-8.0`)
- Устанавливает .NET Runtime в `/usr/bin/dotnet`
- Настраивает переменные окружения `DOTNET_ROOT` и `PATH`
- Пользователь службы: `alphadaemon`
- Порт: 5000 (по умолчанию)

#### 6. package-minimal

**Назначение**: Создание минимального .deb пакета без зависимостей (требует предустановленный .NET Runtime).

**Статус**: **АКТИВЕН**

**Зависимости**: `needs: [build, test, integration-test, extract-version]`

**Основные шаги**:

1. **Checkout repository** — клонирование репозитория

2. **Download build artifacts** — загрузка артефактов из job `build`:
   ```yaml
   uses: actions/download-artifact@v4
   with:
     name: build-artifacts
     path: ./publish/
   ```

3. **Build minimal .deb package** — создание минимального пакета:

   **Имя пакета**: `tn.doc-{full_version}_amd64.deb`

   Пример: `tn.doc-1.3.8-b42-abc12345_amd64.deb`

   **Структура пакета**:
   ```
   deb-minimal/
   ├── DEBIAN/
   │   ├── control
   │   ├── preinst
   │   ├── postinst
   │   ├── prerm
   │   └── postrm
   ├── opt/TN_Doc/          # Приложение
   ├── var/log/TN_Doc/      # Логи
   └── etc/systemd/system/  # Systemd service
   ```

   **control файл**:
   ```
   Package: tn.doc
   Version: {version}
   Section: utils
   Priority: optional
   Architecture: amd64
   Depends: systemd, dotnet-runtime-8.0 (>= 8.0.13)
   Maintainer: DadonovAD <dadonovad@transneft.ru>
   Description: TN_Doc это приложение для работы с документами СОИ СИКН/ОСИКН.
    Требует предустановленный .NET Runtime 8.0.13+ и системные шрифты.
   ```

   **preinst** — проверка наличия .NET Runtime:
   ```bash
   # Проверка .NET Runtime
   if [ ! -d "/usr/bin/dotnet/shared/Microsoft.NETCore.App" ]; then
       echo "ERROR: .NET Runtime 8 is not installed."
       exit 1
   fi

   REQUIRED_VERSION="8.0.13"
   INSTALLED_VERSION=$(ls /usr/bin/dotnet/shared/Microsoft.NETCore.App | grep -E '^8\.0\.[0-9]+$' | sort -V | tail -n 1)

   if [ "$(printf '%s\n' "$INSTALLED_VERSION" "$REQUIRED_VERSION" | sort -V | head -n1)" != "$REQUIRED_VERSION" ]; then
       echo "ERROR: .NET Runtime version $INSTALLED_VERSION < $REQUIRED_VERSION"
       exit 1
   fi

   # Остановка службы и очистка старых директорий
   systemctl stop TN_Doc.service
   rm -rf /opt/TN_Doc /var/log/TN_Doc
   ```

   **postinst** — установка прав и запуск службы:
   ```bash
   chmod 755 /opt/TN_Doc/TN_Doc
   chmod 644 /opt/TN_Doc/*.dll
   chown -R alphadaemon:alphadaemon /opt/TN_Doc
   chown alphadaemon:alphadaemon /var/log/TN_Doc
   systemctl daemon-reload
   systemctl enable TN_Doc.service
   systemctl start TN_Doc.service
   ```

   **Systemd unit** (`/etc/systemd/system/TN_Doc.service`):
   ```ini
   [Unit]
   Description=Service TN_Doc

   [Service]
   WorkingDirectory=/opt/TN_Doc/
   ExecStart=/usr/bin/dotnet/dotnet /opt/TN_Doc/TN_Doc.dll
   Restart=always
   RestartSec=10
   SyslogIdentifier=dotnet-TN_Doc
   User=alphadaemon
   Environment=ASPNETCORE_ENVIRONMENT=Production

   [Install]
   WantedBy=multi-user.target
   ```

4. **Upload minimal package** — сохранение пакета (retention: 30 дней):
   ```yaml
   uses: actions/upload-artifact@v4
   with:
     name: package-minimal
     path: ./packages/*.deb
     retention-days: 30
   ```

#### 7. package-runtime

**Назначение**: Создание отдельного .deb пакета с .NET Runtime 8.0.13.

**Статус**: `if: ${{ false }}` — **ОТКЛЮЧЕН**

**Зависимости**: `needs: [extract-version]`

**Описание**:
- Создает пакет `tn.dotnet-runtime-8.0.13_amd64.deb`
- Устанавливает .NET Runtime в `/usr/bin/dotnet`
- Provides: `dotnet-runtime-8.0 (= 8.0.13)` — виртуальная зависимость
- Replaces и Breaks: `dotnet-runtime-8.0` — замена стандартного пакета

#### 8. package-fonts

**Назначение**: Создание отдельного .deb пакета с системными шрифтами.

**Статус**: `if: ${{ false }}` — **ОТКЛЮЧЕН**

**Зависимости**: `needs: [extract-version]`

**Описание**:
- Создает пакет `tn.fonts-{version}.deb`
- Устанавливает шрифты в `/usr/share/fonts`
- Обновляет кэш шрифтов через `fc-cache -f -v`
- Зависимости: `fontconfig`

#### 9. notify-telegram

**Назначение**: Отправка уведомлений о завершении сборки в Telegram с файлами пакетов.

**Статус**: **АКТИВЕН** (только для тегов)

**Зависимости**: `needs: [extract-version, package-minimal]`

**Условие**: `if: startsWith(github.ref, 'refs/tags/')` — только для релизных тегов

**Основные шаги**:

1. **Download all packages** — загрузка всех артефактов с пакетами:
   ```yaml
   uses: actions/download-artifact@v4
   with:
     path: ./artifacts/
   ```

2. **Prepare packages** — подготовка пакетов для отправки:
   ```bash
   mkdir -p ./packages
   find ./artifacts -name "*.deb" -exec cp {} ./packages/ \;
   ```

3. **Send Telegram notification** — отправка уведомления и файлов:

   **Формат сообщения**:
   ```
   🚀 *TN_Doc Build Completed*

   📦 Version: `{version}`
   🔗 Commit: `{sha:0:8}`
   📋 [Pipeline](link)

   *Packages:*
   • Minimal: tn.doc-{version}_amd64.deb
   ```

   **Функции**:
   - `send_message()` — отправка текстового сообщения (Markdown)
   - `send_file()` — отправка файла с caption (лимит: 50MB)

   **Используемые Secrets**:
   - `TELEGRAM_BOT_TOKEN` — токен Telegram бота
   - `TELEGRAM_CHAT_ID` — ID чата для уведомлений

#### 10. create-release

**Назначение**: Создание GitHub Release с прикрепленными .deb пакетами.

**Статус**: **АКТИВЕН** (только для тегов)

**Зависимости**: `needs: [extract-version, package-minimal]`

**Условие**: `if: startsWith(github.ref, 'refs/tags/')`

**Permissions**: `contents: write` — для создания релиза

**Основные шаги**:

1. **Download all packages** — загрузка всех пакетов

2. **Prepare packages** — подготовка для публикации

3. **Create GitHub Release** — создание релиза:
   ```yaml
   uses: softprops/action-gh-release@v1
   with:
     name: TN_Doc v${{ needs.extract-version.outputs.version }}
     body: |
       ## TN_Doc v{version}

       ### Пакеты
       - **tn.doc** - Минимальный пакет (требует .NET Runtime)

       ### Установка
       ```bash
       # Или раздельная установка
       sudo dpkg -i tn.doc-*.deb
       ```
     files: ./packages/*.deb
     draft: false
     prerelease: false
   ```

---

## Необходимые Secrets

GitHub Secrets должны быть настроены в Settings → Secrets and variables → Actions:

| Secret | Назначение | Используется в |
|--------|------------|----------------|
| `GH_SUBMODULES_TOKEN` | Personal Access Token для доступа к приватным субмодулям на GitHub | Все workflows |
| `FR_NUGET_USERNAME` | Логин для FastReport NuGet Feed | Все workflows |
| `FR_NUGET_PASSWORD` | Пароль для FastReport NuGet Feed | Все workflows |
| `GITHUB_TOKEN` | Стандартный токен GitHub Actions (создается автоматически) | Все workflows |
| `TELEGRAM_BOT_TOKEN` | Токен Telegram бота для уведомлений | `build-and-package.yml` (notify-telegram) |
| `TELEGRAM_CHAT_ID` | ID чата Telegram для отправки уведомлений | `build-and-package.yml` (notify-telegram) |

### Как получить Secrets:

**GH_SUBMODULES_TOKEN**:
1. GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)
2. Generate new token (classic) с правами: `repo` (Full control of private repositories)
3. Скопировать токен и добавить в Secrets

**FR_NUGET_USERNAME / FR_NUGET_PASSWORD**:
- Предоставляются вендором FastReport
- URL Feed: `https://nuget.xn--90aia9aifhdb2cxbdg.xn--p1ai/api/v3/index.json` (punycode для `nuget.fast-report.com`)

**TELEGRAM_BOT_TOKEN**:
1. Создать бота через [@BotFather](https://t.me/botfather)
2. Команда: `/newbot`
3. Получить HTTP API токен

**TELEGRAM_CHAT_ID**:
1. Добавить бота в чат/группу
2. Получить ID через API: `https://api.telegram.org/bot{token}/getUpdates`
3. Найти `"chat":{"id": {chat_id}}` в ответе

---

## Настройка NuGet источников

### В CI/CD (GitHub Actions)

Workflow автоматически настраивает NuGet источники в каждом job:

```bash
# 1. GitHub Packages (настройка через actions/setup-dotnet@v4)
# Source URL: https://nuget.pkg.github.com/tn-ivk/index.json
# Авторизация через NUGET_AUTH_TOKEN (из GITHUB_TOKEN)

# 2. FastReport Feed (добавление вручную)
dotnet nuget add source "https://nuget.xn--90aia9aifhdb2cxbdg.xn--p1ai/api/v3/index.json" \
  --name fr_nuget \
  --username "${{ secrets.FR_NUGET_USERNAME }}" \
  --password "${{ secrets.FR_NUGET_PASSWORD }}" \
  --store-password-in-clear-text
```

### Проверка источников

Workflow включает отладочный шаг в `build-and-package.yml`:
```bash
dotnet nuget list source
```

Вывод должен содержать:
1. `https://api.nuget.org/v3/index.json` — публичный NuGet
2. `https://nuget.pkg.github.com/tn-ivk/index.json` — GitHub Packages
3. `https://nuget.xn--90aia9aifhdb2cxbdg.xn--p1ai/api/v3/index.json` — FastReport
4. `https://nuget.ortpr.ru/v3/index.json` — ORTPR (должен быть настроен локально, если требуется)

---

## Субмодули

### Проблема

Проект TN_Doc использует git субмодули из приватных репозиториев, размещенных на внутреннем GitLab (`git.tncpa.ru`), которые недоступны из GitHub Actions.

### Решение

Workflow выполняет URL маппинг субмодулей на GitHub mirrors перед клонированием:

```bash
# Настройка маппинга URL (выполняется в начале каждого job)
git config --global url."https://x-access-token:${TOKEN}@github.com/tn-ivk/tn-docgeneral.git".insteadOf "https://git.tncpa.ru/orpovy/ivk/tn.docgeneral.git"
git config --global url."https://x-access-token:${TOKEN}@github.com/tn-ivk/tn-tools.git".insteadOf "https://git.tncpa.ru/orpovy/ivk/tn_toolsfastreport.git"
git config --global url."https://x-access-token:${TOKEN}@github.com/tn-ivk/winprutil.git".insteadOf "https://git.tncpa.ru/orpovy/ivk/winprutil.git"
git config --global url."https://x-access-token:${TOKEN}@github.com/tn-ivk/tn-utils.git".insteadOf "https://git.tncpa.ru/orpovy/ivk/tn.utils.git"
```

### Маппинг субмодулей

| GitLab URL (внутренний) | GitHub Mirror URL |
|-------------------------|-------------------|
| `https://git.tncpa.ru/orpovy/ivk/tn.docgeneral.git` | `https://github.com/tn-ivk/tn-docgeneral.git` |
| `https://git.tncpa.ru/orpovy/ivk/tn_toolsfastreport.git` | `https://github.com/tn-ivk/tn-tools.git` |
| `https://git.tncpa.ru/orpovy/ivk/winprutil.git` | `https://github.com/tn-ivk/winprutil.git` |
| `https://git.tncpa.ru/orpovy/ivk/tn.utils.git` | `https://github.com/tn-ivk/tn-utils.git` |

### Клонирование с субмодулями

После настройки маппинга выполняется checkout:

```yaml
- name: Checkout repository
  uses: actions/checkout@v4
  with:
    submodules: recursive    # Рекурсивная загрузка всех субмодулей
    fetch-depth: 0          # Полная история (для git describe и версионирования)
    token: ${{ secrets.GH_SUBMODULES_TOKEN }}  # Токен для приватных репозиториев
```

### Вложенные субмодули

Субмодуль `tn.docgeneral` содержит вложенный субмодуль `tn.utils`. Благодаря `submodules: recursive` и маппингу все вложенные субмодули загружаются автоматически.

---

## Локальный запуск

### Запуск тестов локально (аналогично CI)

**Unit Tests**:
```bash
# Сборка проекта
dotnet build -c Release

# Запуск unit-тестов с фильтрацией
dotnet test Tests/Tests.Unit/Tests.Unit.csproj -c Release --no-build \
  --filter "Namespace~Tests.Controllers|Namespace~Tests.Services|Namespace~Tests.Configs" \
  --verbosity normal \
  --logger "trx;LogFileName=unit-test-results.trx" \
  --logger "console;verbosity=detailed"
```

**Integration Tests**:
```bash
# Сборка проекта
dotnet build -c Release

# Запуск интеграционных тестов с фильтрацией
dotnet test Tests/Tests.Integration/Tests.Integration.csproj -c Release --no-build \
  --filter "Namespace~Tests.Libraries.Integration|Namespace~Tests.Libraries.KMH|Namespace~Tests.Libraries.Common|Namespace~Tests.Libraries.Core" \
  --verbosity normal \
  --logger "trx;LogFileName=integration-test-results.trx" \
  --logger "console;verbosity=detailed"
```

**Все тесты**:
```bash
dotnet test -c Release --verbosity normal
```

### Локальная сборка (аналогично CI)

**Publish для Linux**:
```bash
# Убедитесь, что настроены NuGet sources (см. ниже)
dotnet publish TN_Doc/TN_Doc.csproj -c Release -r linux-x64 --self-contained false -o ./publish
```

**Результат**: скомпилированное приложение в директории `./publish/`

### Настройка NuGet локально

**1. ORTPR Feed**:
```bash
dotnet nuget add source "https://nuget.ortpr.ru/v3/index.json" --name ortpr
```

**2. FastReport Feed**:
```bash
dotnet nuget add source "https://nuget.fast-report.com/api/v3/index.json" --name fr_nuget \
  --username "<YOUR_USERNAME>" \
  --password "<YOUR_PASSWORD>" \
  --store-password-in-clear-text
```

**3. GitHub Packages** (если используется):
```bash
dotnet nuget add source "https://nuget.pkg.github.com/tn-ivk/index.json" --name github \
  --username "<YOUR_GITHUB_USERNAME>" \
  --password "<YOUR_GITHUB_PAT>" \
  --store-password-in-clear-text
```

### Проверка настроенных источников

```bash
dotnet nuget list source
```

Должен вывести:
```
Registered Sources:
  1. nuget.org [Enabled]
     https://api.nuget.org/v3/index.json
  2. ortpr [Enabled]
     https://nuget.ortpr.ru/v3/index.json
  3. fr_nuget [Enabled]
     https://nuget.fast-report.com/api/v3/index.json
  4. github [Enabled]
     https://nuget.pkg.github.com/tn-ivk/index.json
```

### Работа с субмодулями локально

**Инициализация после клонирования**:
```bash
git submodule update --init --recursive
```

**Обновление субмодулей**:
```bash
git submodule update --remote --merge
```

**Проверка статуса субмодулей**:
```bash
git submodule status
```

### Локальная упаковка в .deb

Упаковка выполняется только в CI/CD. Для локального тестирования используйте скомпилированное приложение из `./publish/`.

**Альтернатива** — запуск через `dotnet`:
```bash
cd ./publish
dotnet TN_Doc.dll
```

Приложение будет доступно на `http://localhost:5000`.

---

## Диаграмма CI/CD Pipeline

### tests-on-push.yml (push в develop*)

```
┌──────────────────────────────────────────────────────┐
│                   Push to develop*                    │
└───────────────────┬──────────────────────────────────┘
                    │
         ┌──────────▼──────────┐
         │       build          │
         │  - Clone + submodules│
         │  - Setup .NET 8.0    │
         │  - Add NuGet sources │
         │  - dotnet build      │
         │  - Upload artifacts  │
         └──────────┬──────────┘
                    │
      ┌─────────────┴─────────────┐
      │                           │
┌─────▼────────┐         ┌────────▼─────┐
│  unit-test   │         │integration-  │
│              │         │test          │
│ - Download   │         │ - Download   │
│   artifacts  │         │   artifacts  │
│ - dotnet test│         │ - dotnet test│
│   (unit)     │         │   (integr.)  │
│ - Upload     │         │ - Upload     │
│   results    │         │   results    │
└─────┬────────┘         └────────┬─────┘
      │                           │
      └─────────────┬─────────────┘
                    │
           ┌────────▼────────┐
           │  test-summary   │
           │                 │
           │ - Download all  │
           │   test results  │
           │ - Generate      │
           │   summary       │
           └─────────────────┘
```

### build-and-package.yml (push tag)

```
┌──────────────────────────────────────────────────────┐
│                  Push Tag (v1.3.8)                    │
└───────────┬──────────────────────────────────────────┘
            │
      ┌─────┴──────┬──────────┬──────────────┐
      │            │          │              │
┌─────▼──────┐ ┌──▼────┐ ┌───▼──────┐ ┌─────▼────────┐
│   build    │ │ test  │ │integr-   │ │ extract-     │
│            │ │       │ │test      │ │ version      │
│ - Clone +  │ │       │ │          │ │              │
│   submod.  │ │       │ │          │ │ - Extract    │
│ - Setup    │ │       │ │          │ │   version    │
│   .NET     │ │       │ │          │ │   from tag   │
│ - Build    │ │       │ │          │ │ - Format     │
│ - Publish  │ │       │ │          │ │   full ver.  │
│   linux-x64│ │       │ │          │ │ - Output:    │
│ - Upload   │ │       │ │          │ │   version,   │
│   artifacts│ │       │ │          │ │   full_ver.  │
└─────┬──────┘ └───┬───┘ └────┬─────┘ └─────┬────────┘
      │            │          │              │
      └────────────┴──────────┴──────────────┤
                                             │
         ┌───────────────────────────────────┘
         │
         │  ┌──────────────────┐   ← DISABLED
         │  │  package-full    │
         │  └──────────────────┘
         │
      ┌──▼──────────────┐
      │ package-minimal │
      │                 │
      │ - Download      │
      │   artifacts     │
      │ - Build .deb    │
      │   (minimal)     │
      │ - Upload        │
      │   package       │
      └──┬──────────────┘
         │
         ├──────────────┬──────────────────┐
         │              │                  │
   ┌─────▼────────┐ ┌───▼──────────┐ ┌────▼──────────┐
   │ notify-      │ │ create-      │ │ Other         │
   │ telegram     │ │ release      │ │ packages      │
   │              │ │              │ │ (DISABLED)    │
   │ - Download   │ │ - Download   │ │               │
   │   packages   │ │   packages   │ │ - runtime     │
   │ - Send msg   │ │ - Create     │ │ - fonts       │
   │ - Send files │ │   GitHub     │ │               │
   │   to Telegram│ │   Release    │ └───────────────┘
   └──────────────┘ └──────────────┘
```

---

## Артефакты

### tests-on-push.yml

| Артефакт | Retention | Содержимое |
|----------|-----------|------------|
| `build-output` | 1 день | Скомпилированные bin/ и obj/ директории |
| `test-results-unit` | 7 дней | Unit test results (*.trx файлы) |
| `test-results-integration` | 7 дней | Integration test results (*.trx файлы) |

### build-and-package.yml

| Артефакт | Retention | Содержимое |
|----------|-----------|------------|
| `build-artifacts` | 7 дней | Опубликованное приложение (./publish/) |
| `test-results-unit` | 7 дней | Unit test results (*.trx файлы) |
| `test-results-integration` | 7 дней | Integration test results (*.trx файлы) |
| `package-minimal` | 30 дней | Минимальный .deb пакет |
| `package-full` | 30 дней | Полный .deb пакет (DISABLED) |
| `package-runtime` | 30 дней | .NET Runtime .deb пакет (DISABLED) |
| `package-fonts` | 30 дней | Fonts .deb пакет (DISABLED) |

---

## Типичные проблемы и решения

### Ошибки NuGet источников

**Проблема**: `Unable to load the service index for source https://nuget.fast-report.com/api/v3/index.json`

**Решение**:
1. Проверить Secrets: `FR_NUGET_USERNAME` и `FR_NUGET_PASSWORD`
2. Проверить доступность источника (может быть проблема с сетью)
3. Использовать punycode URL: `https://nuget.xn--90aia9aifhdb2cxbdg.xn--p1ai/api/v3/index.json`

### Ошибки субмодулей

**Проблема**: `fatal: could not read Username for 'https://git.tncpa.ru'`

**Решение**:
1. Убедитесь, что `GH_SUBMODULES_TOKEN` настроен корректно
2. Проверьте маппинг URL субмодулей в workflow
3. Убедитесь, что GitHub mirrors (`github.com/tn-ivk/*`) существуют и доступны

### Ошибки тестов

**Проблема**: Тесты падают в CI, но проходят локально

**Решение**:
1. Проверить различия в окружении (пути, permissions)
2. Убедиться, что все NuGet зависимости восстановлены
3. Проверить логи тестов в артефактах `test-results-*`
4. Добавить `--logger "console;verbosity=detailed"` для детального вывода

### Ошибки упаковки

**Проблема**: `dpkg-deb: error: failed to read archive`

**Решение**:
1. Проверить структуру директории `deb/` или `deb-minimal/`
2. Убедиться, что все скрипты (preinst, postinst, prerm, postrm) имеют права на выполнение
3. Проверить формат `control` файла (не должно быть лишних пробелов)

### Telegram уведомления не приходят

**Проблема**: Job `notify-telegram` завершается успешно, но сообщения не приходят

**Решение**:
1. Проверить Secrets: `TELEGRAM_BOT_TOKEN` и `TELEGRAM_CHAT_ID`
2. Убедиться, что бот добавлен в чат
3. Проверить размер файлов (лимит Telegram: 50MB)
4. Проверить логи job для ошибок API

---

## Best Practices

### Работа с Secrets

- **НЕ храните** Secrets в коде или workflow файлах
- **Используйте** GitHub Secrets для конфиденциальных данных
- **Ограничьте** доступ к Secrets (Repository secrets vs Organization secrets)
- **Ротируйте** токены регулярно (особенно PAT)

### Оптимизация CI/CD

- **Используйте** кэширование NuGet пакетов для ускорения сборки
- **Распараллеливайте** независимые job'ы (например, `unit-test` и `integration-test`)
- **Ограничьте** retention артефактов (1 день для временных, 7-30 дней для важных)
- **Отключите** ненужные job'ы через `if: ${{ false }}`

### Версионирование

- **Используйте** семантическое версионирование (SemVer): `MAJOR.MINOR.PATCH`
- **Создавайте** git теги для релизов: `git tag -a v1.3.8 -m "Release v1.3.8"`
- **Включайте** build metadata в имена пакетов: `tn.doc-1.3.8-b42-abc12345_amd64.deb`

### Тестирование

- **Запускайте** тесты в CI перед каждой сборкой
- **Используйте** фильтры для раздельного запуска unit и integration тестов
- **Сохраняйте** результаты тестов как артефакты для анализа
- **Не игнорируйте** падающие тесты — исправляйте или отключайте

### Мониторинг

- **Настройте** уведомления в Telegram для критичных событий
- **Используйте** GitHub Actions Summary для быстрой диагностики
- **Анализируйте** логи job'ов при ошибках
- **Отслеживайте** время выполнения workflow для оптимизации

---

## Дополнительная информация

### Документация

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [.NET CLI Reference](https://docs.microsoft.com/en-us/dotnet/core/tools/)
- [Debian Binary Package Building](https://www.debian.org/doc/manuals/maint-guide/)

### Связанные документы

- [Architecture Overview](../architecture/overview.md)
- [Testing Guide](../development/testing.md)
- [Linux Deployment](../deployment/linux.md)

---

**Дата создания документа**: 2026-01-23
