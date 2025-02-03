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
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using TN_Doc.Models.Home;
using TN_Doc.Models.Services;
using TN.Doc;
using TN.DocData;
using TN.Utils.Helpers;
using Data = TN_Doc.Models.Home.Data;

namespace TN_Doc.Controllers
{
    public class HomeController : Controller
    {
        CfgApp _cfgApp;
        LastUsedTemplateListCfg _lastTempList;
        readonly ILogger<HomeController> _logger;
        DbContextOptions<DocGeneral> options;
        DocGeneral dbDoc;
        private WebReport FR;
        CancellationToken stoppingToken;
        Device deviceCfg;
        Document docCfg;
        IAppConfigService _appConfig;

        public HomeController(ILogger<HomeController> logger, DbContextOptions<DocGeneral> context, IAppConfigService appConfig)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            options = context;
            _appConfig = appConfig;

            FR = new WebReport();

            InitApp();
        }

        private void InitApp()
        {
            // считывание файла-конфигурации CfgApp.json - основные настройки приложения
            var cfgFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Cfg", "CfgApp.json");
            _cfgApp = CfgFileRW.LoadCfg<CfgApp>(cfgFilePath);
            _cfgApp = _appConfig.GetAppCfg();
            if(_cfgApp is null)
                _logger.LogError($"Невозможно загрузить конфигурацию из файла {cfgFilePath}");
            else
                _logger.LogDebug($"Загрузка конфигурации из файла {cfgFilePath}");
            
            // считывание файла-списка последних шаблонов документов
            var lastTempIdPath = Path.Combine(Directory.GetCurrentDirectory(),"UserPreference", "LastUsedTemplateList.json");
            if (System.IO.File.Exists(lastTempIdPath))
            {
                _logger.LogDebug($"Считывание списка идентификаторов последних просматриваемых шаблонов документов из файла {lastTempIdPath}");
                _lastTempList = CfgFileRW.LoadCfg<LastUsedTemplateListCfg>(lastTempIdPath);
            }
            else
            {
                _logger.LogWarning($"Файл {lastTempIdPath} не существет");
                if (_cfgApp is not null)
                {
                    _logger.LogDebug($"Восстановление списка идентификаторов последних просматриваемых шаблонов документов из файла настроек приложения");
                    _lastTempList = new LastUsedTemplateListCfg()
                    {
                        Devices = _cfgApp.Devices.Select(device => new LastUsedTemplateList()
                            {
                                IdDevice = device.IdDevice,
                                LastTemplateList = device.Docs.Select(doc => new LastUsedTemplate()
                                {
                                    IdDoc = doc.IdDoc, 
                                    LastTemplateId = doc.LastUsedTemplateId
                                }).ToList() 
                            }).ToList()
                    };
                }
                else
                {
                    _logger.LogError($"Невозможно восстановить список идентификаторов последних просматриваемых шаблонов документов. " +
                                     $"Переменная {nameof(_lastTempList)} не инициализированна");
                }
            }
        }

        private DocGeneral LoadDocsModule(int IdDevice, IdDoc idDoc)
        {
            _logger.LogDebug("Загрузка dll");
            deviceCfg = _cfgApp.Devices.Single(x => x.IdDevice == IdDevice);
            docCfg = deviceCfg.Docs.Single(x => x.IdDoc == idDoc);

            Assembly assembly = Assembly.LoadFrom(Directory.GetCurrentDirectory() + docCfg.PathToDocDll);
            var doc = assembly.GetTypes().Single(x => x.BaseType?.Name == "DocGeneral");

            return (DocGeneral)assembly.CreateInstance(
                doc.FullName,
                false,
                BindingFlags.Default,
                null,
                new object[] { options, Path.Combine(Directory.GetCurrentDirectory()), deviceCfg },
                null,
                null);
        }

        public List<ListItem> GetListDevices()
        {
            _logger.LogDebug($"Загрузка списка устройств");
            List<ListItem> devices = _cfgApp.Devices.Where(x => x.Use)
                .Select(u => new ListItem() { Id = u.IdDevice, Name = u.Name }).ToList();

            return devices;
        }

