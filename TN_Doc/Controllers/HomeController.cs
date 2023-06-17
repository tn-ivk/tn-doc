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

using Newtonsoft.Json;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.Data;
using TN_Doc.Class;

using System.Drawing.Printing;
using TN.Doc;
using TN.DocData;


namespace TN_Doc.Controllers
{
    public class HomeController : Controller
    {
        private CfgApp CfgApp;
        private List<RequestListDocs> ReportIncompleteListDoc = new List<RequestListDocs>()
        {
            new RequestListDocs(){ Id = 1, DT = "Незавершенный", Description = "отчет за 2ч"},
            new RequestListDocs(){ Id = 2, DT = "Незавершенный", Description = "отчет за см"},
            new RequestListDocs(){ Id = 3, DT = "Незавершенный", Description = "отчет за сут"},
            new RequestListDocs(){ Id = 4, DT = "Незавершенный", Description = "отчет за м"},
            new RequestListDocs(){ Id = 5, DT = "Незавершенный", Description = "отчет за ТКО"}
        };

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
            _logger = logger;

            //dbIVK = context;
            //dbDoc = context;
            options = context;

            modelReport = new ModelReport();
            FR = new WebReport();

            PathToDocumentFile = Directory.GetCurrentDirectory();
            reportsPath = Directory.GetCurrentDirectory();

            InitApp();

            //InitDocReport();
            //InitDocPassport();
        }
        
        private void InitApp()
        {
            //Если документ задействован, добавляем в List
            //cfgFileRW.LoadCfg<DocReport>(Path.Combine(Directory.GetCurrentDirectory(), $"Cfg", $"CfgReport.json"), ref doc);
            //var doc = DOCS.Single<Root>(x => x.Doc.GUID == GUIDDOC.Report);

            cfgFileRW.LoadCfg<CfgApp>(Path.Combine(Directory.GetCurrentDirectory(), $"Cfg", $"CfgApp.json"), ref CfgApp);

            //LoadDocsModules(PathToDocumentFile);
        }

/*
        private void LoadDocsModules(string path)
        {
            LoadDocsModule(Directory.GetCurrentDirectory() + "/Dll" + "/Report.dll");
            LoadDocsModule(Directory.GetCurrentDirectory() + "/Dll" + "/Passport.dll");
            LoadDocsModule(Directory.GetCurrentDirectory() + "/Dll" + "/Act.dll");
            LoadDocsModule(Directory.GetCurrentDirectory() + "/Dll" + "/Jornal.dll");
            LoadDocsModule(Directory.GetCurrentDirectory() + "/Dll" + "/KMH_PP_Areom.dll");
            LoadDocsModule(Directory.GetCurrentDirectory() + "/Dll" + "/KMH_PW.dll");
            LoadDocsModule(Directory.GetCurrentDirectory() + "/Dll" + "/KMH_PV.dll");
            LoadDocsModule(Directory.GetCurrentDirectory() + "/Dll" + "/KMH_PP.dll");
            LoadDocsModule(Directory.GetCurrentDirectory() + "/Dll" + "/KMH_MI2816.dll");
            LoadDocsModule(Directory.GetCurrentDirectory() + "/Dll" + "/Poverka2816.dll");
            LoadDocsModule(Directory.GetCurrentDirectory() + "/Dll" + "/KMH_PR_PU.dll");
            LoadDocsModule(Directory.GetCurrentDirectory() + "/Dll" + "/KMH_PR_PR.dll");
            LoadDocsModule(Directory.GetCurrentDirectory() + "/Dll" + "/KMH_MPR_MPR.dll");
            LoadDocsModule(Directory.GetCurrentDirectory() + "/Dll" + "/Poverka3380.dll");
            LoadDocsModule(Directory.GetCurrentDirectory() + "/Dll" + "/Poverka3287.dll");
            LoadDocsModule(Directory.GetCurrentDirectory() + "/Dll" + "/Poverka3265_PR_PU.dll");            
        }
        private void LoadDocsModule(string path)
        {
            Assembly assembly = Assembly.LoadFrom(path);

            var docs = assembly.GetTypes().Where(x => x.BaseType.Name == "DocGeneral");

            TN.DocData.Device device = CfgApp.Devices.Single(x => x.Use && x.GuidDevice == IdDevice.IVK1);

            foreach (var item in docs)
            {
                Docs.Add((TN.Doc.DocGeneral)assembly.CreateInstance(item.FullName, false, BindingFlags.Default, null, new object[] { options, Path.Combine(Directory.GetCurrentDirectory()), device }, null, null));
            }
        }
*/

        private DocGeneral LoadDocsModule(int IdDevice, IdDoc idDoc)
        {
            deviceCfg = CfgApp.Devices.Single(x => x.IdDevice == IdDevice);
            docCfg = deviceCfg.Docs.Single(x => x.IdDoc == idDoc);

            Assembly assembly = Assembly.LoadFrom(Directory.GetCurrentDirectory() + docCfg.PathToDocDll);

            var doc = assembly.GetTypes().Single(x => x.BaseType.Name == "DocGeneral");

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
                                                   .Select(u => new ListItem() { Id = u.IdDevice, Name = u.Name}).ToList();

            string rez = System.Text.Json.JsonSerializer.Serialize(devices);

            return devices;
        }

