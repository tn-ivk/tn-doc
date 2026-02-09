# Руководство по сборке проекта

## Обзор процесса сборки

```mermaid
flowchart TB
    Start([Начало]) --> Clean[dotnet clean]
    Clean --> Restore[dotnet restore]
    Restore --> BuildBackend[Сборка Backend]
    BuildBackend --> BuildVue[Сборка Vue StatusBar]
    BuildVue --> Tests[Запуск тестов]
    Tests --> Package[Создание пакета]
    Package --> End([Завершение])
```

## Быстрая сборка

```bash
# Полная сборка с нуля
dotnet clean && dotnet restore && dotnet build

# Сборка клиентских приложений (StatusBar + Configurator)
cd TN_Doc/Client && npm install && npm run build:all && cd ../..

# Сборка и запуск
cd TN_Doc && dotnet run
```

## Детальные команды сборки

### 1. Очистка

```bash
# Очистить выходные директории
dotnet clean

# Очистить NuGet кэш (если нужно)
dotnet nuget locals all --clear
```

### 2. Восстановление пакетов

```bash
# Восстановить все NuGet пакеты
dotnet restore

# Для конкретного проекта
dotnet restore TN_Doc/TN_Doc.csproj
```

### 3. Сборка решения

```bash
# Сборка всего решения
dotnet build

# Сборка в режиме Release
dotnet build -c Release

# Сборка с детальным выводом
dotnet build -v detailed

# Сборка конкретного проекта
dotnet build TN_Doc/TN_Doc.csproj
```

### 4. Сборка клиентских приложений

```bash
cd TN_Doc/Client

# Установка зависимостей (первый раз)
npm install

# Development сборка с watch
npm run dev

# Development сборка configurator
npm run dev:configurator

# Production сборка
npm run build

# Production сборка конфигуратора
npm run build:configurator

# Production сборка всех приложений
npm run build:all
```

## Конфигурации сборки

### Debug Configuration

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <DefineConstants>DEBUG;TRACE</DefineConstants>
  <Optimize>false</Optimize>
  <DebugSymbols>true</DebugSymbols>
  <DebugType>full</DebugType>
</PropertyGroup>
```

Особенности:
- Включены отладочные символы
- Копируются `*.Development.json` файлы
- Подробное логирование

### Release Configuration

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <DefineConstants>RELEASE</DefineConstants>
  <Optimize>true</Optimize>
  <DebugSymbols>false</DebugSymbols>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

Особенности:
- Оптимизация кода
- Development конфиги исключены
- Минимальное логирование

## Публикация

### Linux (Framework-dependent, как в CI)

```bash
dotnet publish TN_Doc/TN_Doc.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained false \
  -o ./publish/linux
```

### Windows (Framework-dependent)

```bash
dotnet publish TN_Doc/TN_Doc.csproj \
  -c Release \
  -r win-x64 \
  --self-contained false \
  -o ./publish/windows
```

### Framework-dependent

```bash
dotnet publish TN_Doc/TN_Doc.csproj \
  -c Release \
  -o ./publish/framework-dependent
```

## Создание .deb пакета (Linux)

```mermaid
flowchart LR
    Publish[dotnet publish] --> Structure[Создать структуру]
    Structure --> Control[Создать DEBIAN/control]
    Control --> Scripts[Добавить скрипты]
    Scripts --> Build[dpkg-deb --build]
    Build --> DEB[tn.doc-full-<FULL_VERSION>_amd64.deb]
```

`<FULL_VERSION>` задается при сборке пакета (например, `1.5.1-b42-a1b2c3d4`). В GitLab CI значение формируется в `extract-version-job` и передается через `version.env`.

## Создание MSI пакета (Windows)

```mermaid
flowchart LR
    Publish[dotnet publish] --> Build[dotnet build wixproj]
    Build --> Heat[Heat: harvest файлов]
    Heat --> Compile[WiX: компиляция + линковка]
    Compile --> MSI[TN_Doc.msi]
```

Проект WiX v6 расположен в `installer/windows/`. Сборка MSI выполняется через `dotnet build` с интегрированным Heat harvesting (автоматический сбор файлов из publish-директории).

### Локальная сборка

```powershell
# 1. Публикация приложения (self-contained)
dotnet publish TN_Doc/TN_Doc.csproj -c Release -r win-x64 --self-contained true -o publish/win-x64-full

# 2. Копирование cfg-elevator в publish-директорию
Copy-Item installer/tools/cfg-elevator-windows-amd64.exe publish/win-x64-full/cfg-elevator.exe

# 3. Сборка MSI (harvest + компиляция интегрированы через MSBuild)
dotnet build installer/windows/TN_Doc.Installer.wixproj -c Release `
  -p:ProductVersion=<VERSION> `
  -p:HarvestPath=../../publish/win-x64-full

# Результат: installer/windows/bin/x64/Release/TN_Doc.msi
```

### Минимальный вариант (без .NET Runtime)

```powershell
# Framework-dependent публикация
dotnet publish TN_Doc/TN_Doc.csproj -c Release -r win-x64 --self-contained false -o publish/win-x64-minimal

# Сборка MSI
dotnet build installer/windows/TN_Doc.Installer.wixproj -c Release `
  -p:ProductVersion=<VERSION> `
  -p:HarvestPath=../../publish/win-x64-minimal
