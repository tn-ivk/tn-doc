using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using TN_Doc.Models;
using FastReport.Web;
using System.IO;
using System.Threading;
using System.Text;
using System.Collections;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TN_Doc.Models.Home;
using TN_DocGeneral.Interfaces;
using TN_DocGeneral.Services;
using TN.Doc;
using TN.DocData;
using TN.Utils;
using Data = TN_Doc.Models.Home.Data;
using FileInfo = System.IO.FileInfo;

namespace TN_Doc.Controllers;

public class HomeController : Controller
{
    CfgApp _cfgApp;
    readonly ILogger<HomeController> _logger;
    DbContextOptions<DocGeneral> options;
    DocGeneral dbDoc;
    private WebReport FR;
    CancellationToken stoppingToken;
    IAppConfigService _appConfig;

    public HomeController(ILogger<HomeController> logger, DbContextOptions<DocGeneral> context, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        options = context;
        _appConfig = AppConfigService.GetInstance(configuration);

        FR = new WebReport();

        _cfgApp = _appConfig.GetAppCfg();
    }

    private DocGeneral LoadDocsModule(int idDevice, IdDoc idDoc)
    {
        _logger.LogDebug($"Загрузка DLL документа {idDoc} устройства {_appConfig.GetDeviceName(idDevice)}");
        try
        {
            var pathToDll = Directory.GetCurrentDirectory() + _appConfig.GetPathToDocDll(idDevice, idDoc);
            if (string.IsNullOrEmpty(pathToDll))
            {
                _logger.LogError($"Невозможно определить путь до файла DLL документа {idDoc}");
                return null;
            }
            _logger.LogTrace($"Файл DLL: {pathToDll}");
            var dllFileInfo = new FileInfo(pathToDll);
            if (!dllFileInfo.Exists)
            {
                _logger.LogError($"Файл {dllFileInfo.FullName} не существует");
                return null;
            }
            
            var assembly = Assembly.LoadFrom(dllFileInfo.FullName);
            var doc = assembly.GetTypes().Single(x => x.BaseType?.Name == "DocGeneral");

            _logger.LogDebug($"Загрузка DLL {doc.FullName}");
            return (DocGeneral)assembly.CreateInstance(
                doc.FullName,
                false,
                BindingFlags.Default,
                null,
                new object[] { options, _appConfig, idDevice, idDoc, Directory.GetCurrentDirectory() },
                null,
                null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка загрузки DLL документа {idDoc}");
            return null;
        }
    }

    public List<ListItem> GetListDevices()
    {
        _logger.LogDebug($"Загрузка списка устройств");
        var usedDevices = _cfgApp.Devices
            .Where(x => x.Use)
            .ToList();
        if (!usedDevices.Any())
        {
            _logger.LogError("В конфигурации нет используемых устройств");
            return new List<ListItem>();
        }
        var devices = usedDevices.Select(u => new ListItem { Id = u.IdDevice, Name = u.Name })
            .ToList();
        _logger.LogTrace($"Загружен список устройств: {string.Join(',', devices.Select(x => x.Name))}");
        return devices;
    }

    public string GetNameDBForDevice(int IdDevice)
    {
        _logger.LogDebug($"Получение имени базы данных из конфигурации для устройства {_appConfig.GetDeviceName(IdDevice)}");
        var device = _appConfig.GetDeviceCfg(IdDevice);
        if (device is null)
        {
            _logger.LogError("В конфигурации отсутствуют устройства");
            return string.Empty;
        }
        var dbName = device.DBConnectionStrings?.FirstOrDefault(x => x.Use)?.Database;
        if (string.IsNullOrEmpty(dbName))
        {
            _logger.LogError($"Невозможно определить имя БД ИВК для устройства {_appConfig.GetDeviceName(IdDevice)}");
            return String.Empty;
        }
        _logger.LogDebug($"Получение имени базы данных: {dbName}");
        return dbName;
    }

    public List<ListItem> GetListDocs(int IdDevice)
    {
        _logger.LogDebug($"Загрузка списка документов для устройства {_appConfig.GetDeviceName(IdDevice)}");
        var device = _appConfig.GetDeviceCfg(IdDevice);
        if (device is null)
        {
            _logger.LogError($"В конфигурации отсутствует устройства {_appConfig.GetDeviceName(IdDevice)}");
            return [];
        }

        var usedDocs = device.Docs.Where(x => x.Use).ToList();
        if (!usedDocs.Any())
        {
            _logger.LogError($"Устройство {device.Name} не имеет документов");
            return [];
        }
        var list = usedDocs.Select(u => new ListItem { Id = (int)u.IdDoc, Name = u.Name })
            .ToList();
        _logger.LogTrace($"Загружено {list.Count} документов для устройства {device.Name}");
        return list;
    }

    public List<ListItem> GetTemplatesDoc(int IdDevice, IdDoc idDoc)
    {
        _logger.LogTrace($"Загрузка шаблонов документа {idDoc} для устройства {_appConfig.GetDeviceName(IdDevice)}");
        var device = _appConfig.GetDeviceCfg(IdDevice);
        if (device is null)
        {
            _logger.LogError($"В конфигурации отсутствует устройства с идентификатором {IdDevice}");
            return [];
        }
        var doc = _appConfig.GetDocCfg(IdDevice, idDoc);
        if (doc is null)
        {
            _logger.LogError($"Отсутствует документ {idDoc}");
            return [];
        }
        var usedTemplates = doc.TemplateDocs.Where(x => x.Use).ToList();
        if (!usedTemplates.Any())
        {
            _logger.LogError($"Отсутствует шаблон документа {idDoc}");
            return [];
        }
        var templates = usedTemplates.Select(x => new ListItem() { Id = x.Id, Name = x.Name })
            .ToList();
        _logger.LogDebug($"Загружено {templates.Count} шаблонов документа {doc.Name}");
        return templates;
    }

    public List<ListItem> GetListProtocolNumber(int IdDevice, IdDoc idDoc)
    {
        _logger.LogDebug($"Загрузка списка протоколов для документа {idDoc} для устройства  {_appConfig.GetDeviceName(IdDevice)}");
        var list = new List<ListItem>
        {
            new() { Id = 1, Name = "Протокол 1" },
            new() { Id = 2, Name = "Протокол 2" }
        };
        return list;
    }

    /// <summary>
    /// Сохранение Id последнего открытого документа
    /// </summary>
    /// <param name="IdDevice">Id устройства</param>
    /// <param name="IdDoc">Id библиотеки</param>
    /// <param name="idTemplateDoc">Id открытого документа</param>
    public void SetIdTemplateDoc(int IdDevice, IdDoc IdDoc, int idTemplateDoc)
    {
        _appConfig.SetLastUsedTemplateId(IdDevice, IdDoc, idTemplateDoc);
    }
    
    public int GetIdTemplateDoc(int IdDevice, IdDoc IdDoc)
    {
        _logger.LogDebug($"Получение идентификатора последнего открытого шаблона документа {IdDoc} для устройства {_appConfig.GetDeviceName(IdDevice)}");
        int id;
        var validResult = false;
        var lastUsedTemplateId = _appConfig.GetLastUsedTemplateId(IdDevice, IdDoc);
        var usedTemplateDocs = _cfgApp.Devices.Single(x => x.IdDevice == IdDevice)
            .Docs.Single(x => x.IdDoc == IdDoc)
            .TemplateDocs
            .Where(x => x.Use)
            .ToArray();

        if (lastUsedTemplateId is not null && usedTemplateDocs.Any())
        {
            var template = usedTemplateDocs.FirstOrDefault(x => x.Id == lastUsedTemplateId);
            if (template != null)
            {
                // Если нашли шаблон с последним ID, используем его
                _logger.LogDebug($"Идентификатор шаблона последнего открытого документа {IdDoc}: {lastUsedTemplateId}");
                id = (int)lastUsedTemplateId;
                validResult = true;
            }
            else
            {
                // Если шаблон с последним ID не найден, используем первый доступный шаблон
                _logger.LogWarning($"Идентификатор шаблона последнего открытого документа {IdDoc}: {lastUsedTemplateId} отсутствует в списке используемых шаблонов");
                id = usedTemplateDocs.First().Id;
                validResult = true;
            }
        }
        else if(usedTemplateDocs.Any())
        {
            _logger.LogWarning($"Идентификатор шаблона последнего открытого документа {IdDoc} не определен: применяется первый из списка используемых");
            id = usedTemplateDocs.First().Id;
            validResult = true;
        }
        else
        {
            _logger.LogError($"Список используемых шаблонов документа {IdDoc} не инициализирован");
            id = lastUsedTemplateId ?? 0;
        }
        if(validResult)
            SetIdTemplateDoc(IdDevice, IdDoc, id);
        return id;
    }
    
    public bool IsUsedSecurity() => _cfgApp.UseSecurityFeatures;
    
    /// <summary>
    /// Проверяем использовать ЕЛИС или нет.
    /// </summary>
    /// <param name="IdDevice"></param>
    /// <returns></returns>
    public bool IsUsedElis(int IdDevice) => _appConfig.IsUsedElis(IdDevice);

    /// <summary>
    /// Получить данные для регистрации устройства в ЕЛИС.
    /// </summary>
    /// <param name="IdDevice"></param>
    /// <returns></returns>
    public Dictionary<string, string> GetDataForRegistrationDeviceInELIS(int IdDevice)
    {
        _logger.LogDebug($"Получение данных для регистрации устройства {_appConfig.GetDeviceName(IdDevice)} в ЕЛИС");
        var device = _appConfig.GetDeviceCfg(IdDevice);

        if (device.Elis == null)
            if (_cfgApp.Elis == null)
            {                    
            }
            else 
                return new Dictionary<string, string>()
                {
                    { "ostKey", _cfgApp.Elis.OstKey },
                    { "siknKey", _cfgApp.Elis.SiknKey },
                    { "clientName", _cfgApp.Elis.ClientName }
                };
        else
            return new Dictionary<string, string>()
            {
                { "ostKey", device.Elis.OstKey },
                { "siknKey", device.Elis.SiknKey },
                { "clientName", device.Elis.ClientName }
            };

        return null;
    }

    public Dictionary<string, string> GetClientToken(int IdDevice)
    {
        string clientToken = string.Empty;
        var device = _cfgApp.Devices.Single(x => x.IdDevice == IdDevice);

        if (device.Elis == null)
            if (_cfgApp.Elis == null) clientToken = string.Empty;
            else clientToken = _cfgApp.Elis.ClientToken;
        else clientToken = device.Elis.ClientToken;

        return String.IsNullOrEmpty(clientToken)
            ? new Dictionary<string, string>() { { "clientToken", null } }
            : new Dictionary<string, string>() { { "clientToken", clientToken } };
    }

    public bool SetClientToken(int IdDevice, string clientToken) =>
        _appConfig.SetElisClientToken(IdDevice, clientToken);
    
    public string GetElisData() => String.Empty;
 
    public IActionResult Index([FromServices]AppInfoProvider provider)
    {
        ViewData["Version"] = provider.Version;
        try
        {
            FastReport.Utils.Config.EnableScriptSecurity = false;
            FR.EnableMargins = true;
            FR.Mode = WebReportMode.Preview;
            FR.SinglePage = true;
            FR.Toolbar = new ToolbarSettings() 
            { 
                Show = false, 
                ShowRefreshButton = false, 
                ShowFirstButton = false 
            };
            FR.Width = "100%";
            FR.Height = "auto";
            FR.Report.Load(Path.Combine(Directory.GetCurrentDirectory(), "Doc", "01_Report_2022-05-05_Release_version.frx"));
            FR.Render();             
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка инициализации построителя отчета");
        }
        
        return View(_cfgApp);
    }

    public List<RequestListDocs> GetList(Data data)
    {
        if (data is null)
        {
            _logger.LogError($"Невозможно получить список документов. Переменная {nameof(data)} is null");
            return [];
        }
        _logger.LogTrace($"Получение списка документов типа {data.IdDoc} для устройства {_appConfig.GetDeviceName(data.IdDevice)}");
        try
        {
            var (unixTimeBegin, unixTimeEnd) = ParseDateRange(data.DTBegin, data.DTEnd);
            var doc = LoadDocsModule(data.IdDevice, data.IdDoc);
            if (doc is null)
            {
                _logger.LogError($"Не удалось загрузить DLL для документа {data.IdDoc}");
                return [];
            }
            return data.IdDoc == IdDoc.ReportIncomplete 
                ? doc.GetList() 
                : doc.GetList(unixTimeBegin, unixTimeEnd);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Ошибка получение списка документов: {ex.Message}");
            return [];
        }
    }

    private (long UnixTimeBegin, long UnixTimeEnd) ParseDateRange(string dtBegin, string dtEnd)
    {
        var beginDate = ParseDateSafely(dtBegin);
        var endDate = ParseDateSafely(dtEnd);
        
        if (endDate < beginDate)
        {
            _logger.LogError($"Конец периода ({dtEnd}) раньше начала ({dtBegin}). Используются значения по умолчанию");
            return (default, default);
        }

        try
        {
            var unixTimeBegin = new DateTimeOffset(beginDate, TimeSpan.Zero).ToUnixTimeSeconds();
            var unixTimeEnd = new DateTimeOffset(endDate, TimeSpan.Zero).ToUnixTimeSeconds() + 1;
            return (unixTimeBegin, unixTimeEnd);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogError(ex, $"Ошибка преобразования дат в Unix время: начало={dtBegin}, конец={dtEnd}");
            return (default, default);
        }
    }

    private DateTime ParseDateSafely(string dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return DateTime.UnixEpoch;

        if (DateTime.TryParse(dateString, out var parsedDate))
            return parsedDate;

        _logger.LogError($"Невозможно разобрать дату '{dateString}'. Используется значение по умолчанию: {DateTime.UnixEpoch}");
        return DateTime.UnixEpoch;
    }
    
    public bool GetDoc(int IdDevice, IdDoc IdDoc, int id, int protocolNumber)
    {
        _logger.LogDebug($"Отображение документа устройства {_appConfig.GetDeviceName(IdDevice)}, документа {IdDoc} c ИД: {id}");
        if (id == 0)
        {
            _logger.LogWarning($"Попытка отображения документа {IdDoc} с нулевым идентификатором");
            return false;
        }

        try
        {
            var doc = LoadDocsModule(IdDevice, IdDoc);
            if (doc is null)
            {
               _logger.LogError($"Не удалось загрузить DLL для документа {IdDoc}");
               return false;
            }
            var pathTemplateFile = doc.GetPathTemplateFile();

            if (string.IsNullOrEmpty(pathTemplateFile))
            {
                _logger.LogError($"Пустой путь для выбранного шаблона документа {nameof(pathTemplateFile)}");
                return false;
            }
                
            var templateFile = new FileInfo(pathTemplateFile);
            if (!templateFile.Exists)
            {
                _logger.LogError($"Отсутствует файл шаблона документа: {pathTemplateFile}");
                return false;
            }
            _logger.LogTrace($"Загрузка шаблона документа: {templateFile.FullName}");
            FR.Report.Load(templateFile.FullName);
            var jsonDoc = IdDoc switch
            {
                IdDoc.KMH_PP_Areom => doc.GetViewDoc(id, protocolNumber),
                IdDoc.KMH_PV => doc.GetViewDoc(id, protocolNumber),
                IdDoc.KMH_PW => doc.GetViewDoc(id, protocolNumber),
                IdDoc.Poverka2816 => doc.GetViewDoc(id, protocolNumber),
                IdDoc.KMH_MI2816 => doc.GetViewDoc(id, protocolNumber),
                _ => doc.GetViewDoc(id)
            };
            if (jsonDoc == null)
            {
                _logger.LogError($"Метод GetViewDoc вернул null для документа {IdDoc} с id: {id}");
                return false;
            }
            FR.Report.SetParameterValue("JsonDoc", jsonDoc);
            FR.Report.Prepare();

            var pdfExport = new FastReport.Export.Pdf.PDFExport()
            {
                ShowProgress = false,
                Subject = "Subject",
                Title = " ",
                Compressed = true,
                AllowPrint = true,
                EmbeddingFonts = true,
                //HideMenubar = true,
                //HideToolbar = true,
                //HideWindowUI= true
            };

            var pdfFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "PDF", "PDF.pdf");
            var pdfFile = new FileInfo(pdfFilePath);
            if (pdfFile.Directory is null)
                throw new Exception("Ошибка определения пути файла PDF.pdf");
            
            if (!pdfFile.Directory.Exists)
            {
                _logger.LogWarning($"Не существует директория: {pdfFile.Directory.FullName}");
                pdfFile.Directory.Create();
            }

            if(!pdfFile.Exists)
                _logger.LogWarning($"Файл не существует: {pdfFile.FullName}");
            
            FR.Report.Export(pdfExport, pdfFile.FullName);

            pdfExport.Dispose();
            FR.Report.Dispose();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка отображения документа {IdDoc} для устройства {_appConfig.GetDeviceName(IdDevice)}");
            return false;;
        }
    }

