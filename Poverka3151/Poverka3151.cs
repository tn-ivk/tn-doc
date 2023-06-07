using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Xml.Linq;
using TN.DocData;

namespace TN.Doc
{
    public class Poverka3151 : DocGeneral
    {
        public Poverka3151(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
        {
            IdDoc = IdDoc.Poverka3151;
            PathToDocConfigFile = GetPathConfigFile();
            PathToDocEditConfigFile = GetPathEditConfigFile();
            PathToDocTemplateFile = GetPathTemplateFile();
        }

        private DbSet<TablePoverka3151List> ListDoc { get; set; }
        private DbSet<TablePoverka3151Data> DataDoc { get; set; }

        private Doc Doc;
        private JObject jsonDoc;

        public override List<RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<RequestListDocs> docs = new();

            var list = (from item in ListDoc.AsNoTracking()
                        where item.DateTimeLong > UTBegin && item.DateTimeLong < UTEnd && item.id > 1
                        select item).ToList<TablePoverka3151List>();

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

            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3151.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3151.IL = list.IL;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3151.GH_Type = list.GH_Type;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3151.Protokol = JsonDeserializeObject<Protokol>(ArrByteToString(list.Data.Protokol));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3151.DateTimeString = list.DateTimeString;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3151.DateTimeLong = list.DateTimeLong;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3151.BIK_ID = list.BIK_ID;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3151.IL_Name = ArrByteToString(list.IL_Name);

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

            //Рисуем и наполняем таблицу AdditionalInfo
            foreach (var item in editDoc.AdditionalInfo.Where(x => x.Use).ToList())
            {
                string currentValue = "";

                HtmlNode TableRow = HtmlNode.CreateNode("<tr></tr>");
                HtmlNode TableData = HtmlNode.CreateNode("<td></td>");

                TableRow.AppendChild(HtmlNode.CreateNode($"<td>{item.Name}</td>"));
                //TableData.InnerHtml = item.Name;

                if (item.Type == "list")
                {
                    HtmlNode Select = HtmlNode.CreateNode("<select></select>");
                    List<string> ListResult = new List<string>() { "", "годен", "не годен" };

                    Select.Attributes.Add($"name", $"{item.Key}");
                    Select.Attributes.Add($"data-edit", $"1");
                    Select.Attributes.Add($"data-key", $"{item.Key}");
                    Select.Attributes.Add($"data-tag", $"AdditionalInfo");

                    if (item.Key == "ARM_Poverka_Result")
                    {
                        ListResult = new List<string>() { "", "годен", "не годен" };
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.ARM_Poverka_Result;
                    }
                    else if (item.Key == "ARM_Result_Type")
                    {
                        ListResult = new List<string>() { "", "рабочего", "контрольного", "рабочего и контрольного" };
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.ARM_Result_Type;
                    }

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

                    if (item.Key == "ProtokolNum")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.ProtokolNum;
                    else if (item.Key == "PR_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.PR_Type;
                    else if (item.Key == "Place_PSP")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.Place_PSP;
                    else if (item.Key == "Place_Factory")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.Place_Factory;
                    else if (item.Key == "PR_Sensor_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.PR_Sensor_Type;
                    else if (item.Key == "PR_Sensor_DU")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.PR_Sensor_DU;
                    else if (item.Key == "PR_Sensor_FactoryNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.PR_Sensor_FactoryNumber;
                    else if (item.Key == "PR_PEP")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.PR_PEP;
                    else if (item.Key == "PR_PEP_FactoryNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.PR_PEP_FactoryNumber;
                    else if (item.Key == "SIKN")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.SIKN;
                    else if (item.Key == "PU_Name")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.PU_Name;
                    else if (item.Key == "PU_Category")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.PU_Category;
                    else if (item.Key == "PU_FactoryNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.PU_FactoryNumber;
                    else if (item.Key == "PU_CheckDate")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.PU_CheckDate;
                    else if (item.Key == "PP_Name")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.PP_Name;
                    else if (item.Key == "PP_FactoryNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.PP_FactoryNumber;
                    else if (item.Key == "PP_CheckDate")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.PP_CheckDate;
                    else if (item.Key == "ARM_Poverka_Result")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.ARM_Poverka_Result;
                    else if (item.Key == "ARM_Result_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.ARM_Result_Type;
                    else if (item.Key == "Certificate_Num")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.Certificate_Num;
                    else if (item.Key == "Certificate_Date")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.Certificate_Date;
                    else if (item.Key == "StaffData_company")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.StaffData_company;
                    else if (item.Key == "StaffData_fio")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo.StaffData_fio;

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
            TablePoverka3151Data row = new();
            GetViewDoc(data.DocID);

            ad = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3151.AdditionalInfo;

            if (data.Values.Exists(x => x.Key == "ProtokolNum"))
                ad.ProtokolNum = data.Values.Where(x => x.Key == "ProtokolNum").Single().Value;
            if (data.Values.Exists(x => x.Key == "PR_Type"))
                ad.PR_Type = data.Values.Where(x => x.Key == "PR_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "Place_PSP"))
                ad.Place_PSP = data.Values.Where(x => x.Key == "Place_PSP").Single().Value;
            if (data.Values.Exists(x => x.Key == "Place_Factory"))
                ad.Place_Factory = data.Values.Where(x => x.Key == "Place_Factory").Single().Value;
            if (data.Values.Exists(x => x.Key == "PR_Sensor_Type"))
                ad.PR_Sensor_Type = data.Values.Where(x => x.Key == "PR_Sensor_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "PR_Sensor_DU"))
                ad.PR_Sensor_DU = data.Values.Where(x => x.Key == "PR_Sensor_DU").Single().Value;
            if (data.Values.Exists(x => x.Key == "PR_Sensor_FactoryNumber"))
                ad.PR_Sensor_FactoryNumber = data.Values.Where(x => x.Key == "PR_Sensor_FactoryNumber").Single().Value;
            if (data.Values.Exists(x => x.Key == "PR_PEP"))
                ad.PR_PEP = data.Values.Where(x => x.Key == "PR_PEP").Single().Value;
            if (data.Values.Exists(x => x.Key == "PR_PEP_FactoryNumber"))
                ad.PR_PEP_FactoryNumber = data.Values.Where(x => x.Key == "PR_PEP_FactoryNumber").Single().Value;
            if (data.Values.Exists(x => x.Key == "SIKN"))
                ad.SIKN = data.Values.Where(x => x.Key == "SIKN").Single().Value;
            if (data.Values.Exists(x => x.Key == "PU_Name"))
                ad.PU_Name = data.Values.Where(x => x.Key == "PU_Name").Single().Value;
            if (data.Values.Exists(x => x.Key == "PU_Category"))
                ad.PU_Category = data.Values.Where(x => x.Key == "PU_Category").Single().Value;
            if (data.Values.Exists(x => x.Key == "PU_FactoryNumber"))
                ad.PU_FactoryNumber = data.Values.Where(x => x.Key == "PU_FactoryNumber").Single().Value;
            if (data.Values.Exists(x => x.Key == "PU_CheckDate"))
                ad.PU_CheckDate = data.Values.Where(x => x.Key == "PU_CheckDate").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP_Name"))
                ad.PP_Name = data.Values.Where(x => x.Key == "PP_Name").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP_FactoryNumber"))
                ad.PP_FactoryNumber = data.Values.Where(x => x.Key == "PP_FactoryNumber").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP_CheckDate"))
                ad.PP_CheckDate = data.Values.Where(x => x.Key == "PP_CheckDate").Single().Value;
            if (data.Values.Exists(x => x.Key == "ARM_Poverka_Result"))
                ad.ARM_Poverka_Result = data.Values.Where(x => x.Key == "ARM_Poverka_Result").Single().Value;
            if (data.Values.Exists(x => x.Key == "ARM_Result_Type"))
                ad.ARM_Result_Type = data.Values.Where(x => x.Key == "ARM_Result_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "Certificate_Num"))
                ad.Certificate_Num = data.Values.Where(x => x.Key == "Certificate_Num").Single().Value;
            if (data.Values.Exists(x => x.Key == "Certificate_Date"))
                ad.Certificate_Date = data.Values.Where(x => x.Key == "Certificate_Date").Single().Value;
            if (data.Values.Exists(x => x.Key == "StaffData_company"))
                ad.StaffData_company = data.Values.Where(x => x.Key == "StaffData_company").Single().Value;
            if (data.Values.Exists(x => x.Key == "StaffData_fio"))
                ad.StaffData_fio = data.Values.Where(x => x.Key == "StaffData_fio").Single().Value;

            row.id = data.DocID;
            row.AdditionalInfo = StringToHexArrByte(ad);

            //DataDoc.Update(dd);

            this.Entry(row).Property(x => x.AdditionalInfo).IsModified = true;

            this.SaveChanges();

            return true;
        }
    }

    [Table("TablePoverka3151")]
    public class TablePoverka3151List
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int IL { get; set; }
        public int GH_Type { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public int BIK_ID { get; set; }
        public byte[] IL_Name { get; set; }
        public TablePoverka3151Data Data { get; set; }
    }

    [Table("TablePoverka3151")]
    public class TablePoverka3151Data
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
            ((DataIVKDoc)Doc.DataIVK).TablePoverka3151 = new TablePoverka3151();
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
        public TablePoverka3151 TablePoverka3151 { get; set; }
    }
    public class TablePoverka3151
    {
        public int id { get; set; }
        public int IL { get; set; }
        public int GH_Type { get; set; }
        public Protokol Protokol { get; set; }
        public AdditionalInfo AdditionalInfo { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public int BIK_ID { get; set; }
        public string IL_Name { get; set; }
    }

    public class Protokol
    {
        public List<string> DetName { get; set; }
        public string OilType { get; set; }
        public Table1 Table1 { get; set; }
        public List<Table2> Table2 { get; set; }
        public List<Table3> Table3 { get; set; }
        public Table4_1 Table4_1 { get; set; }
        public Table4_2 Table4_2 { get; set; }
        public Table4_3 Table4_3 { get; set; }
        public List<TableGH> TableGH { get; set; }
    }
    public class Table1
    {
        public List<string> DetName { get; set; }
        public List<string> V0 { get; set; }
        public string Delta_PU { get; set; }
        public string m_D { get; set; }
        public string m_S { get; set; }
        public string m_E { get; set; }
        public string Alpha_t_PU { get; set; }
        public string Delta_t_PU { get; set; }
        public string Delta_PP { get; set; }
        public string Delta_t_PP { get; set; }
        public string Delta_k_UOI { get; set; }
        public string KF_konf { get; set; }
        public string ZS { get; set; }
    }
    public class Table2
    {
        public int Activated { get; set; }
        public string Ser { get; set; }
        public string Row { get; set; }
        public string Q_ji { get; set; }
        public string Det { get; set; }
        public string T_ji { get; set; }
        public string t_PU_ji { get; set; }
        public string P_PU_ji { get; set; }
        public string Dens_ji { get; set; }
        public string DensTemp_ji { get; set; }
        public string DensPress_ji { get; set; }
        public string N_ji { get; set; }
        public string V_PU_PR_ji { get; set; }
        public string Dens_PP_PR_ji { get; set; }
        public string M_RA_ji { get; set; }
        public string M_mas_ji { get; set; }
        public string MF_ji { get; set; }
    }
    public class Table3
    {
        public string t_P_n { get; set; }
        public string Z_P { get; set; }
    }
    public class Table4_1
    {
        public List<PointsTable4_1> Points { get; set; }
        public SubrangeTable4_1 Subrange { get; set; }
    }
    public class Table4_2
    {
        public List<PointsTable4_2> Points { get; set; }
        public SubrangeTable4_2 Subrange { get; set; }
    }
    public class Table4_3
    {
        public List<PointsTable4_3> Points { get; set; }
        public List<Subranges> Subranges { get; set; }
    }
    public class PointsTable4_1
    {
        public string Point { get; set; }
        public string Q_j { get; set; }
        public string MF_j { get; set; }

    }
    public class PointsTable4_2
    {
        public string Point { get; set; }
        public string Q_j { get; set; }
        public string KF_j { get; set; }
    }
    public class PointsTable4_3
    {
        public string Point { get; set; }
        public string Q_j { get; set; }
        public string KF_j { get; set; }
    }
    public class SubrangeTable4_1
    {
        public string SKO_MF { get; set; }
        public string Delta_0_mas { get; set; }
        public string MF { get; set; }
        public string SKO_MF_ERROR { get; set; }
        public string K_GR { get; set; }
        public string e { get; set; }
        public string Teta_Sum { get; set; }
        public string Delta { get; set; }
        public string Delta_ERROR { get; set; }
    }
    public class SubrangeTable4_2
    {
        public string SKO_KF { get; set; }
        public string Delta_0_mas { get; set; }
        public string KF { get; set; }
        public string SKO_KF_ERROR { get; set; }
        public string Teta_KF { get; set; }
        public string e { get; set; }
        public string Teta_Sum { get; set; }
        public string Delta { get; set; }
        public string Delta_ERROR { get; set; }
    }
    public class Subranges
    {
        public string Point { get; set; }
        public string Q_min { get; set; }
        public string Q_max { get; set; }
        public string SKO_KF { get; set; }
        public string SKO_KF_ERROR { get; set; }
        public string Delta_0_mas { get; set; }
        public string Teta_KF { get; set; }
        public string e { get; set; }
        public string Teta_Sum { get; set; }
        public string Delta { get; set; }
        public string Delta_ERROR { get; set; }
    }
    public class TableGH
    {
        public string Point { get; set; }
        public string Q_j { get; set; }
        public string f_j { get; set; }
        public string K_j { get; set; }
    }
    public class AdditionalInfo
    {
        public string ProtokolNum { get; set; }
        public string PR_Type { get; set; }
        public string Place_PSP { get; set; }
        public string Place_Factory { get; set; }
        public string PR_Sensor_Type { get; set; }
        public string PR_Sensor_DU { get; set; }
        public string PR_Sensor_FactoryNumber { get; set; }
        public string PR_PEP { get; set; }
        public string PR_PEP_FactoryNumber { get; set; }
        public string SIKN { get; set; }
        public string PU_Name { get; set; }
        public string PU_Category { get; set; }
        public string PU_FactoryNumber { get; set; }
        public string PU_CheckDate { get; set; }
        public string PP_Name { get; set; }
        public string PP_FactoryNumber { get; set; }
        public string PP_CheckDate { get; set; }
        public string ARM_Poverka_Result { get; set; }
        public string ARM_Result_Type { get; set; }
        public string Certificate_Num { get; set; }
        public string Certificate_Date { get; set; }
        public string StaffData_company { get; set; }
        public string StaffData_fio { get; set; }
    }

    #endregion

}
