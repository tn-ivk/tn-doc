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

### Linux (Self-contained)

```bash
dotnet publish TN_Doc/TN_Doc.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained false \
  -o ./publish/linux
```

### Windows (Self-contained)

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

`<FULL_VERSION>` задается при сборке пакета (например, `1.4.3`). Если используется CI, версия может формироваться в пайплайне вашей инфраструктуры.

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

```bash
# 1. Публикация приложения (self-contained)
dotnet publish TN_Doc/TN_Doc.csproj -c Release -r win-x64 --self-contained true -o publish/win-x64-full

# 2. Сборка MSI (harvest + компиляция интегрированы через MSBuild)
dotnet build installer/windows/TN_Doc.Installer.wixproj -c Release `
  -p:ProductVersion=1.5.0 `
  -p:HarvestPath=../../publish/win-x64-full

# Результат: installer/windows/bin/x64/Release/TN_Doc.msi
```

### Минимальный вариант (без .NET Runtime)

```bash
# Framework-dependent публикация
dotnet publish TN_Doc/TN_Doc.csproj -c Release -r win-x64 --self-contained false -o publish/win-x64-minimal

# Сборка MSI
dotnet build installer/windows/TN_Doc.Installer.wixproj -c Release `
  -p:ProductVersion=1.5.0 `
  -p:HarvestPath=../../publish/win-x64-minimal
```

### Структура WiX проекта

```
installer/windows/
├── TN_Doc.Installer.wixproj   # WiX SDK-style проект (Heat + HarvestDirectory)
├── Package.wxs                 # Пакет, MajorUpgrade, Features, Codepage 1251
├── Directories.wxs             # Структура директорий (ProgramFiles64Folder)
├── ServiceConfig.wxs           # Windows Service + бэкап при обновлении
├── ExcludeMainExe.xslt         # XSLT: исключает TN_Doc.exe из harvest
├── Scripts/Backup.ps1          # PowerShell бэкап при обновлении
└── UI/
    ├── ServiceNameDlg.wxs      # Диалог имени службы
    └── CustomInstallUI.wxs     # Кастомная UI-последовательность
```

### Тихая установка

```cmd
:: Графическая установка
msiexec /i TN_Doc.msi

:: Тихая установка с параметрами
msiexec /i TN_Doc.msi /quiet INSTALLFOLDER="C:\ProjectVU\DotNetComponents" SERVICENAME="tn.doc"
```

## Автоматическая сборка (CI/CD)

### GitLab CI Pipeline

```yaml
stages:
  - build
  - test
  - package
  - deploy

build:
  stage: build
  script:
    - dotnet restore
    - dotnet build -c Release
    - cd TN_Doc/Client && npm ci && npm run build:all

test:
  stage: test
  script:
    - dotnet test --no-build

package:
  stage: package
  script:
    - dotnet publish -c Release -r linux-x64
    - dpkg-deb --build ./package
```

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
