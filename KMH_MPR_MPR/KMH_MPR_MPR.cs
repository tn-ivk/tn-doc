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
    public class KMH_MPR_MPR : DocGeneral
    {
        public KMH_MPR_MPR(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
        {
            IdDoc = IdDoc.KMH_MPR_MPR;
            PathToDocConfigFile = GetPathConfigFile();
            PathToDocEditConfigFile = GetPathEditConfigFile();
            PathToDocTemplateFile = GetPathTemplateFile();
        }

        private DbSet<TableKMH_MPR_MPRList> ListDoc { get; set; }
        private DbSet<TableKMH_MPR_MPRData> DataDoc { get; set; }

        private Doc Doc;
        private JObject jsonDoc;

        public override List<RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<RequestListDocs> docs = new();

            var list = (from item in ListDoc.AsNoTracking()
                        where item.DateTimeLong > UTBegin && item.DateTimeLong < UTEnd && item.id > 1
                        select item).ToList<TableKMH_MPR_MPRList>();

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

            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_MPR.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_MPR.IL = list.IL;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_MPR.MI = list.MI;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_MPR.Protokol = JsonDeserializeObject<Protokol>(ArrByteToString(list.Data.Protokol));
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_MPR.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_MPR.DateTimeString = list.DateTimeString;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_MPR.DateTimeLong = list.DateTimeLong;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_MPR.BIK_ID = list.BIK_ID;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_MPR.IL_Name = ArrByteToString(list.IL_Name);

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
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_MPR.AdditionalInfo.ARM_KMH_Result;

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
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_MPR.AdditionalInfo.ProtokolNum;
                    else if (item.Key == "LineName")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_MPR.AdditionalInfo.LineName;
                    else if (item.Key == "Place_PSP")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_MPR.AdditionalInfo.Place_PSP;
                    else if (item.Key == "Place_Factory")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_MPR.AdditionalInfo.Place_Factory;
                    else if (item.Key == "Place_SIKN_Name")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_MPR.AdditionalInfo.Place_SIKN_Name;
                    else if (item.Key == "MPR_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_MPR.AdditionalInfo.MPR_Type;
                    else if (item.Key == "MPR_FactoryNum")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_MPR.AdditionalInfo.MPR_FactoryNum;
                    else if (item.Key == "MPR_CTRL_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_MPR.AdditionalInfo.MPR_CTRL_Type;
                    else if (item.Key == "MPR_CTRL_Factory")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_MPR.AdditionalInfo.MPR_CTRL_Factory;
                    else if (item.Key == "MPR_CTRL_CheckDate")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_MPR.AdditionalInfo.MPR_CTRL_CheckDate;
                    else if (item.Key == "ARM_KMH_Result")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_MPR.AdditionalInfo.ARM_KMH_Result;
                    else if (item.Key == "ServiceStaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_MPR.AdditionalInfo.ServiceStaffData;
                    else if (item.Key == "DeliveryStaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_MPR.AdditionalInfo.DeliveryStaffData;
                    else if (item.Key == "RecievingStaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_MPR.AdditionalInfo.RecievingStaffData;
                    else if (item.Key == "ARM_CheckDate")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_MPR.AdditionalInfo.ARM_CheckDate;

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
            TableKMH_MPR_MPRData row = new();
            GetViewDoc(data.DocID);

            ad = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MPR_MPR.AdditionalInfo;

            if (data.Values.Exists(x => x.Key == "ProtokolNum"))
                ad.ProtokolNum = data.Values.Where(x => x.Key == "ProtokolNum").Single().Value;
            if (data.Values.Exists(x => x.Key == "LineName"))
                ad.LineName = data.Values.Where(x => x.Key == "LineName").Single().Value;
            if (data.Values.Exists(x => x.Key == "Place_PSP"))
                ad.Place_PSP = data.Values.Where(x => x.Key == "Place_PSP").Single().Value;
            if (data.Values.Exists(x => x.Key == "Place_Factory"))
                ad.Place_Factory = data.Values.Where(x => x.Key == "Place_Factory").Single().Value;
            if (data.Values.Exists(x => x.Key == "Place_SIKN_Name"))
                ad.Place_SIKN_Name = data.Values.Where(x => x.Key == "Place_SIKN_Name").Single().Value;
            if (data.Values.Exists(x => x.Key == "MPR_Type"))
                ad.MPR_Type = data.Values.Where(x => x.Key == "MPR_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "MPR_FactoryNum"))
                ad.MPR_FactoryNum = data.Values.Where(x => x.Key == "MPR_FactoryNum").Single().Value;
            if (data.Values.Exists(x => x.Key == "MPR_CTRL_Type"))
                ad.MPR_CTRL_Type = data.Values.Where(x => x.Key == "MPR_CTRL_Type").Single().Value;
            if (data.Values.Exists(x => x.Key == "MPR_CTRL_Factory"))
                ad.MPR_CTRL_Factory = data.Values.Where(x => x.Key == "MPR_CTRL_Factory").Single().Value;
            if (data.Values.Exists(x => x.Key == "MPR_CTRL_CheckDate"))
                ad.MPR_CTRL_CheckDate = data.Values.Where(x => x.Key == "MPR_CTRL_CheckDate").Single().Value;
            if (data.Values.Exists(x => x.Key == "ARM_KMH_Result"))
                ad.ARM_KMH_Result = data.Values.Where(x => x.Key == "ARM_KMH_Result").Single().Value;
            if (data.Values.Exists(x => x.Key == "ServiceStaffData"))
                ad.ServiceStaffData = data.Values.Where(x => x.Key == "ServiceStaffData").Single().Value;
            if (data.Values.Exists(x => x.Key == "DeliveryStaffData"))
                ad.DeliveryStaffData = data.Values.Where(x => x.Key == "DeliveryStaffData").Single().Value;
            if (data.Values.Exists(x => x.Key == "RecievingStaffData")) 
                ad.RecievingStaffData = data.Values.Where(x => x.Key == "RecievingStaffData").Single().Value;
            if (data.Values.Exists(x => x.Key == "ARM_CheckDate"))
                ad.ARM_CheckDate = data.Values.Where(x => x.Key == "ARM_CheckDate").Single().Value;            

            row.id = data.DocID;
            row.AdditionalInfo = StringToHexArrByte(ad);

            //DataDoc.Update(dd);

            this.Entry(row).Property(x => x.AdditionalInfo).IsModified = true;

            this.SaveChanges();

            return true;
        }
    }

    [Table("TableKMH_MPR_MPR")]
    public class TableKMH_MPR_MPRList
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int IL { get; set; }
        public int MI { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public int BIK_ID { get; set; }
        public byte[] IL_Name { get; set; }
        public TableKMH_MPR_MPRData Data { get; set; }
    }

    [Table("TableKMH_MPR_MPR")]
    public class TableKMH_MPR_MPRData
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
            ((DataIVKDoc)Doc.DataIVK).TableKMH_MPR_MPR = new TableKMH_MPR_MPR();
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
        public TableKMH_MPR_MPR TableKMH_MPR_MPR { get; set; }
    }
    public class TableKMH_MPR_MPR
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
        public string Visc { get; set; }
        public string Delta_Max { get; set; }
        public string OilType { get; set; }
        public List<Table1> Table1 { get; set; }
    }
    public class Table1
    {
        public string Ser { get; set; }
        public string Row { get; set; }
        public string Q_K_ji { get; set; }
        public string t_K_ji { get; set; }
        public string P_K_ji { get; set; }
        public string Q_P_ji { get; set; }
        public string t_P_ji { get; set; }
        public string P_P_ji { get; set; }
        public string M_K_ji { get; set; }
        public string M_P_ji { get; set; }
        public string Delta_ji { get; set; }
    }
    public class AdditionalInfo
    {
        public string ProtokolNum { get; set; }
        public string LineName { get; set; }
        public string Place_PSP { get; set; }
        public string Place_Factory { get; set; }
        public string Place_SIKN_Name { get; set; }
        public string MPR_Type { get; set; }
        public string MPR_FactoryNum { get; set; }
        public string MPR_CTRL_Type { get; set; }
        public string MPR_CTRL_Factory { get; set; }
        public string MPR_CTRL_CheckDate { get; set; }
        public string ARM_KMH_Result { get; set; }
        public string ServiceStaffData { get; set; }
        public string DeliveryStaffData { get; set; }
        public string RecievingStaffData { get; set; }
        public string ARM_CheckDate { get; set; }
    }

    #endregion


}