```

`<VERSION>` берите из `TN_Doc/TN_Doc.csproj` (или из переменной `VERSION` в CI). Передавайте `-p:ProductVersion` явно, чтобы версия MSI совпадала с версией приложения.

### Структура WiX проекта

```
installer/windows/
├── TN_Doc.Installer.wixproj   # WiX SDK-style проект (Heat + HarvestDirectory)
├── Package.wxs                 # Пакет, MajorUpgrade, Features, UI (WixUI_InstallDir + ServiceNameDlg)
├── Directories.wxs             # Структура директорий (ProgramFiles64Folder)
├── ServiceConfig.wxs           # Windows Service + бэкап + очистка + миграция конфигов
├── ExcludeMainExe.xslt         # XSLT: исключает TN_Doc.exe из harvest
└── Scripts/
    ├── Backup.ps1              # PowerShell бэкап перед установкой (исключает logs/)
    └── MigrateCfg.ps1          # PowerShell миграция конфигов через cfg-elevator

installer/tools/
├── cfg-elevator-linux-amd64            # Go бинарник для Linux
└── cfg-elevator-windows-amd64.exe      # Go бинарник для Windows
```

> **Важно**: UI-элементы (WixUI, диалоги, Publish) должны быть внутри `<Package>` в Package.wxs — WiX линкер отбрасывает нелинкованные Fragment-файлы.

### Тихая установка

```cmd
:: Графическая установка
msiexec /i TN_Doc.msi

:: Тихая установка с параметрами
msiexec /i TN_Doc.msi /quiet INSTALLFOLDER="C:\ProjectVU\DotNetComponents\TN_Doc" SERVICENAME="tn.doc"
```

## Автоматическая сборка (CI/CD)

### GitLab CI Pipeline

Ниже сокращенный фрагмент job'ов, связанных с версионированием и MSI. Linux job'ы (`build-job`, `package-job`, `package-minimal-job`) остаются в pipeline без изменений.

```yaml
stages:
  - build
  - package
  - notify

extract-version-job:
  stage: build
  script:
    - # Формирует VERSION/FULL_VERSION в version.env

build-windows-job:
  stage: build
  image: mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION}
  tags:
    - orpovy
  script:
    - dotnet publish TN_Doc/TN_Doc.csproj -c Release -r win-x64 --self-contained true -o ./publish/win-x64-full
    - dotnet publish TN_Doc/TN_Doc.csproj -c Release -r win-x64 --self-contained false -o ./publish/win-x64-minimal

package-msi-full-job:
  stage: package
  image: mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION}-windowsservercore-ltsc2019
  needs:
    - build-windows-job
    - extract-version-job
  tags:
    - windows
  allow_failure: true
  script:
    - dotnet build installer/windows/TN_Doc.Installer.wixproj ... -p:HarvestPath=../../publish/win-x64-full

package-msi-minimal-job:
  stage: package
  image: mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION}-windowsservercore-ltsc2019
  needs:
    - build-windows-job
    - extract-version-job
  tags:
    - windows
  allow_failure: true
  script:
    - dotnet build installer/windows/TN_Doc.Installer.wixproj ... -p:HarvestPath=../../publish/win-x64-minimal

notify-telegram-job:
  stage: notify
  needs:
    - package-job
    - package-minimal-job
    - package-net-runtime-job
    - package-fonts-job
    - job: package-msi-full-job
      optional: true
    - job: package-msi-minimal-job
      optional: true
```

В текущем `.gitlab-ci.yml` у MSI job'ов (`package-msi-full-job`, `package-msi-minimal-job`) указан `image: mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION}-windowsservercore-ltsc2019` и tag `windows`.
Если MSI job запускается через Windows `shell` runner, поле `image` игнорируется и на хосте должны быть установлены `.NET SDK 8` и `WiX Toolset v6`.

Если Windows runner недоступен, MSI job'ы помечены `allow_failure: true`, а Linux-пакеты продолжают собираться и публиковаться.

### GitHub Actions

**Retention артефактов:**

| Workflow | Артефакт | Хранение |
|----------|----------|----------|
| `tests-on-push.yml` | build-output | 1 день |
| `tests-on-push.yml` | test-results | 7 дней |
| `build-and-package.yml` | build/test | 3 дня |
| `build-and-package.yml` | packages | 7 дней |

## Оптимизация сборки

### Ускорение сборки

```bash
# Параллельная сборка
dotnet build -m

# Пропустить тесты при сборке
dotnet build --no-restore

# Инкрементальная сборка
dotnet build /p:BuildInParallel=true
```

### Минимизация размера

```bash
# Публикация с обрезкой (trimming)
dotnet publish -c Release \
  -r linux-x64 \
  -p:PublishTrimmed=true \
  -p:TrimMode=link

# Компрессия assemblies
dotnet publish -c Release \
  -p:CompressionEnabled=true
```

## Диагностика проблем сборки

```bash
# Детальный вывод
dotnet build -v detailed > build.log 2>&1

# Проверка зависимостей
dotnet list package

# Поиск устаревших пакетов
dotnet list package --outdated

# Проверка уязвимых пакетов
dotnet list package --vulnerable
```

## Артефакты сборки

### Выходные директории

```
TN_Doc/
├── bin/
│   └── Debug/
│       └── net8.0/
│           ├── TN_Doc.dll
│           ├── TN_Doc.pdb
│           └── wwwroot/
└── obj/
    └── Debug/
        └── net8.0/
```

### Публикация

```
publish/
├── TN_Doc.dll
├── TN_Doc.deps.json
├── TN_Doc.runtimeconfig.json
├── appsettings.json
├── wwwroot/
├── Cfg/
├── Doc/
└── ...
```

## См. также

- [Setup Guide](setup.md)
- [Deployment](../deployment/linux.md)
- [Windows Deployment](../deployment/windows.md)
