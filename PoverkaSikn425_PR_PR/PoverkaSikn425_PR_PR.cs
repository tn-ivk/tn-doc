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
    public class PoverkaSikn425_PR_PR : DocGeneral
    {
        public PoverkaSikn425_PR_PR(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
        {
            IdDoc = IdDoc.PoverkaSikn425_UPR_PR;
            PathToDocConfigFile = GetPathConfigFile();
            PathToDocEditConfigFile = GetPathEditConfigFile();
            PathToDocTemplateFile = GetPathTemplateFile();
        }
        
        private DbSet<TablePoverkaSikn425_UPR_PRList> ListDoc { get; set; }
        private DbSet<TablePoverkaSikn425_UPR_PRData> DataDoc { get; set; }

        private Doc Doc;
        private JObject jsonDoc;
        
        public override List<RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<RequestListDocs> docs = new();

            var list = (from item in ListDoc.AsNoTracking()
                where item.DateTimeLong > UTBegin && item.DateTimeLong < UTEnd && item.id > 1
                select item).ToList<TablePoverkaSikn425_UPR_PRList>();

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

                ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.id = list.id;
                ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.Il = list.Il;
                ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.Protokol = JsonDeserializeObject<Protokol>(ArrByteToString(list.Data.Protokol));
                ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo)) ?? new AdditionalInfo()
                {
                    PlacePsp = ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.Protokol.PlacePsp,
                    PlaceSikn = ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.Protokol.PlaceSikn,
                    PlaceFactory = ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.Protokol.PlaceFactory,
                    OilType = ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.Protokol.OilType,
                    MprRsuInfo = ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.Protokol.MprRsuInfo,
                    MprOsuInfo = ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.Protokol.MprOsuInfo,
                    IvkInfo = ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.Protokol.IvkInfo
                };;
                ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.DateTimeString = list.DateTimeString;
                ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.DateTimeLong = list.DateTimeLong;
                ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.SiknId = list.SiknId;
                ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.DirId = list.DirId;
                ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.IlName = ArrByteToString(list.IlName);

                string fileName = string.Format("{0}_Поверка СИКН 425 СРМ РСУ с помощью СРМ ОСУ",
                    ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.DateTimeString.Replace(":", "."));
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

                if (item.Type == "list")
                {
                    HtmlNode Select = HtmlNode.CreateNode("<select></select>");
                    List<string> ListResult = new List<string>() { "", "годен", "не годен" };

                    Select.Attributes.Add($"name", $"{item.Key}");
                    Select.Attributes.Add($"data-edit", $"1");
                    Select.Attributes.Add($"data-key", $"{item.Key}");
                    Select.Attributes.Add($"data-tag", $"AdditionalInfo");

                    if (item.Key == "ARM_Poverka_Result")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.ARM_Poverka_Result;

                    foreach (var result in ListResult)
                    {
                        HtmlNode Option = HtmlNode.CreateNode("<option></option>");

                        if (result == currentValue)
                        {
                            //str += $@"<option selected value = {user.Id++}> {user.FIO} </option>" + "\n";

                            Option.Attributes.Add($"selected", $"");
                            Option.Attributes.Add($"value", $"{result}");
                            Option.InnerHtml = $"{result}";
                        }
                        else
                        {
                            //str += $@"<option value = {user.Id++}> {user.FIO} </option>" + "\n";

                            Option.Attributes.Add($"value", $"{result}");
                            Option.InnerHtml = $"{result}";
                        }

                        Select.AppendChild(Option);
                    }

                    TableData.AppendChild(Select);
                }
                else
                {
                    HtmlNode Input = HtmlNode.CreateNode("<input></input>");

                    if (item.Key == "Place_PSP")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.PlacePsp;
                    else if (item.Key == "Place_SIKN")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.PlaceSikn;
                    else if (item.Key == "Place_Factory")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.PlaceFactory;
                    else if (item.Key == "CPM_RSU_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.MprRsuInfo.Sensor.DevType;
                    else if (item.Key == "CPM_RSU_FactoryNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.MprRsuInfo.Sensor.DevNumb;
                    else if (item.Key == "CPM_RSU_PEP_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.MprRsuInfo.Pep.DevType;
                    else if (item.Key == "CPM_RSU_PEP_FactoryNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.MprRsuInfo.Pep.DevNumb;
                    else if (item.Key == "CPM_RSU_LineName")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.MprRsuInfo.LineName;
                    else if (item.Key == "IVK_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.IvkInfo.DevType;
                    else if (item.Key == "IVK_FactoryNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.IvkInfo.DevNumb;
                    else if (item.Key == "Oil_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.OilType;
                    else if (item.Key == "ProtokolNum")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.ProtokolNum;
                    else if (item.Key == "ARM_Poverka_Result")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.ARM_Poverka_Result;
                    else if (item.Key == "StaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.StaffData;

                    Input.Id = item.Key;
                    Input.Attributes.Add($"data-edit", $"1");
                    Input.Attributes.Add($"data-key", $"{item.Key}");
                    Input.Attributes.Add($"data-tag", $"AdditionalInfo");
                    Input.Attributes.Add($"type", $"{item.Type}");
                    Input.Attributes.Add($"value", $"{currentValue}");

                    if (!item.Edit)
                        Input.Attributes.Add($"disabled", $"disabled");

                    TableData.AppendChild(Input);
                }

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
            TablePoverkaSikn425_UPR_PRData row = new();
            GetViewDoc(data.DocID);

            ad = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo;

            if (data.Values.Exists(x => x.Key == "Place_PSP"))
                ad.PlacePsp = data.Values.Where(x => x.Key == "Place_PSP").Single().Value;
            if (data.Values.Exists(x => x.Key == "Place_SIKN"))
                ad.PlaceSikn = data.Values.Where(x => x.Key == "Place_SIKN").Single().Value;
            if (data.Values.Exists(x => x.Key == "Place_Factory"))
                ad.PlaceFactory = data.Values.Where(x => x.Key == "Place_Factory").Single().Value;
            if (data.Values.Exists(x => x.Key == "CPM_RSU_Type"))
                ad.MprRsuInfo.Sensor.DevType = data.Values.Where(x => x.Key == "CPM_RSU_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "CPM_RSU_FactoryNumber"))
                ad.MprRsuInfo.Sensor.DevNumb = data.Values.Where(x => x.Key == "CPM_RSU_FactoryNumber").Single().Value;
            if (data.Values.Exists(x => x.Key == "CPM_RSU_PEP_Type"))
                ad.MprRsuInfo.Pep.DevType = data.Values.Where(x => x.Key == "CPM_RSU_PEP_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "CPM_RSU_PEP_FactoryNumber"))
                ad.MprRsuInfo.Pep.DevNumb = data.Values.Where(x => x.Key == "CPM_RSU_PEP_FactoryNumber").Single().Value;
            if (data.Values.Exists(x => x.Key == "CPM_RSU_LineName"))
                ad.MprRsuInfo.LineName = data.Values.Where(x => x.Key == "CPM_RSU_LineName").Single().Value;
            if (data.Values.Exists(x => x.Key == "IVK_Type"))
                ad.IvkInfo.DevType = data.Values.Where(x => x.Key == "IVK_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "IVK_FactoryNumber"))
                ad.IvkInfo.DevNumb = data.Values.Where(x => x.Key == "IVK_FactoryNumber").Single().Value;
            if (data.Values.Exists(x => x.Key == "Oil_Type"))
                ad.OilType = data.Values.Where(x => x.Key == "Oil_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "ProtokolNum"))
                ad.ProtokolNum = data.Values.Where(x => x.Key == "ProtokolNum").Single().Value;
            if (data.Values.Exists(x => x.Key == "ARM_Poverka_Result"))
                ad.ARM_Poverka_Result = data.Values.Where(x => x.Key == "ARM_Poverka_Result").Single().Value;
            if (data.Values.Exists(x => x.Key == "StaffData"))
                ad.StaffData = data.Values.Where(x => x.Key == "StaffData").Single().Value;

            row.id = data.DocID;
            row.AdditionalInfo = StringToHexArrByte(ad);
            
            this.Entry(row).Property(x => x.AdditionalInfo).IsModified = true;
            this.SaveChanges();
            
            return true;
        }
    }


    [Table("Msikn425MprByMpr")]
    public class TablePoverkaSikn425_UPR_PRList
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int Il { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public int SiknId { get; set; }
        public int DirId { get; set; }
        public byte[] IlName { get; set; }
        public TablePoverkaSikn425_UPR_PRData Data { get; set; }
    }

    [Table("Msikn425MprByMpr")]
    public class TablePoverkaSikn425_UPR_PRData
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
            ((DataIVKDoc)Doc.DataIVK).TablePoverkaSikn425_UPR_PR = new TablePoverkaSikn425_UPR_PR();
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
        public TablePoverkaSikn425_UPR_PR TablePoverkaSikn425_UPR_PR { get; set; }
    }
    public class TablePoverkaSikn425_UPR_PR
    {
        public int id { get; set; }
        public int Il { get; set; }
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
        public MprInfo MprRsuInfo { get; set; }
        public List<MprInfo> MprOsuInfo { get; set; }
        public DeviceInfo IvkInfo { get; set; }
        public string OilType { get; set; }
        public Table1 Table1 { get; set; }
        public List<Table2> Table2 { get; set; }
        public List<Table3> Table3 { get; set; }
        public List<Table4> Table4 { get; set; }
        public Table5 Table5 { get; set; }
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
        public string TetaM { get; set; }
        public string deltaIVK { get; set; }
        public string Kpm { get; set; }
        public string KmMFust { get; set; }
        public string QmMax { get; set; }
        public string ZS { get; set; }
        public string Qnom { get; set; }
        public string deltaTempDop { get; set; }
        public string tMin { get; set; }
        public string tMax { get; set; }
        public string deltaPdop { get; set; }
        public string Pmin { get; set; }
        public string Pmax { get; set; }
    }
    public class Table2
    {
        public string ser { get; set; }
        public string row { get; set; }
        public string MPR_Id { get; set; }
        public string Q_jik { get; set; }
        public string t_MPR_jik { get; set; }
        public string P_MPR_jik { get; set; }
        public string N_jik { get; set; }
        public string Kpm_jik { get; set; }
        public string M_jik { get; set; }
    }
    public class Table3
    {
        public string ser { get; set; }
        public string row { get; set; }
        public string Q_ji { get; set; }
        public string t_MPR_ji { get; set; }
        public string P_MPR_ji { get; set; }
        public string T_ji { get; set; }
        public string N_ji { get; set; }
        public string Me_ji { get; set; }
        public string M_ji { get; set; }
        public string Km_MF_ji { get; set; }
        public string U_ji { get; set; }
        public string U_error_ji { get; set; }
        public string Q_KMH_ji { get; set; }
        public string M_KMH_ji { get; set; }
        public string delta_KMH_ji { get; set; }
    }
    public class Table4
    {
        public string ser { get; set; }
        public string Q_j { get; set; }
        public string Km_MF_j { get; set; }
        public string n_j { get; set; }
        public string S_j { get; set; }
        public string S_error_j { get; set; }
        public string S0_j { get; set; }
        public string t095_j { get; set; }
        public string e_j { get; set; }
        public string SK_j { get; set; }
    }
    public class Table5
    {
        public string Q_min { get; set; }
        public string Q_max { get; set; }
        public string Km_MF { get; set; }
        public string S0 { get; set; }
        public string e { get; set; }
        public string Teta_A { get; set; }
        public string Teta_Z { get; set; }
        public string t_P { get; set; }
        public string Teta_Mt { get; set; }
        public string P_P { get; set; }
        public string Teta_Mp { get; set; }
        public string Teta_Sum { get; set; }
        public string delta { get; set; }
        public string delta_KMH_max { get; set; }
        public string t_Sum { get; set; }
        public string S_Teta { get; set; }
        public string S_Sum { get; set; }
    }
    public class AdditionalInfo
    {
        public string PlacePsp { get; set; }
        public string PlaceSikn { get; set; }
        public string PlaceFactory { get; set; }
        public string OilType { get; set; }
        public string ProtokolNum { get; set; }
        public string ARM_Poverka_Result { get; set; }
        public string StaffData { get; set; }
        public MprInfo MprRsuInfo { get; set; }
        public List<MprInfo> MprOsuInfo { get; set; }
        public DeviceInfo IvkInfo { get; set; }
        
        public string UPR_Type { get; set; }
        public string UPR_FactoryNumber { get; set; }
        public string PU_Type { get; set; }
        public string PU_FactoryNumber { get; set; }
        public string PP_Type { get; set; }
        public string PP_FactoryNumber { get; set; }
        public string IVK_Type { get; set; }
        public string IVK_FactoryNumber { get; set; }
        
    }
    #endregion
}