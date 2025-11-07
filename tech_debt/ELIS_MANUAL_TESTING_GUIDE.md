# Инструкция для мануального тестирования ELIS интеграции

**Версия**: 1.0
**Дата**: 2025-11-07
**Статус**: Этапы 0-1 завершены, готовность 85%

---

## Предварительные требования

### 1. Запущенные сервисы

- [ ] **TN_Doc** запущен на `http://localhost:38509`
- [ ] **TN.ElisConnector** запущен на `http://localhost:5050`
- [ ] Доступ к тестовому API ELIS
- [ ] Устройство с `isElisUsed: true` в конфигурации

### 2. Настройка окружения

```bash
# 1. Запустить TN_Doc
cd /home/snafu/projects/ivk/tn_doc/TN_Doc
ASPNETCORE_ENVIRONMENT=Development dotnet run

# 2. В отдельном терминале запустить TN.ElisConnector
# (путь к проекту TN.ElisConnector может отличаться)
cd /path/to/TN.ElisConnector
dotnet run
```

### 3. Проверка конфигурации

Убедитесь, что в `TN_Doc/Cfg/CfgApp.json` есть устройство с настройками ELIS:

```json
{
  "Devices": [
    {
      "Id": 1,
      "Name": "Тестовое устройство",
      "IsElisUsed": true
    }
  ],
  "Elis": {
    "Url": "http://localhost:5050",
    "Timeout": 30
  }
}
```

---

## Тест 1: Проверка загрузки Vue редактора

**Цель**: Убедиться, что Vue редактор паспортов загружается корректно

### Шаги:

1. Открыть главное окно TN_Doc: `http://localhost:38509`
2. В левой панели выбрать устройство с ELIS (IsElisUsed: true)
3. В верхнем меню выбрать "Паспорт качества"
4. Нажать кнопку "Создать новый" или "Редактировать" существующий

### Ожидаемый результат:

✅ **Успех**:
- Открывается iframe с Vue редактором
- Отображается форма с полями AdditionalInfo
- Отображается таблица качественных показателей (Edit)
- Нет ошибок в консоли браузера

❌ **Провал**:
- Редактор не загружается
- Отображается старый HTML редактор
- Ошибки в консоли браузера

### Отладка (если провал):

```javascript
// В DevTools Console главного окна:
const iframe = document.querySelector('.FR');
console.log('iframe:', iframe);
console.log('Vue app:', iframe?.contentWindow?.document.querySelector('#app'));
```

---

## Тест 2: Проверка получения данных из ELIS

**Цель**: Убедиться, что данные ELIS запрашиваются и сохраняются в localStorage

### Шаги:

1. В главном окне TN_Doc нажать кнопку **"ЕЛИС"**
2. В появившемся окне должен отобразиться список протоколов испытаний
3. Выбрать любой протокол из списка
4. Нажать кнопку **"Применить"** (или аналогичную)

### Ожидаемый результат:

✅ **Успех**:
- Отображается список протоколов из ELIS
- После выбора протокола появляется сообщение об успешном применении

❌ **Провал**:
- Список протоколов пустой
- Ошибка при запросе данных
- Кнопка "Применить" недоступна

### Проверка localStorage (в DevTools главного окна):

```javascript
// В DevTools Console главного окна:
// 1. Проверить, что данные сохранены в localStorage
const dataPassport = JSON.parse(localStorage.getItem('dataPassport'));
console.log('dataPassport:', dataPassport);

const labInfo = JSON.parse(localStorage.getItem('labInfo'));
console.log('labInfo:', labInfo);

// 2. Проверить структуру данных
console.log('parameters keys:', Object.keys(dataPassport.parameters || {}));
console.log('labInfo keys:', Object.keys(labInfo || {}));
console.log('signers:', dataPassport.signers);
```

### Пример корректных данных:

```json
{
  "labInfo": {
    "labName": "ООО 'Тестовая лаборатория'",
    "accreditationNumber": "RA.RU.21АБ01",
    "labAddress": "г. Москва, ул. Тестовая, 1"
  },
  "parameters": {
    "Массовая доля воды(%)": {
      "value": 0.3,
      "valueString": "Менее 0,5",
      "testMethodName": "ГОСТ 2477-2014"
    }
  },
  "signers": {
    "laboratory": {
      "givenName": "Иван",
      "middleName": "Петрович",
      "familyName": "Сидоров",
      "post": "Начальник лаборатории",
      "company": "ООО 'Тестовая лаборатория'"
    }
  },
  "startPeriodTime": "2025-11-06T14:30:00Z",
  "endPeriodTime": "2025-11-06T16:30:00Z",
  "protocolNumber": "ПИ-12345"
}
```

