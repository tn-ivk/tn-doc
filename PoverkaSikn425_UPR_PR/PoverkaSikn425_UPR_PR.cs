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
    public class PoverkaSikn425_UPR_PR : DocGeneral
    {
        public PoverkaSikn425_UPR_PR(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
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
                        Description = $"{ArrByteToString(item.IL_Name)}"
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
                ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.IL = list.IL;
                ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.Protokol = JsonDeserializeObject<Protokol>(ArrByteToString(list.Data.Protokol));
                ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));
                ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.DateTimeString = list.DateTimeString;
                ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.DateTimeLong = list.DateTimeLong;
                ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.BIK_ID = list.BIK_ID;
                ((DataIVKDoc)doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.IL_Name = ArrByteToString(list.IL_Name);

                string fileName = string.Format("{0}_Поверка СИКН 425 УПР с помощью ПУ ПП и СРМ",
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
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.Place_PSP;
                    else if (item.Key == "Place_SIKN")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.Place_SIKN;
                    else if (item.Key == "Place_Factory")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.Place_Factory;
                    else if (item.Key == "UPR_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.UPR_Type;
                    else if (item.Key == "UPR_FactoryNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.UPR_FactoryNumber;
                    else if (item.Key == "PU_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.PU_Type;
                    else if (item.Key == "PU_FactoryNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.PU_FactoryNumber;
                    else if (item.Key == "PP_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.PP_Type;
                    else if (item.Key == "PP_FactoryNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.PP_FactoryNumber;
                    else if (item.Key == "IVK_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.IVK_Type;
                    else if (item.Key == "IVK_FactoryNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.IVK_FactoryNumber;
                    else if (item.Key == "Oil_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverkaSikn425_UPR_PR.AdditionalInfo.Oil_Type;
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
                ad.Place_PSP = data.Values.Where(x => x.Key == "Place_PSP").Single().Value;
            if (data.Values.Exists(x => x.Key == "Place_SIKN"))
                ad.Place_SIKN = data.Values.Where(x => x.Key == "Place_SIKN").Single().Value;
            if (data.Values.Exists(x => x.Key == "Place_Factory"))
                ad.Place_Factory = data.Values.Where(x => x.Key == "Place_Factory").Single().Value;
            if (data.Values.Exists(x => x.Key == "UPR_Type"))
                ad.UPR_Type = data.Values.Where(x => x.Key == "UPR_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "UPR_FactoryNumber"))
                ad.UPR_FactoryNumber = data.Values.Where(x => x.Key == "UPR_FactoryNumber").Single().Value;
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
                ad.Oil_Type = data.Values.Where(x => x.Key == "Oil_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "ProtokolNum"))
                ad.ProtokolNum = data.Values.Where(x => x.Key == "ProtokolNum").Single().Value;
            if (data.Values.Exists(x => x.Key == "ARM_Poverka_Result"))
                ad.ARM_Poverka_Result = data.Values.Where(x => x.Key == "ARM_Poverka_Result").Single().Value;
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
        public string Place_PSP { get; set; }
        public string Place_SIKN { get; set; }
        public string Place_Factory { get; set; }
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
        public string Point { get; set; }
        public string Q_j { get; set; }
        public string f_j { get; set; }
        public string K_j { get; set; }
        public string S_j { get; set; }
        public string n_j { get; set; }
        public string S0_j { get; set; }
        public string t095_j { get; set; }
        public string e_j { get; set; }
        public string SK_j { get; set; }
    }
    public class Table5
    {
        public string Q_min { get; set; }
        public string Q_max { get; set; }
        public string K { get; set; }
        public string S0 { get; set; }
        public string e { get; set; }
        public string Teta_A { get; set; }
        public string Teta_t { get; set; }
        public string Teta_p { get; set; }
        public string Teta_Sum { get; set; }
        public string Delta { get; set; }
    }
    public class AdditionalInfo
    {
        public string Place_PSP { get; set; }
        public string Place_SIKN { get; set; }
        public string Place_Factory { get; set; }
        public string UPR_Type { get; set; }
        public string UPR_FactoryNumber { get; set; }
        public string PU_Type { get; set; }
        public string PU_FactoryNumber { get; set; }
        public string PP_Type { get; set; }
        public string PP_FactoryNumber { get; set; }
        public string IVK_Type { get; set; }
        public string IVK_FactoryNumber { get; set; }
        public string Oil_Type { get; set; }
        public string ProtokolNum { get; set; }
        public string ARM_Poverka_Result { get; set; }
        public string StaffData { get; set; }
        public List<PRs_Info> PRs_Info { get; set; }
    }
    public class PRs_Info
    {
        public string PR_Type { get; set; }
        public string PR_FactoryNumber { get; set; }
        public string PR_LineName { get; set; }
        public string MM_Sensor_Type { get; set; }
        public string MM_Sensor_DU { get; set; }
        public string MM_Sensor_FactoryNumber { get; set; }
        public string MM_PEP { get; set; }
        public string MM_PEP_FactoryNumber { get; set; }
    }

    #endregion

}