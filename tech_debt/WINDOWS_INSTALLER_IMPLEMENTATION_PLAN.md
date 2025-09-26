# План внедрения Windows-установщика для `TN_Doc`

## Цель
Организовать сборку и поставку инсталлятора для Windows (x64) в существующем GitLab CI, где уже собираются deb-пакеты. Результатом должен быть `.exe`-установщик, устанавливающий самодостаточную сборку ASP.NET Core приложения `TN_Doc` и создающий Windows-службу.

## Подход
- Основной: NSIS-инсталлятор, собираемый на Linux-раннере (через `makensis`).
- Альтернатива: MSI через WiX Toolset 4 (требуется Windows-раннер).

## План работ (высокоуровневые шаги)
1. Включить публикацию под `win-x64` в пайплайне (`dotnet publish` self-contained, single-file, R2R).
2. Добавить поддержку запуска как Windows-службы в `TN_Doc/Program.cs` (условно для Windows — `UseWindowsService`).
3. Добавить NSIS-скрипт `installer/tn_doc.nsi` (копирование файлов, создание ярлыков, установка/перезапуск службы).
4. Добавить job в `.gitlab-ci.yml` для сборки инсталлятора (`makensis`) и публикации артефакта `dist/TN_Doc-Setup-<version>.exe`.
5. Проверить установку вручную на тестовой VM/хосте Windows (создание/старт службы, доступность порта/URL).
6. (Опционально) Добавить правила фаервола/портов и логику миграций.

---

## Детали реализации

### 1) Публикация под Windows (`win-x64`)
Собираем самодостаточную сборку, одним файлом, с ReadyToRun:

```bash
dotnet publish TN_Doc/TN_Doc.csproj \
  -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:PublishReadyToRun=true \
  -o out/win-x64
```

Рекомендации:
- Версию передавать через `-p:Version=...` (например, из `CI_COMMIT_TAG`).
- При необходимости добавить тримминг (`-p:PublishTrimmed=true`) после проверки совместимости.

### 2) Поддержка Windows Service в `Program.cs`
Добавить запуск как службу только на Windows.

```csharp
// TN_Doc/Program.cs (фрагмент)
var builder = WebApplication.CreateBuilder(args);

if (OperatingSystem.IsWindows())
{
    builder.Host.UseWindowsService();
}

// ... остальная конфигурация приложения ...
var app = builder.Build();
app.Run();
```

Примечания:
- Убедиться, что Kestrel слушает нужный адрес/порт из `appsettings.*.json` для продуктивного окружения.
- Если используется `Startup.cs`, сохраните текущую конфигурацию, добавив лишь `UseWindowsService()` в хост.

### 3) NSIS-скрипт инсталлятора `installer/tn_doc.nsi`
Создать файл `installer/tn_doc.nsi` со следующим содержимым:

```nsi
!define AppName "TN_Doc"
!define CompanyName "IVK"
!ifndef VERSION
  !define VERSION "0.0.0"
!endif
!ifndef OUTDIR
  !define OUTDIR "out/win-x64"
!endif

Unicode true
RequestExecutionLevel admin

!include "MUI2.nsh"
!include "FileFunc.nsh"
!include "nsDialogs.nsh"
!include "LogicLib.nsh"

Name "${AppName}"
OutFile "dist/TN_Doc-Setup-${VERSION}.exe"
InstallDir "$PROGRAMFILES\${AppName}"
BrandingText "${CompanyName}"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "Russian"

Section "Install"
  SetOutPath "$INSTDIR"
  File /r "${OUTDIR}/*.*"

  CreateDirectory "$SMPROGRAMS\${AppName}"
  CreateShortCut "$SMPROGRAMS\${AppName}\Запуск ${AppName}.lnk" "$INSTDIR\TN_Doc.exe"
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  CreateShortCut "$SMPROGRAMS\${AppName}\Удалить ${AppName}.lnк" "$INSTDIR\Uninstall.exe"

  ; Пересоздать службу
  nsExec::Exec 'sc.exe stop ${AppName}'
  nsExec::Exec 'sc.exe delete ${AppName}'
  nsExec::Exec 'sc.exe create ${AppName} binPath= "\"$INSTDIR\TN_Doc.exe\"" start= auto DisplayName= "${AppName}"'
  nsExec::Exec 'sc.exe description ${AppName} "TN_Doc ASP.NET Core Service"'
  nsExec::Exec 'sc.exe start ${AppName}'
SectionEnd

Section "Uninstall"
  nsExec::Exec 'sc.exe stop ${AppName}'
  nsExec::Exec 'sc.exe delete ${AppName}'

  Delete "$SMPROGRAMS\${AppName}\Запуск ${AppName}.lnk"
  Delete "$SMPROGRAMS\${AppName}\Удалить ${AppName}.lnк"
  RMDir  "$SMPROGRAMS\${AppName}"

  RMDir /r "$INSTDIR"
SectionEnd
```

