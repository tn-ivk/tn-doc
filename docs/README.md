# TN_Doc Documentation

Централизованная документация для проекта TN_Doc - системы генерации технических документов для измерительно-вычислительных комплексов.

## Быстрый старт

- **Начало работы**: [development/setup.md](development/setup.md)
- **Сборка проекта**: [development/building.md](development/building.md)
- **Архитектура системы**: [architecture/overview.md](architecture/overview.md)
- **Основные правила проекта**: [../CLAUDE.md](../CLAUDE.md)

## Содержание по категориям

### 🏗️ Architecture (Архитектура)

Документация по архитектуре и дизайну системы:

- **[overview.md](architecture/overview.md)** - Общий обзор архитектуры TN_Doc (актуально для текущего кода)
- **[document-modules.md](architecture/document-modules.md)** - Система модулей документов (актуально для текущего кода)
- **[configurator.md](architecture/configurator.md)** - Редактор справочников/конфигураций (актуально для текущего UI)
- **[document-editor.md](architecture/document-editor.md)** - Документация по механизму редактирования (HTML/JS)
- **[passport-editor.md](architecture/passport-editor.md)** - Редактирование паспорта качества (текущий механизм)

### 👨‍💻 Development (Разработка)

Руководства для разработчиков:

- **[setup.md](development/setup.md)** - Настройка окружения разработки
- **[building.md](development/building.md)** - Инструкции по сборке проекта
- **[testing.md](development/testing.md)** - Тестирование (Unit, Integration, E2E)
- **[ci-cd.md](development/ci-cd.md)** - CI/CD (GitHub Actions workflows)

### 🚀 Deployment (Развертывание)

Руководства по развертыванию и установке:

- **[linux.md](deployment/linux.md)** - Установка и настройка на Linux (systemd)

### 🔧 Operations (Эксплуатация)

Руководства по эксплуатации и обслуживанию:

- **[logging.md](operations/logging.md)** - ⭐ Управление логами (просмотр, копирование, архивирование)

### 🔌 Integration (Интеграция)

Документация по интеграции с внешними системами:

- **[elis.md](integration/elis.md)** - Интеграция с ELIS (Единая Лабораторная Информационная Система)
- **[elis-summary.md](elis-summary.md)** - Краткое описание интеграции с ELIS

### 🌐 API

Документация API:

- **[endpoints.md](api/endpoints.md)** - Описание HTTP API endpoints

### ⚙️ Configuration (Конфигурация)

Документация по файлам конфигурации:

- **[passport.md](configs/passport.md)** - Конфигурация модуля паспорта качества

### ✨ Features (Функциональность)

Документация по специфическим функциям:

- **[field-history.md](features/field-history.md)** - Система истории изменений полей паспорта качества (планируется, в коде отсутствует)

### 📝 Additional Documents

Дополнительные документы:

- **[ui-design.md](ui-design.md)** - Дизайн пользовательского интерфейса
- **[passport-labinfo-fix.md](passport-labinfo-fix.md)** - Исправление полей лабораторной информации в паспорте

## Поиск по документации

### По задачам

**Я хочу...**

- **Начать разработку** → [development/setup.md](development/setup.md)
- **Собрать проект** → [development/building.md](development/building.md)
- **Запустить тесты** → [development/testing.md](development/testing.md)
- **Развернуть на Linux** → [deployment/linux.md](deployment/linux.md)
- **Посмотреть логи** → [operations/logging.md](operations/logging.md)
- **Интегрировать с ELIS** → [integration/elis.md](integration/elis.md)
- **Понять архитектуру** → [architecture/overview.md](architecture/overview.md)
- **Написать новый модуль документа** → [architecture/document-modules.md](architecture/document-modules.md)
- **Настроить паспорт качества** → [configs/passport.md](configs/passport.md)

### По ролям

**Разработчик:**
- [development/setup.md](development/setup.md)
- [development/building.md](development/building.md)
- [development/testing.md](development/testing.md)
- [development/ci-cd.md](development/ci-cd.md)
- [architecture/overview.md](architecture/overview.md)
- [architecture/document-modules.md](architecture/document-modules.md)
- [api/endpoints.md](api/endpoints.md)

**Системный администратор:**
- [deployment/linux.md](deployment/linux.md)
- [operations/logging.md](operations/logging.md)
- [integration/elis.md](integration/elis.md)

**DevOps инженер:**
- [deployment/linux.md](deployment/linux.md)
- [operations/logging.md](operations/logging.md)
- [development/ci-cd.md](development/ci-cd.md)

**Аналитик/Тестировщик:**
- [development/testing.md](development/testing.md)
- [api/endpoints.md](api/endpoints.md)
- [features/field-history.md](features/field-history.md)
- [configs/passport.md](configs/passport.md)

## Основные документы проекта

Помимо этой документации, важные файлы в корне проекта:

- **[CLAUDE.md](../CLAUDE.md)** - Главное руководство для работы с проектом
- **[AGENTS.md](../AGENTS.md)** - Правила работы с репозиторием
- **[CHANGELOG.md](../CHANGELOG.md)** - История версий
- **[TN_Doc/changes.md](../TN_Doc/changes.md)** - Детальный лог изменений

## Планы на будущее

Документация, которая будет добавлена:

### Operations
- **Backup & Recovery** - Резервное копирование и восстановление
- **Performance Monitoring** - Мониторинг производительности
- **Troubleshooting Guide** - Руководство по устранению неполадок
- **Database Maintenance** - Обслуживание баз данных

### Deployment
- **Windows Installation** - Установка и настройка на Windows
- **Docker Deployment** - Развертывание через Docker
- **Configuration Management** - Управление конфигурацией

### Development
- ~~**Testing Guide**~~ - Документация создана: [development/testing.md](development/testing.md)
- ~~**CI/CD Guide**~~ - Документация создана: [development/ci-cd.md](development/ci-cd.md)
- **Code Style Guide** - Руководство по стилю кода
- **Contributing** - Как внести вклад в проект

## Обратная связь

Если вы нашли ошибку в документации или хотите предложить улучшение:

1. Создайте issue в системе отслеживания задач
2. Отправьте pull request с исправлениями
3. Свяжитесь с командой разработки

---

_Последнее обновление: 2026-01-23_
_Версия проекта: 1.3.8_
_Добавлена документация по тестированию и CI/CD_
