using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using TN_Doc.Models;
using TN_Doc.Models.Home;
using FastReport.Web;
using System.IO;
using System.Threading;
using System.Text;
using System.Collections;
using Newtonsoft.Json.Linq;
using TN.Doc;
using TN.DocData;
using TN.Utils.Helpers;


namespace TN_Doc.Controllers
{
    public class HomeController : Controller
    {
        private CfgApp CfgApp;

        string PathToDocumentFile = "";

        private readonly ILogger<HomeController> _logger;
        private Microsoft.EntityFrameworkCore.DbContextOptions<DocGeneral> options;

        /// <summary>
        /// Docs содержит экземпляры классов для работы с документами
        /// </summary>
        private List<DocGeneral> Docs = new List<DocGeneral>();

        private DocGeneral dbDoc;

        private ModelReport modelReport;

        private WebReport FR;

        string reportsPath = "";

        CancellationToken stoppingToken;

        Device deviceCfg;
        Document docCfg;

        public HomeController(ILogger<HomeController> logger, Microsoft.EntityFrameworkCore.DbContextOptions<DocGeneral> context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            options = context;

            modelReport = new ModelReport();
            FR = new WebReport();

            PathToDocumentFile = Directory.GetCurrentDirectory();
            reportsPath = Directory.GetCurrentDirectory();

            InitApp();
        }

        private void InitApp()
        {
            CfgFileRW.LoadCfg<CfgApp>(Path.Combine(Directory.GetCurrentDirectory(), $"Cfg", $"CfgApp.json"), ref CfgApp);
        }

