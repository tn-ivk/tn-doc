# Чек-лист интеграции ELIS - Быстрая проверка

## 🎯 Текущий статус: Требуется доработка FormField (75%)

### ✅ Что готово

- [x] ⭐ Проанализирована реальная конфигурация `CfgEditPassport_GOSTR50.2.040(I).json`
- [x] ⭐ Обнаружен реальный формат `ElisAlias`: массив строк `["key1", "key2"]`
- [x] ⭐ Документирована смешанная номенклатура (camelCase vs русские названия)
- [x] TypeScript типы для ELIS данных
- [x] Документация для разработчиков (обновлена с учётом реальной конфигурации)
- [x] Поддержка `elisAlias` в типах (требуется изменить на `string[]`)
- [x] CSS переменная `--md-elis-highlight` для подсветки

### ⚠️ Что в процессе / планируется

- [ ] ⚠️ **Обновить TypeScript типы**: `elisAlias?: string[]` (было `string?`)
- [ ] Vue композабл `useElisIntegration.ts` с функцией `findElisValue()` (зависит от Этапа 0)
- [ ] Обновлен `Common.js` для отправки postMessage (зависит от Этапа 0)
- [ ] Интеграция в `DocumentPassportEditor.vue` (зависит от Этапа 0)
- [ ] **FormField.vue с универсальной подсветкой** (Этап 0 - критичный блокер)

### 🚧 Что нужно сделать СЕЙЧАС

#### ⭐ Этап 0: Доработка FormField (КРИТИЧНЫЙ - БЛОКИРУЕТ ВСЁ!)

**Приоритет**: Высший
**Статус**: Не начат
**Время**: 0.5-1 день

- [ ] Добавить опциональный проп `highlightColor` в `FormField.vue`
- [ ] Реализовать computed свойство `fieldBackgroundStyle`
- [ ] Применить стили на все типы полей:
  - [ ] InputText (text)
  - [ ] InputNumber (number)
  - [ ] Select (select)
  - [ ] DatePicker (date, datetime-local)
- [ ] Протестировать безопасность изменений:
  - [ ] DocumentEditor.vue (без подсветки)
  - [ ] DocumentActEditor.vue (без подсветки)
  - [ ] DocumentPassportEditor.vue (с подсветкой для ELIS)
- [ ] Обновить JSDoc документацию компонента
- [ ] Code review

**Почему критично?**
Все последующие этапы зависят от универсального механизма подсветки. Без него нельзя реализовать визуальное отображение ELIS данных.

---

#### 🔍 Этап 1: Проверка конфигурации (ВАЖНО!)

- [x] ✅ Открыть файл `TN_Doc/Cfg/Passport/CfgEditPassport_GOSTR50.2.040(I).json` - **ВЫПОЛНЕНО**
- [x] ✅ Проверить наличие `ElisAlias` в секции `AdditionalInfo[]` - **ПОДТВЕРЖДЕНО: 11 полей из 16**
- [x] ✅ Проверить наличие `ElisAlias` в секции `Parameters[]` - **ПОДТВЕРЖДЕНО: 12 параметров из 19**
- [x] ✅ Документировать формат `ElisAlias` - **это массив строк `["key1", "key2"]`**
- [ ] ⚠️ Убедиться, что ключи соответствуют данным из ELIS API (требуется тестирование с реальными данными)
- [ ] Проверить маппинг PascalCase → camelCase на сервере (ElisAlias → elisAlias)
- [ ] Проверить, что устаревшее поле `KeyELIS` не используется

**⚠️ КРИТИЧНЫЕ НАХОДКИ**:
- `ElisAlias` это **массив строк**, не строка с разделителем `|`
- **AdditionalInfo** использует **camelCase** (`["labName"]`)
- **Parameters** использует **русские полные названия** (`["Массовая доля воды(%)"]`)
- Механизм fallback реализован через массив, а не через `|`

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

// 2. Проверить конфигурацию полей (с учётом массивов)
console.log('Fields with elisAlias:',
  store.fields
    .filter(f => f.elisAlias)
    .map(f => ({
      key: f.key,
      elisAlias: f.elisAlias,
      isArray: Array.isArray(f.elisAlias)
    }))
);

// 3. Проверить конфигурацию параметров (с учётом массивов)
console.log('Parameters with elisAlias:',
  store.config.qualityParametersSchema
    .filter(p => p.elisAlias)
    .map(p => ({
      key: p.key,
      elisAlias: p.elisAlias,
      isArray: Array.isArray(p.elisAlias)
    }))
);