---

## Тест 3: Проверка отправки postMessage в Vue редактор

**Цель**: Убедиться, что Common.js обнаруживает Vue редактор и отправляет данные через postMessage

### Шаги:

1. Открыть DevTools в **главном окне** (не в iframe!)
2. Открыть вкладку **Console**
3. В главном окне нажать кнопку **"ЕЛИС"** → выбрать протокол → **"Применить"**

### Ожидаемый результат:

✅ **Успех** (в консоли главного окна):

```
Начало заполнения данных паспорта из ЕЛИС
ПИ ЕЛИС:
{...} (данные протокола)
Информация о лаборатории:
{...}
Обнаружен Vue редактор, отправка данных ЕЛИС через postMessage
Данные ЕЛИС отправлены в Vue редактор
```

❌ **Провал** (в консоли главного окна):

```
Форма паспорта не настроена для заполнения данными с ЕЛИС
```

Это означает, что Common.js НЕ обнаружил Vue редактор и пытается использовать legacy механизм.

### Отладка (если провал):

```javascript
// В DevTools Console главного окна:
const iframe = document.querySelector('.FR');
const isVueEditor = iframe?.contentWindow?.document.querySelector('#app');
console.log('isVueEditor:', isVueEditor);

// Если isVueEditor === null, значит Vue редактор не загрузился
// Проверить URL iframe:
console.log('iframe.src:', iframe.src);
```

---

## Тест 4: Проверка получения postMessage в Vue редакторе

**Цель**: Убедиться, что Vue композабл получает данные из главного окна

### Шаги:

1. Открыть DevTools, переключиться на **iframe с Vue редактором**:
   - В DevTools нажать на выпадающий список вверху (обычно там написано "top")
   - Выбрать iframe с редактором (может называться `localhost:38509/editor/...`)
2. Открыть вкладку **Console**
3. В главном окне нажать кнопку **"ЕЛИС"** → выбрать протокол → **"Применить"**

### Ожидаемый результат:

✅ **Успех** (в консоли iframe):

```
[useElisIntegration] Слушатель postMessage зарегистрирован
[useElisIntegration] Получены данные ELIS из главного окна: {...}
[ELIS] Данные обогащены автоматически сформированными полями: {
  chiefLabShortSign: "И. П. Сидоров",
  chiefLabPosition: "Начальник лаборатории",
  chiefLabOrganization: "ООО 'Тестовая лаборатория'"
}
[DocumentPassportEditor] Получены данные ELIS, начинаем заполнение формы
```

❌ **Провал**:
- Нет сообщений `[useElisIntegration]` в консоли
- Нет сообщений `[ELIS]` в консоли

### Отладка (если провал):

```javascript
// В DevTools Console iframe:
// 1. Проверить, что слушатель зарегистрирован
window.addEventListener('message', (e) => {
  console.log('Получено postMessage:', e.data);
});

// 2. Повторить "Применить" ELIS в главном окне
```

---

## Тест 5: Проверка заполнения полей AdditionalInfo

**Цель**: Убедиться, что поля AdditionalInfo заполняются из данных ELIS

### Шаги:

1. Применить данные ELIS (Тест 3)
2. Визуально проверить форму редактора
3. Открыть DevTools Console iframe

### Ожидаемый результат:

✅ **Успех**:

**Визуально**:
- Поля с ELIS данными заполнены значениями
- Заполненные поля имеют **зелёный фон** (`background-color: #d4edda`)
- Пустые поля (без ElisAlias) остались белыми

**В консоли iframe**:

```
[ELIS] Поле "Laboratory" заполнено значением: "ООО 'Тестовая лаборатория'"
[ELIS] Поле "Laboratory_IOF" заполнено значением: "И. П. Сидоров"
[ELIS] Поле "Laboratory_Post" заполнено значением: "Начальник лаборатории"
[ELIS] Поле "PassportPeriodDT.Begin" заполнено значением: "2025-11-06T14:30:00Z"
...
[ELIS] Применено 11 обновлений полей из данных ELIS
```

❌ **Провал**:
- Поля остались пустыми
- Нет зелёной подсветки
- В консоли: `[ELIS] Не найдено ни одного поля для заполнения из данных ELIS`