        private DocGeneral LoadDocsModule(int IdDevice, IdDoc idDoc)
        {
            deviceCfg = CfgApp.Devices.Single(x => x.IdDevice == IdDevice);
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

        public class ListItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        public List<ListItem> GetListDevices()
        {
            List<ListItem> devices = CfgApp.Devices.Where(x => x.Use)
                .Select(u => new ListItem() { Id = u.IdDevice, Name = u.Name }).ToList();

            string rez = System.Text.Json.JsonSerializer.Serialize(devices);

            return devices;
        }

        public string GetNameDBForDevice(int IdDevice)
        {
            var device = CfgApp.Devices.Single(x => x.IdDevice == IdDevice);
            return device.DBConnectionStrings.First(x => x.Use).Database;
        }

        public List<ListItem> GetListDocs(int IdDevice)
        {
            var device = CfgApp.Devices.Single(x => x.IdDevice == IdDevice);
            List<ListItem> list = device.Docs.Where(x => x.Use)
                .Select(u => new ListItem() { Id = (int)u.IdDoc, Name = u.Name }).ToList();
            return list;
        }

        public List<ListItem> GetTemplatesDoc(int IdDevice, IdDoc idDoc)
        {
            var device = CfgApp.Devices.Single(x => x.IdDevice == IdDevice);
            var doc = device.Docs.Single(x => x.IdDoc == idDoc);

            return doc.TemplateDocs.Where(x => x.Use).Select(x => new ListItem() { Id = x.Id, Name = x.Name }).ToList();
        }

        public List<ListItem> GetListProtocolNumber(int IdDevice, IdDoc idDoc)
        {
            //var device = CfgApp.Devices.Single(x => x.IdDevice == IdDevice);
            //var doc = device.Docs.Single(x => x.IdDoc == idDoc);

            List<ListItem> list = new List<ListItem>()
            {
                new ListItem() { Id = 1, Name = "Протокол 1" },
                new ListItem() { Id = 2, Name = "Протокол 2" }
            };

            return list;
        }

        public void SetIdTemplateDoc(int IdDevice, IdDoc IdDoc, int IdTemplateDoc)
        {
            CfgApp.Devices.Single(x => x.IdDevice == IdDevice)
                .Docs.Single(x => x.IdDoc == IdDoc)
                .LastUsedTemplateId = IdTemplateDoc;

            CfgFileRW.SaveCfg(Path.Combine(Directory.GetCurrentDirectory(), $"Cfg"), $"/CfgApp.json", CfgApp);
        }
        public int GetIdTemplateDoc(int IdDevice, IdDoc IdDoc)
        {
            int lastUsedTemplateId = CfgApp.Devices.Single(x => x.IdDevice == IdDevice)
                .Docs.Single(x => x.IdDoc == IdDoc)
                .LastUsedTemplateId;

            var usedTemplateDocs = CfgApp.Devices.Single(x => x.IdDevice == IdDevice)
                .Docs.Single(x => x.IdDoc == IdDoc)
                .TemplateDocs
                .Where(x => x.Use);

            if (usedTemplateDocs.Where(x => x.Id == lastUsedTemplateId).Count() > 0)
                return lastUsedTemplateId;
            else
            {
                int id = usedTemplateDocs.First().Id;
                SetIdTemplateDoc(IdDevice, IdDoc, id);
                return id;
            }
        }
        public string GetPathTemplateDoc(int IdDevice, IdDoc IdDoc, int IdTemplateDoc)
        {
            return CfgApp.Devices.Single(x => x.IdDevice == IdDevice)
                            .Docs.Single(x => x.IdDoc == IdDoc)
                            .TemplateDocs.Single(x => x.Id == IdTemplateDoc).PathToDocTemplateFile;
        }


        public bool IsUsedSecurity() => CfgApp.UseSecurityFeatures;


        /// <summary>
        /// Проверяем использовать ЕЛИС или нет.
        /// </summary>
        /// <param name="IdDevice"></param>
        /// <returns></returns>
        public bool IsUsedElis(int IdDevice)
        {
            var device = CfgApp.Devices.Single(x => x.IdDevice == IdDevice);

            if (device.Elis == null)
                if (CfgApp.Elis == null) return false;
                else return CfgApp.Elis.Use;
            else return device.Elis.Use;
        }

        /// <summary>
        /// Получить данные для регистрации устройства в ЕЛИС.
        /// </summary>
        /// <param name="IdDevice"></param>
        /// <returns></returns>
        
        public Dictionary<string, string> GetDataForRegistrationDeviceInELIS(int IdDevice)
        {
            //Dictionary<string, string> retData;
            
            var device = CfgApp.Devices.Single(x => x.IdDevice == IdDevice);

            if (device.Elis == null)
                if (CfgApp.Elis == null)
                {                    
                }
                else 
                    return new Dictionary<string, string>()
                    {
                        { "ostKey", CfgApp.Elis.OstKey },
                        { "siknKey", CfgApp.Elis.SiknKey },
                        { "clientName", CfgApp.Elis.ClientName }
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
            var device = CfgApp.Devices.Single(x => x.IdDevice == IdDevice);

            if (device.Elis == null)
                if (CfgApp.Elis == null) clientToken = string.Empty;
                else clientToken = CfgApp.Elis.ClientToken;
            else clientToken = device.Elis.ClientToken;

            return String.IsNullOrEmpty(clientToken)
                ? new Dictionary<string, string>() { { "clientToken", null } }
                : new Dictionary<string, string>() { { "clientToken", clientToken } };
        }

        public bool SetClientToken(int IdDevice, string clientToken)
        {
            var device = CfgApp.Devices.Single(x => x.IdDevice == IdDevice);

            if (device.Elis == null)
                if (CfgApp.Elis == null)
                    return false;
                else 
                    CfgApp.Elis.ClientToken = clientToken;
            else 
                device.Elis.ClientToken = clientToken;

            CfgFileRW.SaveCfg(Path.Combine(Directory.GetCurrentDirectory(), $"Cfg"), $"/CfgApp.json", CfgApp);

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
            
            return View(CfgApp);
        }

        public class data
        {
            public int IdDevice { get; set; }
            public IdDoc IdDoc { get; set; }
            public string DTBegin { get; set; }
            public string DTEnd { get; set; }
        }
        public class ListDoc
        {
            public int Id { get; set; }
            public string DT { get; set; }
            public string Description { get; set; }
        }

        public List<RequestListDocs> GetList(data data)
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
            else
                return doc.GetList(UTBegin, UTEnd);
        }

        public bool GetDoc(int IdDevice, IdDoc IdDoc, int id, int protocolNumber)
        {
            var doc = LoadDocsModule(IdDevice, IdDoc);
            //var pathTemplateDoc = Directory.GetCurrentDirectory() +
            //    GetPathTemplateDoc(IdDevice, IdDoc, GetIdTemplateDoc(IdDevice, IdDoc));

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
            var doc = LoadDocsModule(IdDevice, IdDoc);
            //var doc = Docs.Single(x => x.IdDoc == IdDoc);                       

            return doc.GetEditDoc(id);
        }

        public string ExportDoc(int IdDevice, IdDoc IdDoc, int id, string format)
        {                
            var doc = LoadDocsModule(IdDevice, IdDoc);

            if (!System.IO.Directory.Exists($"{CfgApp.ExportDoc.Path}/{docCfg.Name}"))
            {
                System.IO.Directory.CreateDirectory($"{CfgApp.ExportDoc.Path}/{docCfg.Name}");
            }

            FR.Report.Load(doc.GetPathTemplateFile());

            var jsonDoc = doc.GetViewDoc(id);
            FR.Report.SetParameterValue("JsonDoc", jsonDoc);

            FR.Report.Prepare();         

            string path = $"{CfgApp.ExportDoc.Path}/{docCfg.Name}/{JObject.Parse(doc.GetViewDoc(id).ToString())["Doc"]["Settings"]["General"]["FileNameForExportDoc"]}";

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

        // public IActionResult Privacy()
        // {
        //     return View();
        // }

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