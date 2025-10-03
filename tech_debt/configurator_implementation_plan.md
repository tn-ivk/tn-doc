# План реализации конфигуратора настроек

## 1. Подготовка инфраструктуры

### Backend (ASP.NET Core)
- Создать **ConfiguratorController** с эндпоинтами:
  - `GET /api/configurator/config` - получение текущей конфигурации из CfgApp.json
  - `POST /api/configurator/config` - сохранение конфигурации
  - `GET /api/configurator/validate` - валидация настроек
- Создать DTO модели для конфигурации (общие настройки, устройства)
- Добавить сервис **ConfigurationService** для работы с CfgApp.json
- Использовать существующий **AppConfigService** для чтения/записи настроек; при необходимости, **ConfigurationService** как фасад над ним.
- Вынести парсинг и валидаторы OPC/DB в отдельные классы в `TN_Doc/Services` (например, `OpcConfigValidator`, `DbConfigValidator`).

### Frontend (Vue 3)
- Создать новый проект: `/TN_Doc/Client/configurator/`
- Настроить Vite + TypeScript + PrimeVue (по аналогии с statusbar)
- Установить зависимости: vue, pinia, primevue, axios

## 2. Архитектура компонентов

```
/configurator/
├── src/
│   ├── App.vue                    # Корневой компонент с TabView
│   ├── stores/
│   │   └── configStore.ts         # Pinia store для состояния конфига
│   ├── components/
│   │   ├── GeneralTab.vue         # Вкладка "Общие"
│   │   ├── DevicesTab.vue         # Вкладка "Устройства"
│   │   ├── DeviceList.vue         # Список устройств (слева)
│   │   ├── DeviceEditor.vue       # Редактор устройств (справа)
│   │   ├── OpcSettings.vue        # Переиспользуемый компонент OPC
│   │   └── MixedStateField.vue    # Компонент для "смешанного состояния"
│   ├── types/
│   │   └── config.types.ts        # TypeScript интерфейсы
│   └── services/
│       └── api.service.ts         # HTTP клиент для API
```

## 3. State Management (Pinia Store)

**configStore.ts**:
```typescript
- originalConfig: Config           // исходная конфигурация
- currentConfig: Config            // текущее состояние
- selectedDeviceIds: string[]      // выбранные устройства
- isDirty: boolean                 // есть несохранённые изменения
- validationErrors: Map<string, string>

Actions:
- loadConfig()
- saveConfig()
- updateGeneralSettings()
- updateDeviceSettings()
- validateConfig()
```

## 4. Реализация по компонентам

### App.vue
- **PrimeVue TabView** с двумя вкладками
- Кнопка "Применить" внизу (fixed position)
- Обработка валидации перед сохранением
- Toast для уведомлений (PrimeVue)
- Предохранитель о несохранённых изменениях: `beforeunload` + `route guard` на основе `isDirty`.

### GeneralTab.vue
- **InputText** для ExportDoc.Path (обязательное поле)
- **InputSwitch** для UseSecurityFeatures
- **OpcSettings** компонент с props:
  - `modelValue` - настройки OPC
  - `showTypeSelector` - показывать переключатель DA/UA

### DevicesTab.vue
- Layout: **Splitter** (PrimeVue) для двух панелей
- Левая панель: **DeviceList**
- Правая панель: **DeviceEditor**

### DeviceList.vue
- **IconField** с поиском (фильтрация по имени)
- **Listbox** с `multiple` режимом
- Виртуализация больших списков: PrimeVue `VirtualScroller` (или кастомная реализация) + debounce поиска.
- Computed: filtered devices

