# Чек-лист интеграции ELIS - Быстрая проверка

## 🎯 Текущий статус: Базовая реализация готова (80%)

### ✅ Что готово

- [x] Vue композабл `useElisIntegration.ts`
- [x] TypeScript типы для ELIS данных
- [x] Обновлен `Common.js` для отправки postMessage
- [x] CSS стили для подсветки ELIS полей
- [x] Документация для разработчиков
- [x] Интеграция в `DocumentPassportEditor.vue`
- [x] Поддержка `elisAlias` в типах

### ⏳ Что нужно проверить и доработать

#### 🔍 Этап 1: Проверка конфигурации (ВАЖНО!)

- [ ] Открыть файл `TN_Doc/Cfg/CfgEditPassport.json`
- [ ] Проверить наличие `ElisAlias` в секции `AdditionalInfo.Fields[]`
- [ ] Проверить наличие `ElisAlias` в секции `Edit.Parameters[]`
- [ ] Убедиться, что ключи соответствуют данным из ELIS API
- [ ] Проверить маппинг PascalCase → camelCase (ElisAlias → elisAlias)

**Критично**: Без правильного `ElisAlias` в конфигурации заполнение не будет работать!

#### 🧪 Этап 2: Минимальное тестирование

**Быстрый тест (5 минут)**:

1. [ ] Запустить приложение: `cd TN_Doc && dotnet run`
2. [ ] Запустить TN.ElisConnector на порту 5050
3. [ ] Открыть главное окно, выбрать устройство с ELIS
4. [ ] Создать новый паспорт
5. [ ] Нажать "ЕЛИС" → выбрать протокол → "Применить"
6. [ ] Проверить в DevTools Console (iframe):
   ```javascript
   // Должно быть сообщение:
   "[useElisIntegration] Получены данные ELIS из главного окна"
   ```
7. [ ] Проверить, что поля заполнены и имеют зеленый фон

**Если не работает** → смотри раздел "Отладка" ниже

#### 🐛 Быстрая отладка

**Проблема 1: Поля не заполняются**

```javascript
// В DevTools Console (iframe Vue компонента):

// 1. Проверить, что postMessage получен
window.addEventListener('message', e => console.log('Message:', e.data));

// 2. Проверить конфигурацию полей
console.log('Fields with elisAlias:',
  store.fields.filter(f => f.elisAlias)
);

// 3. Проверить данные ELIS
console.log('ELIS data:', localStorage.getItem('dataPassport'));
```

**Проблема 2: Подсветка не работает**

```javascript
// В DevTools Console (iframe):
// Проверить флаги заполнения
Object.keys(store.formData)
  .filter(k => k.includes('__elisFilled'))
  .forEach(k => console.log(k, store.formData[k]));
```

**Проблема 3: Ошибки в консоли**

```javascript
// Проверить логи
// Искать сообщения с префиксом [useElisIntegration]
```

### 📝 Следующие приоритетные задачи

1. **СЕЙЧАС**: Проверить `ElisAlias` в `CfgEditPassport.json`
2. **СЕЙЧАС**: Провести быстрый тест
3. **ВАЖНО**: Протестировать с реальными данными ELIS
4. **ВАЖНО**: Добавить обработку ошибок
5. **ЖЕЛАТЕЛЬНО**: Добавить UX улучшения (Toast, индикатор загрузки)

### 🚀 Готовность к использованию

- **Для тестирования**: ✅ Готово (80%)
- **Для pilot на 1-2 устройствах**: ⚠️ После тестирования (90%)
- **Для production на всех устройствах**: ❌ Нужны доработки (см. полный план)

### 📚 Полезные ссылки

- Полный план: `tech_debt/ELIS_INTEGRATION_PLAN.md`
- Документация: `TN_Doc/Client/document-editor/ELIS_INTEGRATION.md`
- Старая реализация: `TN_Doc/wwwroot/js/Common.js:1423-1661`

### 🎯 Критические файлы для проверки

```
TN_Doc/Cfg/CfgEditPassport.json           ← НАЧАТЬ С ЭТОГО!
TN_Doc/Client/document-editor/src/composables/useElisIntegration.ts
TN_Doc/Client/document-editor/src/views/DocumentPassportEditor.vue
TN_Doc/wwwroot/js/Common.js (строки 1444-1464)
```

---

**Последнее обновление**: 2025-11-06
**Ответственный**: Разработчик, работающий с ELIS интеграцией
**Следующий шаг**: Проверить `ElisAlias` в конфигурации
