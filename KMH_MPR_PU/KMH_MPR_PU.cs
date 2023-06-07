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
    public class KMH_MPR_PU : DocGeneral
    {
        public KMH_MPR_PU(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
        {
            IdDoc = IdDoc.KMH_MPR_PU;
            PathToDocConfigFile = GetPathConfigFile();
            PathToDocEditConfigFile = GetPathEditConfigFile();
            PathToDocTemplateFile = GetPathTemplateFile();
        }

        private DbSet<TableKMH_MPR_PUList> ListDoc { get; set; }
        private DbSet<TableKMH_MPR_PUData> DataDoc { get; set; }

        private Doc Doc;
        private JObject jsonDoc;

        public override List<RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<RequestListDocs> docs = new();

            var list = (from item in ListDoc.AsNoTracking()
                        where item.DateTimeLong > UTBegin && item.DateTimeLong < UTEnd && item.id > 1
                        select item).ToList<TableKMH_MPR_PUList>();

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

            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_PU.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_PU.IL = list.IL;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_PU.MI = list.MI;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_PU.Protokol = JsonDeserializeObject<Protokol>(ArrByteToString(list.Data.Protokol));
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_PU.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_PU.DateTimeString = list.DateTimeString;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_PU.DateTimeLong = list.DateTimeLong;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_PU.BIK_ID = list.BIK_ID;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_PU.IL_Name = ArrByteToString(list.IL_Name);

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
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_PU.AdditionalInfo.ARM_KMH_Result;

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
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_PU.AdditionalInfo.ProtokolNum;
                    else if (item.Key == "Place_PSP")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_PU.AdditionalInfo.Place_PSP;
                    else if (item.Key == "Place_Factory")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_PU.AdditionalInfo.Place_Factory;
                    else if (item.Key == "SIKN")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_PU.AdditionalInfo.SIKN;
                    else if (item.Key == "PR_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_PU.AdditionalInfo.PR_Type;
                    else if (item.Key == "PR_FactoryNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_PU.AdditionalInfo.PR_FactoryNumber;
                    else if (item.Key == "PU_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_PU.AdditionalInfo.PU_Type;
                    else if (item.Key == "PU_FactoryNumber")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_PU.AdditionalInfo.PU_FactoryNumber;
                    else if (item.Key == "PU_CheckDate")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_PU.AdditionalInfo.PU_CheckDate;
                    else if (item.Key == "ServiceStaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_PU.AdditionalInfo.ServiceStaffData;
                    else if (item.Key == "DeliveryStaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_PU.AdditionalInfo.DeliveryStaffData;
                    else if (item.Key == "RecievingStaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_PU.AdditionalInfo.RecievingStaffData;
                    else if (item.Key == "ARM_KMH_Result")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_PU.AdditionalInfo.ARM_KMH_Result;

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
            TableKMH_MPR_PUData row = new();
            GetViewDoc(data.DocID);

            ad = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_PU.AdditionalInfo;

            if (data.Values.Exists(x => x.Key == "ProtokolNum"))
                ad.ProtokolNum = data.Values.Where(x => x.Key == "ProtokolNum").Single().Value;
            if (data.Values.Exists(x => x.Key == "Place_PSP")) 
                ad.Place_PSP = data.Values.Where(x => x.Key == "Place_PSP").Single().Value;
            if (data.Values.Exists(x => x.Key == "Place_Factory"))
                ad.Place_Factory = data.Values.Where(x => x.Key == "Place_Factory").Single().Value;
            if (data.Values.Exists(x => x.Key == "SIKN"))
                ad.SIKN = data.Values.Where(x => x.Key == "SIKN").Single().Value;
            if (data.Values.Exists(x => x.Key == "PR_Type"))
                ad.PR_Type = data.Values.Where(x => x.Key == "PR_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "PR_FactoryNumber"))
                ad.PR_FactoryNumber = data.Values.Where(x => x.Key == "PR_FactoryNumber").Single().Value;
            if (data.Values.Exists(x => x.Key == "PU_Type"))
                ad.PU_Type = data.Values.Where(x => x.Key == "PU_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "PU_FactoryNumber"))
                ad.PU_FactoryNumber = data.Values.Where(x => x.Key == "PU_FactoryNumber").Single().Value;
            if (data.Values.Exists(x => x.Key == "PU_CheckDate"))
                ad.PU_CheckDate = data.Values.Where(x => x.Key == "PU_CheckDate").Single().Value;
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

    [Table("TableKMH_MPR_PU")]
    public class TableKMH_MPR_PUList
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int IL { get; set; }
        public int MI { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public int BIK_ID { get; set; }
        public byte[] IL_Name { get; set; }
        public TableKMH_MPR_PUData Data { get; set; }
    }

    [Table("TableKMH_MPR_PU")]
    public class TableKMH_MPR_PUData
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
            ((DataIVKDoc)Doc.DataIVK).TableKMH_MPR_PU = new TableKMH_MPR_PU();
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
        public TableKMH_MPR_PU TableKMH_MPR_PU { get; set; }
    }
    public class TableKMH_MPR_PU
    {
        public int id { get; set; }
        public int IL { get; set; }
        public int MI { get; set; }
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
        public string Delta_max { get; set; }
        public string DeltaMaxError { get; set; }
        public string Visc { get; set; }
        public Table1 Table1 { get; set; }
        public List<Table2> Table2 { get; set; }
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
        public string Alpha_k1_PU { get; set; }
        public string Alpha_t_cm { get; set; }
        public string Delta_t_PU { get; set; }
        public string Delta_PP { get; set; }
        public string Delta_t_PP { get; set; }
        public string Delta_k_UOI { get; set; }
        public string KF_konf { get; set; }
        public string ZS { get; set; }
    }
    public class Table2
    {
        public string Ser { get; set; }
        public string Row { get; set; }
        public string Q_ji { get; set; }
        public string T_ji { get; set; }
        public string t_PU_ji { get; set; }
        public string P_PU_ji { get; set; }
        public string t_D_ji { get; set; }
        public string Dens_ji { get; set; }
        public string DensTemp_ji { get; set; }
        public string DensPress_ji { get; set; }
        public string KinimVisc_ji { get; set; }
        public string N_ji { get; set; }
        public string V_PU_PR_ji { get; set; }
        public string Dens_PP_PR_ji { get; set; }
        public string M_RA_ji { get; set; }
        public string M_from_GH_ji { get; set; }
        public string Delta_KMH_ji { get; set; }
        public string Delta_KMH_ji_ERROR { get; set; }
    }
    public class AdditionalInfo
    {
        public string ProtokolNum { get; set; }
        public string Place_PSP { get; set; }
        public string Place_Factory { get; set; }
        public string SIKN { get; set; }
        public string PR_Type { get; set; }
        public string PR_FactoryNumber { get; set; }
        public string PU_Type { get; set; }
        public string PU_FactoryNumber { get; set; }
        public string PU_CheckDate { get; set; }
        public string ServiceStaffData { get; set; }
        public string DeliveryStaffData { get; set; }
        public string RecievingStaffData { get; set; }
        public string ARM_KMH_Result { get; set; }
    }
    #endregion


}