    public string GetDocEdit(int IdDevice, IdDoc IdDoc, int id)
    {
        _logger.LogDebug($"Отображение формы редактирования документа устройства {_appConfig.GetDeviceName(IdDevice)}, документа {IdDoc} c ИД: {id}");
        if (id == 0)
        {
            _logger.LogWarning($"Попытка редактирования документа {IdDoc} с нулевым идентификатором");
            return string.Empty;
        }
        
        var doc = LoadDocsModule(IdDevice, IdDoc);
        if (doc is null)
        {
            _logger.LogError($"Не удалось загрузить DLL для документа {IdDoc}");
            return string.Empty;
        }
        return doc.GetEditDoc(id);
    }

    public string ExportDoc(int IdDevice, IdDoc IdDoc, int id, string format, int protocolNumber)
    {         
        _logger.LogDebug($"Экспорт документа {IdDoc} c ИД: {id}, номер протокола {protocolNumber} для устройства {_appConfig.GetDeviceName(IdDevice)}");
        try
        {
            var doc = LoadDocsModule(IdDevice, IdDoc);
            if (doc is null)
            {
                _logger.LogError($"Не удалось загрузить DLL для документа {IdDoc}");
                return string.Empty;
            }

            var exportDirPath = Path.Combine(_appConfig.GetAppCfg().ExportDoc.Path, _appConfig.GetDocCfg(IdDevice, IdDoc).Name);
            if (!Directory.Exists(exportDirPath))
                Directory.CreateDirectory(exportDirPath);

            FR.Report.Load(doc.GetPathTemplateFile());
            var jsonDoc = doc.GetViewDoc(id, protocolNumber);
            if (jsonDoc == null)
            {
                _logger.LogError($"Метод GetViewDoc вернул null для документа {IdDoc} с id: {id}, номер протокола {protocolNumber}");
                return string.Empty;
            }

            FR.Report.SetParameterValue("JsonDoc", jsonDoc);
            FR.Report.Prepare();
            var exportFileName = JObject.Parse(doc.GetViewDoc(id).ToString() ?? string.Empty)["Doc"]?["Settings"]?["General"]?["FileNameForExportDoc"]?.ToString();
            if (string.IsNullOrEmpty(exportFileName))
            {
                _logger.LogError("Невозможно определить имя для экспортируемого файла");
                exportFileName = "undefined";
            }

            var exportFilePath = Path.Combine(exportDirPath, exportFileName);

            switch (format)
            {
                case "pdf":
                    FR.Report.Export(new FastReport.Export.Pdf.PDFExport() { ShowProgress = false }, exportFilePath += ".pdf");
                    break;
                case "excel":
                    FR.Report.Export(new FastReport.Export.OoXML.Excel2007Export() { ShowProgress = false }, exportFilePath += ".xlsx");
                    break;
                case "ods":
                    FR.Report.Export(new FastReport.Export.Odf.ODSExport() { ShowProgress = false }, exportFilePath += ".ods");
                    break;
                case "xml":
                    FR.Report.Export(new FastReport.Export.Xml.XMLExport() { ShowProgress = false }, exportFilePath += ".xml");
                    break;
                default:
                    throw new NotSupportedException($"Формат {format} не поддерживается");
            }

            FR.Report.Dispose();
            return exportFilePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка экспорта документа {IdDoc}");
            return string.Empty;
        }
    }

