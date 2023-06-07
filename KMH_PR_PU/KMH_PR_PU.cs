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
    public class KMH_PR_PU : DocGeneral
    {
        public KMH_PR_PU(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
        {
            IdDoc = IdDoc.KMH_PR_PU;
            PathToDocConfigFile = GetPathConfigFile();
            PathToDocEditConfigFile = GetPathEditConfigFile();
            PathToDocTemplateFile = GetPathTemplateFile();
        }

        private DbSet<TableKMH_PR_PUList> ListDoc { get; set; }
        private DbSet<TableKMH_PR_PUData> DataDoc { get; set; }

        private Doc Doc;
        private JObject jsonDoc;

        public override List<RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<RequestListDocs> docs = new();

            var list = (from item in ListDoc.AsNoTracking()
                        where item.DateTimeLong > UTBegin && item.DateTimeLong < UTEnd && item.id > 1
                        select item).ToList<TableKMH_PR_PUList>();

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

            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PR_PU.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PR_PU.MI = list.MI;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PR_PU.IL = list.IL;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PR_PU.Protokol = JsonDeserializeObject<Protokol>(ArrByteToString(list.Data.Protokol));
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PR_PU.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PR_PU.DateTimeString = list.DateTimeString;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PR_PU.DateTimeLong = list.DateTimeLong;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PR_PU.BIK_ID = list.BIK_ID;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PR_PU.IL_Name = ArrByteToString(list.IL_Name);

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

                    if (item.Key == "ARM_KMH_Result")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PR_PU.AdditionalInfo.ARM_KMH_Result;

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

                    if (item.Key == "Place")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PR_PU.AdditionalInfo.Place;
                    else if (item.Key == "Place_SIKN")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PR_PU.AdditionalInfo.Place_SIKN;
                    else if (item.Key == "Place_Factory")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PR_PU.AdditionalInfo.Place_Factory;
                    else if (item.Key == "PR_Name")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PR_PU.AdditionalInfo.PR_Name;
                    else if (item.Key == "PR_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PR_PU.AdditionalInfo.PR_Type;
                    else if (item.Key == "PR_FactoryNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PR_PU.AdditionalInfo.PR_FactoryNumber;
                    else if (item.Key == "PU_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PR_PU.AdditionalInfo.PU_Type;
                    else if (item.Key == "PU_FactoryNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PR_PU.AdditionalInfo.PU_FactoryNumber;
                    else if (item.Key == "PU_CheckDate")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PR_PU.AdditionalInfo.PU_CheckDate;
                    else if (item.Key == "ServiceStaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PR_PU.AdditionalInfo.ServiceStaffData;
                    else if (item.Key == "DeliveryStaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PR_PU.AdditionalInfo.DeliveryStaffData;
                    else if (item.Key == "RecievingStaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PR_PU.AdditionalInfo.RecievingStaffData;
                    else if (item.Key == "ARM_KMH_Result")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PR_PU.AdditionalInfo.ARM_KMH_Result;

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
            TableKMH_PR_PUData row = new();
            GetViewDoc(data.DocID);

            ad = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PR_PU.AdditionalInfo;

            if (data.Values.Exists(x => x.Key == "ServiceStaffData"))
                ad.ServiceStaffData = data.Values.Where(x => x.Key == "ServiceStaffData").Single().Value;
            if (data.Values.Exists(x => x.Key == "DeliveryStaffData"))
                ad.DeliveryStaffData = data.Values.Where(x => x.Key == "DeliveryStaffData").Single().Value;
            if (data.Values.Exists(x => x.Key == "RecievingStaffData"))
                ad.RecievingStaffData = data.Values.Where(x => x.Key == "RecievingStaffData").Single().Value;
            if (data.Values.Exists(x => x.Key == "ARM_KMH_Result"))
                ad.ARM_KMH_Result = data.Values.Where(x => x.Key == "ARM_KMH_Result").Single().Value;

            row.id = data.DocID;
            row.AdditionalInfo = StringToHexArrByte(ad);

            //DataDoc.Update(dd);

            this.Entry(row).Property(x => x.AdditionalInfo).IsModified = true;

            this.SaveChanges();

            return true;
        }
    }

    [Table("TableKMH_PR_PU")]
    public class TableKMH_PR_PUList
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int MI { get; set; }
        public int IL { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public int BIK_ID { get; set; }
        public byte[] IL_Name { get; set; }
        public TableKMH_PR_PUData Data { get; set; }

    }

    [Table("TableKMH_PR_PU")]
    public class TableKMH_PR_PUData
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
            ((DataIVKDoc)Doc.DataIVK).TableKMH_PR_PU = new TableKMH_PR_PU();
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
        public TableKMH_PR_PU TableKMH_PR_PU { get; set; }
    }
    public class TableKMH_PR_PU
    {
        public int id { get; set; }
        public int MI { get; set; }
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
        public int IsUSR { get; set; }
        public string Visc { get; set; }
        public string Delta_KMH_Max { get; set; }
        public string Delta_KMH_Max_ERROR { get; set; }
        public string OilType { get; set; }
        public int Use_fv { get; set; }
        public Table1 Table1 { get; set; }
        public Table2 Table2 { get; set; }
        public Table2_1 Table2_1 { get; set; }
    }
    public class Table1
    {
        public List<string> DetName { get; set; }
        public List<string> V0 { get; set; }
        public string m_D { get; set; }
        public string m_S { get; set; }
        public string m_E { get; set; }
        public string Alpha_t { get; set; }
        public string Alpha_D { get; set; }
        public List<string> Q { get; set; }
    }
    public class Table2
    {
        public List<Obj> Obj { get; set; }
    }
    public class Table2_1
    {
        public List<Obj> Obj { get; set; }
    }
    public class Obj
    {
        public string Ser { get; set; }
        public string Row { get; set; }
        public string Det { get; set; }
        public string Q_ji { get; set; }
        public string f_ji { get; set; }
        public string fv_ji { get; set; }
        public string t_PU_ji { get; set; }
        public string P_PU_ji { get; set; }
        public string t_PR_ji { get; set; }
        public string P_PR_ji { get; set; }
        public string t_cm_ji { get; set; }
        public string k_tp_ji { get; set; }
        public string V_ji { get; set; }
        public string N_ji { get; set; }
        public string K_ji { get; set; }
        public string Point { get; set; }
        public string K_j { get; set; }
        public string K_rasch_j { get; set; }
        public string Delta_j { get; set; }
        public string Delta_j_ERROR { get; set; }
        public string SKO_j { get; set; }
        public string SKO_j_ERROR { get; set; }
    }
    public class AdditionalInfo
    {
        public string Place { get; set; }
        public string Place_SIKN { get; set; }
        public string Place_Factory { get; set; }
        public string PR_Name { get; set; }
        public string PR_Type { get; set; }
        public string PR_FactoryNumber { get; set; }
        public string PU_Type { get; set; }
        public string PU_FactoryNumber { get; set; }
        public string PU_CheckDate { get; set; }
        public string ServiceStaffData { get; set; }
        public string DeliveryStaffData { get; set; }
        public string RecievingStaffData { get; set; }
        public string ARM_KMH_Result { get; set; }
        public string ProtokolNum { get; set; }
    }

    #endregion


}
