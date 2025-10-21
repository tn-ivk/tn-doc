# Трекер прогресса массовой миграции

**Дата начала:** 2025-10-21
**Текущая фаза:** Stage 4.1 - Простые Poverka документы

---

## ✅ Завершено

### Подготовка
- [x] Анализ существующих 43 библиотек документов
- [x] Создание детального плана миграции (MASS_MIGRATION_PLAN.md)
- [x] Группировка документов по сложности
- [x] Изучение паттерна IDocumentEditor из DocReport/DocAct/DocJornal

---

## ⏳ В процессе

### Stage 4.1: Простые Poverka (9 документов)

#### Инфраструктура
- [ ] Создать базовый класс `BasePoverkaEditor` в `TN.DocGeneral/Editors/`
  - Методы: `BuildFieldsFromAdditionalInfo()`, `ExtractAdditionalInfo()`, `SaveAdditionalInfo()`
  - Обработка "text" и "list" типов полей
  - Поддержка Edit=true/false

#### Pilot: Poverka2816
- [ ] Обновить `Poverka2816.cs` - добавить реализацию IDocumentEditor
  - [ ] `GetEditConfig(int id)`
  - [ ] `SaveDocument(int id, Dictionary<string, object> values)`
- [ ] Тестирование Poverka2816 через Vue Editor
- [ ] Проверка сохранения в БД

#### Остальные простые Poverka (8 документов)
- [ ] Poverka3151
- [ ] Poverka3189
- [ ] Poverka3266
- [ ] Poverka3267
- [ ] Poverka3272
- [ ] Poverka3287
- [ ] Poverka3288
- [ ] Poverka3380

**Прогресс:** 0/9 (0%)
**Оценка:** 5-7 дней
**Статус:** Начато

---

## 📋 Ожидают выполнения

### Stage 4.2: Poverka 1974 серия (4 документа) - 10-14 дней
- [ ] CommonPoverka1974 - добавить поддержку IDocumentEditor
- [ ] Poverka1974
- [ ] Poverka1974_04
- [ ] Poverka1974_89
- [ ] Poverka1974_95
- [ ] Vue компонент для таблицы измерений с вязкостью
- [ ] Логика корректировки вязкости

### Stage 4.3: Poverka 3265/3312 серия (5 документов) - 7-10 дней
- [ ] Базовый класс `Poverka3265Base`
- [ ] Базовый класс `Poverka3312Base`
- [ ] Poverka3265_PR_PU, Poverka3265_UPR_PR, Poverka3265_UPR_PU
- [ ] Poverka3312_PR_PU, Poverka3312_UPR_PR

### Stage 4.4: PoverkaSikn425 (2 документа) - 5-7 дней
- [ ] CommonSikn425 - добавить поддержку IDocumentEditor
- [ ] PoverkaSikn425_PR_PR
- [ ] PoverkaSikn425_PR_PU

### Stage 5.1: Простые KMH (7 документов) - 7-10 дней
- [ ] Базовый класс `SimpleKMHEditor`
- [ ] KMH_PV, KMH_PW
- [ ] KMH_PP, KMH_PP_Areom
- [ ] KMH_PR_PR, KMH_PR_PU
- [ ] KMH_MI2816

### Stage 5.2: KMH MPR серия (3 документа) - 5-7 дней
- [ ] Базовый класс `KMHMPRBase`
- [ ] KMH_MPR_MPR, KMH_MPR_PU, KMH_MPR_TPR

### Stage 5.3: KMH 3265/3288/3312 серия (5 документов) - 5-7 дней
- [ ] KMH3265_PR_PU, KMH3265_UPR_PR
- [ ] KMH3288_MPR_TPR
- [ ] KMH3312_PR_PU, KMH3312_UPR_PR

### Stage 5.4: KMX Sikn425 (2 документа) - 5-7 дней
- [ ] KMX_Sikn425_PR_PR
- [ ] KMX_Sikn425_PR_PU

### Stage 6: Финализация (2 недели)
- [ ] ActProducer, ActRoute (3-5 дней)
- [ ] Common библиотеки (2-3 дня)
- [ ] Финальное тестирование всех документов (5-7 дней)
- [ ] MIGRATION_COMPLETE.md и обновление документации (2-3 дня)

---

## 📊 Общий прогресс

**Документов мигрировано:** 0/39 (0%)
**Фаз завершено:** 0/10
**Оценка завершения:** 12-17.5 недель от старта
**Текущая дата:** 2025-10-21

---

## 🔄 История изменений

| Дата | Фаза | Действие | Документы |
|------|------|----------|-----------|
| 2025-10-21 | Подготовка | Создан план миграции | - |
| 2025-10-21 | Stage 4.1 | Начата работа | - |

---

## 📝 Примечания

### Технические решения:
- Базовые классы для переиспользования кода
- Унифицированный паттерн IDocumentEditor
- Пошаговое тестирование каждой группы

### Риски:
- Сложность некоторых документов может быть недооценена
- ELIS интеграция может потребовать дополнительного времени
- Специфические бизнес-правила могут усложнить реализацию

### Следующий шаг:
Создать `BasePoverkaEditor` и реализовать pilot - Poverka2816
