# Настройка окружения разработки

## Системные требования

| Компонент | Требование |
|-----------|------------|
| ОС | Windows 10/11, Ubuntu 20.04+, macOS 12+ |
| .NET SDK | 8.0 или выше (для разработки) |
| .NET Runtime | 8.0.13 или выше (для запуска) |
| RAM | 4 GB (рекомендуется 8 GB) |
| Дисковое пространство | 2 GB |
| IDE | Visual Studio 2022, VS Code, Rider |

**Дополнительно (опционально):**
- Node.js 18+ и npm 8+ — только для вспомогательных скриптов документации (например, `docs/ui-screenshots/`). Для сборки приложения не требуется.

## Проверка требований

```bash
# .NET SDK
dotnet --version
# Ожидается: 8.0.100 или выше

# .NET Runtime
dotnet --list-runtimes | grep "Microsoft.AspNetCore.App 8.0"
# Ожидается: 8.0.13 или выше
```

## Установка .NET SDK

Скачать актуальный .NET SDK 8.x: https://dotnet.microsoft.com/

## Установка дополнительных зависимостей (Linux)

```bash
sudo apt-get install libgdiplus
```

## Клонирование репозитория

```bash
# Основной репозиторий
git clone http://192.168.100.100/orpovy/ivk/tn_doc.git
cd tn_doc

# Инициализация git submodules (обязательно!)
git submodule update --init --recursive
```

## Настройка NuGet источников

```bash
dotnet nuget add source "https://nuget.ortpr.ru/v3/index.json" --name ortpr
dotnet nuget add source "https://nuget.fast-report.com/api/v3/index.json" --name fr_nuget \
  --username "<USERNAME>" --password "<PASSWORD>" --store-password-in-clear-text
```

## Сборка и запуск

```bash
# Восстановить зависимости
dotnet restore

# Собрать решение
dotnet build

# Запуск
cd TN_Doc
dotnet run
```

По умолчанию Kestrel стартует на `http://localhost:5000`.
При запуске через IIS Express в Visual Studio — `http://localhost:38509`.

## IDE

### Visual Studio 2022
- Открыть `TN_Doc.sln`
- Запустить профиль **TN_Doc** или **IIS Express**

### VS Code / Rider
- Запустить `dotnet run` в `TN_Doc/`
- Переменная окружения (по желанию): `ASPNETCORE_ENVIRONMENT=Development`

## Проверка установки

```bash
# 1. Submodules
git submodule status

# 2. Сборка
dotnet build

# 3. Тесты
dotnet test
```

## Частые проблемы

### Ошибки сборки / отсутствуют модули
Причина: submodules не инициализированы.

```bash
git submodule update --init --recursive
```

### Ошибки восстановления NuGet
Проверьте источники `ortpr` и `fr_nuget`.

### Изменения в конфигурации не применяются
После правки `Cfg*.json` перезапустите приложение (кэш конфигурации).