        public string GetNameDBForDevice(int IdDevice)
        {
            var device = _cfgApp.Devices.Single(x => x.IdDevice == IdDevice);
            return device.DBConnectionStrings.First(x => x.Use).Database;
        }

        public List<ListItem> GetListDocs(int IdDevice)
        {
            _logger.LogDebug($"Загрузка списка документов");
            var device = _cfgApp.Devices.Single(x => x.IdDevice == IdDevice);
            var list = device.Docs.Where(x => x.Use)
                .Select(u => new ListItem { Id = (int)u.IdDoc, Name = u.Name }).ToList();
            return list;
        }

        public List<ListItem> GetTemplatesDoc(int IdDevice, IdDoc idDoc)
        {
            _logger.LogDebug("Загрузка шаблонов документа");
            var device = _cfgApp.Devices.Single(x => x.IdDevice == IdDevice);
            var doc = device.Docs.Single(x => x.IdDoc == idDoc);

            return doc.TemplateDocs.Where(x => x.Use).Select(x => new ListItem() { Id = x.Id, Name = x.Name }).ToList();
        }

        public List<ListItem> GetListProtocolNumber(int IdDevice, IdDoc idDoc)
        {
            _logger.LogDebug("Загрузка списка протоколов");
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
        /// <param name="idDevice">Id устройства</param>
        /// <param name="idDoc">Id библиотеки</param>
        /// <param name="idTemplateDoc">Id открытого документа</param>
        public void SetIdTemplateDoc(int idDevice, IdDoc idDoc, int idTemplateDoc)
        {
            if (_lastTempList is null)
            {
                _logger.LogError($"Сохранение Id последнего открытого документа невозможно. " +
                                 $"Список последних открытых шаблонов документов не инициализирован {nameof(_lastTempList)}");
                return;
            }

            // TODO: сделать в классе Device метод проверки наличия и добавление устройства
            var lastUsedTemplate = _lastTempList.Devices.FirstOrDefault(x => x.IdDevice == idDevice)
                ?.LastTemplateList.FirstOrDefault(x => x.IdDoc == idDoc);
            if (lastUsedTemplate is null)
            {
                _logger.LogError("Сохранение Id последнего открытого документа невозможно. " +
                                 $"Список последних открытых шаблонов документов не инициализирован {nameof(_lastTempList)}"); 
                return;
            }
            lastUsedTemplate.LastTemplateId = idTemplateDoc;
            _logger.LogDebug($"Сохранение Id последнего открытого документа");
            CfgFileRW.SaveCfg(Path.Combine(Directory.GetCurrentDirectory(), "UserPreference"), "/LastUsedTemplateList.json", _lastTempList);
        }
        
        public int GetIdTemplateDoc(int idDevice, IdDoc idDoc)
        {
            var lastUsedTemplateId = _lastTempList.Devices.FirstOrDefault(x => x.IdDevice == idDevice)
                ?.LastTemplateList.FirstOrDefault(x => x.IdDoc == idDoc)
                ?.LastTemplateId ?? -1;

            if(lastUsedTemplateId < 0)
                _logger.LogWarning($"Идентификатор последних открытых шаблонов документов не инициализирован {nameof(_lastTempList)}");
            
            var usedTemplateDocs = _cfgApp.Devices.Single(x => x.IdDevice == idDevice)
                .Docs.Single(x => x.IdDoc == idDoc)
                .TemplateDocs
                .Where(x => x.Use)
                .ToArray();

            if (usedTemplateDocs.Any(x => x.Id == lastUsedTemplateId))
            {
                _logger.LogDebug($"Идентификатор шаблона последнего открытого документа: {lastUsedTemplateId}");
                return lastUsedTemplateId;
            }
            if (!usedTemplateDocs.Any())
            {
                _logger.LogError("Не удалось загрузить список используемых шаблонов приложения");
                return lastUsedTemplateId;
            }
            
            var id = usedTemplateDocs.FirstOrDefault()?.Id ?? -1;
            if(id >= 0)
                SetIdTemplateDoc(idDevice, idDoc, id);
            return id;
        }
        
        public string GetPathTemplateDoc(int IdDevice, IdDoc IdDoc, int IdTemplateDoc)
        {
            return _cfgApp.Devices.Single(x => x.IdDevice == IdDevice)
                            .Docs.Single(x => x.IdDoc == IdDoc)
                            .TemplateDocs.Single(x => x.Id == IdTemplateDoc).PathToDocTemplateFile;
        }


        public bool IsUsedSecurity() => _cfgApp.UseSecurityFeatures;


        /// <summary>
        /// Проверяем использовать ЕЛИС или нет.
        /// </summary>
        /// <param name="IdDevice"></param>
        /// <returns></returns>
        public bool IsUsedElis(int IdDevice)
        {
            var device = _cfgApp.Devices.Single(x => x.IdDevice == IdDevice);
            var cfgElisUse = _cfgApp.Elis?.Use ?? false;

            return device.Elis?.Use ?? cfgElisUse;
        }

        /// <summary>
        /// Получить данные для регистрации устройства в ЕЛИС.
        /// </summary>
        /// <param name="IdDevice"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetDataForRegistrationDeviceInELIS(int IdDevice)
        {
            _logger.LogDebug("Получение данных для регистрации устройства в ЕЛИС");
            var device = _cfgApp.Devices.Single(x => x.IdDevice == IdDevice);

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

        public bool SetClientToken(int IdDevice, string clientToken)
        {
            var device = _cfgApp.Devices.Single(x => x.IdDevice == IdDevice);

            if (device.Elis == null)
                if (_cfgApp.Elis == null)
                    return false;
                else 
                    _cfgApp.Elis.ClientToken = clientToken;
            else 
                device.Elis.ClientToken = clientToken;

            CfgFileRW.SaveCfg(Path.Combine(Directory.GetCurrentDirectory(), $"Cfg"), $"/CfgApp.json", _cfgApp);

            return true;
        }

        public string GetElisData()
        {
                return "";
        }

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
                FR.Report.Load(Path.Combine(Directory.GetCurrentDirectory(), $"01_Report_2022-05-05_Release_version.frx"));
                FR.Render();             
            }
            catch (Exception ex)
            {
            }
            
            return View(_cfgApp);
        }