### Проверка маппинга (в консоли iframe):

```javascript
// 1. Проверить конфигурацию полей с ElisAlias
const store = window.__VUE_DEVTOOLS_GLOBAL_HOOK__?.apps?.[0]?.config?.globalProperties?.$pinia?._s?.get('document');

if (store) {
  const fieldsWithElis = store.fields.filter(f => f.elisAlias);
  console.table(fieldsWithElis.map(f => ({
    Key: f.key,
    Label: f.label,
    ElisAlias: f.elisAlias.join(', '),
    Value: store.formData[f.key],
    Filled: store.formData[`${f.key}__elisFilled`]
  })));
}

// 2. Проверить данные ELIS
const elisData = JSON.parse(localStorage.getItem('dataPassport'));
console.log('ELIS labInfo:', elisData?.labInfo);
console.log('ELIS корень:', Object.keys(elisData || {}).filter(k => !['parameters', 'labInfo', 'signers'].includes(k)));
```

---

## Тест 6: Проверка заполнения параметров качества (Parameters)

**Цель**: Убедиться, что таблица качественных показателей заполняется из данных ELIS

### Шаги:

1. Применить данные ELIS (Тест 3)
2. Прокрутить форму вниз до таблицы "Качественные показатели"
3. Проверить столбцы: **Измерение (ХАЛ)**, **Метод испытаний**, **Результат**

### Ожидаемый результат:

✅ **Успех**:

**Визуально**:
- Столбец **"Измерение (ХАЛ)"**: заполнены числовые значения (например, `0.3`), **зелёный фон**
- Столбец **"Метод испытаний"**: выбран метод из списка (например, `ГОСТ 2477-2014`), **зелёный фон**
- Столбец **"Результат"**: заполнены текстовые значения (например, `Менее 0,5`), **зелёный фон**

**В консоли iframe**:

```
[ELIS] Параметр "MassWaterFracCorrection" measurement заполнен: 0.3
[ELIS] Параметр "MassWaterFracCorrection" result заполнен: Менее 0,5
[ELIS] Параметр "MassWaterFracCorrection" method найден: ГОСТ 2477-2014
[ELIS] Параметр "Chloride_Salts.MassFraction" measurement заполнен: 15
...
```

❌ **Провал**:
- Поля таблицы остались пустыми
- Нет зелёной подсветки в таблице
- В консоли: предупреждения `[ELIS] Не найдено значение для алиасов: [...]`

### Проверка маппинга параметров (в консоли iframe):

```javascript
// Проверить конфигурацию параметров с ElisAlias
const store = window.__VUE_DEVTOOLS_GLOBAL_HOOK__?.apps?.[0]?.config?.globalProperties?.$pinia?._s?.get('document');

if (store && store.config?.docType === 'Passport') {
  const paramsWithElis = store.config.qualityParametersSchema?.filter(p => p.elisAlias) || [];

  console.table(paramsWithElis.map(p => ({
    Key: p.key,
    Name: p.name,
    ElisAlias: p.elisAlias.join(', '),
    Measurement: store.formData[`value.${p.key}`],
    Result: store.formData[`result.${p.key}`],
    Method: store.formData[`method.${p.key}`]?.name
  })));
}

// Проверить данные ELIS parameters
const elisData = JSON.parse(localStorage.getItem('dataPassport'));
console.log('ELIS parameters keys:', Object.keys(elisData?.parameters || {}));
```

---

## Тест 7: Проверка fallback механизма (множественные алиасы)

**Цель**: Убедиться, что механизм fallback работает (перебирает массив алиасов)

### Пример поля с множественными алиасами:

Согласно конфигурации, поле `Chloride_Salts.MassFraction` имеет fallback:

```json
{
  "Key": "Chloride_Salts.MassFraction",
  "ElisAlias": [
    "Массовая концентрация хлористых солей(%)",
    "Массовая доля хлористых солей(%)"
  ]
}
```

### Ожидаемый результат (в консоли iframe):

```
[ELIS] Найдено значение по алиасу "Массовая доля хлористых солей(%)" в "parameters": {...}
```

Или:

```
[ELIS] Найдено значение по алиасу "Массовая концентрация хлористых солей(%)" в "parameters": {...}
```

✅ **Успех**: Используется **первый найденный** алиас из массива

❌ **Провал**: Логируется предупреждение:

