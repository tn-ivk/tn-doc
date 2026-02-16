# Changelog

Все значимые изменения в проекте TN_Doc будут документироваться в этом файле.

Формат основан на [Keep a Changelog](https://keepachangelog.com/ru/1.0.0/),
и проект придерживается [Semantic Versioning](https://semver.org/lang/ru/).

## [1.3.8] - 2026-01-19

### Added
- Восстановлена тестовая инфраструктура (директория Tests/)
  - 35 тестовых файлов по 5 категориям: Controllers, Services, Libraries (Core/Common/KMH), Configs/Fixtures/Integration
  - ~315 работающих тестов (~48%), ~335 отключенных тестов (~52%)
  - Полная документация в Tests/README.md
  - КМХ тесты полностью актуализированы (7 файлов, ~168 тестов)
  - Common тесты работают (CommonPoverka1974, CommonSikn425)
- Добавлена документация проекта (docs/)
  - Архитектура, разработка, развертывание, операции, интеграции
  - README.md с навигацией по документации

### Changed
- Откат с версии 1.4.x на стабильную версию 1.3.8 для промышленной эксплуатации
- Субмодуль tn.docgeneral откачен до совместимой версии

### Fixed
- Исправлены конструкторы КМХ документов (убран несуществующий параметр IConfigurationCacheService)
- Исправлены пути в Tests.csproj для корректной сборки тестов

### Known Issues
- Core тесты (ActDocumentTests, JornalDocumentTests, PassportDocumentTests, ReportDocumentTests) требуют ручной правки конструкторов
- ConfigurationCacheService, DbSchemaCache не реализованы
- ClientLogController, PdfController не реализованы
- PrintControllerTests требуют интерфейс IPrinterService

---

## [Unreleased] - 2026-02-16

### Added
- `ConfigEncodingTests` для автоматической проверки JSON-конфигураций в `TN_Doc/Cfg`:
  - отсутствие символов замены `U+FFFD`
  - валидность UTF-8
  - корректность JSON-синтаксиса
- Negative unit-тесты для `PrinterService`, `WindowsPrinter`, `LinuxPrinter` (проверки null/empty/exception, платформенных и конкурентных сценариев).

### Changed
- Рефакторинг unit-тестов на использование реальных компонентов через Reflection вместо тестовых наследников/дублей:
  - `HomeControllerTests` (создание контроллера через `RuntimeHelpers.GetUninitializedObject`)
  - `PrinterServiceTests`, `WindowsPrinterTests`, `LinuxPrinterTests`
- Обновлен submodule `tn.docgeneral` до `ee8641af`:
  - `KMH_PP_Areom` поддерживает старый и новый формат данных протокола (через `Protokol.version` и fallback-логику)
  - улучшено логирование и трассировка в `DocKMH_PP_Areom`
  - `DocGeneral._logger` сделан `protected` для переиспользования в потомках
- Обновлена вёрстка главной страницы (`TN_Doc/Views/Home/Index.cshtml`, `TN_Doc/wwwroot/css/LeftPanel.css`, `TN_Doc/wwwroot/css/site.css`):
  - ограничена ширина комбобокса шаблона документа: `max-width: 450px`
  - увеличена ширина кнопки «Получить данные»: `40% → 45%`, добавлен `white-space: nowrap`
  - удалён лишний тег `<body>` из `Index.cshtml`
  - уменьшена ширина ячейки кнопки режима редактирования: `250px → 200px`
  - добавлен `overflow: hidden` для контейнера `.cont`
  - удалено дублирование блока sticky footer в `site.css`

### Fixed
- Исправлена кодировка `FieldSIKN` в 27 JSON-конфигах (`TN_Doc/Cfg/*`): удалены поврежденные символы, приведено к корректному значению `СИКН`.
- Добавлена обработка ошибок в `ReadTagCache` и `ReadTagCacheARM`:
  - обработка HTTP `404/500`
  - `try/catch` вокруг AJAX-вызовов
  - отправка ошибок в серверный лог через `Logger.js` (`/api/ClientLog/logging`)

### Documentation
- Обновлены `docs/development/testing.md` и `docs/configs/passport.md` (учтен запуск `ConfigEncodingTests`, актуализированы примеры тестов и чек-листы проверок).

### Planned Features (ветка docs-elis)

> **Примечание**: Следующие функции были реализованы в ветке docs-elis и будут постепенно портированы в стабильную версию.

- Механизм связанных параметров (master-slave) для паспорта качества
  - Поле `SlaveKey` в конфигурации параметров
  - Связь DNP.kPa (мастер) → DNP.mercury_mm (слейв)
- Объединение методов испытаний для связанных параметров (LinkedParameters)
  - Поле `LinkedParameter` в конфигурации
  - Объединённый комбобокс метода для пар параметров
- Система истории изменений полей паспорта качества
  - Отслеживание источника данных (ELIS, ручное редактирование)
  - Визуальные индикаторы источников в UI
- Act: условное отображение кнопки ручного ввода подписанта
- Document Editor (Vue 3 компонент) - улучшения
- Configurator: визуальный редактор конфигурации паспорта качества

---

## [1.3.7] - 2025-XX-XX

### Added
- Ручная корректировка (подстановки) вязкости для первого и последнего измерения Поверка 3380

### Fixed
- Невозможно экспортировать Отчеты (любые документы у которых нет 2-ого протокола). Рефакторинг GetViewDoc
- Экспорт 2-ого протокола
- Кор. шаблона `KMH_PP_Areom_GOSTR8.1011-2022(F3).frx`: СИКН/СИКНП -> [FieldSikn]
- Кор. шаблона `KMH_PP_GOSTR8.1011-2022(F1).frx`: нефти/нефтипродуктов -> [OilName]
- Кор. шаблона `16_Poverka3189_Release_version.frx`: мелкие правки
- Кор. шаблона `Poverka1974_04.frx`: правка таблицы исходных данных по замечаниям Лукойл
- Кор. шаблона `Poverka1974_89.frx`: правка таблицы исходных данных по замечаниям Лукойл

## [1.3.6] - 2025-XX-XX

### Fixed
- Исправление СИКН/СИКНП в `CfgKMH_PW.json`
- Исправления TotalPage для `KMH_PR_PU_GOSTR8.1011-2022(U2).frx`
- Кор. шаблона `16_Poverka3189_Release_version.frx`: Qnom = Q_nom_or_max
- Доработки шаблонов паспортов по Р 50.2.040 (мелкие правки сносок)

## [1.3.5] - 2025-XX-XX

### Changed
- По умолчанию выбрана форма паспорта по ГОСТ, приложение И

### Fixed
- Исправлена загрузка списка документов для устройств ИВК-2, ИВК-3
- Кор. формы протокола `KMH_MPR_MPR_GOSTR8.1011-2022(U5).frx`: Номер "заключения"
- Кор. формы протокола `Poverka3380.frx`: перенос табл. 4 на новую страницу
- Кор. формы протокола `Poverka1974_89.frx`

## [1.3.4] - 2025-XX-XX

### Changed
- Определение пути для логов в зависимости от ОС:
  - Windows: `basedir/TN_Doc/logs`
  - Unix: `/opt/TN_Doc/logs` (временно)
- В конфигурации ИВК по умолчанию включено использование PVL и PVS
- Приведение IP-адресов к "новым" стандартам

### Fixed
- Исправлен ввод Мин. значения Метода испытания в Справочниках
- Кор. `KMH_MPR_MPR_GOSTR8.1011-2022(U5).frx`: размещение на 1 стр. до 5 измерений
- Кор. шаблона `Poverka1974_89.frx`, `Poverka1974_95.frx`
- Кор. шаблона `16_Poverka3189_Release_version.frx`: исправление единиц измерения расхода

---

## Легенда

- **Bug Fix** - Исправление ошибки
- **Breaking Change** - Изменение, требующее внимания при обновлении
- **New Feature** - Новая функциональность
- **Documentation** - Обновление документации
- **Maintenance** - Техническое обслуживание кода

[1.3.8]: https://github.com/orpovy/ivk/tn_doc/compare/v1.3.7...v1.3.8
[1.3.7]: https://github.com/orpovy/ivk/tn_doc/compare/v1.3.6...v1.3.7
[1.3.6]: https://github.com/orpovy/ivk/tn_doc/compare/v1.3.5...v1.3.6
[1.3.5]: https://github.com/orpovy/ivk/tn_doc/compare/v1.3.4...v1.3.5
[1.3.4]: https://github.com/orpovy/ivk/tn_doc/releases/tag/v1.3.4