        public List<RequestListDocs> GetList(Data data)
        {
            try
            {
                DateTime DTBegin = new();
                DateTime DTEnd = new();

                long UTBegin, UTEnd;

                if (data.DTBegin != null)
                    DTBegin = DateTime.Parse(data.DTBegin);
                if (data.DTEnd != null)
                    DTEnd = DateTime.Parse(data.DTEnd);

                UTBegin = new DateTimeOffset(DTBegin, TimeSpan.Zero).ToUnixTimeSeconds();
                UTEnd = new DateTimeOffset(DTEnd, TimeSpan.Zero).ToUnixTimeSeconds();
                UTEnd++;

                var doc = LoadDocsModule(data.IdDevice, data.IdDoc);

                if (data.IdDoc == IdDoc.ReportIncomplete)
                    return doc.GetList();
            
                return doc.GetList(UTBegin, UTEnd);
            }
            catch (Exception e)
            {
                return new List<RequestListDocs>();
            }
        }

        public bool GetDoc(int IdDevice, IdDoc IdDoc, int id, int protocolNumber)
        {
            if (id == 0)
            {
                _logger.LogWarning($"Попытка отображения документа {IdDoc} с нулевым идентификатором");
                return false;
            }
            
            var doc = LoadDocsModule(IdDevice, IdDoc);
            string pathTemplateFile = doc.GetPathTemplateFile();
            
            if (string.IsNullOrEmpty(pathTemplateFile))
                return false;

            FR.Report.Load(pathTemplateFile);

            if (IdDoc == IdDoc.KMH_PP_Areom)
                FR.Report.SetParameterValue("JsonDoc", doc.GetViewDoc(id, protocolNumber));
            else if (IdDoc == IdDoc.KMH_PV)
                FR.Report.SetParameterValue("JsonDoc", doc.GetViewDoc(id, protocolNumber));
            else if (IdDoc == IdDoc.KMH_PW)
                FR.Report.SetParameterValue("JsonDoc", doc.GetViewDoc(id, protocolNumber));
            else if (IdDoc == IdDoc.Poverka2816)
                FR.Report.SetParameterValue("JsonDoc", doc.GetViewDoc(id, protocolNumber));
            else if (IdDoc == IdDoc.KMH_MI2816)
                FR.Report.SetParameterValue("JsonDoc", doc.GetViewDoc(id, protocolNumber));

            else
                FR.Report.SetParameterValue("JsonDoc", doc.GetViewDoc(id));

            FR.Report.Prepare();

            FastReport.Export.Pdf.PDFExport pdfExport = new FastReport.Export.Pdf.PDFExport();
            pdfExport.ShowProgress = false;
            pdfExport.Subject = "Subject";
            pdfExport.Title = " ";
            pdfExport.Compressed = true;
            pdfExport.AllowPrint = true;
            pdfExport.EmbeddingFonts = true;
            //pdfExport.HideMenubar = true;
            //pdfExport.HideToolbar = true;
            //pdfExport.HideWindowUI= true;
            
            FR.Report.Export(pdfExport, Directory.GetCurrentDirectory() + "/wwwroot/PDF/PDF.pdf");

            pdfExport.Dispose();
            FR.Report.Dispose();

            return true;     
        }