    public void SaveDoc(int IdDevice, IdDoc IdDoc, string data)
    {
        _logger.LogDebug($"Сохранение документа {IdDoc} для устройства {_appConfig.GetDeviceName(IdDevice)}");
        try
        {
            var doc = LoadDocsModule(IdDevice, IdDoc);
            if (doc is null)
            {
                _logger.LogError($"Не удалось загрузить DLL для документа {IdDoc}");
                return;
            }
            doc.SaveDoc(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка сохранения документа {IdDoc}");
        }
    }

    [HttpPost]
    public void UpdateDoc(int IdDevice, IdDoc IdDoc, string data)
    {
        _logger.LogDebug($"Обновление документа {IdDoc} для устройства {_appConfig.GetDeviceName(IdDevice)}");
        try
        {
            if (IdDoc != IdDoc.Passport)
            {
                _logger.LogWarning($"Обновление данных не применяется для документов типа {IdDoc}");
                return;
            }

            if (string.IsNullOrEmpty(data))
            {
                _logger.LogError("Данные для обновления пустые или отсутсвуют");
                return;                
            }
            
            var doc = LoadDocsModule(IdDevice, IdDoc);
            if (doc is null)
            {
                _logger.LogError($"Не удалось загрузить DLL для документа {IdDoc}");
                return;
            }
            if(doc is IDocUpdater docUpdater)
                docUpdater.DocUpdate(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка обновления документа {IdDoc}");
        }
    }
    
    public PeriodDocument GetPeriodDocument(int IdDevice, IdDoc IdDoc, int id)
    {
        _logger.LogDebug($"Получение периода документа {IdDoc} для устройства {_appConfig.GetDeviceName(IdDevice)}");
        try
        {
            var doc = LoadDocsModule(IdDevice, IdDoc);
            if (doc is null)
            {
                _logger.LogError($"Не удалось загрузить DLL для документа {IdDoc}");
                return null;
            }
            var period = doc.GetPeriodDocument(id);
            return period;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка получения периода документа {IdDoc}");
            return null;
        }
    }

    public async Task<string> GetListUsers()
    {
        _logger.LogDebug($"Получение списка пользователей");
        try
        {
            var result = await _appConfig.GetDictionariesJsonAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка получения списка пользователей");
            return String.Empty;
        }
    }

    public string GetInvalideChars(int IdDevice)
    {
        _logger.LogDebug($"Получение списка неразрешенных символов для устройства {_appConfig.GetDeviceName(IdDevice)}");
        try
        {
            var result = JsonConvert.SerializeObject(_appConfig.GetDeviceCfg(IdDevice).InvalidChars);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка получения списка неразрешенных символов для устройства {_appConfig.GetDeviceName(IdDevice)}");
            return string.Empty;
        }
    }

    public string GetSaveBtnText(int IdDevice, IdDoc IdDoc)
    {
        _logger.LogDebug($"Получение текста кнопки сохранения для документа {IdDoc} для устройства {_appConfig.GetDeviceName(IdDevice)}");
        try
        {
            if (!IsUsedElis(IdDevice))
                return "Сохранить";

            if (IdDoc is IdDoc.Act or IdDoc.Passport)
                return "Завершить редактирование и отправить";

            return "Сохранить";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка получения текста кнопки сохранения для документа {IdDoc} для устройства {IdDevice}");
            return "Сохранить";
        }
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private void GetPropertyValue(object obj, string key, ref object result)
    {
        if (obj is null) return;

        if (obj is IEnumerable)
        {
            IEnumerable enumerable = (IEnumerable)obj;
            foreach (object item in enumerable)
                GetPropertyValue(item, key, ref result);
        }
        else
        {
            Type objType = obj.GetType();
            PropertyInfo[] properties = objType.GetProperties();

            foreach (var property in properties)
            {
                object propValue = property.GetValue(obj, null);

                if (property.PropertyType.IsPrimitive)
                {
                    Debug.WriteLine(property.Name);
                    if (property.Name == key)
                    {
                        result = property.GetValue(obj);
                    }
                }
                else if (property.PropertyType.IsClass)
                {
                    Debug.WriteLine(property.Name);
                    if (propValue is string)
                    {
                        if (property.Name == key)
                        {
                            result = property.GetValue(obj);
                        }
                    }
                    else if (property.PropertyType.IsArray || property.PropertyType.IsSerializable)
                    {
                        IEnumerable enumerable = (IEnumerable)propValue;
                        foreach (object item in enumerable)
                            GetPropertyValue(item, key, ref result);
                    }
                    else
                    {
                        if (property.Name == key)
                        {
                            result = property.GetValue(obj);
                        }
                        else
                        {
                            GetPropertyValue(propValue, key, ref result);
                        }
                    }
                }
            }
        }
    }

    public string arrByteToString(object arrByte)
    {
        if (string.IsNullOrEmpty(arrByte.ToString()))
            return "";

        return Encoding.UTF8.GetString((byte[])arrByte);
    }

    public string StringToHexArrByte(string str)
    {
        string strResult = "0x";
        byte[] arrByte = Encoding.UTF8.GetBytes(str);

        for (int i = 0; i < arrByte.Length; i++)
        {
            strResult += string.Format("{0:x2}", arrByte[i]);
        }
        return strResult;
    }
}