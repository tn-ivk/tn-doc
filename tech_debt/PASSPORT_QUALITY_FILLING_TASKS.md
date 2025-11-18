## PASSPORT Quality Filling — задачи по фазам

Документ синхронизирован с `tech_debt/PASSPORT_QUALITY_FILLING_PLAN.md` (раздел 11). Каждая задача включает краткое описание, основные действия и критерии приёмки/тесты.

### Фаза 0 — Подготовка конфигов
1. **CFG-0.1: Проставить `IsBalast` во всех `CfgEditPassport_GOSTR50.2.040(I).json`**
   - Действия: обновить JSON, добавить новое поле для параметров, оставить старые конфиги без изменений логики (отсутствие поля = `false`).
   - Acceptance: конфиги проходят `tn_toolsfastreport validate-config`; эксплуатация одобряет diff; rollback описан.
   - Tests: unit `Tests/Configs/CfgEditPassportTests.cs`, ручная проверка устройств ELIS on/off.
2. **CFG-0.2: Скрипт миграции (`tools/passport/migrate_is_balast.ps1`)**
   - Действия: реализовать dry-run/commit режимы, формировать отчёт (JSON/CSV) с итогами.
   - Acceptance: отчёт прикладывается к PR; скрипт задокументирован в `docs/configs/passport.md`.

### Фаза 1 — Стратегия и контракты (бэкенд)
1. **BE-1.1: Внедрить `IPassportQualityStrategy` в `DocPassport`**
   - Действия: выделить стратегию для ELIS on/off, учитывать `IsBalast`, `resultEditMode`.
   - Acceptance: unit `DocPassportStrategyTests` покрывает основные ветки; старый контракт для устройств без `IsBalast` не ломается.
2. **BE-1.2: Обновить DTO/JSON/TS-типы**
   - Действия: добавить `isBalast`, `methodSource`, `resultEditMode` в модели (`TN.DocEditor.Passport.*`, `passport.types.ts`).
   - Tests: `dotnet test Tests/Services --filter PassportQuality`, проверка сериализации.

### Фаза 2 — Сохранение и история (бэкенд)
1. **BE-2.1: Обновить `DocUpdate` и `AddOrUpdateLabInfo`**
   - Действия: синхронизация балластных value/result, вычисление `methodSource`, запись истории «Manual overrides ELIS».
   - Acceptance: интеграционные тесты `DocUpdateTests` закрывают сценарии из плана (ручной метод, ELIS → Manual).
2. **BE-2.2: Логирование и предупреждения**
   - Действия: добавить Trace/Info логи, предупреждение при расхождении value/result, фиксация источников.
   - Tests: проверка логов в unit/integration (через mock logger) или ручной сценарий.

### Фаза 3 — UI и UX
1. **FE-3.1: Обновить `usePassportEditor`, `PassportQualityTable`**
   - Действия: добавить `isBalast`, `methodSource`, пересчёт результатов с учётом стратегий, подсветки.
   - Tests: `npm run test -- usePassportEditor`, компонентные тесты таблицы.
2. **FE-3.2: Модалки результатов и методов**
   - Действия: реализовать UI (PrimeVue), валидацию, запись истории, подсказки об источниках.
   - Acceptance: макеты согласованы; Storybook/E2E сценарии показывают корректное поведение.
3. **FE-3.3: Локализация и стили**
   - Действия: обновить `material3.css`, добавить строки в `document-editor/src/locales/ru.json`.

### Фаза 4 — E2E и документация
1. **DOC-4.1: Обновить документацию**
   - Действия: описать новые поля/UX в `docs/features/field-history.md`, `docs/elis-summary.md`, `docs/architecture/document-editor.md`, `docs/configs/passport.md`.
2. **QA-4.2: E2E сценарии**
   - Действия: настроить Cypress/Playwright тесты (импорт ELIS → ручное редактирование, ручной метод, печать).
   - Acceptance: чек-лист из раздела 9 плана закрыт; отчёт приложен к PR.

### Фаза 5 — Регресс и релиз
1. **REL-5.1: Сборка и smoke**
   - Действия: `dotnet build TN_Doc.sln`, `npm run build` (statusbar/document-editor), ручные smoke-наборы ELIS on/off.
2. **REL-5.2: Release notes**
   - Действия: подготовить описание изменений, шаги миграции конфигов, скриншоты UI, статус тестов.
