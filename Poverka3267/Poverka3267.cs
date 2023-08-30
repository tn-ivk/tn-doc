using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using TN.Doc;
using TN.DocData;

namespace Poverka3267
{
    public class Poverka3267 : DocGeneral
    {
        public Poverka3267(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
        {
            IdDoc = IdDoc.Poverka3267;
            PathToDocConfigFile = GetPathConfigFile();
            PathToDocEditConfigFile = GetPathEditConfigFile();
            PathToDocTemplateFile = GetPathTemplateFile();
        }

        private DbSet<TablePoverka3267List> ListDoc { get; set; }
        private DbSet<TablePoverka3267Data> DataDoc { get; set; }

        private Doc Doc;
        private JObject jsonDoc;

        public override List<RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<RequestListDocs> docs = new();

            var list = (from item in ListDoc.AsNoTracking()
                        where item.DateTimeLong > UTBegin && item.DateTimeLong < UTEnd && item.id > 1
                        select item).ToList<TablePoverka3267List>();

            if (list.Count != 0)
            {
                foreach (var item in list)
                {
                    docs.Add(new()
                    {
                        Id = item.id,
                        DT = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(item.DateTimeLong).ToString("dd.MM.yy HH:mm"),
                        Description = item.IL_Name == null ? $"ИЛ-{item.IL}" : $"{ArrByteToString(item.IL_Name)}"
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

            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3267.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3267.IL = list.IL;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3267.Protokol = JsonDeserializeObject<Protokol>(ArrByteToString(list.Data.Protokol));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3267.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3267.DateTimeString = list.DateTimeString;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3267.DateTimeLong = list.DateTimeLong;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3267.BIK_ID = list.BIK_ID;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3267.IL_Name = list.IL_Name == null ? $"ИЛ-{list.IL}" : $"{ArrByteToString(list.IL_Name)}";

            string fileName = string.Format("{0}_Поверка МИ3267",
                ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3267.DateTimeString);
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
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3267.AdditionalInfo.ARM_Poverka_Result;

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
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3267.AdditionalInfo.Place_PSP;
                    else if (item.Key == "Place_SIKN")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3267.AdditionalInfo.Place_SIKN;
                    else if (item.Key == "Place_Factory")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3267.AdditionalInfo.Place_Factory;
                    else if (item.Key == "PR_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3267.AdditionalInfo.PR_Type;
                    else if (item.Key == "PR_FactoryNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3267.AdditionalInfo.PR_FactoryNumber;
                    else if (item.Key == "PR_LineName")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3267.AdditionalInfo.PR_LineName;
                    else if (item.Key == "EPR_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3267.AdditionalInfo.EPR_Type;
                    else if (item.Key == "EPR_FactoryNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3267.AdditionalInfo.EPR_FactoryNumber;
                    else if (item.Key == "IVK_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3267.AdditionalInfo.IVK_Type;
                    else if (item.Key == "IVK_FactoryNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3267.AdditionalInfo.IVK_FactoryNumber;
                    else if (item.Key == "OilType")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3267.AdditionalInfo.OilType;
                    else if (item.Key == "ProtokolNum")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3267.AdditionalInfo.ProtokolNum;
                    else if (item.Key == "StaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3267.AdditionalInfo.StaffData;

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
            TablePoverka3267Data row = new();
            GetViewDoc(data.DocID);

            ad = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3267.AdditionalInfo;

            if (data.Values.Exists(x => x.Key == "Place_PSP"))
                ad.Place_PSP = data.Values.Where(x => x.Key == "Place_PSP").Single().Value;
            if (data.Values.Exists(x => x.Key == "Place_SIKN"))
                ad.Place_SIKN = data.Values.Where(x => x.Key == "Place_SIKN").Single().Value;
            if (data.Values.Exists(x => x.Key == "Place_Factory"))
                ad.Place_Factory = data.Values.Where(x => x.Key == "Place_Factory").Single().Value;
            if (data.Values.Exists(x => x.Key == "PR_Type"))
                ad.PR_Type = data.Values.Where(x => x.Key == "PR_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "PR_FactoryNumber"))
                ad.PR_FactoryNumber = data.Values.Where(x => x.Key == "PR_FactoryNumber").Single().Value;
            if (data.Values.Exists(x => x.Key == "PR_LineName"))
                ad.PR_LineName = data.Values.Where(x => x.Key == "PR_LineName").Single().Value;
            if (data.Values.Exists(x => x.Key == "EPR_Type"))
                ad.EPR_Type = data.Values.Where(x => x.Key == "EPR_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "EPR_FactoryNumber"))
                ad.EPR_FactoryNumber = data.Values.Where(x => x.Key == "EPR_FactoryNumber").Single().Value;
            if (data.Values.Exists(x => x.Key == "IVK_Type"))
                ad.IVK_Type = data.Values.Where(x => x.Key == "IVK_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "IVK_FactoryNumber"))
                ad.IVK_FactoryNumber = data.Values.Where(x => x.Key == "IVK_FactoryNumber").Single().Value;
            if (data.Values.Exists(x => x.Key == "OilType"))
                ad.OilType = data.Values.Where(x => x.Key == "OilType").Single().Value;
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

    [Table("TablePoverka3267")]
    public class TablePoverka3267List
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int IL { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public int BIK_ID { get; set; }
        public byte[] IL_Name { get; set; }
        public TablePoverka3267Data Data { get; set; }
    }

    [Table("TablePoverka3267")]
    public class TablePoverka3267Data
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
            ((DataIVKDoc)Doc.DataIVK).TablePoverka3267 = new TablePoverka3267();
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
        public TablePoverka3267 TablePoverka3267 { get; set; }
    }
    public class TablePoverka3267
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
        public string Visc { get; set; }
        public string OilType { get; set; }
        public Table1 Table1 { get; set; }
        public List<Table2> Table2 { get; set; }
        public List<Table3> Table3 { get; set; }
        public Table4 Table4 { get; set; }
    }
    public class Table1
    {
        public string Delta_EPR { get; set; }
        public string Delt_t_EPR { get; set; }
        public string Delt_t_PR { get; set; }
        public string Delt_IVK { get; set; }
        public string Delt_Visc { get; set; }
    }
    public class Table2
    {
        public string Ser { get; set; }
        public string Row { get; set; }
        public string Q_ji { get; set; }
        public string T_ji { get; set; }
        public string t_EPR_ji { get; set; }
        public string P_EPR_ji { get; set; }
        public string N_EPR_ji { get; set; }
        public string K_EPR_ji { get; set; }
        public string Dens_ji { get; set; }
        public string DensTemp_ji { get; set; }
        public string DensPress_ji { get; set; }
        public string Beta_ji { get; set; }
        public string v_ji { get; set; }
        public string t_PR_ji { get; set; }
        public string P_PR_ji { get; set; }
        public string f_ji { get; set; }
        public string N_ji { get; set; }
        public string K_ji { get; set; }
        public string U_ji { get; set; }
        public string U_ERROR_ji { get; set; }
    }
    public class Table3
    {
        public string Point { get; set; }
        public string Q_j { get; set; }
        public string f_j { get; set; }
        public string K_j { get; set; }
        public string S_j { get; set; }
        public string S_j_ERROR { get; set; }
        public string SK_j { get; set; }
        public string n_j { get; set; }
        public string S0_j { get; set; }
        public string t095_j { get; set; }
        public string e_j { get; set; }
    }
    public class Table4
    {
        public string Q_min { get; set; }
        public string Q_max { get; set; }
        public string v_min { get; set; }
        public string v_max { get; set; }
        public string S0 { get; set; }
        public string e { get; set; }
        public string Teta_A { get; set; }
        public string Teta_t { get; set; }
        public string Teta_Sum { get; set; }
        public string Delta { get; set; }
        public string Delta_ERROR { get; set; }
    }
    public class AdditionalInfo
    {
        public string Place_PSP { get; set; }
        public string Place_SIKN { get; set; }
        public string Place_Factory { get; set; }
        public string PR_Type { get; set; }
        public string PR_FactoryNumber { get; set; }
        public string PR_LineName { get; set; }
        public string EPR_Type { get; set; }
        public string EPR_FactoryNumber { get; set; }
        public string IVK_Type { get; set; }
        public string IVK_FactoryNumber { get; set; }
        public string OilType { get; set; }
        public string ProtokolNum { get; set; }
        public string ARM_Poverka_Result { get; set; }
        public string StaffData { get; set; }
    }

    #endregion
}