### DeviceEditor.vue
Логика мультиредактирования:
- Computed свойство `isMixed(fieldPath)` - проверяет различия в значениях
- **MixedStateField** wrapper для всех полей
- Секции:
  1. Use - InputSwitch
  2. Docs.Use - MultiSelect с чекбоксами
  3. DB - Dropdown для выбора подключения, поля Server/Timeout, RadioButton группа UsedSI
  4. OPC - компонент OpcSettings
  5. InvalidChars - три Checkbox (\", ', \\)

### MixedStateField.vue
Props: `isMixed`, `type` (checkbox/input/select)
- Отображает warning при `isMixed=true`
- Placeholder "— разные значения —"
- Стили для индикации смешанного состояния

### OpcSettings.vue
- **SelectButton** для выбора DA/UA
- Динамическое отображение полей через `v-if`
- InputText/InputNumber для полей

## 5. PrimeVue компоненты

- **TabView** - вкладки
- **Splitter** - разделение панелей
- **Listbox** - список устройств
- **VirtualScroller** - виртуализация списка устройств
- **InputText** / **InputNumber** - поля ввода
- **InputSwitch** - переключатели
- **SelectButton** / **RadioButton** - выбор типа
- **Checkbox** - множественный выбор
- **Dropdown** / **MultiSelect** - списки
- **Button** - кнопка "Применить"
- **Toast** - уведомления
- **Message** - ошибки валидации

## 6. Интеграция с backend

**api.service.ts**:
```typescript
- getConfig(): Promise<Config>
- saveConfig(config: Config): Promise<void>
- validateConfig(config: Config): Promise<ValidationResult>
```

### Аудит и логирование изменений
- Логировать через NLog каждое изменение конфигурации: что изменил (до/после) с вычислением diff по ключам.

## 7. Интеграция в ASP.NET Core

- Добавить маршрут в **Startup.cs** для `/configurator`
- Создать View **Configurator/Index.cshtml** с точкой монтирования Vue
- Добавить пункт меню в навигацию

## 8. Этапы разработки

1. ✅ **Backend API** - контроллер, сервис, DTO
2. ✅ **Базовая структура** - проект Vue, routing, store
3. ✅ **Вкладка "Общие"** - простые поля, OpcSettings
4. ✅ **Список устройств** - DeviceList с поиском
5. ✅ **Редактор (одно устройство)** - все поля DeviceEditor
6. ✅ **Мультиредактирование** - логика смешанного состояния, MixedStateField
7. ✅ **Валидация** - обязательные поля, форматы
8. ✅ **Сохранение** - интеграция с API, обработка ошибок
9. ✅ **Интеграция в приложение** - меню, layout
10. ✅ **Тестирование** - различные сценарии

## 9. Ключевые технические решения

- **Reactivity**: computed для определения смешанного состояния
- **Deep watching**: watch на selectedDeviceIds для обновления формы
- **Immutability**: клонирование конфига при изменениях
- **Типизация**: строгие TypeScript интерфейсы для всей конфигурации
- **Validation**: композабл `useValidation()` для переиспользования логики

Начать рекомендую с пунктов 1-2, затем итеративно добавлять функциональность.

## Требования к функционалу

### Вкладка «Общие»
- Поле для указания пути экспорта: **ExportDoc.Path**
- Переключатель: **UseSecurityFeatures** (ролевой доступ)
- Блок настроек локального OPC-клиента **ArmOpcConnectionSettings**:
  - Переключатель типа (OPC DA / OPC UA)
  - Для OPC DA: StartPrefix, Host, ProgId, UpdateRate
  - Для OPC UA: ConfigFilename, UpdateRate, StartPrefix

### Вкладка «Устройства»

**Две панели**:
- Слева — панель со списком устройств с поиском и возможностью выделять несколько устройств
- Справа — панель настройки выбранных устройств

**Логика выбора**:
- Если выбрано одно устройство → форма работает индивидуально
- Если выбрано несколько → форма работает как групповой редактор:
  - Вносимые изменения применяются ко всем выбранным
  - Если у выбранных устройств разные значения в поле → контрол показывает «смешанное состояние»:
    - для чекбоксов и переключателей → предупреждение «У выбранных устройств разные значения»
    - для текстовых/числовых полей → placeholder «— разные значения —»
    - для select → опция «— разные значения —» (disabled)

**Редактируемые свойства устройств**:
- **Use** (вкл/выкл устройство)
- **Docs.Use** (список документов, чекбоксы)
- **DB**:
  - список подключений
  - поля Server, Timeout
  - UsedSI (группа переключателей: PR, PP, PVL, PVS и «вторые» СИ)
- **OPC**:
  - переключатель типа (DA/UA)
  - поля DA: StartPrefix, Host, ProgId, UpdateRate
  - поля UA: ConfigFilename, UpdateRate, StartPrefix
- **InvalidChars**: три переключателя (\", ', \\)

### Управление
- Внизу одна кнопка «Применить» (сохраняет текущее состояние, в демо достаточно alert)
- Валидация: если обязательные поля пустые (например, ExportDoc.Path), показывать сообщение об ошибке
- В случае мультивыбора — подсветка различий (warn-label)
