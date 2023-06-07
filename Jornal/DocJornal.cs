using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using TN.DocData;

namespace TN.Doc
{
    public class DocJornal : DocGeneral
    {
        public DocJornal(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
        {
            IdDoc = IdDoc.Jornal;
            PathToDocConfigFile = GetPathConfigFile();
            PathToDocEditConfigFile = GetPathEditConfigFile();
            PathToDocTemplateFile = GetPathTemplateFile();
        }

        private DbSet<TableMeasurementJornalList> ListDoc { get; set; }
        private DbSet<TableMeasurementJornalData> DataDoc { get; set; }

        public override List<RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<RequestListDocs> docs = new();

            var year = UnixTimestampToDatetime(UTBegin).Year;
            var month = UnixTimestampToDatetime(UTBegin).Month;

            var list = (from item in ListDoc
                        where item.Year == year && item.Month == month
                        select item).ToList<TableMeasurementJornalList>();

            if (list.Count != 0)
            {
                foreach (var item in list)
                {
                    docs.Add(new RequestListDocs()
                    {
                        Id = item.id,
                        DT = new DateTime(item.Year, item.Month, item.Day).ToString("dd.MM.yy"),
                        Description = "Журнал СИ"
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

            ((DataIVKDoc)doc.Doc.DataIVK).TableMeasurementJornal.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TableMeasurementJornal.Day = list.Day;
            ((DataIVKDoc)doc.Doc.DataIVK).TableMeasurementJornal.Month = list.Month;
            ((DataIVKDoc)doc.Doc.DataIVK).TableMeasurementJornal.Year = list.Year;
            ((DataIVKDoc)doc.Doc.DataIVK).TableMeasurementJornal.BIK_ID = list.BIK_ID;
            ((DataIVKDoc)doc.Doc.DataIVK).TableMeasurementJornal.AdditionalInfo = JsonDeserializeObject<Additional>(ArrByteToString(list.Data.Additional));
            ((DataIVKDoc)doc.Doc.DataIVK).TableMeasurementJornal.Data = JsonDeserializeObject<Data>(ArrByteToString(list.Data.Data));
            ((DataIVKDoc)doc.Doc.DataIVK).TableMeasurementJornal.DataARM = JsonDeserializeObject<DataARM>(ArrByteToString(list.Data.DataARM));

            return JsonSerializeObject<Doc>(doc);
        }

        public override string GetEditDoc(int id)
        {
            return "";
        }

    }

    [Table("TableMeasurementJornal")]
    public class TableMeasurementJornalList
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int BIK_ID { get; set; }
        public TableMeasurementJornalData Data { get; set; }
    }

    [Table("TableMeasurementJornal")]
    public class TableMeasurementJornalData
    {
        [Key]
        public int id { get; set; }
        public byte[] Additional { get; set; }
        public byte[] Data { get; set; }
        public string DataARM { get; set; }
    }

    public class Doc : Root
    {
        public Doc()
        {
            Doc = new();

            Doc.Settings = new Settings
            {
                Header = new HeaderDoc(),
                Data = new DataDoc(),
                Footer = new FooterDoc(),
                Dictionarys = new DictionarysDoc()
            };
            Doc.DataIVK = new DataIVKDoc();
            ((DataIVKDoc)Doc.DataIVK).TableMeasurementJornal = new TableMeasurementJornal();
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
    public class DataDoc : TN.DocData.Data
    {

    }
    #endregion

    #region Doc.Settings.Footer
    public class FooterDoc : Footer
    {

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
        public TableMeasurementJornal TableMeasurementJornal { get; set; }
    }
    public class TableMeasurementJornal
    {
        public int id { get; set; }
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int BIK_ID { get; set; }
        public Additional AdditionalInfo { get; set; }
        public Data Data { get; set; }
        public DataARM DataARM { get; set; }
    }
    public class Additional
    {
        public string SIKN_Name { get; set; }
        public string Factory { get; set; }
        public int ShiftNum { get; set; }
    }
    public class Data
    {
        public List<Rows> Rows { get; set; }
        public List<string> Shift { get; set; }
        public string Day { get; set; }
    }
    public class Rows
    {
        public int Used { get; set; }
        public string strBegin { get; set; }
        public string strEnd { get; set; }
        public List<BIK> BIK { get; set; }
        public List<Line> Line { get; set; }
        public List<SIKN> SIKN { get; set; }
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
    public class DataARM
    {
        public string DeliveryFIO1 { get; set; }
        public string DeliveryFIO2 { get; set; }
        public string ReceiveFIO1 { get; set; }
        public string ReceiveFIO2 { get; set; }
    }
    #endregion


}