        public string GetNameDBForDevice(int IdDevice)
        {
            var device = CfgApp.Devices.Single(x => x.IdDevice == IdDevice);
            return device.DBConnectionStrings.First(x=> x.Use).Database;
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

            cfgFileRW.SaveCfg(Path.Combine(Directory.GetCurrentDirectory(), $"Cfg"), $"/CfgApp.json", CfgApp);
        }
        public int GetIdTemplateDoc(int IdDevice, IdDoc IdDoc)
        {
            return CfgApp.Devices.Single(x => x.IdDevice == IdDevice)
                .Docs.Single(x => x.IdDoc == IdDoc)
                .LastUsedTemplateId;
        }
        public string GetPathTemplateDoc(int IdDevice, IdDoc IdDoc, int IdTemplateDoc)
        {
            return CfgApp.Devices.Single(x => x.IdDevice == IdDevice)
                            .Docs.Single(x => x.IdDoc == IdDoc)
                            .TemplateDocs.Single(x => x.Id == IdTemplateDoc).PathToDocTemplateFile;
        }

        public IActionResult Index()
        {
            try
            {
                //FastReport.Utils.Config.WebMode = false;
                FastReport.Utils.Config.EnableScriptSecurity = false;
                FR.EnableMargins = true;
                //modelReport.FR.ShowExports = true;
                FR.Mode = WebReportMode.Preview;
                FR.SinglePage = true;
                //modelReport.FR.SplitReportPagesInTabs = true;
                FR.Toolbar = new ToolbarSettings() 
                { 
                    Show = false, 
                    ShowRefreshButton = false, 
                    ShowFirstButton=false 
                };
                //modelReport.FR.ToolbarHeight = 300;

                FR.Width = "100%";
                FR.Height = "auto";
                //FR.Report.Load(Path.Combine(reportsPath, $"Report.frx"));
                FR.Report.Load(Path.Combine(Directory.GetCurrentDirectory(), $"01_Report_2022-05-05_Release_version.frx"));
                FR.Render();             
            }
            catch (Exception ex)
            {

            }

            //(string, string) tuple = new("string 1", "string 2");
            //return View(tuple);

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
            if (data.IdDoc == IdDoc.ReportIncomplete)
                return ReportIncompleteListDoc;

            List<ListDoc> docs = new List<ListDoc>();

            DateTime DTBegin = new();
            DateTime DTEnd = new();

            long UTBegin, UTEnd;

            if (data.DTBegin != null)
                DTBegin = DateTime.Parse(data.DTBegin);
            if (data.DTEnd != null)
                DTEnd = DateTime.Parse(data.DTEnd);

            UTBegin = new DateTimeOffset(DTBegin).ToUnixTimeSeconds();
            UTEnd = new DateTimeOffset(DTEnd).ToUnixTimeSeconds();

            var doc = LoadDocsModule(data.IdDevice, data.IdDoc);

            //var doc = Docs.Single(x => x.IdDoc == data.IdDoc);

            //DocGeneral.CurrentCfgDevice = CfgApp.Devices.Single(x => x.Use && x.GuidDevice == data.GuidDevice);
            //List <RequestListDocs> docsList = doc.GetList(UTBegin, UTEnd);
            //foreach (var item in docsList)
            //    item.DT = item.DT.Replace('.', '/');

            //return docsList;

            return doc.GetList(UTBegin, UTEnd);
        }

        public Microsoft.AspNetCore.Html.HtmlString GetDoc(int IdDevice, IdDoc IdDoc, int id, int protocolNumber)
        {
            var doc = LoadDocsModule(IdDevice, IdDoc);
            //var pathTemplateDoc = Directory.GetCurrentDirectory() +
            //    GetPathTemplateDoc(IdDevice, IdDoc, GetIdTemplateDoc(IdDevice, IdDoc));

            FR.Report.Load(doc.GetPathTemplateFile());

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

            //FR.EnableMargins = true;
            //FR.Mode = WebReportMode.Preview;
            //FR.SinglePage = false;
            //FR.Toolbar = new ToolbarSettings()
            //{
            //    Show = true,
            //    ShowPrevButton = true,
            //    ShowPrint = true,
            //    ShowOnDialogPage = true,
            //    ShowRefreshButton = false,
            //    ShowZoomButton = false,
            //    ShowFirstButton = false,
            //    ShowLastButton = true,
            //    ShowNextButton = true,

            //    Position = Positions.Left
            //};

            //FR.Width = "100%";
            ////FR.Height = "auto";
            //FR.Height = "100%";

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
            
            //MemoryStream strm = new MemoryStream();

            FR.Report.Export(pdfExport, Directory.GetCurrentDirectory() + "/wwwroot/PDF/PDF.pdf");

            pdfExport.Dispose();
            FR.Report.Dispose();
            
            Microsoft.AspNetCore.Html.HtmlString html = new Microsoft.AspNetCore.Html.HtmlString("");
            return html;// FR.Render().Result;        
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

        public string GetListUsers()
        {
            return DocGeneral.JsonSerializeObject(DocGeneral.DictionarysDoc).ToString();
        }

        public IActionResult Privacy()
        {
            return View();
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

    public class cfgFileRW
    {
        //Сохранить файл конфигурации
        public static bool SaveCfg(string path, string FileName, object st)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists) dirInfo.Create();

            string PathFileName = path + FileName;
            if (File.Exists(PathFileName)) File.Delete(PathFileName);
            File.WriteAllText(PathFileName, JsonConvert.SerializeObject(st), Encoding.UTF8);

            //using (StreamWriter file = File.CreateText(path))
            //{
            //    JsonSerializer ser = new JsonSerializer();
            //    ser.Serialize(file, st);
            //}

            return true;
        }

        //Загрузить файл конфигурации
        public static bool LoadCfg<T>(string path, ref T st)
        {
            if (File.Exists(path))
            {
                using (StreamReader file = new StreamReader(File.Open(path, FileMode.Open), Encoding.UTF8))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    st = (T)serializer.Deserialize(file, typeof(T));
                    return true;
                }
            }

            return false;
        }
    }
}

