using HtmlAgilityPack;
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
    public class Poverka3380 : DocGeneral
    {
        public Poverka3380(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
        {
            IdDoc = IdDoc.Poverka3380;
            PathToDocConfigFile = GetPathConfigFile();
            PathToDocEditConfigFile = GetPathEditConfigFile();
            PathToDocTemplateFile = GetPathTemplateFile();
        }

        private DbSet<TablePoverka3380List> ListDoc { get; set; }
        private DbSet<TablePoverka3380Data> DataDoc { get; set; }

        private Doc Doc;
        private JObject jsonDoc;

        public override List<RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<RequestListDocs> docs = new();

            var list = (from item in ListDoc.AsNoTracking()
                        where item.DateTimeLong > UTBegin && item.DateTimeLong < UTEnd && item.id > 1
                        select item).ToList<TablePoverka3380List>();

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

            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3380.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3380.IL = list.IL;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3380.Protokol = JsonDeserializeObject<Protokol>(ArrByteToString(list.Data.Protokol));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3380.DateTimeString = list.DateTimeString;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3380.DateTimeLong = list.DateTimeLong;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3380.BIK_ID = list.BIK_ID;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3380.IL_Name = ArrByteToString(list.IL_Name);

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
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.ARM_Poverka_Result;
                    }
                    else if (item.Key == "ARM_Poverka_Result_Type")
                    {
                        ListResult = new List<string>() { "", "рабочего", "резервно-контрольного" };
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.ARM_Poverka_Result_Type;
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

                    if (item.Key == "PR_Model_Top")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.PR_Model_Top;
                    else if (item.Key == "PoverkaPlace")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.PoverkaPlace;
                    else if (item.Key == "PlaceSIKN")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.PlaceSIKN;
                    else if (item.Key == "PlaceFactory")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.PlaceFactory;
                    else if (item.Key == "PR_Model")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.PR_Model;
                    else if (item.Key == "PR_DN")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.PR_DN;
                    else if (item.Key == "PR_PN")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.PR_PN;
                    else if (item.Key == "PR_FactoryNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.PR_FactoryNumber;
                    else if (item.Key == "LineName")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.LineName;
                    else if (item.Key == "Oil")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.Oil;
                    else if (item.Key == "PU")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.PU;
                    else if (item.Key == "PU_Category")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.PU_Category;
                    else if (item.Key == "PU_FactoryNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.PU_FactoryNumber;
                    else if (item.Key == "PU_PN")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.PU_PN;
                    else if (item.Key == "PU_Poverka_Date")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.PU_Poverka_Date;
                    else if (item.Key == "ProtokolNum")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.ProtokolNum;
                    else if (item.Key == "StaffData_company")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.StaffData_company;
                    else if (item.Key == "StaffData_fio")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.StaffData_fio;
                    else if (item.Key == "ARM_Poverka_Result")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.ARM_Poverka_Result;
                    else if (item.Key == "ARM_Poverka_Result_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo.ARM_Poverka_Result_Type;

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
            TablePoverka3380Data row = new();
            GetViewDoc(data.DocID);

            ad = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3380.AdditionalInfo;

            if (data.Values.Exists(x => x.Key == "PR_Model_Top"))
                ad.PR_Model_Top = data.Values.Where(x => x.Key == "PR_Model_Top").Single().Value;
            if (data.Values.Exists(x => x.Key == "PoverkaPlace"))
                ad.PoverkaPlace = data.Values.Where(x => x.Key == "PoverkaPlace").Single().Value;
            if (data.Values.Exists(x => x.Key == "PlaceSIKN"))
                ad.PlaceSIKN = data.Values.Where(x => x.Key == "PlaceSIKN").Single().Value;
            if (data.Values.Exists(x => x.Key == "PlaceFactory"))
                ad.PlaceFactory = data.Values.Where(x => x.Key == "PlaceFactory").Single().Value;
            if (data.Values.Exists(x => x.Key == "PR_Model"))
                ad.PR_Model = data.Values.Where(x => x.Key == "PR_Model").Single().Value;
            if (data.Values.Exists(x => x.Key == "PR_DN"))
                ad.PR_DN = data.Values.Where(x => x.Key == "PR_DN").Single().Value;
            if (data.Values.Exists(x => x.Key == "PR_PN"))
                ad.PR_PN = data.Values.Where(x => x.Key == "PR_PN").Single().Value;
            if (data.Values.Exists(x => x.Key == "PR_FactoryNumber"))
                ad.PR_FactoryNumber = data.Values.Where(x => x.Key == "PR_FactoryNumber").Single().Value;
            if (data.Values.Exists(x => x.Key == "LineName"))
                ad.LineName = data.Values.Where(x => x.Key == "LineName").Single().Value;
            if (data.Values.Exists(x => x.Key == "Oil"))
                ad.Oil = data.Values.Where(x => x.Key == "Oil").Single().Value;
            if (data.Values.Exists(x => x.Key == "PU"))
                ad.PU = data.Values.Where(x => x.Key == "PU").Single().Value;
            if (data.Values.Exists(x => x.Key == "PU_Category"))
                ad.PU_Category = data.Values.Where(x => x.Key == "PU_Category").Single().Value;
            if (data.Values.Exists(x => x.Key == "PU_FactoryNumber"))
                ad.PU_FactoryNumber = data.Values.Where(x => x.Key == "PU_FactoryNumber").Single().Value;
            if (data.Values.Exists(x => x.Key == "PU_PN"))
                ad.PU_PN = data.Values.Where(x => x.Key == "PU_PN").Single().Value;
            if (data.Values.Exists(x => x.Key == "PU_Poverka_Date"))
                ad.PU_Poverka_Date = data.Values.Where(x => x.Key == "PU_Poverka_Date").Single().Value;
            if (data.Values.Exists(x => x.Key == "ProtokolNum"))
                ad.ProtokolNum = data.Values.Where(x => x.Key == "ProtokolNum").Single().Value;
            if (data.Values.Exists(x => x.Key == "StaffData_company"))
                ad.StaffData_company = data.Values.Where(x => x.Key == "StaffData_company").Single().Value;
            if (data.Values.Exists(x => x.Key == "StaffData_fio"))
                ad.StaffData_fio = data.Values.Where(x => x.Key == "StaffData_fio").Single().Value;
            if (data.Values.Exists(x => x.Key == "ARM_Poverka_Result"))
                ad.ARM_Poverka_Result = data.Values.Where(x => x.Key == "ARM_Poverka_Result").Single().Value;
            if (data.Values.Exists(x => x.Key == "ARM_Poverka_Result_Type"))
                ad.ARM_Poverka_Result_Type = data.Values.Where(x => x.Key == "ARM_Poverka_Result_Type").Single().Value;

            row.id = data.DocID;
            row.AdditionalInfo = StringToHexArrByte(ad);

            //DataDoc.Update(dd);

            this.Entry(row).Property(x => x.AdditionalInfo).IsModified = true;

            this.SaveChanges();

            return true;
        }
    }

    [Table("TablePoverka3380")]
    public class TablePoverka3380List
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int IL { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public int BIK_ID { get; set; }
        public byte[] IL_Name { get; set; }
        public TablePoverka3380Data Data { get; set; }
    }

    [Table("TablePoverka3380")]
    public class TablePoverka3380Data
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
            ((DataIVKDoc)Doc.DataIVK).TablePoverka3380 = new TablePoverka3380();
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
        public TablePoverka3380 TablePoverka3380 { get; set; }
    }
    public class TablePoverka3380
    {
        public int id { get; set; }
        public int IL { get; set; }
        public Protokol Protokol { get; set; }
        public AdditionalInfo AdditionalInfo { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public int BIK_ID { get; set; }
        public string IL_Name { get; set; }
    }
    public class Protokol
    {
        public string OilType { get; set; }
        public int UseFV { get; set; }
        public Table1 Table1 { get; set; }
        public List<Table2> Table2 { get; set; }
        public List<Table3> Table3 { get; set; }
        public List<Table3Control> Table3_Control { get; set; }
        public Table4_921 Table4_921 { get; set; }
        public List<Table4_922> Table4_922 { get; set; }
        public Table4_923 Table4_923 { get; set; }
        public List<Table5> Table5 { get; set; }
        public List<Parabola> Parabola { get; set; }
        public CfgTables CfgTables { get; set; }
    }
    public class Table1
    {
        public List<string> DetName { get; set; }
        public List<string> V0 { get; set; }
        public string m_D { get; set; }
        public string m_S { get; set; }
        public string m_E { get; set; }
        public string Alpha_t_PU { get; set; }
        public string Alpha_t_cm { get; set; }
        public string Delta_t_PU { get; set; }
        public string Delta_k_soi { get; set; }
        public string Delta_t_PR { get; set; }
        public string Delta_PU { get; set; }
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
        public string t_cm_ji { get; set; }
        public string f_ji { get; set; }
        public string fv_ji { get; set; }
        public string t_PR_ji { get; set; }
        public string P_PR_ji { get; set; }
        public string N_ji { get; set; }
        public string Dens_ji { get; set; }
        public string DensTemp_ji { get; set; }
        public string DensPress_ji { get; set; }
        public string KinVisc_ji { get; set; }
        public string W_ji { get; set; }
        public string V_PU_ji { get; set; }
        public string K_ji { get; set; }
        public string CTL_PU_ji { get; set; }
        public string CPL_PU_ji { get; set; }
        public string CTL_PR_ji { get; set; }
        public string CPL_PR_ji { get; set; }
        public string U_ji { get; set; }
        public string U_ERROR_ji { get; set; }
    }
    public class Table3
    {
        public string t_P_n { get; set; }
        public string Z_P { get; set; }
    }
    public class Table3Control
    {
        public string t_P_n { get; set; }
        public string Z_P { get; set; }
    }
    public class Table4_921
    {
        public string Q_min { get; set; }
        public string Q_max { get; set; }
        public string SKO { get; set; }
        public string SKO_ERROR { get; set; }
        public string K { get; set; }
        public string e { get; set; }
        public string Teta_A { get; set; }
        public string Teta_Sum { get; set; }
        public string Delta { get; set; }
        public string Delta_ERROR { get; set; }
    }
    public class Table4_922
    {
        public string Point { get; set; }
        public string Q_min { get; set; }
        public string Q_max { get; set; }
        public string SKO { get; set; }
        public string SKO_ERROR { get; set; }
        public string K { get; set; }
        public string e { get; set; }
        public string Teta_A { get; set; }
        public string Teta_Sum { get; set; }
        public string Delta { get; set; }
        public string Delta_ERROR { get; set; }
    }
    public class Table4_923
    {
        public List<Points> Points { get; set; }
        public List<Subranges> Subranges { get; set; }
    }
    public class Points
    {
        public string Point { get; set; }
        public string Q_j { get; set; }
        public string f_j { get; set; }
        public string fv_j { get; set; }
        public string K_j { get; set; }
    }
    public class Subranges
    {
        public string Point { get; set; }
        public string Q_min { get; set; }
        public string Q_max { get; set; }
        public string SKO { get; set; }
        public string SKO_ERROR { get; set; }
        public string e { get; set; }
        public string Teta_A { get; set; }
        public string Teta_Sum { get; set; }
        public string Delta { get; set; }
        public string Delta_ERROR { get; set; }
    }
    public class Table5
    {
        public string Point { get; set; }
        public string Q_j { get; set; }
        public string f_j { get; set; }
        public string fv_j { get; set; }
        public string S_j { get; set; }
        public string S_j_ERROR { get; set; }
        public string K_j { get; set; }
        public string e_j { get; set; }
        public string Theta_Sum_j { get; set; }
        public string Delta_j { get; set; }
        public string Delta_j_ERROR { get; set; }
    }
    public class Parabola
    {
        public string fv_begin { get; set; }
        public string fv_end { get; set; }
        public string A { get; set; }
        public string B { get; set; }
        public string C { get; set; }
        public string Delta { get; set; }
    }
    public class CfgTables
    {
        public int Table4_921 { get; set; }
        public int Table4_922 { get; set; }
        public int Table4_923 { get; set; }
        public int Table5 { get; set; }
        public int Parabola { get; set; }
        public int PUType { get; set; }
        public int PUDetNum { get; set; }
        public int GHType { get; set; }
        public int PRStatus { get; set; }
    }
    public class AdditionalInfo
    {

        public string PR_Model_Top { get; set; }
        public string PoverkaPlace { get; set; }
        public string PlaceSIKN { get; set; }
        public string PlaceFactory { get; set; }
        public string PR_Model { get; set; }
        public string PR_DN { get; set; }
        public string PR_PN { get; set; }
        public string PR_FactoryNumber { get; set; }
        public string LineName { get; set; }
        public string Oil { get; set; }
        public string PU { get; set; }
        public string PU_Category { get; set; }
        public string PU_FactoryNumber { get; set; }
        public string PU_PN { get; set; }
        public string PU_Poverka_Date { get; set; }
        public string ProtokolNum { get; set; }
        public string StaffData_company { get; set; }
        public string StaffData_fio { get; set; }
        public string ARM_Poverka_Result { get; set; }
        public string ARM_Poverka_Result_Type { get; set; }
    }

    #endregion


}
