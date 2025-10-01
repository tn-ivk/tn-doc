# StatusBar Component (PrimeVue Edition)

Компонент статус-бара для TN_Doc, переписанный с использованием PrimeVue.

## Установка зависимостей

```bash
cd TN_Doc/Client/statusbar
npm install
```

## Разработка

```bash
npm run dev
```

## Сборка для production

```bash
npm run build
```

Собранные файлы будут в `dist/` и автоматически копируются в `wwwroot/js/statusbar/`.

## Использованные компоненты PrimeVue

- **Badge** - индикаторы статуса устройств и сервисов
- **Button** - кнопка обновления
- **Tag** - индикатор SignalR подключения
- **Message** - уведомления об ошибках
- **Tooltip** - всплывающие подсказки (директива)

## Основные изменения

### Что изменилось

1. **Добавлены зависимости:**
   - `primevue` - библиотека UI компонентов
   - `@primevue/themes` - система тем
   - `primeicons` - набор иконок

2. **Обновлен main.ts:**
   - Настроена PrimeVue с темой Aura
   - Добавлена полная русская локализация
   - Зарегистрирована директива Tooltip

3. **Переписан StatusIndicator.vue:**
   - Использует `Badge` вместо кастомных стилей
   - Иконки из `PrimeIcons` (pi pi-check-circle, pi pi-times-circle)
   - Автоматический цвет на основе severity (success/danger/warn)

4. **Переписан StatusBar.vue:**
   - `Button` с loading состоянием для обновления
   - `Tag` для отображения статуса SignalR
   - `Message` для красивых уведомлений об ошибках
   - Использует CSS переменные PrimeVue для единого стиля

### Что осталось прежним

- Вся бизнес-логика (Pinia store, SignalR, composables)
- TypeScript типы
- Структура проекта
- API интерфейс компонентов

## Преимущества PrimeVue версии

✅ **Профессиональный дизайн** - компоненты enterprise-уровня
✅ **Консистентность** - единая система дизайна
✅ **Accessibility** - встроенная поддержка a11y
✅ **Темизация** - легко изменить цветовую схему
✅ **Русификация** - полная поддержка русского языка
✅ **Меньше кода** - не нужно писать CSS с нуля
✅ **Адаптивность** - работает на всех устройствах

## Следующие шаги

При полной миграции фронтенда на Vue:

1. **Справочники** - DataTable + Dialog для CRUD
2. **Формы редактирования** - InputText, InputNumber, Calendar, Dropdown
3. **Главная страница** - DataTable с поиском и сортировкой
4. **Генерация отчетов** - ProgressBar, FileUpload

## Документация

- [PrimeVue Docs](https://primevue.org/)
- [PrimeIcons](https://primevue.org/icons)
- [Theme Designer](https://primevue.org/theming)
