# Синхронизация методов LinkedParameters при заполнении из ELIS

**Статус:** Отложено (требует дополнительной проверки)
**Дата:** 2025-12-11

## Проблема

При заполнении данных из ELIS для групповых параметров (LinkedParameters) каждый параметр получает свой собственный метод из ELIS. Это приводит к рассинхронизации методов в группе:

- **Leader** получает метод "ГОСТ 21534-76"
- **Follower** получает метод "ГОСТ 12345-99" (свой собственный из ELIS)

При этом в UI отображается только метод leader'а (rowspan), но в БД записываются разные значения.

## Ожидаемое поведение

При заполнении из ELIS методы в группе LinkedParameters должны быть синхронизированы:
- Метод leader'а копируется в follower
- Оба параметра получают одинаковый метод

## Попытка реализации

### Изменения в `DocumentPassportEditor.vue`

```typescript
// 1. Добавлена проверка isLinkedFollower для пропуска заполнения метода follower'а
if (elisParam.testMethodName && !param.isLinkedFollower) {
  // ... заполнение метода
}

// 2. Добавлена синхронизация метода от leader'а в follower
if (param.linkedParameter) {
  const linkedMethodKey = `method.${param.linkedParameter}`;
  updates[linkedMethodKey] = methodJson;
  updates[`${linkedMethodKey}__elisFilled`] = true;
  updates[`${linkedMethodKey}__elisOriginal`] = matchingMethod.name;
  updates[`${linkedMethodKey}__elisOption`] = matchingMethod;
  trackElisLoad(linkedMethodKey, matchingMethod.name, elisData.protocolNumber);

  logger.debug('[handleElisData] Синхронизация метода ELIS в связанный параметр', {
    leaderKey: param.key,
    followerKey: param.linkedParameter,
    methodName: matchingMethod.name
  });
}

// 3. Изменена проверка elisMissing для follower'ов
} else if (!param.isLinkedFollower) {
  // elisMissing только для независимых и leader'ов
  updates[`${methodKey}__elisMissing`] = true;
  trackElisMissing(methodKey, elisData.protocolNumber);
}
```

### Полный diff

```diff
--- a/TN_Doc/Client/document-editor/src/views/DocumentPassportEditor.vue
+++ b/TN_Doc/Client/document-editor/src/views/DocumentPassportEditor.vue
@@ -569,7 +569,9 @@ const handleElisData = (elisData: ElisPassportData) => {
         }

         // Создать метод испытаний из ELIS данных
-        if (elisParam.testMethodName) {
+        // Для follower'ов в группе LinkedParameters метод НЕ заполняется из ELIS,
+        // он будет синхронизирован от leader'а
+        if (elisParam.testMethodName && !param.isLinkedFollower) {
           const elisMethod = createMethodFromElisData(elisParam);
           if (elisMethod) {

@@ -606,13 +608,31 @@ const handleElisData = (elisData: ElisPassportData) => {

             // Создать запись истории для ELIS (сохраняем только name, а не весь объект)
             trackElisLoad(methodKey, matchingMethod.name, elisData.protocolNumber);
+
+            // Синхронизация метода в связанный параметр (LinkedParameters)
+            // Если текущий параметр является лидером группы - копируем метод в follower
+            if (param.linkedParameter) {
+              const linkedMethodKey = `method.${param.linkedParameter}`;
+              updates[linkedMethodKey] = methodJson;
+              updates[`${linkedMethodKey}__elisFilled`] = true;
+              updates[`${linkedMethodKey}__elisOriginal`] = matchingMethod.name;
+              updates[`${linkedMethodKey}__elisOption`] = matchingMethod;
+              trackElisLoad(linkedMethodKey, matchingMethod.name, elisData.protocolNumber);
+
+              logger.debug('[handleElisData] Синхронизация метода ELIS в связанный параметр', {
+                leaderKey: param.key,
+                followerKey: param.linkedParameter,
+                methodName: matchingMethod.name
+              });
+            }
           } else {
             // Метод не удалось создать
             updates[`${methodKey}__elisMissing`] = true;
             trackElisMissing(methodKey, elisData.protocolNumber);
           }
-        } else {
-          // method ожидалось, но не пришло
+        } else if (!param.isLinkedFollower) {
+          // method ожидалось, но не пришло (только для независимых параметров и leader'ов)
+          // Для follower'ов метод синхронизируется от leader'а
           updates[`${methodKey}__elisMissing`] = true;
           trackElisMissing(methodKey, elisData.protocolNumber);
         }
```

## Причина отката

Требуется дополнительная проверка корректности работы:
1. Убедиться, что порядок обработки параметров гарантирует обработку leader'а до follower'а
2. Проверить работу с разными конфигурациями LinkedParameters
3. Уточнить бизнес-требования: нужно ли использовать метод leader'а или данные из ELIS для каждого параметра независимо

## Связанные файлы

- `TN_Doc/Client/document-editor/src/views/DocumentPassportEditor.vue` — функция `handleElisData`
- `TN_Doc/Client/document-editor/src/composables/usePassportEditor.ts` — функция `handleMethodUpdate` (ручная синхронизация)
- `docs/configs/passport.md` — документация LinkedParameters

## Альтернативные подходы

1. **Синхронизация на бэкенде** — при сохранении документа проверять и синхронизировать методы
2. **Двухфазная обработка** — сначала заполнить все параметры из ELIS, затем синхронизировать методы в группах
3. **Приоритет ELIS** — не синхронизировать, каждый параметр получает свой метод из ELIS независимо