// 4. Проверить данные ELIS
const elisData = JSON.parse(localStorage.getItem('dataPassport'));
console.log('ELIS labInfo:', elisData?.labInfo);
console.log('ELIS parameters keys:', elisData?.parameters ? Object.keys(elisData.parameters) : 'отсутствует');
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

1. ⭐ **СЕЙЧАС (блокирует всё!)**: Доработать `FormField.vue` - добавить универсальную подсветку (Этап 0)
2. **ПОСЛЕ Этапа 0**: Обновить TypeScript типы - изменить `elisAlias?: string` на `elisAlias?: string[]`
3. **ПОСЛЕ Этапа 0**: Создать композабл `useElisIntegration.ts` с функцией `findElisValue()`
4. **ПОСЛЕ Этапа 0**: Обновить `Common.js` для отправки postMessage
5. **ПОСЛЕ Этапа 0**: Интегрировать в `DocumentPassportEditor.vue`
6. **ПОСЛЕ ВСЕГО**: Протестировать с реальными данными ELIS и проверить соответствие ключей

### ⚠️ Важно: OPC интеграция

- **OPC интеграция при сохранении паспорта - это отдельная независимая задача**
- Не входит в scope этой задачи по ELIS интеграции
- Текущий механизм OPC работает корректно
- **Основное требование**: НЕ СЛОМАТЬ текущую реализацию OPC

### ⚠️ Критичные изменения TypeScript типов

```typescript
// БЫЛО (неправильно):
export interface FormField {
  elisAlias?: string;  // ❌
}

// ДОЛЖНО БЫТЬ:
export interface FormField {
  elisAlias?: string[];  // ✅ массив строк
}

// То же для PassportQualityParameterSchema
export interface PassportQualityParameterSchema {
  elisAlias?: string[];  // ✅ массив строк
}
```

### 🚀 Готовность к использованию

- **Для начала разработки**: ⚠️ Требуется Этап 0 (75%)
- **Для тестирования**: ❌ После завершения Этапа 0 и обновления типов (80%)
- **Для pilot на 1-2 устройствах**: ❌ После тестирования (90%)
- **Для production на всех устройствах**: ❌ Нужны доработки (см. полный план)

### 📚 Полезные ссылки

- Полный план: `tech_debt/ELIS_INTEGRATION_PLAN.md`
- Документация: `TN_Doc/Client/document-editor/ELIS_INTEGRATION.md`
- Старая реализация: `TN_Doc/wwwroot/js/Common.js:1423-1661`

### 🎯 Критические файлы для работы

**Этап 0 (сейчас):**
```
TN_Doc/Client/document-editor/src/components/FormField.vue  ← НАЧАТЬ С ЭТОГО!
```

**После Этапа 0:**
```
TN_Doc/Client/document-editor/src/types/document.types.ts  ← Обновить типы elisAlias
TN_Doc/Cfg/Passport/CfgEditPassport_GOSTR50.2.040(I).json  ← Проверено ✅
TN_Doc/Client/document-editor/src/composables/useElisIntegration.ts (создать)
TN_Doc/Client/document-editor/src/views/DocumentPassportEditor.vue
TN_Doc/wwwroot/js/Common.js (строки 1444-1464)
```

### 📊 Статистика по конфигурации

**CfgEditPassport_GOSTR50.2.040(I).json:**
- **AdditionalInfo**: 11 полей с ELIS из 16 (69%)
  - `labName`, `accreditationNumber`, `chiefLabPosition`, `chiefLabOrganization`, `chiefLabShortSign`
  - `pointDeliveryName`, `startPeriodTime`, `endPeriodTime`, `protocolNumber`
- **Parameters**: 12 параметров с ELIS из 19 (63%)
  - Массовая доля воды, Хлористые соли (2 варианта), Механические примеси, Сера
  - ДНП (2 варианта), Сероводород, Меркаптаны, Органические хлориды

---

**Последнее обновление**: 2025-11-06
**Ответственный**: Разработчик, работающий с ELIS интеграцией
**Следующий шаг**: ⭐ Доработать `FormField.vue` - добавить универсальную подсветку (Этап 0)

## История изменений

- **2025-11-06**: Обновлён чек-лист на основе анализа реальной конфигурации
  - Добавлена статистика по конфигурации (11/16 AdditionalInfo, 12/19 Parameters)
  - Обновлены скрипты отладки с учётом массивов `elisAlias`
  - Добавлен раздел о критичных изменениях TypeScript типов
  - Документированы критичные находки (массив, смешанная номенклатура)
- **2025-11-07**: Добавлено предупреждение по OPC интеграции
  - OPC интеграция при сохранении паспорта - отдельная задача
  - Не входит в scope ELIS интеграции
  - Основное требование: не сломать текущую реализацию OPC
