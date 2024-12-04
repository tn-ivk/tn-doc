using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using TN.DocData;

namespace TN.Doc
{
    public class PoverkaSikn425_PR_PU : DocGeneral
    {
        public PoverkaSikn425_PR_PU(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
        {
            IdDoc = IdDoc.PoverkaSikn425_PR_PU;
            PathToDocConfigFile = GetPathConfigFile();
            PathToDocEditConfigFile = GetPathEditConfigFile();
            PathToDocTemplateFile = GetPathTemplateFile();
        }
        
        private DbSet<TablePoverkaSikn425_PR_PUList> ListDoc { get; set; }
        private DbSet<TablePoverkaSikn425_PR_PUData> DataDoc { get; set; }

        private Doc Doc;
        private JObject jsonDoc;
        
        public override List<RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<RequestListDocs> docs = new();

            var list = (from item in ListDoc.AsNoTracking()
                        where item.DateTimeLong > UTBegin && item.DateTimeLong < UTEnd && item.id > 1
                        select item).ToList<TablePoverkaSikn425_PR_PUList>();

            if (list.Count != 0)
            {
                foreach (var item in list)
                {
                    docs.Add(new()
                    {
                        Id = item.id,
                        DT = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(item.DateTimeLong).ToString("dd.MM.yy HH:mm"),
                        Description = $"{ArrByteToString(item.IlName)}"
                    });
                }
            }

            return docs;
        }

        public override object GetViewDoc(int id)
        {
            var list = (from item in ListDoc.AsNoTracking()
                        where item.id == id
                        select item).First();

            list.Data = (from item in DataDoc.AsNoTracking()
                         where item.id == id
                         select item).First();

            Doc doc = new();

            LoadCfg(PathToDocConfigFile, ref doc);

            doc.Doc.Settings.General.NefType = CfgGeneral.Doc.Settings.General.NefType;
            doc.Doc.Settings.General.ObjType = CfgGeneral.Doc.Settings.General.ObjType;
            doc.Doc.Settings.Dictionarys.UsersGroup = CfgGeneral.Doc.Settings.Dictionarys.UsersGroup;
            doc.Doc.Settings.Dictionarys.Users = CfgGeneral.Doc.Settings.Dictionarys.Users;
            doc.Doc.Settings.Dictionarys.Licenses = CfgGeneral.Doc.Settings.Dictionarys.Licenses;

            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.Il = list.Il;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.RsuLineIndx = list.RsuLineIndx;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.Protokol = JsonDeserializeObject<Protokol>(ArrByteToString(list.Data.Protokol));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo)) ?? new AdditionalInfo()
            {
                PlacePsp = ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.Protokol.PlacePsp,
                PlaceSikn = ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.Protokol.PlaceSikn,
                PlaceFactory = ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.Protokol.PlaceFactory,
                OilType = ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.Protokol.OilType,
                MprInfo = ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.Protokol.MprInfo,
                PuInfo = ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.Protokol.PuInfo,
                PpInfo = ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.Protokol.PpInfo,
                IvkInfo = ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.Protokol.IvkInfo,
            };
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.DateTimeString = list.DateTimeString;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.DateTimeLong = list.DateTimeLong;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.SiknId = list.SiknId;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.DirId = list.DirId;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.IlName = ArrByteToString(list.IlName);

            string fileName = string.Format("{0}_Поверка СИКН 425 МПР по ПУ",
                ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.DateTimeString.Replace(":", "."));
            doc.Doc.Settings.General.FileNameForExportDoc = $"{fileName}";


            jsonDoc = JObject.Parse(JsonSerializeObject<Doc>(doc).ToString());
            Doc = doc;

