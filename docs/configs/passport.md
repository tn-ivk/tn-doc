## Паспорт качества: конфигурации

### Матрица устройств и активация ELIS
- Фактическая матрица `устройство ↔ UseElis` выгружается из `TN_Doc/Cfg/CfgApp.json` в артефакт `docs/configs/passport_devices_useElis.json`.  
- Файл содержит:
  - глобальное значение `Elis.Use`;
  - список активных устройств с указанием источника признака (`device` или `global`);
  - перечень конфигов паспортов, назначенных устройству.  
- При изменении `CfgApp.json` пересоздавайте артефакт командой:
  ```bash
  cd /home/snafu/projects/ivk/tn_doc
  python3 scripts/generate_passport_matrix.py
  ```

### Поле `IsBallast`
- Добавлено в `TN.Doc.Edit.Parameter` и все рабочие конфиги `CfgEditPassport*.json`.  
- Назначение:
  - `true` — показатель считается балластным, столбец Result синхронизируется с Measurement;
  - `false` — поведение прежнее, Result редактируется операторами.
- Отсутствие поля трактуется как `false` (значение по умолчанию в модели).
- Классификация из плана:
  - **Балластные**: `TempCorrection`, `PressCorrection`, `DensCorrection`, `Dens20Correction`, `Dens15Correction`, `MassWaterFracCorrection`, `Chloride_Salts.Concentration`, `Chloride_Salts.MassFraction`, `Impurity`.
  - **Небалластные**: `SulfurCorrection`, `DNP.kPa`, `DNP.mercury_mm`, `Yield_fraction_200`, `Yield_fraction_300`, `Yield_fraction_350`, `Mass_fraction_of_paraffin`, `Mass_fraction_of_hydrogen_sulfide`, `Mass_fraction_of_methyl_and_ethyl_mercaptan`, `Mass_fraction_of_organic_chlorides`.

### Порядок обновления конфигов
1. **Инвентаризация**  
   - Сформировать список файлов через `glob TN_Doc/Cfg/**/CfgEditPassport*.json`.  
   - Зафиксировать новые конфиги/устройства в `passport_devices_useElis.json`.
2. **Правки эталона**  
   - Внести изменения в `TN_Doc/Cfg/Passport/CfgEditPassport_GOSTR50.2.040(I).json`.  
   - Проверить валидность: `jq '.' TN_Doc/Cfg/Passport/CfgEditPassport_GOSTR50.2.040(I).json`.
3. **Тиражирование**  
   - Синхронизировать остальные `CfgEditPassport*.json` (включая `Cfg/Passport` и корневые `Cfg/`).  
   - Для файлов с наследованными комментариями `/* ... */` допустимо временно их удалять при автоматической обработке, но комментарий нужно вернуть в исходное место.
4. **Проверки**  
   - Минимум три файла прогнать через `jq '.'` (пример: MI3532(15), EAC, Export).  
   - Запустить `dotnet test Tests/Tests.csproj --filter CfgEditPassport` (см. раздел о тестах).  
   - Ручной smoke: открыть паспорт на устройстве с `UseElis=true` и на устройстве с `UseElis=false`, убедиться что редактор грузится без ошибок.
5. **Согласование**  
   - Сохранить диффы и отправить эксплуатационщикам ответственных площадок (ГОСТ/МИ/ЕАЭС) перед выкладкой.

### Rollback
- Конфиги лежат в git, поэтому откат выполняется командой `git checkout -- <path>` + публикация через обычный процесс доставки.  
- При необходимости откатить только одно устройство:
  1. `git show <commit> -- TN_Doc/Cfg/Passport/CfgEditPassport_<variant>.json > backup.json`.
  2. Скопировать файл на стенд и перезапустить сервисы TN_Doc.
- После отката повторить smoke на устройствах `UseElis=true/false` и обновить `passport_devices_useElis.json`, чтобы отразить фактическое состояние.

### Тесты
- Основные проверки сосредоточены в `Tests/Configs/CfgEditPassportTests.cs`:
  - парсинг конфигов с `IsBalast`;
  - обратная совместимость при отсутствии поля.
- Команда прогона в репозитории:
  ```bash
  cd /home/snafu/projects/ivk/tn_doc
  dotnet test Tests/Tests.csproj --filter CfgEditPassport
  ```
- Перед сдачей фазы 0 фиксируйте, какие файлы проходили валидацию `jq`, и прикладывайте результаты тестов в описании merge request.