Примечания:
- `File /r "${OUTDIR}/*.*"` берёт содержимое из директории публикации.
- Можно добавить создание ярлыка на рабочем столе или правила фаервола через `netsh advfirewall` (по необходимости).

### 4) Job в `.gitlab-ci.yml` для сборки инсталлятора
Добавить отдельный job (используется Linux-образ с .NET SDK, в нём устанавливаем `nsis`):

```yaml
build:win-installer:
  stage: package
  image: mcr.microsoft.com/dotnet/sdk:8.0
  rules:
    - if: '$CI_COMMIT_TAG'          # собирать инсталлеры на тегах
    - if: '$CI_PIPELINE_SOURCE == "push"'
      when: manual
  before_script:
    - apt-get update && apt-get install -y nsis
  script:
    - dotnet restore
    - dotnet publish TN_Doc/TN_Doc.csproj -c Release -r win-x64 --self-contained true \
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true \
        -p:Version=${CI_COMMIT_TAG:-0.0.0} -o out/win-x64
    - mkdir -p dist
    - makensis -DVERSION="${CI_COMMIT_TAG:-$CI_COMMIT_SHORT_SHA}" -DOUTDIR="out/win-x64" installer/tn_doc.nsi
  artifacts:
    paths:
      - dist/TN_Doc-Setup-*.exe
    expire_in: 2 weeks
```

Замечания:
- Если в репозитории ещё нет `installer/tn_doc.nsi`, добавьте его в рамках MR вместе с этим job.
- При необходимости ограничить запуск только на тегах — оставьте единственное правило `if: '$CI_COMMIT_TAG'`.

### 5) Проверка локально (как в раннере)
На Linux:

```bash
sudo apt-get update && sudo apt-get install -y nsis
dotnet publish TN_Doc/TN_Doc.csproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true \
  -o out/win-x64
makensis -DVERSION=0.1.0 -DOUTDIR="out/win-x64" installer/tn_doc.nsi
```

На Windows после установки:

```powershell
Get-Service TN_Doc
Get-Content "C:\\Program Files\\TN_Doc\\logs\\*.log" -ErrorAction SilentlyContinue
```

### 6) Альтернатива: MSI (WiX v4, Windows runner)
Для более глубокой интеграции с реестром/фичами Windows можно собрать MSI через WiX 4:
- Требуется Windows runner с .NET SDK и `wix` (`dotnet tool install --global wix`).
- Добавить проект `installer/wix/` с `Product.wxs` и соответствующим CI-job.
- Плюсы: стандартный MSI, GUID-компоненты, удобная модификация/ремув.
- Минусы: необходим Windows runner и поддержка WiX-скриптов.

---

## Валидация
- Smoke-тест: служба создаётся, запускается, отвечает на HTTP(S) по ожидаемому адресу/порту.
- Логи: пишутся в целевую директорию, нет ошибок загрузки конфигураций/зависимостей.
- Деинсталляция: служба удаляется, папка приложения очищается, ярлыки удаляются.

## Риски и меры
- Неверная конфигурация портов — проверить `appsettings.*.json` и фаервол.
- Антивирус/SmartScreen — подписывать двоичный файл сертификатом (по необходимости).
- Обновления: NSIS-секция должна корректно перезаписывать файлы и перезапускать службу.

## Результат
В пайплайне появится артефакт `dist/TN_Doc-Setup-<version>.exe`, который устанавливает приложение в `C:\\Program Files\\TN_Doc`, создаёт и запускает службу `TN_Doc`, добавляет ярлыки и деинсталлятор.



