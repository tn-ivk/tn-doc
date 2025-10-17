# 🚀 Quick Start - Document Editor PoC

## Быстрый запуск (3 минуты)

### 1. Установка
```bash
cd /home/snafu/projects/ivk/tn_doc/TN_Doc/Client/document-editor
npm install
```

### 2. Запуск backend (новый терминал)
```bash
cd /home/snafu/projects/ivk/tn_doc/TN_Doc
dotnet run
```
✅ Backend: http://localhost:38509

### 3. Запуск frontend dev сервера
```bash
cd /home/snafu/projects/ivk/tn_doc/TN_Doc/Client/document-editor
npm run dev
```
✅ Frontend: http://localhost:5173

### 4. Тестирование

#### API Health Check
```bash
curl http://localhost:38509/api/documents/health
```

#### Открыть редактор (замените deviceId и docId на реальные)
```
http://localhost:5173/document-editor/edit/{deviceId}/Jornal/{docId}
```

Пример:
```
http://localhost:5173/document-editor/edit/00000000-0000-0000-0000-000000000001/Jornal/123
```

### 5. Сборка для production
```bash
npm run build
```
Результат → `TN_Doc/wwwroot/document-editor/`

После сборки доступно на:
```
http://localhost:38509/document-editor/edit/{deviceId}/Jornal/{docId}
```

## 📦 Что создано

### Backend
- ✅ `TN_Doc/Controllers/DocumentEditController.cs` - REST API
- ✅ `tn.docgeneral/TN.DocGeneral/IDocumentEditor.cs` - интерфейс
- ✅ `tn.docgeneral/Jornal/DocJornal.cs` - реализация для Jornal

### Frontend
- ✅ Vue 3 + TypeScript SPA
- ✅ Vue Router для маршрутизации
- ✅ Pinia для состояния
- ✅ PrimeVue для UI
- ✅ Валидация полей
- ✅ API клиент (Axios)

## 🎯 API Endpoints

| Method | URL | Описание |
|--------|-----|----------|
| GET | `/api/documents/{deviceId}/{docType}/edit/{id}` | Получить конфигурацию |
| POST | `/api/documents/{deviceId}/{docType}/save/{id}` | Сохранить документ |
| GET | `/api/documents/health` | Health check |

## 📖 Документация

- Полное описание: `docs/DOCUMENT_EDITOR_POC.md`
- Детали проекта: `TN_Doc/Client/document-editor/README.md`

## ❓ Проблемы?

### Backend не запускается
```bash
dotnet restore
dotnet build
```

### Frontend ошибки
```bash
rm -rf node_modules package-lock.json
npm install
```

### API возвращает 404
Проверьте, что:
1. Backend запущен
2. DeviceId и DocId существуют в БД
3. DocType = "Jornal" (с заглавной буквы)

## 🎉 Успех!

Если вы видите форму с 4 полями (DeliveryIOF1/2, ReceiveIOF1/2) и кнопку "Сохранить" - **всё работает!**

Теперь можно:
1. Изменить значения в полях
2. Нажать "Сохранить"
3. Проверить в БД что данные обновились

## 📝 Следующие шаги

1. Добавить Toast уведомления
2. Реализовать автосохранение
3. Добавить поддержку Passport (сложный документ)
4. Мигрировать остальные типы документов

**Proof of Concept готов к демонстрации!** 🎊
