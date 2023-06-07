using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using TN.DocData;

namespace TN.Doc
{
    public class DocReport: DocGeneral
    {
        public DocReport(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
        {
            IdDoc = IdDoc.Report;
            PathToDocConfigFile = GetPathConfigFile();
            PathToDocEditConfigFile = GetPathEditConfigFile();
            PathToDocTemplateFile = GetPathTemplateFile();
        }

        private DbSet<TableReportList> ListDoc { get; set; }
        private DbSet<TableReportData> DataDoc { get; set; }
        
        private List<ReportType> ReportType { get; set; } = new List<ReportType>()
        {
            new ReportType() { Id = 1, Name = "Отчет за два часа", ShortName = "Отчет за 2ч" },
            new ReportType() { Id = 2, Name = "Отчет за смену", ShortName = "Отчет за см" },
            new ReportType() { Id = 3, Name = "Отчет за сутки", ShortName = "Отчет за сут" },
            new ReportType() { Id = 4, Name = "Отчет за месяц", ShortName = "Отчет за м" },
            new ReportType() { Id = 5, Name = "Отчет за время ведения ТКО", ShortName = "Отчет за ТКО" }
        };

        public override List<RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<RequestListDocs> docs = new();

            //var zzz = ListDoc.FromSqlRaw($"SELECT * FROM TableReport");

            var list = (from item in ListDoc
                        where item.End > UTBegin && item.End < UTEnd
                        select item).ToList();

            if (list.Count != 0)
            {
                foreach (var item in list)
                {
                    docs.Add(new RequestListDocs()
                    {
                        Id = item.id,
                        //DT = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(item.End).ToString("dd.MM.yy HH:mm"),
                        DT = item.strEnd,
                        Description = ReportType.Where(x => x.Id == item.ReportType).First().Name
                    });
                }
            }

            return docs;
        }
        public override object GetViewDoc(int id)
        {
            var list = (from item in ListDoc
                        where item.id == id
                        select item).First();

            list.Data = (from item in DataDoc
                         where item.id == id
                         select item).First();

            Doc doc = new();

            LoadCfg(PathToDocConfigFile, ref doc);

            ((DataIVKDoc)doc.Doc.DataIVK).TableReport.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TableReport.strBegin = list.strBegin;
            ((DataIVKDoc)doc.Doc.DataIVK).TableReport.Begin = list.Begin;
            ((DataIVKDoc)doc.Doc.DataIVK).TableReport.strEnd = list.strEnd;
            ((DataIVKDoc)doc.Doc.DataIVK).TableReport.End = list.End;
            ((DataIVKDoc)doc.Doc.DataIVK).TableReport.ReportType = list.ReportType;
            ((DataIVKDoc)doc.Doc.DataIVK).TableReport.ReportPeriod = list.ReportPeriod;
            ((DataIVKDoc)doc.Doc.DataIVK).TableReport.BIK_ID = list.BIK_ID;
            ((DataIVKDoc)doc.Doc.DataIVK).TableReport.Report = JsonDeserializeObject<Report>(ArrByteToString(list.Data.Report));

            ((HeaderDoc)doc.Doc.Settings.Header).NameIVK = CurrentCfgDevice.Name;

            JObject ReportRaw = JObject.Parse(ArrByteToString(list.Data.ReportRaw));

            ((DataIVKDoc)doc.Doc.DataIVK).TableReport.Report.Obj.BikIllegalTime = JsonDeserializeObject<BikIllegalTime>(ReportRaw.SelectToken("BIK.BIKIllegalTime").ToString());

            ((DataIVKDoc)doc.Doc.DataIVK).TableReport.Report.Obj.LineIllegalTime = new List<LineIllegalTime>();
            foreach (var item in ReportRaw["Line"])
            {
                ((DataIVKDoc)doc.Doc.DataIVK).TableReport.Report.Obj.LineIllegalTime.Add(JsonDeserializeObject<LineIllegalTime>(item.SelectToken("LineIllegalTime").ToString()));
            }

            return JsonSerializeObject<Doc>(doc);
        }
        public override string GetEditDoc(int id)
        {
            GetViewDoc(id);

            //TN.Doc.Edit.CfgEditPassport editDoc = new();

            //LoadCfg(PathToDocEditConfigFile, ref editDoc);

            var doc = new HtmlDocument();
            doc.Load(PathToRootDirectory + $"/wwwroot/HTML/DocEdit.html");

            return "";
        }
    }