```
[ELIS] Не найдено значение для алиасов: [Массовая концентрация хлористых солей(%), Массовая доля хлористых солей(%)] в "parameters"
```

### Отладка:

```javascript
// В консоли iframe:
const elisData = JSON.parse(localStorage.getItem('dataPassport'));

// Проверить точное написание ключей в ELIS данных
console.log('Parameters keys:', Object.keys(elisData.parameters || {}));

// Сравнить с ElisAlias в конфигурации
const param = store.config.qualityParametersSchema.find(p => p.key === 'Chloride_Salts.MassFraction');
console.log('ElisAlias:', param?.elisAlias);
```

---

## Тест 8: Проверка ручного редактирования и сброса подсветки

**Цель**: Убедиться, что при ручном редактировании поля зелёная подсветка исчезает

### Шаги:

1. Применить данные ELIS (Тест 3)
2. Найти любое поле с зелёной подсветкой (например, "Лаборатория предприятия")
3. **Изменить значение** поля вручную (ввести текст)
4. Нажать Enter или переключиться на другое поле (blur event)

### Ожидаемый результат:

✅ **Успех**:
- После изменения значения **зелёный фон исчезает**
- Поле становится белым (стандартный фон)

❌ **Провал**:
- Зелёный фон остаётся после редактирования

### Проверка флага (в консоли iframe):

```javascript
// Проверить, что флаг __elisFilled сброшен
const store = window.__VUE_DEVTOOLS_GLOBAL_HOOK__?.apps?.[0]?.config?.globalProperties?.$pinia?._s?.get('document');

// Для поля "Laboratory"
console.log('Laboratory__elisFilled:', store.formData['Laboratory__elisFilled']);
// Должно быть: false или undefined после редактирования
```

---

## Тест 9: Проверка сохранения документа

**Цель**: Убедиться, что документ сохраняется корректно и флаги `__elisFilled` не попадают в базу данных

### Шаги:

1. Применить данные ELIS (Тест 3)
2. Проверить, что поля заполнены (Тест 5, 6)
3. В главном окне нажать кнопку **"Сохранить"**
4. Дождаться сообщения об успешном сохранении

### Ожидаемый результат:

✅ **Успех**:
- Документ сохраняется без ошибок
- Появляется сообщение "Документ успешно сохранён" (или аналогичное)
- Нет ошибок в консоли

❌ **Провал**:
- Ошибка при сохранении
- Сообщение "Заполните все обязательные поля"

### Проверка отправляемых данных (в консоли iframe):

```javascript
// В консоли iframe перед сохранением:
const store = window.__VUE_DEVTOOLS_GLOBAL_HOOK__?.apps?.[0]?.config?.globalProperties?.$pinia?._s?.get('document');

// Проверить, что флаги __elisFilled не попадут в сохранение
const saveData = { ...store.formData };
const elisFlags = Object.keys(saveData).filter(k => k.includes('__elisFilled'));
console.log('ELIS flags (не должны сохраняться):', elisFlags);
```

**Примечание**: Флаги `__elisFilled` используются только для подсветки в UI и **НЕ** должны сохраняться в базу данных.

---

## Тест 10: Проверка обратной совместимости с legacy HTML редактором

**Цель**: Убедиться, что старый HTML редактор продолжает работать

### Шаги:

1. Временно **отключить Vue редактор**:
   - Переименовать файл `TN_Doc/wwwroot/document-editor/index.html` → `index.html.bak`
2. Перезапустить TN_Doc
3. Открыть редактор паспорта
4. Применить данные ELIS

### Ожидаемый результат:

✅ **Успех**:
- Открывается **старый HTML редактор** (не Vue)
- Данные ELIS применяются через legacy механизм (`.elis-data` элементы)
- Форма заполняется корректно

❌ **Провал**:
- Ошибка при применении ELIS
- Форма не заполняется

### Восстановление Vue редактора:

```bash
mv TN_Doc/wwwroot/document-editor/index.html.bak TN_Doc/wwwroot/document-editor/index.html
```

---

## Критические ошибки и как их распознать

### ❌ Ошибка 1: "CORS error" в консоли

**Симптом**:
```
Access to XMLHttpRequest at 'http://localhost:5050' from origin 'http://localhost:38509' has been blocked by CORS policy
```

**Решение**:
- Проверить, что TN.ElisConnector запущен
- Проверить настройки CORS в TN.ElisConnector

