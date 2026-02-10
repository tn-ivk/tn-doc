# Управление логами TN_Doc

Руководство по конфигурации, просмотру и управлению логами приложения TN_Doc.

## Оглавление
- [Расположение логов](#расположение-логов)
- [Конфигурация логирования](#конфигурация-логирования)
- [Уровни логирования](#уровни-логирования)
- [Просмотр логов](#просмотр-логов)
- [Системный журнал ОС (ELIS)](#системный-журнал-ос-elis)
- [Копирование логов](#копирование-логов)
- [Ротация и архивирование](#ротация-и-архивирование)
- [Устранение проблем](#устранение-проблем)

---

## Расположение логов

### Windows

**Режим разработки (Development):**
```
C:\dev\dotnet\ivk\tn_doc\TN_Doc\bin\Debug\net8.0\logs\
```

**Production (установлен как служба):**
```
C:\Program Files\TN_Doc\logs\
```
или
```
C:\TN_Doc\logs\
```
(в зависимости от места установки)

**Структура файлов логов:**
```
logs/
├── 2025-01-21.log          # Лог за текущий день
├── 2025-01-20.log          # Вчерашний лог
├── 2025-01-19.log          # Позавчерашний лог
└── internal-nlog-log.txt   # Внутренние логи NLog (при ошибках конфигурации)
```

### Linux

**Production (установлен как systemd служба):**
```
/opt/TN_Doc/logs/
```

**Права доступа:**
- Владелец: `alphadaemon:alphadaemon`
- Права на директорию: `755` (drwxr-xr-x)
- Права на файлы: `644` (-rw-r--r--)

**Структура файлов логов:**
```
/opt/TN_Doc/logs/
├── 2025-01-21.log
├── 2025-01-20.log
└── internal-nlog-log.txt
```

---

## Конфигурация логирования

Конфигурация логирования находится в файле:
```
TN_Doc/nlog.config
```

### Основные параметры

```xml
<variable name="logLevel" value="Info" />
<variable name="logDirectory" value="${basedir}/logs" />
```

### Формат записей лога

```
2025-01-21 15:30:45.1234 | INFO | Сообщение | TN.DocGeneral.Services.StatusProvider | 12345 | 8
```

Компоненты:
1. **Дата и время** - `2025-01-21 15:30:45.1234`
2. **Уровень** - `INFO`, `WARN`, `ERROR`, `FATAL`, `DEBUG`, `TRACE`
3. **Сообщение** - текст лога
4. **Логгер** - имя класса, который записал лог
5. **Process ID** - идентификатор процесса
6. **Thread ID** - идентификатор потока

---

## Уровни логирования

### Доступные уровни (от меньшего к большему)

| Уровень | Описание | Использование |
|---------|----------|---------------|
| `Trace` | Детальная трассировка | Только для глубокой отладки |
| `Debug` | Отладочная информация | Разработка и отладка |
| `Info` | Информационные сообщения | **По умолчанию для Production** |
| `Warn` | Предупреждения | Потенциальные проблемы |
| `Error` | Ошибки | Ошибки выполнения |
| `Fatal` | Критические ошибки | Критические сбои системы |

### Изменение уровня логирования

#### Временное изменение (до перезапуска)

**Windows (PowerShell с правами администратора):**
```powershell
# Открыть nlog.config в блокноте
notepad "C:\Program Files\TN_Doc\nlog.config"

# Изменить строку:
<variable name="logLevel" value="Debug" />

# Перезапустить службу
Restart-Service TN_Doc
```

**Linux:**
```bash
# Редактировать конфигурацию
sudo nano /opt/TN_Doc/nlog.config

# Изменить строку:
<variable name="logLevel" value="Debug" />

# Перезапустить службу
sudo systemctl restart tn_doc
```

#### Рекомендации по уровням

- **Production**: `Info` (по умолчанию)
- **Debugging**: `Debug` или `Trace`
- **Performance testing**: `Warn` (минимальный overhead)
- **Troubleshooting**: `Debug`

⚠️ **ВАЖНО**: После отладки верните уровень обратно на `Info`, так как `Debug` и `Trace` генерируют большие объемы логов.

---

## Просмотр логов

### Windows

#### PowerShell
```powershell
# Просмотр последних 50 строк текущего лога
Get-Content "C:\Program Files\TN_Doc\logs\$(Get-Date -Format 'yyyy-MM-dd').log" -Tail 50

# Мониторинг в реальном времени (аналог tail -f)
Get-Content "C:\Program Files\TN_Doc\logs\$(Get-Date -Format 'yyyy-MM-dd').log" -Wait -Tail 20

# Поиск ошибок за сегодня
Get-Content "C:\Program Files\TN_Doc\logs\$(Get-Date -Format 'yyyy-MM-dd').log" | Select-String "ERROR|FATAL"

# Фильтрация по временному диапазону
Get-Content "C:\Program Files\TN_Doc\logs\2025-01-21.log" | Select-String "15:3[0-9]:"

# Поиск по всем логам за последние 7 дней
Get-ChildItem "C:\Program Files\TN_Doc\logs\*.log" |
  Where-Object {$_.LastWriteTime -gt (Get-Date).AddDays(-7)} |
  ForEach-Object {Get-Content $_.FullName | Select-String "ERROR"}
```

#### Notepad++ (рекомендуется для Windows)
```
1. Установить Notepad++ (https://notepad-plus-plus.org/)
2. Открыть файл лога
3. Ctrl+F → вкладка "Найти" → ввести поисковый запрос
4. F5 → обновить содержимое файла
```

#### Windows Event Viewer (для службы)
```
1. Win+R → eventvwr.msc
2. Windows Logs → Application
3. Для ошибок ELIS фильтр по источнику ".NET Runtime" и поиску "[ELIS]"
```

### Linux

#### Базовые команды
```bash
# Просмотр последних 50 строк
tail -n 50 /opt/TN_Doc/logs/$(date +%Y-%m-%d).log

# Мониторинг в реальном времени
tail -f /opt/TN_Doc/logs/$(date +%Y-%m-%d).log

# Поиск ошибок
grep -E "ERROR|FATAL" /opt/TN_Doc/logs/$(date +%Y-%m-%d).log

# Поиск с контекстом (5 строк до и после)
grep -E "ERROR|FATAL" -C 5 /opt/TN_Doc/logs/$(date +%Y-%m-%d).log

# Подсчет ошибок
grep -c "ERROR" /opt/TN_Doc/logs/$(date +%Y-%m-%d).log

# Поиск по нескольким файлам
grep "ERROR" /opt/TN_Doc/logs/*.log

# Фильтрация по времени
grep "15:3[0-9]:" /opt/TN_Doc/logs/2025-01-21.log
```

#### Продвинутый анализ
```bash
# Топ-10 самых частых ошибок
grep "ERROR" /opt/TN_Doc/logs/*.log | cut -d'|' -f3 | sort | uniq -c | sort -rn | head -10

# Количество записей по уровням
awk -F'|' '{print $2}' /opt/TN_Doc/logs/$(date +%Y-%m-%d).log | sort | uniq -c

# Логи по конкретному процессу
grep "processid:12345" /opt/TN_Doc/logs/*.log
```

#### Systemd journal (если настроен)
```bash
# Логи за последний час
journalctl -u tn_doc --since "1 hour ago"

# Логи с уровнем error и выше
journalctl -u tn_doc -p err

# Следить за логами в реальном времени
journalctl -u tn_doc -f

# Экспорт логов в файл
journalctl -u tn_doc --since "2025-01-20" --until "2025-01-21" > tn_doc_logs.txt
```

## Системный журнал ОС (ELIS)

С `2026-01-16` ошибки из `ElisController.ErrorMessage` пишутся не только в NLog-файлы, но и в системный журнал ОС через `ISystemJournalService`.

### Windows (Event Log)

- Источник: `.NET Runtime`
- Тип события: `Error`
- EventId: `1000`
- Формат сообщения: `[ELIS] <текст ошибки>`

```powershell
# Последние ошибки ELIS из Application log
Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='.NET Runtime'; Level=2} |
  Where-Object { $_.Message -like '*[ELIS]*' } |
  Select-Object -First 20 TimeCreated, Id, ProviderName, Message
```

### Linux (syslog / journald)

- Запись выполняется командой `logger`
- Приоритет: `user.err`
- Тег: `TN_Doc:ELIS`

```bash
# ELIS ошибки по tag (если journald собирает syslog)
journalctl -t TN_Doc:ELIS -p err --since "today"

# Альтернатива через syslog-файл
grep "TN_Doc:ELIS" /var/log/syslog
```

---

## Копирование логов

### Сценарий 1: Копирование логов для отправки в техподдержку

#### Windows (PowerShell)

```powershell
# Создать архив логов за последние 7 дней
$today = Get-Date -Format "yyyy-MM-dd"
$archiveName = "TN_Doc_logs_$today.zip"
$logsPath = "C:\Program Files\TN_Doc\logs"

# Создать временную папку
$tempFolder = "$env:TEMP\TN_Doc_logs_$today"
New-Item -ItemType Directory -Force -Path $tempFolder | Out-Null

# Скопировать логи за последние 7 дней
Get-ChildItem "$logsPath\*.log" |
  Where-Object {$_.LastWriteTime -gt (Get-Date).AddDays(-7)} |
  Copy-Item -Destination $tempFolder

# Скопировать конфигурационные файлы (без паролей!)
Copy-Item "$logsPath\..\nlog.config" -Destination $tempFolder
Copy-Item "$logsPath\..\appsettings.json" -Destination $tempFolder

# Создать архив
Compress-Archive -Path "$tempFolder\*" -DestinationPath "$env:USERPROFILE\Desktop\$archiveName"

# Очистить временную папку
Remove-Item -Recurse -Force $tempFolder

Write-Host "Архив создан: $env:USERPROFILE\Desktop\$archiveName"
```

**Упрощенный вариант (последний лог файл):**
```powershell
$today = Get-Date -Format "yyyy-MM-dd"
Copy-Item "C:\Program Files\TN_Doc\logs\$today.log" "$env:USERPROFILE\Desktop\TN_Doc_$today.log"
```

#### Linux (bash)

```bash
#!/bin/bash
# Скрипт для создания архива логов

TODAY=$(date +%Y-%m-%d)
ARCHIVE_NAME="TN_Doc_logs_${TODAY}.tar.gz"
LOGS_PATH="/opt/TN_Doc/logs"
OUTPUT_PATH="$HOME/${ARCHIVE_NAME}"

# Создать архив логов за последние 7 дней
find "$LOGS_PATH" -name "*.log" -mtime -7 -type f | \
  tar -czf "$OUTPUT_PATH" -T -

# Добавить конфигурационные файлы
tar -czf "$OUTPUT_PATH" \
  --exclude='*password*' \
  --exclude='*secret*' \
  -C /opt/TN_Doc logs/*.log nlog.config appsettings.json

echo "Архив создан: $OUTPUT_PATH"
echo "Размер: $(du -h $OUTPUT_PATH | cut -f1)"
```

**Сделать скрипт исполняемым и запустить:**
```bash
chmod +x collect_logs.sh
./collect_logs.sh
```

**Упрощенный вариант:**
```bash
# Скопировать текущий лог на рабочий стол
cp /opt/TN_Doc/logs/$(date +%Y-%m-%d).log ~/TN_Doc_$(date +%Y-%m-%d).log
```

### Сценарий 2: Резервное копирование логов на сетевой диск

#### Windows
```powershell
# Настроить переменные
$logsPath = "C:\Program Files\TN_Doc\logs"
$backupPath = "\\server\backups\TN_Doc\logs"
$daysToKeep = 30

# Создать папку если не существует
New-Item -ItemType Directory -Force -Path $backupPath | Out-Null

# Скопировать все логи
Copy-Item "$logsPath\*.log" -Destination $backupPath -Force

# Удалить старые резервные копии
Get-ChildItem $backupPath -Filter "*.log" |
  Where-Object {$_.LastWriteTime -lt (Get-Date).AddDays(-$daysToKeep)} |
  Remove-Item -Force
```

#### Linux
```bash
#!/bin/bash
LOGS_PATH="/opt/TN_Doc/logs"
BACKUP_PATH="/mnt/backups/TN_Doc/logs"
DAYS_TO_KEEP=30

# Создать директорию если не существует
mkdir -p "$BACKUP_PATH"

# Копировать логи
rsync -av --include='*.log' --exclude='*' "$LOGS_PATH/" "$BACKUP_PATH/"

# Удалить старые резервные копии
find "$BACKUP_PATH" -name "*.log" -type f -mtime +$DAYS_TO_KEEP -delete
```

### Сценарий 3: Копирование логов через SSH (удаленный сервер)

```bash
# С удаленного Linux сервера на локальную машину
scp user@server:/opt/TN_Doc/logs/$(date +%Y-%m-%d).log ./

# Скопировать все логи за последние 7 дней
ssh user@server "find /opt/TN_Doc/logs -name '*.log' -mtime -7 -print0 | tar -czf - --null -T -" > tn_doc_logs.tar.gz

# Скопировать все логи рекурсивно
scp -r user@server:/opt/TN_Doc/logs ./tn_doc_logs_backup
```

---

## Ротация и архивирование

### Автоматическая ротация (NLog)

NLog автоматически создает новый файл каждый день благодаря шаблону имени:
```xml
fileName="${var:logDirectory}/${shortdate}.log"
```

### Ручная очистка старых логов

#### Windows (PowerShell - запускать от администратора)
```powershell
# Удалить логи старше 90 дней
$logsPath = "C:\Program Files\TN_Doc\logs"
$daysToKeep = 90

Get-ChildItem "$logsPath\*.log" |
  Where-Object {$_.LastWriteTime -lt (Get-Date).AddDays(-$daysToKeep)} |
  Remove-Item -Force

Write-Host "Старые логи удалены"
```

#### Linux
```bash
# Удалить логи старше 90 дней
find /opt/TN_Doc/logs -name "*.log" -type f -mtime +90 -delete

# Или переместить в архив
find /opt/TN_Doc/logs -name "*.log" -type f -mtime +30 -mtime -90 \
  -exec gzip {} \; \
  -exec mv {}.gz /opt/TN_Doc/logs/archive/ \;
```

### Автоматическое архивирование (Linux cron)

```bash
# Редактировать crontab
sudo crontab -e

# Добавить задачу (каждый день в 2:00 ночи)
0 2 * * * /opt/TN_Doc/scripts/archive_logs.sh
```

**Скрипт `/opt/TN_Doc/scripts/archive_logs.sh`:**
```bash
#!/bin/bash
LOGS_PATH="/opt/TN_Doc/logs"
ARCHIVE_PATH="/opt/TN_Doc/logs/archive"
DAYS_TO_ARCHIVE=30
DAYS_TO_DELETE=90

# Создать директорию архива
mkdir -p "$ARCHIVE_PATH"

# Архивировать логи старше 30 дней
find "$LOGS_PATH" -maxdepth 1 -name "*.log" -type f -mtime +$DAYS_TO_ARCHIVE \
  -exec gzip -9 {} \; \
  -exec mv {}.gz "$ARCHIVE_PATH/" \;

# Удалить архивы старше 90 дней
find "$ARCHIVE_PATH" -name "*.log.gz" -type f -mtime +$DAYS_TO_DELETE -delete

# Логировать результат
echo "$(date): Log archiving completed" >> /opt/TN_Doc/logs/archive.log
```

**Сделать скрипт исполняемым:**
```bash
sudo chmod +x /opt/TN_Doc/scripts/archive_logs.sh
```

### Автоматическая очистка (Windows Task Scheduler)

**PowerShell скрипт `C:\Program Files\TN_Doc\scripts\CleanupLogs.ps1`:**
```powershell
$logsPath = "C:\Program Files\TN_Doc\logs"
$daysToKeep = 90
$logFile = "$logsPath\cleanup.log"

try {
    $deleted = Get-ChildItem "$logsPath\*.log" |
      Where-Object {$_.LastWriteTime -lt (Get-Date).AddDays(-$daysToKeep)} |
      ForEach-Object {
          Remove-Item $_.FullName -Force
          $_.Name
      }

    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Add-Content -Path $logFile -Value "$timestamp : Удалено логов: $($deleted.Count)"
}
catch {
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Add-Content -Path $logFile -Value "$timestamp : ОШИБКА: $_"
}
```

**Создать задачу в Task Scheduler:**
```powershell
$action = New-ScheduledTaskAction -Execute "PowerShell.exe" `
  -Argument "-NoProfile -ExecutionPolicy Bypass -File 'C:\Program Files\TN_Doc\scripts\CleanupLogs.ps1'"

$trigger = New-ScheduledTaskTrigger -Daily -At 2:00AM

$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -RunLevel Highest

Register-ScheduledTask -TaskName "TN_Doc Log Cleanup" `
  -Action $action -Trigger $trigger -Principal $principal `
  -Description "Автоматическая очистка логов TN_Doc старше 90 дней"
```

---

## Устранение проблем

### Проблема: Логи не создаются

**Проверки:**

1. **Права доступа (Linux):**
```bash
ls -la /opt/TN_Doc/logs/
# Должно быть: drwxr-xr-x alphadaemon alphadaemon

# Исправить права:
sudo chown -R alphadaemon:alphadaemon /opt/TN_Doc/logs/
sudo chmod 755 /opt/TN_Doc/logs/
```

2. **Права доступа (Windows):**
```powershell
# Проверить права для службы
icacls "C:\Program Files\TN_Doc\logs"

# Дать полный доступ для NETWORK SERVICE
icacls "C:\Program Files\TN_Doc\logs" /grant "NT AUTHORITY\NETWORK SERVICE:(OI)(CI)F"
```

3. **Проверить internal-nlog-log.txt:**
```bash
# Linux
cat /opt/TN_Doc/logs/internal-nlog-log.txt

# Windows
type "C:\Program Files\TN_Doc\logs\internal-nlog-log.txt"
```

4. **Валидация nlog.config:**
```bash
# Проверить синтаксис XML
xmllint --noout /opt/TN_Doc/nlog.config  # Linux
```

### Проблема: Ошибки ELIS не видны в системном журнале ОС

**Проверки:**

1. **Проверить, что сообщение пришло в `ElisController.ErrorMessage`:**
```bash
grep "ELIS" /opt/TN_Doc/logs/$(date +%Y-%m-%d).log
```

2. **Linux: проверить наличие `logger`:**
```bash
which logger
logger --version
```

3. **Linux: проверить запись по тегу `TN_Doc:ELIS`:**
```bash
journalctl -t TN_Doc:ELIS -p err --since "today"
```

4. **Windows: проверить Application log по `.NET Runtime`:**
```powershell
Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='.NET Runtime'; Level=2} |
  Where-Object { $_.Message -like '*[ELIS]*' } |
  Select-Object -First 20 TimeCreated, Message
```

### Проблема: Логи занимают слишком много места

**Диагностика:**

```bash
# Linux - размер директории логов
du -sh /opt/TN_Doc/logs/
du -h /opt/TN_Doc/logs/*.log | sort -rh | head -10

# Windows PowerShell
Get-ChildItem "C:\Program Files\TN_Doc\logs" -Recurse |
  Measure-Object -Property Length -Sum |
  Select-Object @{Name="Size(MB)";Expression={[math]::Round($_.Sum/1MB, 2)}}
```

**Решение:**
1. Понизить уровень логирования с `Debug` на `Info`
2. Настроить более агрессивную ротацию
3. Использовать архивирование (gzip снижает размер на ~90%)

### Проблема: Производительность деградирует при включении Debug

**Рекомендации:**
1. Использовать `Debug` только временно для диагностики
2. После устранения проблемы вернуть на `Info`
3. Для production мониторинга использовать `Warn` или `Error`

### Проблема: Нужны логи конкретного модуля

**Изменить правила в nlog.config:**

```xml
<rules>
    <!-- Логировать только ошибки по умолчанию -->
    <logger name="*" minlevel="Error" writeTo="logfile" />

    <!-- Детальное логирование конкретного модуля -->
    <logger name="TN.DocGeneral.Services.StatusProvider" minlevel="Trace" writeTo="logfile" final="true" />
    <logger name="TN_Doc.Controllers.*" minlevel="Debug" writeTo="logfile" final="true" />
</rules>
```

### Проблема: Логи не ротируются

**Проверка:**
1. Убедитесь что в fileName используется `${shortdate}` или другая динамическая переменная
2. Проверьте что `autoReload="true"` в корневом элементе `<nlog>`
3. Перезапустите приложение

---

## Полезные команды для быстрого доступа

### Alias для PowerShell (Windows)

Добавить в профиль PowerShell (`$PROFILE`):

```powershell
# Функции для работы с логами TN_Doc
function Get-TNDocLog {
    param([int]$Lines = 50)
    $today = Get-Date -Format 'yyyy-MM-dd'
    Get-Content "C:\Program Files\TN_Doc\logs\$today.log" -Tail $Lines
}

function Watch-TNDocLog {
    $today = Get-Date -Format 'yyyy-MM-dd'
    Get-Content "C:\Program Files\TN_Doc\logs\$today.log" -Wait -Tail 20
}

function Find-TNDocError {
    $today = Get-Date -Format 'yyyy-MM-dd'
    Get-Content "C:\Program Files\TN_Doc\logs\$today.log" | Select-String "ERROR|FATAL"
}

Set-Alias -Name tnlog -Value Get-TNDocLog
Set-Alias -Name tnwatch -Value Watch-TNDocLog
Set-Alias -Name tnerr -Value Find-TNDocError
```

**Использование:**
```powershell
tnlog         # Показать последние 50 строк
tnwatch       # Мониторинг в реальном времени
tnerr         # Найти ошибки
```

### Alias для bash (Linux)

Добавить в `~/.bashrc` или `~/.bash_aliases`:

```bash
# TN_Doc log shortcuts
alias tnlog='tail -n 50 /opt/TN_Doc/logs/$(date +%Y-%m-%d).log'
alias tnwatch='tail -f /opt/TN_Doc/logs/$(date +%Y-%m-%d).log'
alias tnerr='grep -E "ERROR|FATAL" /opt/TN_Doc/logs/$(date +%Y-%m-%d).log'
alias tnlogsize='du -sh /opt/TN_Doc/logs/'

# Функция для поиска в логах
tnfind() {
    grep -r "$1" /opt/TN_Doc/logs/*.log
}
```

**Применить изменения:**
```bash
source ~/.bashrc
```

**Использование:**
```bash
tnlog          # Последние 50 строк
tnwatch        # Мониторинг в реальном времени
tnerr          # Найти ошибки
tnfind "error" # Найти "error" во всех логах
tnlogsize      # Размер директории логов
```

---

## Контрольный чеклист для отправки логов в поддержку

- [ ] Собраны логи за период проблемы (минимум 1 день до и после)
- [ ] Включен файл `internal-nlog-log.txt` (если есть)
- [ ] Удалены пароли и конфиденциальные данные из конфигов
- [ ] Указана дата и время инцидента
- [ ] Указана версия приложения (из `appsettings.json` или `/api/status`)
- [ ] Указана ОС и версия (.NET Runtime)
- [ ] Архив создан и проверен на целостность
- [ ] Размер архива разумный (< 50 MB, если больше - уточнить у поддержки)

---

## Дополнительная информация

- **Документация NLog**: https://nlog-project.org/documentation/
- **Конфигурация NLog**: `TN_Doc/nlog.config`
- **Основная документация проекта**: `README.md`, `CLAUDE.md`
- **Изменения логирования**: `CHANGELOG.md`

---

_Последнее обновление: 2026-02-09_