    public struct ReportType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
    }

    [Table("TableReport")]
    public class TableReportList
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public string strBegin { get; set; }
        public int Begin { get; set; }
        public string strEnd { get; set; }
        public int End { get; set; }
        public int ReportType { get; set; }
        public int ReportPeriod { get; set; }
        public int BIK_ID { get; set; }

        [NotMapped]
        public int DIR_ID { get; set; }

        public TableReportData Data { get; set; }
    }
    
    [Table("TableReport")]
    public class TableReportData
    {
        [Key]
        public int id { get; set; }
        public byte[] Report { get; set; }
        public byte[] ReportRaw { get; set; }

        [NotMapped]
        public string DataARM { get; set; }
    }

    public class Doc : Root
    {
        public Doc()
        {
            Doc = new()
            {
                Settings = new Settings()
                {
                    Header = new HeaderDoc(),
                    Data = new DataDoc(),
                    Footer = new FooterDoc(),
                    Dictionarys = new DictionarysDoc()
                },
                DataIVK = new DataIVKDoc()
                {
                    TableReport = new TableReport()
                }
            };
        }
    }

    #region Doc.Settings

    #region Doc.Settings.Header
    public class HeaderDoc : Header
    {
        public string Prefix_SIKN_Name { get; set; }
        public string NameIVK { get; set; }
    }

    #endregion

    #region Doc.Settings.Data
    public class DataDoc : Data
    {
        public TableBIK TableBIK { get; set; }
        public TableLine TableLine { get; set; }
    }
    public class TableBIK
    {
        public bool Visible { get; set; }
        public List<Parameters> Parameters { get; set; }
    }
    public class TableLine
    {
        public bool Visible { get; set; }
        public int ShowNumberColumns { get; set; }
        public double ColumnsWidth { get; set; }
        public List<Parameters> Parameters { get; set; }
        public ColumnSIKN ColumnSIKN { get; set; }
    }
    public class ColumnSIKN
    {
        public bool Visible { get; set; }
        public string Name { get; set; }
        public double Width { get; set; }
    }
    public class Parameters
    {
        public bool Visible { get; set; }
        public string Name { get; set; }
        public string SI { get; set; }
        public string Key { get; set; }
    }
    #endregion

    #region Doc.Settings.Footer
    public class FooterDoc : Footer
    {
        public List<Signers> Signers { get; set; }
    }
    public class Signers
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<ShowSigner> ShowSigner { get; set; }
    }
    public class ShowSigner
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Visible { get; set; }
    }
    #endregion

    #region Doc.Settings.Dictionarys
    public class DictionarysDoc : Dictionarys
    {

    }
    #endregion

    #endregion

    #region Doc.DataIVK
    public class DataIVKDoc : DataIVK
    {
        public TableReport TableReport { get; set; }
    }
    public class TableReport
    {
        public int id { get; set; }
        public string strBegin { get; set; }
        public int Begin { get; set; }
        public string strEnd { get; set; }
        public int End { get; set; }
        public int ReportType { get; set; }
        public int ReportPeriod { get; set; }
        public int BIK_ID { get; set; }
        public Report Report { get; set; }
    }
    public class Report
    {
        public Obj Obj { get; set; }
    }
    public class Obj
    {
        public List<Additional> Additional { get; set; }
        public List<BIK> BIK { get; set; }
        public List<Line> Line { get; set; }
        public List<SIKN> SIKN { get; set; }
        public BikIllegalTime BikIllegalTime { get; set; }
        public List<LineIllegalTime> LineIllegalTime { get; set; }
    }
    public class Additional
    {
        public string SIKN_Name { get; set; }
        public string Factory { get; set; }
        public int NumOfUsedDens { get; set; }
        public string TankerName { get; set; }
        public string ExtendAddInfo { get; set; }
    }
    public class BIK
    {
        public string Dens { get; set; }
        public string Dens15 { get; set; }
        public string Dens20 { get; set; }
        public string DynamicVisc { get; set; }
        public string KinemVisc { get; set; }
        public string Flow { get; set; }
        public string VolWater { get; set; }
        public string MasWater { get; set; }
        public string MasSulphur { get; set; }
        public string DensTemp { get; set; }
        public string DensPress { get; set; }
        public string Oil_Type { get; set; }
        public string ViscTemp { get; set; }
    }
    public class Line
    {
        public int Used { get; set; }
        public string Name { get; set; }
        public string Dens { get; set; }
        public string MassFlow { get; set; }
        public string VolFlow { get; set; }
        public string Vol { get; set; }
        public string Vol20 { get; set; }
        public string Vol15 { get; set; }
        public string Mass { get; set; }
        public string Temp { get; set; }
        public string Press { get; set; }
        public string BeginVol { get; set; }
        public string EndVol { get; set; }
        public string BeginVol15 { get; set; }
        public string EndVol15 { get; set; }
        public string BeginVol20 { get; set; }
        public string EndVol20 { get; set; }
        public string BeginMass { get; set; }
        public string EndMass { get; set; }
        public string BeginVolTotal { get; set; }
        public string EndVolTotal { get; set; }
        public string BeginVol15Total { get; set; }
        public string EndVol15Total { get; set; }
        public string BeginVol20Total { get; set; }
        public string EndVol20Total { get; set; }
        public string BeginMassTotal { get; set; }
        public string EndMassTotal { get; set; }
        public string CheckDeltaVol { get; set; }
        public string CheckDeltaVol20 { get; set; }
        public string CheckDeltaVol15 { get; set; }
        public string CheckDeltaMass { get; set; }
        public string VolTotal { get; set; }
        public string Vol20Total { get; set; }
        public string Vol15Total { get; set; }
        public string MassTotal { get; set; }
        public string Vol15Flow { get; set; }
        public string Vol20Flow { get; set; }
    }
    public class SIKN
    {
        public string Dens { get; set; }
        public string MassFlow { get; set; }
        public string VolFlow { get; set; }
        public string Vol { get; set; }
        public string Vol20 { get; set; }
        public string Vol15 { get; set; }
        public string Mass { get; set; }
        public string Temp { get; set; }
        public string Press { get; set; }
        public string BeginVol { get; set; }
        public string EndVol { get; set; }
        public string BeginVol15 { get; set; }
        public string EndVol15 { get; set; }
        public string BeginVol20 { get; set; }
        public string EndVol20 { get; set; }
        public string BeginMass { get; set; }
        public string EndMass { get; set; }
        public string BeginVolTotal { get; set; }
        public string EndVolTotal { get; set; }
        public string BeginVol15Total { get; set; }
        public string EndVol15Total { get; set; }
        public string BeginVol20Total { get; set; }
        public string EndVol20Total { get; set; }
        public string BeginMassTotal { get; set; }
        public string EndMassTotal { get; set; }
        public string CheckDeltaVol { get; set; }
        public string CheckDeltaVol20 { get; set; }
        public string CheckDeltaVol15 { get; set; }
        public string CheckDeltaMass { get; set; }
        public string VolTotal { get; set; }
        public string Vol20Total { get; set; }
        public string Vol15Total { get; set; }
        public string MassTotal { get; set; }
        public string Vol15Flow { get; set; }
        public string Vol20Flow { get; set; }
    }
    public class BikIllegalTime
    {
        public long Dens { get; set; }
        public long Dens15 { get; set; }
        public long Dens20 { get; set; }
        public long DensPress { get; set; }
        public long DensTemp { get; set; }
        public long KinemVisc { get; set; }
        public long DynamicVisc { get; set; }
        public long ViscTemp { get; set; }
        public long ViscPress { get; set; }
        public long MasSulphur { get; set; }
        public long VolWater { get; set; }
        public long WaterTemp { get; set; }
        public long WaterPress { get; set; }
        public long MasWater { get; set; }
        public long Flow { get; set; }
    }
    public class LineIllegalTime
    {
        public long Temp { get; set; }
        public long Press { get; set; }
        public long Dens { get; set; }
        public long VolFlow { get; set; }
        public long Vol15Flow { get; set; }
        public long Vol20Flow { get; set; }
        public long MassFlow { get; set; }
        public long Dens15 { get; set; }
        public long Dens20 { get; set; }
        public long Vol { get; set; }
        public long Vol15 { get; set; }
        public long Vol20 { get; set; }
        public long Mass { get; set; }
    }
    #endregion
}

namespace TN.Doc.Edit
{
    public class CfgEditReport
    {
        public List<DataARM> AdditionalInfo = new();
    }

    public class DataARM
    {
        public bool Use { get; set; }
        public string Key { set; get; }
        public string Type { set; get; }
        public string Name { set; get; }
    }
}