---

### ❌ Ошибка 2: "Cannot read property 'qualityParametersSchema' of undefined"

**Симптом**: Ошибка TypeScript в консоли

**Решение**:
- Пересобрать Vue компонент: `cd TN_Doc/Client/document-editor && npm run build`
- Перезапустить TN_Doc

---

### ❌ Ошибка 3: Поля не заполняются, но данные ELIS есть

**Симптом**: В консоли iframe:
```
[ELIS] Не найдено значение для алиасов: [labName] в "labInfo"
```

**Причина**: Несоответствие ключей в конфигурации и данных ELIS

**Отладка**:

```javascript
// В консоли iframe:
const elisData = JSON.parse(localStorage.getItem('dataPassport'));
const store = window.__VUE_DEVTOOLS_GLOBAL_HOOK__?.apps?.[0]?.config?.globalProperties?.$pinia?._s?.get('document');

// Сравнить ключи
console.log('ElisAlias в конфигурации:', store.fields.find(f => f.key === 'Laboratory')?.elisAlias);
console.log('Доступные ключи в labInfo:', Object.keys(elisData?.labInfo || {}));
```

---

## Чек-лист для быстрого тестирования

- [ ] **Тест 1**: Vue редактор загружается
- [ ] **Тест 2**: Данные ELIS запрашиваются и сохраняются в localStorage
- [ ] **Тест 3**: postMessage отправляется из главного окна
- [ ] **Тест 4**: postMessage получается в Vue редакторе
- [ ] **Тест 5**: Поля AdditionalInfo заполняются с зелёной подсветкой
- [ ] **Тест 6**: Параметры качества заполняются с зелёной подсветкой
- [ ] **Тест 7**: Fallback механизм работает (множественные алиасы)
- [ ] **Тест 8**: Ручное редактирование сбрасывает подсветку
- [ ] **Тест 9**: Документ сохраняется корректно
- [ ] **Тест 10**: Legacy HTML редактор работает (обратная совместимость)

---

## Полезные команды для отладки

### Проверка версии сборки Vue компонента

```bash
cd TN_Doc/wwwroot/document-editor
ls -lah assets/index-*.js
# Должен быть файл index-BA_NcSUL.js (из последней сборки)
```

### Очистка кэша браузера

1. В Chrome: `Ctrl + Shift + Delete` → очистить кэш
2. Или: `Ctrl + F5` для жёсткой перезагрузки страницы

### Просмотр логов TN_Doc

```bash
# Linux
tail -f /opt/TN_Doc/logs/tn_doc_*.log

# Windows
# Открыть TN_Doc/logs/ в проводнике
```

### Просмотр логов TN.ElisConnector

```bash
# В терминале, где запущен TN.ElisConnector
# Логи выводятся в консоль
```

---

## Отчёт о тестировании (заполнить после тестирования)

### Дата тестирования: __________

### Результаты:

| Тест | Статус | Комментарий |
|------|--------|-------------|
| Тест 1: Загрузка Vue редактора | ⬜ Pass / ⬜ Fail | |
| Тест 2: Получение данных ELIS | ⬜ Pass / ⬜ Fail | |
| Тест 3: Отправка postMessage | ⬜ Pass / ⬜ Fail | |
| Тест 4: Получение postMessage | ⬜ Pass / ⬜ Fail | |
| Тест 5: Заполнение AdditionalInfo | ⬜ Pass / ⬜ Fail | |
| Тест 6: Заполнение Parameters | ⬜ Pass / ⬜ Fail | |
| Тест 7: Fallback механизм | ⬜ Pass / ⬜ Fail | |
| Тест 8: Сброс подсветки | ⬜ Pass / ⬜ Fail | |
| Тест 9: Сохранение документа | ⬜ Pass / ⬜ Fail | |
| Тест 10: Обратная совместимость | ⬜ Pass / ⬜ Fail | |

### Найденные проблемы:

1. **Проблема**: _________________________________
   - **Шаги для воспроизведения**: _________________________________
   - **Ожидаемый результат**: _________________________________
   - **Фактический результат**: _________________________________
   - **Скриншот/Логи**: _________________________________

2. **Проблема**: _________________________________
   - ...

### Общий вывод:

⬜ Готово к production
⬜ Требуются доработки
⬜ Критические ошибки

---

**Примечание**: После завершения тестирования отправьте заполненный отчёт разработчику для анализа.
