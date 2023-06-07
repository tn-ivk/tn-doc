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
    public class KMH_PV : DocGeneral
    {
        public KMH_PV(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
        {
            IdDoc = IdDoc.KMH_PV;
            PathToDocConfigFile = GetPathConfigFile();
            PathToDocEditConfigFile = GetPathEditConfigFile();
            PathToDocTemplateFile = GetPathTemplateFile();
        }

        private DbSet<TableKMH_PVList> ListDoc { get; set; }
        private DbSet<TableKMH_PVData> DataDoc { get; set; }

        private Doc Doc;
        private JObject jsonDoc;

        public override List<RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<RequestListDocs> docs = new();

            var list = (from item in ListDoc.AsNoTracking()
                        where item.DateTimeLong > UTBegin && item.DateTimeLong < UTEnd && item.id > 1
                        select item).ToList<TableKMH_PVList>();

            if (list.Count != 0)
            {
                foreach (var item in list)
                {
                    docs.Add(new()
                    {
                        Id = item.id,
                        DT = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(item.DateTimeLong).ToString("dd.MM.yy HH:mm"),
                        Description = item.KMH_TYPE == 0 ? "Динамическая вязкость" : "Кинематическая вязкость"
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

            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PV.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PV.BIK = list.BIK;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PV.PV = list.PV;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PV.KMH_TYPE = list.KMH_TYPE;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PV.Protocol = JsonDeserializeObject<Protocol>(ArrByteToString(list.Data.Protokol));
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PV.DateTimeString = list.DateTimeString;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PV.DateTimeLong = list.DateTimeLong;

            jsonDoc = JObject.Parse(JsonSerializeObject<Doc>(doc).ToString());
            Doc = doc;

            return JsonSerializeObject<Doc>(doc);
        }
        public override object GetViewDoc(int id, int protocolNumber)
        {
            var list = (from item in ListDoc.AsNoTracking()
                        where item.id == id
                        select item).First();

            list.Data = (from item in DataDoc.AsNoTracking()
                         where item.id == id
                         select item).First();

            Doc doc = new();

            LoadCfg(PathToDocConfigFile, ref doc);

            doc.Doc.Settings.General.ProtocolNumber = protocolNumber;

            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PV.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PV.BIK = list.BIK;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PV.PV = list.PV;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PV.KMH_TYPE = list.KMH_TYPE;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PV.Protocol = JsonDeserializeObject<Protocol>(ArrByteToString(list.Data.Protokol));
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PV.DateTimeString = list.DateTimeString;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PV.DateTimeLong = list.DateTimeLong;

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

                    if (item.Key == "PV1_AddInfo.ARM_KMH_Result")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.PV_AddInfo[0].ARM_KMH_Result;
                    if (item.Key == "PV2_AddInfo.ARM_KMH_Result")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.PV_AddInfo[1].ARM_KMH_Result;

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
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.Place_PSP;
                    else if (item.Key == "Place_Factory")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.Place_Factory;
                    else if (item.Key == "Place_SIKN_Name")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.Place_SIKN_Name;
                    else if (item.Key == "Laboratory_Name")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.Laboratory_Name;
                    else if (item.Key == "LAB_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.LAB_Type;
                    else if (item.Key == "LAB_ManufNum")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.LAB_ManufNum;
                    else if (item.Key == "LAB_CheckDate")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.LAB_CheckDate;
                    else if (item.Key == "ServiceStaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.ServiceStaffData;
                    else if (item.Key == "DeliveryStaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.DeliveryStaffData;
                    else if (item.Key == "RecievingStaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.RecievingStaffData;
                    else if (item.Key == "PV1_AddInfo.ProtokolNum")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.PV_AddInfo[0].ProtokolNum;
                    else if (item.Key == "PV1_AddInfo.PV_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.PV_AddInfo[0].PV_Type;
                    else if (item.Key == "PV1_AddInfo.PV_ManufNum")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.PV_AddInfo[0].PV_ManufNum;
                    else if (item.Key == "PV1_AddInfo.PV_CheckDate")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.PV_AddInfo[0].PV_CheckDate;
                    else if (item.Key == "PV1_AddInfo.ARM_KMH_Result")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.PV_AddInfo[0].ARM_KMH_Result;
                    else if (item.Key == "PV2_AddInfo.ProtokolNum")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.PV_AddInfo[1].ProtokolNum;
                    else if (item.Key == "PV2_AddInfo.PV_Type")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.PV_AddInfo[1].PV_Type;
                    else if (item.Key == "PV2_AddInfo.PV_ManufNum")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.PV_AddInfo[1].PV_ManufNum;
                    else if (item.Key == "PV2_AddInfo.PV_CheckDate")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.PV_AddInfo[1].PV_CheckDate;
                    else if (item.Key == "PV2_AddInfo.ARM_KMH_Result")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo.PV_AddInfo[1].ARM_KMH_Result;


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
            TableKMH_PVData row = new();
            GetViewDoc(data.DocID);

            ad = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PV.AdditionalInfo;

            if (data.Values.Exists(x => x.Key == "ServiceStaffData"))
                ad.ServiceStaffData = data.Values.Where(x => x.Key == "ServiceStaffData").Single().Value;
            if (data.Values.Exists(x => x.Key == "DeliveryStaffData"))
                ad.DeliveryStaffData = data.Values.Where(x => x.Key == "DeliveryStaffData").Single().Value;
            if (data.Values.Exists(x => x.Key == "RecievingStaffData"))
                ad.RecievingStaffData = data.Values.Where(x => x.Key == "RecievingStaffData").Single().Value;
            if (data.Values.Exists(x => x.Key == "PV1_AddInfo.ProtokolNum"))
                ad.PV_AddInfo[0].ProtokolNum = data.Values.Where(x => x.Key == "PV1_AddInfo.ProtokolNum").Single().Value;
            if (data.Values.Exists(x => x.Key == "PV1_AddInfo.ARM_KMH_Result"))
                ad.PV_AddInfo[0].ARM_KMH_Result = data.Values.Where(x => x.Key == "PV1_AddInfo.ARM_KMH_Result").Single().Value;
            if (data.Values.Exists(x => x.Key == "PV2_AddInfo.ProtokolNum"))
                ad.PV_AddInfo[1].ProtokolNum = data.Values.Where(x => x.Key == "PV2_AddInfo.ProtokolNum").Single().Value;
            if (data.Values.Exists(x => x.Key == "PV2_AddInfo.ARM_KMH_Result"))
                ad.PV_AddInfo[1].ARM_KMH_Result = data.Values.Where(x => x.Key == "PV2_AddInfo.ARM_KMH_Result").Single().Value;

            row.id = data.DocID;
            row.AdditionalInfo = StringToHexArrByte(ad);

            //DataDoc.Update(dd);

            this.Entry(row).Property(x => x.AdditionalInfo).IsModified = true;

            this.SaveChanges();

            return true;
        }

    }

    [Table("TableKMH_PV")]
    public class TableKMH_PVList
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int BIK { get; set; }
        public int PV { get; set; }
        public int KMH_TYPE { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public TableKMH_PVData Data { get; set; }

    }

    [Table("TableKMH_PV")]
    public class TableKMH_PVData
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
            ((DataIVKDoc)Doc.DataIVK).TableKMH_PV = new TableKMH_PV();
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
        public Dictionary<int, string> Parameter01Name { get; set; }
        public Dictionary<int, string> Parameter02Name { get; set; }
        public Dictionary<int, string> ControlConditions { get; set; }
        public Dictionary<int, string> UnitsMeasurement { get; set; }
        public Dictionary<int, string> OilTechParams { get; set; }
        public Dictionary<int, string> Dens { get; set; }
        public Dictionary<int, string> Visc_LAB { get; set; }
        public Dictionary<int, string> Delta { get; set; }
    }

    #endregion

    #endregion

    #region Doc.DataIVK

    public class DataIVKDoc : DataIVK
    {
        public TableKMH_PV TableKMH_PV { get; set; }
    }
    public class TableKMH_PV
    {
        public int id { get; set; }
        public int BIK { get; set; }
        public int PV { get; set; }
        public int KMH_TYPE { get; set; }
        public Protocol Protocol { get; set; }
        public AdditionalInfo AdditionalInfo { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
    }
    public class Protocol
    {
        public int TwoPV { get; set; }
        public int KMH_Mode { get; set; }
        public List<Protokol> Protokol { get; set; }
    }
    public class Protokol 
    {
        public string KMX_Condition { get; set; }
        public Table1 Table1 { get; set; }
        public Table2 Table2 { get; set; }
        public string Error_PV { get; set; }
    }
    public class Table1
    {
        public string UpLimitMeas_PV { get; set; }
        public string Delta_PV { get; set; }
        public string UpLimitMeas_LAB { get; set; }
        public string Delta_LAB { get; set; }
    }
    public class Table2
    {
        public string Q { get; set; }
        public string t { get; set; }
        public string P { get; set; }
        public string Dens { get; set; }
        public string Visc { get; set; }
        public string Visc_LAB { get; set; }
        public string t_LAB { get; set; }
        public string Delta { get; set; }
    }
    public class AdditionalInfo
    {
        public string Place_PSP { get; set; }
        public string Place_Factory { get; set; }
        public string Place_SIKN_Name { get; set; }
        public string Laboratory_Name { get; set; }
        public string LAB_Type { get; set; }
        public string LAB_ManufNum { get; set; }
        public string LAB_CheckDate { get; set; }
        public string ServiceStaffData { get; set; }
        public string DeliveryStaffData { get; set; }
        public string RecievingStaffData { get; set; }
        public List<PVAddInfo> PV_AddInfo { get; set; }
    }
    public class PVAddInfo
    {
        public string ProtokolNum { get; set; }
        public string PV_Type { get; set; }
        public string PV_ManufNum { get; set; }
        public string PV_CheckDate { get; set; }
        public string ARM_KMH_Result { get; set; }
    }

    #endregion
}
