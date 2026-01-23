# Spec and build

## Configuration
- **Artifacts Path**: {@artifacts_path} → `.zenflow/tasks/{task_id}`

---

## Agent Instructions

Ask the user questions when anything is unclear or needs their input. This includes:
- Ambiguous or incomplete requirements
- Technical decisions that affect architecture or user experience
- Trade-offs that require business context

Do not make assumptions on important decisions — get clarification first.

---

## Workflow Steps

### [x] Step: Technical Specification
<!-- chat-id: e7a4ad74-7207-412d-8709-8fdd4def2cfa -->

**Результат:** Создана спецификация в `spec.md`

**Сложность:** medium

**Выявленные проблемы:**
1. `docs/configs/passport.md` - поле `IsDefault` помечено как "ПЛАНИРУЕТСЯ", но уже реализовано
2. Отсутствует документация по CI/CD (GitHub Actions workflows)
3. Отсутствует документация по E2E тестам (Playwright)
4. `docs/README.md` не содержит ссылок на CI/CD и тестирование

---

### [x] Step 1: Обновление docs/configs/passport.md

**Задача:** Актуализировать документацию по конфигурации паспортов качества

**Изменения:**
- Убрать статус "ПЛАНИРУЕТСЯ" для поля `IsDefault`
- Добавить описание реализованного функционала автоподстановки методов испытаний
- Обновить примеры конфигурации

**Верификация:** Проверить соответствие документации реальным конфигурационным файлам

---

### [x] Step 2: Создание docs/development/testing.md

**Задача:** Создать документацию по тестированию проекта

**Содержание:**
- Структура тестовых проектов (Tests.Unit, Tests.Integration, Tests.E2E, Tests.Shared)
- Команды запуска тестов
- E2E тесты на Playwright (справочники, UI)
- Интеграционные тесты (КМХ, документы)
- Руководство по написанию тестов

---

### [x] Step 3: Создание docs/development/ci-cd.md

**Задача:** Создать документацию по CI/CD

**Содержание:**
- GitHub Actions workflows:
  - `tests-on-push.yml` - запуск тестов при push в develop*
  - `build-and-package.yml` - сборка и упаковка
- Необходимые secrets (GH_SUBMODULES_TOKEN, FR_NUGET_USERNAME, FR_NUGET_PASSWORD)
- Порядок работы pipelines

---

### [x] Step 4: Обновление docs/README.md

**Задача:** Добавить ссылки на новую документацию

**Изменения:**
- Добавить раздел "CI/CD" с ссылкой на `ci-cd.md`
- Добавить раздел "Тестирование" с ссылкой на `testing.md`
- Обновить дату и версию документации

---

### [x] Step 5: Проверка и финализация

**Задача:** Проверить согласованность документации

**Действия:**
- Проверить все ссылки в документах
- Убедиться в согласованности версии 1.3.8
- Написать отчёт в `report.md`
