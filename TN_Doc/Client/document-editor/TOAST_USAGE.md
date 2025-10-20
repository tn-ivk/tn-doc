# Toast уведомления в Document Editor

## Обзор

В Document Editor реализованы Toast уведомления от PrimeVue вместо стандартных `alert()` и `confirm()`. Это обеспечивает более современный и удобный пользовательский интерфейс.

## Компоненты

### 1. ToastService и ConfirmationService

Зарегистрированы в `main.ts`:

```typescript
import ToastService from 'primevue/toastservice';
import ConfirmationService from 'primevue/confirmationservice';

app.use(ToastService);
app.use(ConfirmationService);
```

### 2. Глобальные компоненты

Добавлены в `App.vue`:

```vue
<template>
  <div id="app">
    <Toast />
    <ConfirmDialog />
    <RouterView />
  </div>
</template>
```

## Использование в компонентах

### Базовое использование Toast

```typescript
import { useToast } from 'primevue/usetoast';

const toast = useToast();

// Успешное уведомление
toast.add({
  severity: 'success',
  summary: 'Успешно',
  detail: 'Операция выполнена успешно',
  life: 3000  // Автоматически закрывается через 3 секунды
});

// Уведомление об ошибке
toast.add({
  severity: 'error',
  summary: 'Ошибка',
  detail: 'Не удалось выполнить операцию',
  life: 5000  // Ошибки показываются дольше
});

// Информационное уведомление
toast.add({
  severity: 'info',
  summary: 'Информация',
  detail: 'Полезная информация для пользователя',
  life: 2000
});

// Предупреждение
toast.add({
  severity: 'warn',
  summary: 'Предупреждение',
  detail: 'Возможна потеря данных',
  life: 4000
});
```

### Использование ConfirmDialog

```typescript
import { useConfirm } from 'primevue/useconfirm';

const confirm = useConfirm();

confirm.require({
  message: 'У вас есть несохранённые изменения. Отменить изменения?',
  header: 'Подтверждение',
  icon: 'pi pi-exclamation-triangle',
  rejectLabel: 'Нет',
  acceptLabel: 'Да',
  accept: () => {
    // Действие при подтверждении
    toast.add({
      severity: 'info',
      summary: 'Отменено',
      detail: 'Изменения отменены',
      life: 2000
    });
  },
  reject: () => {
    // Действие при отказе (опционально)
  }
});
```

## Примеры из Document Editor

### 1. Успешное сохранение документа

```typescript
async function handleSave() {
  try {
    await store.saveDocument();
    toast.add({
      severity: 'success',
      summary: 'Успешно',
      detail: 'Документ успешно сохранён',
      life: 3000
    });
  } catch (error: any) {
    toast.add({
      severity: 'error',
      summary: 'Ошибка',
      detail: error.message || 'Не удалось сохранить документ',
      life: 5000
    });
  }
}
```

### 2. Подтверждение отмены изменений

```typescript
function handleCancel() {
  if (store.hasUnsavedChanges) {
    confirm.require({
      message: 'У вас есть несохранённые изменения. Отменить изменения?',
      header: 'Подтверждение',
      icon: 'pi pi-exclamation-triangle',
      rejectLabel: 'Нет',
      acceptLabel: 'Да',
      accept: () => {
        // Перезагрузить данные
        store.loadConfig(deviceId, docType, id);
        toast.add({
          severity: 'info',
          summary: 'Отменено',
          detail: 'Изменения отменены',
          life: 2000
        });
      }
    });
  }
}
```

### 3. Уведомление о загрузке

```typescript
try {
  await store.loadConfig(deviceId, docType, id);
  toast.add({
    severity: 'success',
    summary: 'Успешно',
    detail: 'Документ загружен',
    life: 2000
  });
} catch (error: any) {
  toast.add({
    severity: 'error',
    summary: 'Ошибка загрузки',
    detail: error.message || 'Не удалось загрузить документ',
    life: 5000
  });
}
```

## Типы severity

- `success` - Зелёный, для успешных операций
- `error` - Красный, для ошибок
- `warn` - Оранжевый, для предупреждений
- `info` - Синий, для информационных сообщений

## Рекомендации

1. **Время показа (`life`)**:
   - Успешные операции: 2000-3000 мс
   - Ошибки: 5000 мс
   - Информация: 2000 мс

2. **Текст сообщений**:
   - `summary` - краткий заголовок (1-3 слова)
   - `detail` - подробное описание (1-2 предложения)

3. **Не перегружайте уведомлениями**:
   - Показывайте только важные события
   - Не дублируйте информацию из UI

4. **Используйте ConfirmDialog для критичных действий**:
   - Удаление данных
   - Отмена изменений
   - Выход без сохранения

## Ссылки

- [PrimeVue Toast Documentation](https://primevue.org/toast/)
- [PrimeVue ConfirmDialog Documentation](https://primevue.org/confirmdialog/)
