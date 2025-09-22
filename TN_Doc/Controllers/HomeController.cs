using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TN_Doc.Models;
using FastReport.Web;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using TN_DocGeneral.Interfaces;
using TN_DocGeneral.Models;
using TN_DocGeneral.Services;
using TN.Doc;
using TN.DocData;
using TN.Utils;
using TN_Doc.Services;
using Data = TN_Doc.Models.Home.Data;
using FileInfo = System.IO.FileInfo;
using IdDoc = TN.DocData.IdDoc;

namespace TN_Doc.Controllers;

public class HomeController : Controller
{
    private readonly CfgApp _cfgApp;
    private readonly ILogger<HomeController> _logger;
    private readonly DbContextOptions<DocGeneral> _options;
    private readonly WebReport _fr;
    private readonly IAppConfigService _appConfig;
    private readonly IReportBuffer _reportBuffer;
    private readonly IDocModuleLoader _docModuleLoader;
    private readonly IDbSchemaCache _dbSchemaCache;

    public HomeController(ILogger<HomeController> logger, DbContextOptions<DocGeneral> context, IReportBuffer reportBuffer, 
        IDocModuleLoader docModuleLoader, IAppConfigService appConfig, IDbSchemaCache dbSchemaCache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = context;
        _appConfig = appConfig;
        _fr = new WebReport();
        _cfgApp = _appConfig.GetAppCfg();
        _reportBuffer = reportBuffer;
        _docModuleLoader = docModuleLoader;
        _dbSchemaCache = dbSchemaCache;
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
            _fr.EnableMargins = true;
            _fr.Mode = WebReportMode.Preview;
            _fr.SinglePage = true;
            _fr.Toolbar = new ToolbarSettings() 
            { 
                Show = false, 
                ShowRefreshButton = false, 
                ShowFirstButton = false 
            };
            _fr.Width = "100%";
            _fr.Height = "auto";
            _fr.Report.Load(Path.Combine(Directory.GetCurrentDirectory(), "Doc", "01_Report_2022-05-05_Release_version.frx"));
            _fr.Render();             
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
            var doc = _docModuleLoader.LoadDocsModule(_options, data.IdDevice, data.IdDoc, Directory.GetCurrentDirectory());
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
            var doc = _docModuleLoader.LoadDocsModule(_options, IdDevice, IdDoc, Directory.GetCurrentDirectory());
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
            _fr.Report.Load(templateFile.FullName);
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
            _fr.Report.SetParameterValue("JsonDoc", jsonDoc);
            _fr.Report.Prepare();

            using (var ms = new MemoryStream())
            {
                using var pdfExport = new FastReport.Export.Pdf.PDFExport();
                pdfExport.ShowProgress = false;
                pdfExport.Subject = "Subject";
                pdfExport.Title = " ";
                pdfExport.Compressed = true;
                pdfExport.AllowPrint = true;
                pdfExport.EmbeddingFonts = true;
                _fr.Report.Export(pdfExport, ms);
                var bytes = ms.ToArray();
                _reportBuffer.SetPdfBytes(bytes);
            }

            _fr.Report.Dispose();
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
        
        var doc = _docModuleLoader.LoadDocsModule(_options, IdDevice, IdDoc, Directory.GetCurrentDirectory());
        if (doc is null)
        {
            _logger.LogError($"Не удалось загрузить DLL для документа {IdDoc}");
            return string.Empty;
        }
        return doc.GetEditDoc(id);
    }


    public void SaveDoc(int IdDevice, IdDoc IdDoc, string data)
    {
        _logger.LogDebug($"Сохранение документа {IdDoc} для устройства {_appConfig.GetDeviceName(IdDevice)}");
        try
        {
            var doc = _docModuleLoader.LoadDocsModule(_options, IdDevice, IdDoc, Directory.GetCurrentDirectory());
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
            
            var doc = _docModuleLoader.LoadDocsModule(_options, IdDevice, IdDoc, Directory.GetCurrentDirectory());
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
            var doc = _docModuleLoader.LoadDocsModule(_options, IdDevice, IdDoc, Directory.GetCurrentDirectory());
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
            return result ?? "[]";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка получения списка пользователей");
            return "[]";
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

    [HttpGet]
    public IActionResult CanEditDocument(int idDevice, IdDoc idDoc)
    {
        _logger.LogDebug($"Проверка возможности редактирования документа {idDoc} для устройства {_appConfig.GetDeviceName(idDevice)}");
        try
        {
            var canEdit = idDoc switch
            {
                IdDoc.Report => _dbSchemaCache.HasDataArm(idDevice, idDoc),
                IdDoc.ReportIncomplete => false,
                IdDoc.Jornal => _dbSchemaCache.HasDataArm(idDevice, idDoc),
                _ => true
            }; 
            
            _logger.LogTrace($"Результат возможности редактирования документа {idDoc}: {canEdit}");
            return Json(new { canEdit = canEdit });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка проверки возможности редактирования документа {idDoc} для устройства {_appConfig.GetDeviceName(idDevice)}");
            return Json(new { canEdit = true });
        }
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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