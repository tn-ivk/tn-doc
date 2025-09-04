# GitLab CI/CD Pipeline Configuration для проектов TN

Этот репозиторий содержит три отдельных пайплайна для автоматической сборки и упаковки приложений:
- `TN_Doc` - главное приложение для работы с документами
- `TN_KMH` - приложение для ручного ввода метрологических характеристик 
- `TN_MessagingService` - служба коммуникации с OPC серверами

## Файлы пайплайнов

- `.gitlab-ci-tn-doc.yml` - для проекта TN_Doc
- `.gitlab-ci-tn-kmh.yml` - для проекта TN_KMH
- `.gitlab-ci-tn-messagingservice.yml` - для проекта TN_MessagingService

## Установка и настройка

### 1. Копирование файлов

Скопируйте соответствующий файл пайплайна в корень каждого проекта:

```bash
# Для TN_Doc
cp .gitlab-ci-tn-doc.yml ../tn_doc/.gitlab-ci.yml

# Для TN_KMH  
cp .gitlab-ci-tn-kmh.yml ../tn_kmh/.gitlab-ci.yml

# Для TN_MessagingService
cp .gitlab-ci-tn-messagingservice.yml ../tn_messagingservice/.gitlab-ci.yml
```

### 2. Настройка переменных GitLab

В настройках каждого проекта GitLab добавьте следующие переменные:

#### Обязательные переменные:
- `PROJECT_API_TOKEN` - Personal Access Token для работы с GitLab API
  - Scope: `api`, `read_repository`, `write_repository`

#### Только для TN_Doc:
- `FR_NUGET_USERNAME` - логин для FastReport NuGet
- `FR_NUGET_PASSWORD` - пароль для FastReport NuGet

### 3. Подготовка зависимостей (опционально)

Если вы хотите включить зависимости в пакет для офлайн установки, поместите следующие файлы в папку `ansible/`:

- `aspnetcore-runtime-8.0.13-linux-x64.tar.gz` - .NET Runtime
- `libgdiplus_4.2-1_amd64.deb` - библиотека для работы с графикой (только для TN_Doc и TN_KMH)

## Использование

### Запуск пайплайна

Пайплайны запускаются автоматически при создании тега в формате:
- `1.0` 
- `1.0.0`
- `2.1.5`

```bash
# Создание и отправка тега
git tag v1.4.1
git push origin v1.4.1
```

### Этапы пайплайна

1. **Build Stage**:
   - `build-job` - сборка приложения
   - `extract-version-job` - извлечение версии и управление build number

2. **Package Stage**:
   - `package-job` - создание .deb пакета с автоустановкой зависимостей

## Особенности каждого приложения

### TN_Doc
- **Зависимости**: .NET Runtime 8.0.13+, libgdiplus, FastReport NuGet
- **Сложность**: Высокая (множество конфигураций и шаблонов)
- **Порты**: 38509 (HTTP), 44357 (HTTPS)
- **Папки**: `/opt/TN_Doc/`, `/var/log/TN_Doc/`

### TN_KMH  
- **Зависимости**: .NET Runtime 8.0.13+, libgdiplus
- **Сложность**: Средняя (веб-интерфейс для ручного ввода)
- **Папки**: `/opt/TN_KMH/`, `/var/log/TN_KMH/`

### TN_MessagingService
- **Зависимости**: Только .NET Runtime 8.0.13+ (libgdiplus НЕ требуется)
- **Сложность**: Низкая (сервис без UI)
- **Особенности**: SignalR Hub, OPC клиенты
- **Папки**: `/opt/TN_MessagingService/`, `/var/log/TN_MessagingService/`

## Автоматическая установка зависимостей

Все пайплайны включают автоматическую установку зависимостей в postinst скрипте:

### .NET Runtime 8.0.13+
1. **Приоритет 1**: Установка из локального архива (если присутствует в ansible/)
2. **Приоритет 2**: Установка через Microsoft официальный репозиторий

### libgdiplus (только для TN_Doc и TN_KMH)
1. **Приоритет 1**: Установка из локального .deb пакета (если присутствует в ansible/)  
2. **Приоритет 2**: Установка из системного репозитория

## Systemd Services

Каждое приложение устанавливается как systemd служба:

```bash
# Управление службами
sudo systemctl status TN_Doc.service
sudo systemctl status TN_KMH.service  
sudo systemctl status TN_MessagingService.service

# Перезапуск
sudo systemctl restart TN_Doc.service
sudo systemctl restart TN_KMH.service
sudo systemctl restart TN_MessagingService.service

# Логи
sudo journalctl -u TN_Doc.service -f
sudo journalctl -u TN_KMH.service -f
sudo journalctl -u TN_MessagingService.service -f
```

## Структура .deb пакета

```
package/
├── DEBIAN/
│   ├── control          # Метаданные пакета
│   ├── preinst          # Скрипт подготовки установки
│   ├── postinst         # Скрипт постустановки (с автоустановкой зависимостей)
│   ├── prerm            # Скрипт предудаления
│   └── postrm           # Скрипт постудаления
├── opt/{PROJECT_NAME}/  # Файлы приложения
├── var/log/{PROJECT_NAME}/ # Директория логов
└── etc/systemd/system/  # Systemd service файл
```

## Версионирование

Система автоматического управления версиями:
- Версия извлекается из `.csproj` файла
- Build number управляется через GitLab API переменные
- Формат: `{VERSION}-b{BUILD_NUMBER}-{COMMIT_SHA}`
- Пример: `1.4.1-b15-a1b2c3d4`

## Устранение проблем

### Типичные проблемы:

1. **"Version not found in .csproj"**
   - Убедитесь что в файле `{PROJECT_NAME}.csproj` присутствует тег `<Version>`

2. **"FastReport NuGet источник недоступен"** (только TN_Doc)
   - Проверьте переменные `FR_NUGET_USERNAME` и `FR_NUGET_PASSWORD`

3. **"Build failed - missing dependencies"**
   - Убедитесь что все submodule'ы корректно инициализированы
   - Проверьте настройку `GIT_SUBMODULE_STRATEGY: recursive`

4. **"Package installation failed"**
   - Проверьте права доступа к системным директориям
   - Убедитесь в наличии интернет-соединения для установки зависимостей

### Логи пайплайна:
- GitLab CI/CD > Pipelines > конкретный pipeline > Job
- Артефакты сборки доступны в разделе "Artifacts"

## Развертывание

### Установка пакета:
```bash
# Установка
sudo dpkg -i tn.doc-1.4.1-b15-a1b2c3d4_amd64.deb

# При ошибках зависимостей
sudo apt-get install -f

# Удаление
sudo apt remove tn.doc
```

### Проверка установки:
```bash
# Статус службы
systemctl status TN_Doc.service

# Проверка версий зависимостей
dotnet --list-runtimes
dpkg -l | grep libgdiplus
```

## Контакты

**Сопровождающий**: DadonovAD <dadonovad@transneft.ru>

**Теги GitLab**: `orpovy`

**Требования к системе**:
- Ubuntu 18.04+ / Debian 10+
- Systemd
- Доступ к интернету (для автоустановки зависимостей)