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
    public class DocDocKMH_PP_Areom : DocGeneral
    {
        public DocDocKMH_PP_Areom(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
        {
            IdDoc = IdDoc.KMH_PP_Areom;
            PathToDocConfigFile = GetPathConfigFile();
            PathToDocEditConfigFile = GetPathEditConfigFile();
            PathToDocTemplateFile = GetPathTemplateFile();
        }

        private DbSet<TableKMH_PP_AreomList> ListDoc { get; set; }
        private DbSet<TableKMH_PP_AreomData> DataDoc { get; set; }

        private Doc Doc;
        private JObject jsonDoc;

        public override List<RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<RequestListDocs> docs = new();

            var list = (from item in ListDoc.AsNoTracking()
                        where item.DateTimeLong > UTBegin && item.DateTimeLong < UTEnd && item.id > 1
                        select item).ToList<TableKMH_PP_AreomList>();

            if (list.Count != 0)
            {
                foreach (var item in list)
                {
                    docs.Add(new()
                    {
                        Id = item.id,
                        DT = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(item.DateTimeLong).ToString("dd.MM.yy HH:mm"),
                        Description = item.KMH_TYPE == 0 ? "КМХ ПП по лаб. ПЛ" : "КМХ ПП по ареометру"
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

            Doc doc = new ();

            LoadCfg(PathToDocConfigFile, ref doc);

            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PP_Areom.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PP_Areom.BIK = list.BIK;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PP_Areom.KMH_TYPE = list.KMH_TYPE;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PP_Areom.Protokol = JsonDeserializeObject<Protokol>(ArrByteToString(list.Data.Protokol));
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PP_Areom.DateTimeString = list.DateTimeString;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PP_Areom.DateTimeLong = list.DateTimeLong;

            //JsonDeserializeObject<DocKMH_PP_Areom.AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));

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

            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PP_Areom.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PP_Areom.BIK = list.BIK;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PP_Areom.KMH_TYPE = list.KMH_TYPE;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PP_Areom.Protokol = JsonDeserializeObject<Protokol>(ArrByteToString(list.Data.Protokol));
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PP_Areom.DateTimeString = list.DateTimeString;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_PP_Areom.DateTimeLong = list.DateTimeLong;

            //JsonDeserializeObject<DocKMH_PP_Areom.AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));

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

                    if (item.Key == "PP1_AddInfo.ARM_KMH_Result")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP1_AddInfo.ARM_KMH_Result;
                    else if (item.Key == "PP2_AddInfo.ARM_KMH_Result")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP2_AddInfo.ARM_KMH_Result;

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

                    if (item.Key == "PP1_AddInfo.SIKN_Num")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP1_AddInfo.SIKN_Num;
                    else if (item.Key == "PP1_AddInfo.PSP_Name")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP1_AddInfo.PSP_Name;
                    else if (item.Key == "PP1_AddInfo.SensName_Oper")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP1_AddInfo.SensName_Oper;
                    else if (item.Key == "PP1_AddInfo.ManufNum_Oper")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP1_AddInfo.ManufNum_Oper;
                    else if (item.Key == "PP1_AddInfo.CheckDate_Oper")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP1_AddInfo.CheckDate_Oper;
                    else if (item.Key == "PP1_AddInfo.SensName_Areom")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP1_AddInfo.SensName_Areom;
                    else if (item.Key == "PP1_AddInfo.ManufNum_Areom")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP1_AddInfo.ManufNum_Areom;
                    else if (item.Key == "PP1_AddInfo.CheckDate_Areom")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP1_AddInfo.CheckDate_Areom;
                    else if (item.Key == "PP1_AddInfo.AbsError_Areom")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP1_AddInfo.AbsError_Areom;
                    else if (item.Key == "PP1_AddInfo.SensName_Areom2")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP1_AddInfo.SensName_Areom2;
                    else if (item.Key == "PP1_AddInfo.ManufNum_Areom2")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP1_AddInfo.ManufNum_Areom2;
                    else if (item.Key == "PP1_AddInfo.CheckDate_Areom2")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP1_AddInfo.CheckDate_Areom2;
                    else if (item.Key == "PP1_AddInfo.AbsError_Areom2")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP1_AddInfo.AbsError_Areom2;
                    else if (item.Key == "PP1_AddInfo.ServiceStaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP1_AddInfo.ServiceStaffData;
                    else if (item.Key == "PP1_AddInfo.DeliveryStaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP1_AddInfo.DeliveryStaffData;
                    else if (item.Key == "PP1_AddInfo.RecievingStaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP1_AddInfo.RecievingStaffData;
                    else if (item.Key == "PP1_AddInfo.ARM_KMH_Result")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP1_AddInfo.ARM_KMH_Result;
                    else if (item.Key == "PP1_AddInfo.Error")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP1_AddInfo.Error;
                    else if (item.Key == "PP2_AddInfo.SIKN_Num")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP2_AddInfo.SIKN_Num;
                    else if (item.Key == "PP2_AddInfo.PSP_Name")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP2_AddInfo.PSP_Name;
                    else if (item.Key == "PP2_AddInfo.SensName_Oper")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP2_AddInfo.SensName_Oper;
                    else if (item.Key == "PP2_AddInfo.ManufNum_Oper")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP2_AddInfo.ManufNum_Oper;
                    else if (item.Key == "PP2_AddInfo.CheckDate_Oper")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP2_AddInfo.CheckDate_Oper;
                    else if (item.Key == "PP2_AddInfo.SensName_Areom")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP2_AddInfo.SensName_Areom;
                    else if (item.Key == "PP2_AddInfo.ManufNum_Areom")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP2_AddInfo.ManufNum_Areom;
                    else if (item.Key == "PP2_AddInfo.CheckDate_Areom")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP2_AddInfo.CheckDate_Areom;
                    else if (item.Key == "PP2_AddInfo.AbsError_Areom")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP2_AddInfo.AbsError_Areom;
                    else if (item.Key == "PP2_AddInfo.SensName_Areom2")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP2_AddInfo.SensName_Areom2;
                    else if (item.Key == "PP2_AddInfo.ManufNum_Areom2")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP2_AddInfo.ManufNum_Areom2;
                    else if (item.Key == "PP2_AddInfo.CheckDate_Areom2")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP2_AddInfo.CheckDate_Areom2;
                    else if (item.Key == "PP2_AddInfo.AbsError_Areom2")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP2_AddInfo.AbsError_Areom2;
                    else if (item.Key == "PP2_AddInfo.ServiceStaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP2_AddInfo.ServiceStaffData;
                    else if (item.Key == "PP2_AddInfo.DeliveryStaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP2_AddInfo.DeliveryStaffData;
                    else if (item.Key == "PP2_AddInfo.RecievingStaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP2_AddInfo.RecievingStaffData;
                    else if (item.Key == "PP2_AddInfo.ARM_KMH_Result")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP2_AddInfo.ARM_KMH_Result;
                    else if (item.Key == "PP2_AddInfo.Error")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo.PP2_AddInfo.Error;

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
            AdditionalInfo ad = new() { PP1_AddInfo = new(), PP2_AddInfo = new ()};
            TableKMH_PP_AreomData row = new ();
            GetViewDoc(data.DocID);

            ad = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_PP_Areom.AdditionalInfo;

            if (data.Values.Exists(x => x.Key == "PP1_AddInfo.SensName_Areom"))
                ad.PP1_AddInfo.SensName_Areom = data.Values.Where(x => x.Key == "PP1_AddInfo.SensName_Areom").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP1_AddInfo.ManufNum_Areom"))
                ad.PP1_AddInfo.ManufNum_Areom = data.Values.Where(x => x.Key == "PP1_AddInfo.ManufNum_Areom").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP1_AddInfo.CheckDate_Areom"))
                ad.PP1_AddInfo.CheckDate_Areom = data.Values.Where(x => x.Key == "PP1_AddInfo.CheckDate_Areom").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP1_AddInfo.AbsError_Areom"))
                ad.PP1_AddInfo.AbsError_Areom = data.Values.Where(x => x.Key == "PP1_AddInfo.AbsError_Areom").Single().Value;

            if (data.Values.Exists(x => x.Key == "PP1_AddInfo.SensName_Areom"))
                ad.PP1_AddInfo.SensName_Areom2 = data.Values.Where(x => x.Key == "PP1_AddInfo.SensName_Areom").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP1_AddInfo.ManufNum_Areom"))
                ad.PP1_AddInfo.ManufNum_Areom2 = data.Values.Where(x => x.Key == "PP1_AddInfo.ManufNum_Areom").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP1_AddInfo.CheckDate_Areom"))
                ad.PP1_AddInfo.CheckDate_Areom2 = data.Values.Where(x => x.Key == "PP1_AddInfo.CheckDate_Areom").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP1_AddInfo.AbsError_Areom"))
                ad.PP1_AddInfo.AbsError_Areom2 = data.Values.Where(x => x.Key == "PP1_AddInfo.AbsError_Areom").Single().Value;

            if (data.Values.Exists(x => x.Key == "PP1_AddInfo.ARM_KMH_Result"))
                ad.PP1_AddInfo.ARM_KMH_Result = data.Values.Where(x => x.Key == "PP1_AddInfo.ARM_KMH_Result").Single().Value;

            if (data.Values.Exists(x => x.Key == "PP1_AddInfo.ServiceStaffData"))
                ad.PP1_AddInfo.ServiceStaffData = data.Values.Where(x => x.Key == "PP1_AddInfo.ServiceStaffData").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP1_AddInfo.DeliveryStaffData"))
                ad.PP1_AddInfo.DeliveryStaffData = data.Values.Where(x => x.Key == "PP1_AddInfo.DeliveryStaffData").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP1_AddInfo.RecievingStaffData"))
                ad.PP1_AddInfo.RecievingStaffData = data.Values.Where(x => x.Key == "PP1_AddInfo.RecievingStaffData").Single().Value;

            //ad.PP1_AddInfo.CheckDate_Oper = "";
            //ad.PP1_AddInfo.Error = "";
            //ad.PP1_AddInfo.ManufNum_Oper = "";
            //ad.PP1_AddInfo.PSP_Name = "";
            //ad.PP1_AddInfo.SensName_Oper = "";
            //ad.PP1_AddInfo.SIKN_Num = "";

            if (data.Values.Exists(x => x.Key == "PP2_AddInfo.SensName_Areom"))
                ad.PP2_AddInfo.SensName_Areom = data.Values.Where(x => x.Key == "PP2_AddInfo.SensName_Areom").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP2_AddInfo.ManufNum_Areom"))
                ad.PP2_AddInfo.ManufNum_Areom = data.Values.Where(x => x.Key == "PP2_AddInfo.ManufNum_Areom").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP2_AddInfo.CheckDate_Areom"))
                ad.PP2_AddInfo.CheckDate_Areom = data.Values.Where(x => x.Key == "PP2_AddInfo.CheckDate_Areom").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP2_AddInfo.AbsError_Areom"))
                ad.PP2_AddInfo.AbsError_Areom = data.Values.Where(x => x.Key == "PP2_AddInfo.AbsError_Areom").Single().Value;

            if (data.Values.Exists(x => x.Key == "PP2_AddInfo.SensName_Areom"))
                ad.PP2_AddInfo.SensName_Areom2 = data.Values.Where(x => x.Key == "PP2_AddInfo.SensName_Areom").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP2_AddInfo.ManufNum_Areom"))
                ad.PP2_AddInfo.ManufNum_Areom2 = data.Values.Where(x => x.Key == "PP2_AddInfo.ManufNum_Areom").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP2_AddInfo.CheckDate_Areom"))
                ad.PP2_AddInfo.CheckDate_Areom2 = data.Values.Where(x => x.Key == "PP2_AddInfo.CheckDate_Areom").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP2_AddInfo.AbsError_Areom"))
                ad.PP2_AddInfo.AbsError_Areom2 = data.Values.Where(x => x.Key == "PP2_AddInfo.AbsError_Areom").Single().Value;

            if (data.Values.Exists(x => x.Key == "PP2_AddInfo.ARM_KMH_Result"))
                ad.PP2_AddInfo.ARM_KMH_Result = data.Values.Where(x => x.Key == "PP2_AddInfo.ARM_KMH_Result").Single().Value;

            if (data.Values.Exists(x => x.Key == "PP2_AddInfo.ServiceStaffData"))
                ad.PP2_AddInfo.ServiceStaffData = data.Values.Where(x => x.Key == "PP2_AddInfo.ServiceStaffData").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP2_AddInfo.DeliveryStaffData"))
                ad.PP2_AddInfo.DeliveryStaffData = data.Values.Where(x => x.Key == "PP2_AddInfo.DeliveryStaffData").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP2_AddInfo.RecievingStaffData"))
                ad.PP2_AddInfo.RecievingStaffData = data.Values.Where(x => x.Key == "PP2_AddInfo.RecievingStaffData").Single().Value;

            //ad.PP2_AddInfo.CheckDate_Oper = "";
            //ad.PP2_AddInfo.Error = "";
            //ad.PP2_AddInfo.ManufNum_Oper = "";
            //ad.PP2_AddInfo.PSP_Name = "";
            //ad.PP2_AddInfo.SensName_Oper = "";
            //ad.PP2_AddInfo.SIKN_Num = "";            

            row.id = data.DocID;
            row.AdditionalInfo = StringToHexArrByte(ad);

            //DataDoc.Update(dd);

            this.Entry(row).Property(x => x.AdditionalInfo).IsModified = true;

            this.SaveChanges();            

            return true;
        }
    }

    [Table("TableKMH_PP_Areom")]
    public class TableKMH_PP_AreomList
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int BIK { get; set; }
        public int KMH_TYPE { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public TableKMH_PP_AreomData Data { get; set; }
    }

    [Table("TableKMH_PP_Areom")]
    public class TableKMH_PP_AreomData
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

            Doc.Settings = new Settings
            {
                Header = new HeaderDoc(),
                Data = new DataDoc(),
                Footer = new FooterDoc(),
                Dictionarys = new DictionarysDoc()
            };
            Doc.DataIVK = new DataIVKDoc();
            ((DataIVKDoc)Doc.DataIVK).TableKMH_PP_Areom = new TableKMH_PP_Areom();
        }
    }

    #region Doc.Settings

    #region Doc.Settings.Header
    public class HeaderDoc : Header
    {
        public string FieldSIKN { get; set; }
        public string FieldPSP { get; set; }

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
        public Dictionary<int, string> ProtocolType { get; set; }
        public Dictionary<int, string> DensityMeterType { get; set; }
        public Dictionary<int, string> OilTechParams { get; set; }
    }

    #endregion

    #endregion

    #region Doc.DataIVK

    public class DataIVKDoc : DataIVK
    {
        public TableKMH_PP_Areom TableKMH_PP_Areom { get; set; }
    }
    public class TableKMH_PP_Areom
    {
        public int id { get; set; }
        public int BIK { get; set; }
        public int KMH_TYPE { get; set; }
        public Protokol Protokol { get; set; }
        public AdditionalInfo AdditionalInfo { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
    }
    public class Protokol
    {
        public Table1 Table1_1 { get; set; }
        public List<Table2> Table2_1 { get; set; }
        public Table1 Table1_2 { get; set; }
        public List<Table2> Table2_2 { get; set; }
    }
    public class Table1
    {
        public string Delta_PP { get; set; }
        public string Delta_Syst { get; set; }
        public string Delta_Meth { get; set; }
    }
    public class Table2
    {
        public int Activated { get; set; }
        public string Q { get; set; }
        public string t { get; set; }
        public string P { get; set; }
        public string Dens_PP { get; set; }
        public List<LabMeas> LabMeas { get; set; }
        public string Dens_Priv { get; set; }
        public string Delta { get; set; }
        public string Error { get; set; }
        public string DensLabAvg { get; set; }
        public string tempLabAvg { get; set; }
        public string BetaPP { get; set; }
        public string GamaPP { get; set; }
        public string BetaLab { get; set; }
        public string GamaLab { get; set; }
        public string DensLabPrivAvg { get; set; }
        public string DensLab15Avg { get; set; }
    }
    public class LabMeas
    {
        public string DensLab { get; set; }
        public string tempLab { get; set; }
        public string DensLab_wCorr { get; set; }
        public string BetaLab { get; set; }
        public string GamaLab { get; set; }
        public string DensPP { get; set; }
        public string BetaPP { get; set; }
        public string GamaPP { get; set; }
        public string Dens15 { get; set; }
    }
    public class AdditionalInfo
    {
        public PP_AddInfo PP1_AddInfo { get; set; }
        public PP_AddInfo PP2_AddInfo { get; set; }
    }
    public class PP_AddInfo
    {
        public string SIKN_Num { get; set; } = "";
        public string PSP_Name { get; set; } = "";
        public string SensName_Oper { get; set; } = "";
        public string ManufNum_Oper { get; set; } = "";
        public string CheckDate_Oper { get; set; } = "";
        public string SensName_Areom { get; set; } = "";
        public string ManufNum_Areom { get; set; } = "";
        public string CheckDate_Areom { get; set; } = "";
        public string AbsError_Areom { get; set; } = "";
        public string SensName_Areom2 { get; set; } = "";
        public string ManufNum_Areom2 { get; set; } = "";
        public string CheckDate_Areom2 { get; set; } = "";
        public string AbsError_Areom2 { get; set; } = "";
        public string ServiceStaffData { get; set; } = "";
        public string DeliveryStaffData { get; set; } = "";
        public string RecievingStaffData { get; set; } = "";
        public string ARM_KMH_Result { get; set; } = "";
        public string Error { get; set; } = "";
    }

    #endregion
}