            return JsonSerializeObject<Doc>(doc);
        }

        public override string GetEditDoc(int id)
        {
            GetViewDoc(id);

            CfgEdit editDoc = new();

            LoadCfg(PathToDocEditConfigFile, ref editDoc);

            var doc = new HtmlDocument();
            doc.Load(PathToRootDirectory + $"/wwwroot/HTML/DocEdit.html");

            HtmlNode node = doc.GetElementbyId("AdditionalInfo").ChildNodes["tbody"];

            foreach (var item in editDoc.AdditionalInfo.Where(x => x.Use).ToList())
            {
                string currentValue = "";

                HtmlNode TableRow = HtmlNode.CreateNode("<tr></tr>");
                HtmlNode TableData = HtmlNode.CreateNode("<td></td>");

                TableRow.AppendChild(HtmlNode.CreateNode($"<td>{item.Name}</td>"));

                HtmlNode Input = HtmlNode.CreateNode("<input></input>");

                if (item.Key == "Place_PSP")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo.PlacePsp;
                else if (item.Key == "Place_SIKN")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo.PlaceSikn;
                else if (item.Key == "Place_Factory")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo.PlaceFactory;
                else if (item.Key == "PR_Type")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo.PR_Type;
                else if (item.Key == "PR_FactoryNumber")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo.PR_FactoryNumber;
                else if (item.Key == "PR_LineName")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo.PR_LineName;
                else if (item.Key == "MM_Sensor_Type")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo.MM_Sensor_Type;
                else if (item.Key == "MM_Sensor_DU")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo.MM_Sensor_DU;
                else if (item.Key == "MM_Sensor_FactoryNumber")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo.MM_Sensor_FactoryNumber;
                else if (item.Key == "MM_PEP")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo.MM_PEP;
                else if (item.Key == "MM_PEP_FactoryNumber")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo.MM_PEP_FactoryNumber;
                else if (item.Key == "PU_Type")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo.PU_Type;
                else if (item.Key == "PU_FactoryNumber")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo.PU_FactoryNumber;
                else if (item.Key == "PP_Type")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo.PP_Type;
                else if (item.Key == "PP_FactoryNumber")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo.PP_FactoryNumber;
                else if (item.Key == "IVK_Type")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo.IVK_Type;
                else if (item.Key == "IVK_FactoryNumber")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo.IVK_FactoryNumber;
                else if (item.Key == "Oil_Type")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo.OilType;
                else if (item.Key == "ProtokolNum")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo.ProtokolNum;
                else if (item.Key == "StaffData")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo.StaffData;

                Input.Id = item.Key;
                Input.Attributes.Add($"data-edit", $"1");
                Input.Attributes.Add($"data-key", $"{item.Key}");
                Input.Attributes.Add($"data-tag", $"AdditionalInfo");
                Input.Attributes.Add($"type", $"{item.Type}");
                Input.Attributes.Add($"value", $"{currentValue}");

                if (!item.Edit)
                    Input.Attributes.Add($"disabled", $"disabled");

                TableData.AppendChild(Input);

                TableRow.AppendChild(TableData);
                doc.GetElementbyId("AdditionalInfo").ChildNodes["tbody"].AppendChild(TableRow);
            }

            doc.Save(PathToRootDirectory + $"/wwwroot/HTML/html.html");

            return "";
        }

        public override bool SaveDoc(string jsonData)
        {
            CorrectionData data = JsonDeserializeObject<CorrectionData>(jsonData);
            AdditionalInfo ad = new();
            TablePoverkaSikn425_PR_PUData row = new();
            GetViewDoc(data.DocID);

            ad = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_PR_PU.AdditionalInfo;

            if (data.Values.Exists(x => x.Key == "Place_PSP"))
                ad.PlacePsp = data.Values.Where(x => x.Key == "Place_PSP").Single().Value;
            if (data.Values.Exists(x => x.Key == "Place_SIKN"))
                ad.PlaceSikn = data.Values.Where(x => x.Key == "Place_SIKN").Single().Value;
            if (data.Values.Exists(x => x.Key == "Place_Factory"))
                ad.PlaceFactory = data.Values.Where(x => x.Key == "Place_Factory").Single().Value;
            if (data.Values.Exists(x => x.Key == "PR_Type"))
                ad.PR_Type = data.Values.Where(x => x.Key == "PR_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "PR_FactoryNumber"))
                ad.PR_FactoryNumber = data.Values.Where(x => x.Key == "PR_FactoryNumber").Single().Value;
            if (data.Values.Exists(x => x.Key == "PR_LineName"))
                ad.PR_LineName = data.Values.Where(x => x.Key == "PR_LineName").Single().Value;
            if (data.Values.Exists(x => x.Key == "MM_Sensor_Type"))
                ad.MM_Sensor_Type = data.Values.Where(x => x.Key == "MM_Sensor_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "MM_Sensor_DU"))
                ad.MM_Sensor_DU = data.Values.Where(x => x.Key == "MM_Sensor_DU").Single().Value;
            if (data.Values.Exists(x => x.Key == "MM_Sensor_FactoryNumber"))
                ad.MM_Sensor_FactoryNumber = data.Values.Where(x => x.Key == "MM_Sensor_FactoryNumber").Single().Value;
            if (data.Values.Exists(x => x.Key == "MM_PEP"))
                ad.MM_PEP = data.Values.Where(x => x.Key == "MM_PEP").Single().Value;
            if (data.Values.Exists(x => x.Key == "MM_PEP_FactoryNumber"))
                ad.MM_PEP_FactoryNumber = data.Values.Where(x => x.Key == "MM_PEP_FactoryNumber").Single().Value;
            if (data.Values.Exists(x => x.Key == "PU_Type"))
                ad.PU_Type = data.Values.Where(x => x.Key == "PU_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "PU_FactoryNumber"))
                ad.PU_FactoryNumber = data.Values.Where(x => x.Key == "PU_FactoryNumber").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP_Type"))
                ad.PP_Type = data.Values.Where(x => x.Key == "PP_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP_FactoryNumber"))
                ad.PP_FactoryNumber = data.Values.Where(x => x.Key == "PP_FactoryNumber").Single().Value;
            if (data.Values.Exists(x => x.Key == "IVK_Type"))
                ad.IVK_Type = data.Values.Where(x => x.Key == "IVK_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "IVK_FactoryNumber"))
                ad.IVK_FactoryNumber = data.Values.Where(x => x.Key == "IVK_FactoryNumber").Single().Value;
            if (data.Values.Exists(x => x.Key == "Oil_Type"))
                ad.OilType = data.Values.Where(x => x.Key == "Oil_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "ProtokolNum"))
                ad.ProtokolNum = data.Values.Where(x => x.Key == "ProtokolNum").Single().Value;
            if (data.Values.Exists(x => x.Key == "StaffData"))
                ad.StaffData = data.Values.Where(x => x.Key == "StaffData").Single().Value;

            row.id = data.DocID;
            row.AdditionalInfo = StringToHexArrByte(ad);

            //DataDoc.Update(dd);

            this.Entry(row).Property(x => x.AdditionalInfo).IsModified = true;

            this.SaveChanges();

            return true;
        }

    }


    //[Table("TablePoverkaSikn425_PR_PU")]
    [Table("Msikn425MprByProver")]
    public class TablePoverkaSikn425_PR_PUList
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int Il { get; set; }
        public int RsuLineIndx { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public int SiknId { get; set; }
        public int DirId { get; set; }
        public byte[] IlName { get; set; }
        public TablePoverkaSikn425_PR_PUData Data { get; set; }
    }

    [Table("Msikn425MprByProver")]
    public class TablePoverkaSikn425_PR_PUData
    {
        [Key]
        public int id { get; set; }
        public byte[] Protokol { get; set; }
        public byte[] AdditionalInfo { get; set; }
    }


    public class Doc : Root
    {
        public Doc()
        {
            Doc = new();

            Doc.Settings = new Settings();

            Doc.Settings.Header = new HeaderDoc();
            Doc.Settings.Data = new DataDoc();
            Doc.Settings.Footer = new FooterDoc();
            Doc.Settings.Dictionarys = new DictionarysDoc();
            Doc.DataIVK = new DataIVKDoc();
            ((DataIVKDoc)Doc.DataIVK).TablePoverkaSikn425_PR_PU = new TablePoverkaSikn425_PR_PU();
        }
    }

    #region Doc.Settings

    #region Doc.Settings.Header

    public class HeaderDoc : Header
    {
        public string FieldSIKN { get; set; }
    }

    #endregion

    #region Doc.Settings.Data

    public class DataDoc : Data
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
        public TablePoverkaSikn425_PR_PU TablePoverkaSikn425_PR_PU { get; set; }
    }

    public class TablePoverkaSikn425_PR_PU
    {
        public int id { get; set; }
        public int Il { get; set; }
        public int RsuLineIndx { get; set; }
        public Protokol Protokol { get; set; }
        public AdditionalInfo AdditionalInfo { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public int SiknId { get; set; }
        public int DirId { get; set; }
        public string IlName { get; set; }
    }
    public class Protokol
    {
        public string PlacePsp { get; set; }
        public string PlaceSikn { get; set; }
        public string PlaceFactory { get; set; }
        public MprInfo MprInfo { get; set; }
        public DeviceInfo PuInfo { get; set; }
        public DeviceInfo PpInfo { get; set; }
        public DeviceInfo IvkInfo { get; set; }
        public string OilType { get; set; }
        public Table1 Table1 { get; set; }
        public List<Table2> Table2 { get; set; }
        public Table3 Table3 { get; set; }
    }
    
    public class MprInfo
    {
        public string LineName { get; set; }
        public DeviceInfo Sensor { get; set; }
        public DeviceInfo Pep { get; set; }
    }
    
    public class DeviceInfo
    {
        public string DevType { get; set; }
        public string DevNumb { get; set; }
    }
    
    public class Table1
    {
        public List<string> DetName { get; set; }
        public List<string> V0 { get; set; }
        public string D_V0 { get; set; }
        public string S_V0 { get; set; }
        public string E_V0 { get; set; }
        public string alpha_t { get; set; }
        public List<string> TetaSum0 { get; set; }
        public List<string> TetaV0 { get; set; }
        public string Delta_tPU { get; set; }
        public string Delta_tPP { get; set; }
        public string Delta_DensPP { get; set; }
        public string deltaIVK { get; set; }
        public string ZSk { get; set; }
    }
    public class Table2
    {
        public string ser { get; set; }
        public string row { get; set; }
        public string Q_jik { get; set; }
        public string Det { get; set; }
        public string T_jik { get; set; }
        public string t_PU_jik { get; set; }
        public string P_PU_jik { get; set; }
        public string dens_PP_jik { get; set; }
        public string t_PP_jik { get; set; }
        public string P_PP_jik { get; set; }
        public string beta_jik { get; set; }
        public string N_jik { get; set; }
        public string Mpu_jik { get; set; }
        public string Kpm_jik { get; set; }
        public string U_jik { get; set; }
        public string Uerror_jik { get; set; }
        public string dens15_jik { get; set; }
        public string alpha15_jik { get; set; }
        public string CTS_jik { get; set; }
        public string CPS_jik { get; set; }
        public string CTL_PU_jik { get; set; }
        public string CPL_PU_jik { get; set; }
        public string CTL_PP_jik { get; set; }
        public string CPL_PP_jik { get; set; }
    }
    public class Table3
    {
        public string Teta_t_k { get; set; }
        public string Teta_p_k { get; set; }
        public string delta_k { get; set; }
        public List<Points> Point { get; set; }
    }
    public class Points
    {
        public string ser { get; set; }
        public string Q_jk { get; set; }
        public string Kpm_jk { get; set; }
        public string S_jk { get; set; }
        public string S_error_jk { get; set; }
        public string n_jk { get; set; }
        public string S0_jk { get; set; }
        public string t095_jk { get; set; }
        public string e_jk { get; set; }
        public string Teta_Z_jk { get; set; }
        public string Teta_Sum_jk { get; set; }
        public string delta_jk { get; set; }
        public string SK_jk { get; set; }
        public string t_Sum_jk { get; set; }
        public string S_Teta_jk { get; set; }
        public string S_Sum_jk { get; set; }
    }
    public class AdditionalInfo
    {
        public string PlacePsp { get; set; }
        public string PlaceSikn { get; set; }
        public string PlaceFactory { get; set; }
        public MprInfo MprInfo { get; set; }
        public DeviceInfo PuInfo { get; set; }
        public DeviceInfo PpInfo { get; set; }
        public DeviceInfo IvkInfo { get; set; }
        public string PR_Type { get; set; }
        public string PR_FactoryNumber { get; set; }
        public string PR_LineName { get; set; }
        public string MM_Sensor_Type { get; set; }
        public string MM_Sensor_DU { get; set; }
        public string MM_Sensor_FactoryNumber { get; set; }
        public string MM_PEP { get; set; }
        public string MM_PEP_FactoryNumber { get; set; }
        public string PU_Type { get; set; }
        public string PU_FactoryNumber { get; set; }
        public string PP_Type { get; set; }
        public string PP_FactoryNumber { get; set; }
        public string IVK_Type { get; set; }
        public string IVK_FactoryNumber { get; set; }
        public string OilType { get; set; }
        public string ProtokolNum { get; set; }
        public string StaffData { get; set; }
    }

    #endregion

}