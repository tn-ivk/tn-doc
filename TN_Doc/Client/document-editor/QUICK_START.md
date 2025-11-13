# Быстрый старт Document Editor

## 🚀 Запуск

### 1. Установите зависимости
```bash
cd /mnt/c/dev/dotnet/ivk/tn_doc/TN_Doc/Client
npm install
```

### 2. Запустите backend
Терминал 1:
```bash
cd /mnt/c/dev/dotnet/ivk/tn_doc/TN_Doc
ASPNETCORE_ENVIRONMENT=Development dotnet run
```
Backend: `http://localhost:38509`

### 3. Запустите frontend
Терминал 2:
```bash
cd /mnt/c/dev/dotnet/ivk/tn_doc/TN_Doc/Client
npm run dev:editor
```
Frontend: `http://localhost:5174`

## 🧪 Тесты

### API Health Check
```bash
curl http://localhost:38509/api/documents/health
```

### Получить конфигурацию Report
```bash
# Замените 1 на ваш deviceId, 123 на реальный ID документа
curl "http://localhost:38509/api/documents/1/Report/edit/123"
```

### Открыть редактор в браузере
```
http://localhost:5174/edit/1/Report/123
```
Замените `1` на ваш deviceId, `123` на ID документа

## ✅ Что проверить

1. ✅ Редактор загружается
2. ✅ Отображаются поля с выпадающими списками пользователей
3. ✅ При изменении появляется badge "Несохранённые изменения"
4. ✅ Кнопка "Сохранить" работает
5. ✅ Данные сохраняются в БД

## 📦 Production Build
```bash
cd /mnt/c/dev/dotnet/ivk/tn_doc/TN_Doc/Client
npm run build:editor
```

Файлы будут в `TN_Doc/wwwroot/document-editor/`

Откройте: `http://localhost:38509/document-editor/edit/1/Report/123`

## ❌ Проблемы

**"Device not found"** → Проверьте deviceId в CfgApp.json

**"Does not support Vue editor"** → Пересоберите: `dotnet build`

**Frontend не загружается** → Проверьте порт 5174

**Пустые списки** → Проверьте таблицу Users в БД