        public string GetDocEdit(int IdDevice, IdDoc IdDoc, int id)
        {
            if (id == 0)
            {
                _logger.LogWarning($"Попытка редактирования документа {IdDoc} с нулевым идентификатором");
                return string.Empty;
            }
            
            var doc = LoadDocsModule(IdDevice, IdDoc);
            return doc.GetEditDoc(id);
        }

        public string ExportDoc(int IdDevice, IdDoc IdDoc, int id, string format)
        {                
            var doc = LoadDocsModule(IdDevice, IdDoc);

            if (!Directory.Exists($"{_cfgApp.ExportDoc.Path}/{docCfg.Name}"))
            {
                Directory.CreateDirectory($"{_cfgApp.ExportDoc.Path}/{docCfg.Name}");
            }

            FR.Report.Load(doc.GetPathTemplateFile());

            var jsonDoc = doc.GetViewDoc(id);
            FR.Report.SetParameterValue("JsonDoc", jsonDoc);

            FR.Report.Prepare();         

            string path = $"{_cfgApp.ExportDoc.Path}/{docCfg.Name}/{JObject.Parse(doc.GetViewDoc(id).ToString())["Doc"]["Settings"]["General"]["FileNameForExportDoc"]}";

            if (format == "pdf")
                FR.Report.Export(new FastReport.Export.Pdf.PDFExport() { ShowProgress = false }, path += ".pdf");
            else if (format == "excel")
                FR.Report.Export(new FastReport.Export.OoXML.Excel2007Export() { ShowProgress = false }, path += ".xlsx");
            else if (format == "ods")
                FR.Report.Export(new FastReport.Export.Odf.ODSExport() { ShowProgress = false }, path += ".ods");
            else if (format == "xml")
                FR.Report.Export(new FastReport.Export.Xml.XMLExport() { ShowProgress = false }, path += ".xml");

            FR.Report.Dispose();

            return path;
        }

        public void SaveDoc(int IdDevice, IdDoc IdDoc, string data)
        {
            var doc = LoadDocsModule(IdDevice, IdDoc);
            doc.SaveDoc(data);
        }

        public PeriodDocument GetPeriodDocument(int IdDevice, IdDoc IdDoc, int id)
        {
            var doc = LoadDocsModule(IdDevice, IdDoc);
            doc.GetPeriodDocument(id);
            return doc.GetPeriodDocument(id);
        }

        public string GetListUsers()
        {
            return DocGeneral.JsonSerializeObject(DocGeneral.DictionarysDoc).ToString();
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
}