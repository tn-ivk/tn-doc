# Промт для ИИ-агента: Добавление бэкапирования в CI/CD workflows

## Задача

Добавить автоматическое бэкапирование TN_Doc в .deb пакеты для GitLab CI и GitHub Actions.

## Файлы для модификации

1. `.gitlab-ci.yml` — jobs: `package-job`, `package-minimal-job`
2. `.github/workflows/build-and-package.yml` — jobs: `package-full`, `package-minimal`

## Спецификация

| Параметр | Значение |
|----------|----------|
| Что бэкапить | `/opt/TN_Doc/` (без логов) |
| Куда сохранять | `/var/backups/TN_Doc/` |
| Формат имени | `YYYY-MM-DD_HH-mm-ss_TN_Doc_before-update-to_v${VERSION}.tar.gz` |
| Источник версии | Переменная `${VERSION}` из CI/CD (уже доступна в package jobs) |
| Этап | preinst скрипт |
| Ротация | Без автоматической ротации |

## Логика обработки ошибок

```
ЕСЛИ /opt/TN_Doc не существует:
    → Первая установка, бэкап не нужен, продолжить
ИНАЧЕ:
    ЕСЛИ бэкап успешен:
        → Продолжить установку
    ИНАЧЕ (нет места, ошибка записи):
        → ERROR и прервать установку (exit 1)
```

## Код для добавления в preinst

**Важно:** Использовать heredoc **без кавычек** (`EOF`, не `'EOF'`), чтобы `${VERSION}` подставилась при сборке пакета.

Добавить в preinst **ПЕРЕД** секцией удаления старых директорий:

```bash
# === Backup section ===
create_backup() {
    local OPT_DIR="/opt/TN_Doc"
    local BACKUP_DIR="/var/backups/TN_Doc"
    local NEW_VERSION="${VERSION}"  # Подставляется при сборке пакета

    # Первая установка — бэкап не нужен
    if [ ! -d "\$OPT_DIR" ]; then
        echo "First installation detected, no backup needed"
        return 0
    fi

    # Создание директории для бэкапов
    echo "=== Creating backup before update to v\${NEW_VERSION} ==="
    mkdir -p "\$BACKUP_DIR"
    if [ \$? -ne 0 ]; then
        echo "✗ ERROR: Cannot create backup directory \$BACKUP_DIR"
        exit 1
    fi

    # Формирование имени файла
    local TIMESTAMP=\$(date +%Y-%m-%d_%H-%M-%S)
    local BACKUP_FILE="\${BACKUP_DIR}/\${TIMESTAMP}_TN_Doc_before-update-to_v\${NEW_VERSION}.tar.gz"

    echo "Source: \$OPT_DIR"
    echo "Destination: \$BACKUP_FILE"

    # Создание бэкапа
    tar -czf "\$BACKUP_FILE" -C /opt TN_Doc 2>&1
    if [ \$? -ne 0 ]; then
        echo "✗ ERROR: Failed to create backup"
        echo "Check available disk space and permissions"
        rm -f "\$BACKUP_FILE"
        exit 1
    fi

    # Проверка файла
    if [ ! -s "\$BACKUP_FILE" ]; then
        echo "✗ ERROR: Backup file is empty"
        exit 1
    fi

    local BACKUP_SIZE=\$(du -h "\$BACKUP_FILE" | cut -f1)
    echo "✓ Backup created: \$BACKUP_FILE (\$BACKUP_SIZE)"
}

create_backup
# === End backup section ===
```

## Скрипт восстановления

Добавить файл `/usr/local/bin/tn-doc-restore.sh` в пакет:

```bash
#!/bin/bash
set -e

BACKUP_DIR="/var/backups/TN_Doc"
OPT_DIR="/opt/TN_Doc"
SERVICE="TN_Doc.service"

usage() {
    echo "Usage: sudo $0 [--list | --latest | <backup_file>]"
    echo "  --list    Show available backups"
    echo "  --latest  Restore latest backup"
}

list_backups() {
    echo "=== Available backups ==="
    ls -lh "$BACKUP_DIR"/*.tar.gz 2>/dev/null || echo "No backups found"
}

restore() {
    local FILE="$1"
    [ ! -f "$FILE" ] && echo "File not found: $FILE" && exit 1

    echo "=== Restoring from: $FILE ==="
    systemctl stop "$SERVICE" 2>/dev/null || true
    rm -rf "$OPT_DIR"
    tar -xzf "$FILE" -C /opt
    chown -R alphadaemon:alphadaemon "$OPT_DIR"
    chmod 755 "$OPT_DIR/TN_Doc" 2>/dev/null || true
    systemctl start "$SERVICE"

    sleep 2
    systemctl is-active --quiet "$SERVICE" && echo "✓ Restored successfully" || echo "! Service may have failed"
}

[ "$EUID" -ne 0 ] && echo "Run as root" && exit 1

case "${1:-}" in
    --list) list_backups ;;
    --latest) restore "$(ls -t "$BACKUP_DIR"/*.tar.gz 2>/dev/null | head -1)" ;;
    "") usage ;;
    *) [[ "$1" != /* ]] && restore "$BACKUP_DIR/$1" || restore "$1" ;;
esac
```

## Изменения в структуре пакета

В секции создания структуры пакета добавить:

```bash
# Создание директории для скрипта восстановления
mkdir -p deb/usr/local/bin

# Генерация скрипта восстановления
cat << 'RESTORE_EOF' > deb/usr/local/bin/tn-doc-restore.sh
#!/bin/bash
# ... содержимое скрипта восстановления ...
RESTORE_EOF

chmod 755 deb/usr/local/bin/tn-doc-restore.sh
```

**Для minimal пакета:** использовать `deb-minimal/usr/local/bin/`

## Порядок выполнения

1. Прочитать `.gitlab-ci.yml` и `.github/workflows/build-and-package.yml`
2. В `package-job` и `package-minimal-job` (GitLab) / `package-full` и `package-minimal` (GitHub):
   - Найти секцию генерации preinst
   - Добавить функцию `create_backup()` и её вызов **ПЕРЕД** удалением старых директорий
   - Добавить генерацию `tn-doc-restore.sh` в структуру пакета
3. Убедиться, что для preinst используется `EOF` без кавычек (для интерполяции `${VERSION}`)
4. Для скрипта восстановления использовать `'RESTORE_EOF'` с кавычками (без интерполяции)
5. Проверить синтаксис YAML

## Ключевые моменты

- `${VERSION}` уже доступна в package jobs — не нужно извлекать заново
- В GitLab экранировать переменные внутри heredoc как `\$VAR`
- В GitHub для preinst использовать именованный heredoc без кавычек
- Директория `/var/backups/TN_Doc/` создаётся в preinst с правами root
