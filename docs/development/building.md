# Руководство по сборке проекта

## Обзор

TN_Doc **v1.3.8** — ASP.NET Core 8.0 MVC‑приложение с генерацией документов через FastReport.
Во фронтенде используются **Razor‑представления + статические JS/CSS** в `TN_Doc/wwwroot/` и HTML‑шаблоны редактирования в `TN_Doc/wwwroot/HTML/` и модулях документов. Отдельной сборки SPA нет.

**Состав:**
- Backend: .NET 8.0 (ASP.NET Core MVC)
- Документные модули: `tn.docgeneral/` (подключён как submodule)
- Шаблоны: `TN_Doc/Doc/*.frx`
- Статика: `TN_Doc/wwwroot/`

## Обязательные требования

- .NET SDK 8.0+ (для разработки и сборки)
- Инициализированные Git submodules

> Node.js не требуется для сборки приложения. Он нужен только для вспомогательных скриптов документации (например, `docs/ui-screenshots/`).

## Быстрая сборка

```bash
# 1. Инициализировать git submodules (обязательно!)
git submodule update --init --recursive

# 2. Восстановить .NET зависимости
dotnet restore

# 3. Собрать решение
dotnet build

# 4. Запустить приложение
cd TN_Doc
# Kestrel по умолчанию: http://localhost:5000 (и https://localhost:5001)
dotnet run
```

**IIS Express (через Visual Studio):** по умолчанию `http://localhost:38509` (см. `TN_Doc/Properties/launchSettings.json`).

## Детальные команды

### Очистка
```bash
dotnet clean
```

### Сборка
```bash
# Сборка всего решения
dotnet build

# Release‑сборка
dotnet build -c Release
```

### Тесты
```bash
# Все тесты
dotnet test

# Фильтрация по классу
dotnet test --filter "ClassName=TestClass"

# Фильтрация по namespace
dotnet test --filter "Namespace~KMH"
```

### Публикация
```bash
# Публикация (пример для Linux)
dotnet publish -c Release -r linux-x64 --self-contained false
```

## Изменения фронтенда

Отдельной сборки фронтенда нет. Все статические ресурсы находятся в:
- `TN_Doc/wwwroot/js/`
- `TN_Doc/wwwroot/css/`
- `TN_Doc/wwwroot/HTML/`

Изменения достаточно сохранить и пересобрать backend (`dotnet build`).

