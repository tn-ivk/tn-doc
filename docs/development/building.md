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

# Сборка Vue компонента
cd TN_Doc/Client/statusbar && npm run build && cd ../../..

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

### 4. Сборка StatusBar

```bash
cd TN_Doc/Client/statusbar

# Установка зависимостей (первый раз)
npm install

# Development сборка с watch
npm run dev

# Production сборка
npm run build
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

`<FULL_VERSION>` формируется в CI на основе тега версии и номера сборки (см. `.gitlab-ci.yml`).

См. `.gitlab-ci.yml` для полного процесса.

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
    - cd TN_Doc/Client/statusbar && npm ci && npm run build

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
- [Testing](testing.md)
- [Deployment](../deployment/linux.md)
