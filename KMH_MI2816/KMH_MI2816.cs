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
    public class KMH_MI2816 : DocGeneral
    {
        public KMH_MI2816(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
        {
            IdDoc = IdDoc.KMH_MI2816;
            PathToDocConfigFile = GetPathConfigFile();
            PathToDocEditConfigFile = GetPathEditConfigFile();
            PathToDocTemplateFile = GetPathTemplateFile();
        }

        private DbSet<TableKMH_MI2816List> ListDoc { get; set; }
        private DbSet<TableKMH_MI2816Data> DataDoc { get; set; }

        private Doc Doc;
        private JObject jsonDoc;

        public override List<RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<RequestListDocs> docs = new();

            var list = (from item in ListDoc.AsNoTracking()
                        where item.DateTimeLong > UTBegin && item.DateTimeLong < UTEnd && item.id > 1
                        select item).ToList<TableKMH_MI2816List>();

            if (list.Count != 0)
            {
                foreach (var item in list)
                {
                    docs.Add(new()
                    {
                        Id = item.id,
                        DT = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(item.DateTimeLong).ToString("dd.MM.yy HH:mm"),
                        Description = ""
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

            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MI2816.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MI2816.BIK = list.BIK;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MI2816.Protokol = JsonDeserializeObject<Protokol>(ArrByteToString(list.Data.Protokol));
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MI2816.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));

            if (string.IsNullOrEmpty(list.Data.ARMData))
                ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MI2816.ARMData = new ARMData();
            else
                ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MI2816.ARMData = JsonDeserializeObject<ARMData>(list.Data.ARMData);
            
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MI2816.DateTimeString = list.DateTimeString;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MI2816.DateTimeLong = list.DateTimeLong;

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

            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MI2816.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MI2816.BIK = list.BIK;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MI2816.Protokol = JsonDeserializeObject<Protokol>(ArrByteToString(list.Data.Protokol));
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MI2816.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));

            if (string.IsNullOrEmpty(list.Data.ARMData))
                ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MI2816.ARMData = new ARMData();
            else
                ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MI2816.ARMData = JsonDeserializeObject<ARMData>(list.Data.ARMData);

            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MI2816.DateTimeString = list.DateTimeString;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MI2816.DateTimeLong = list.DateTimeLong;

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

                    if (item.Key == "ARM_KMH_Result1")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.ARM_KMH_Result1;
                    else if (item.Key == "ARM_KMH_Result2")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.ARM_KMH_Result2;

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

                    if (item.Key == "Picn1.P11")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn1.P11;
                    else if (item.Key == "Picn1.P12")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn1.P12;
                    else if (item.Key == "Picn1.P13")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn1.P13;
                    else if (item.Key == "Picn1.P14")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn1.P14;
                    else if (item.Key == "Picn1.P15")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn1.P15;
                    else if (item.Key == "Picn1.P16")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn1.P16;
                    else if (item.Key == "Picn1.P17")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn1.P17;
                    else if (item.Key == "Picn1.P18")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn1.P18;
                    else if (item.Key == "Picn1.P19")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn1.P19;
                    else if (item.Key == "Picn1.P110")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn1.P110;
                    else if (item.Key == "Picn1.P111")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn1.P111;

                    if (item.Key == "Picn2.P11")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn2.P11;
                    else if (item.Key == "Picn2.P12")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn2.P12;
                    else if (item.Key == "Picn2.P13")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn2.P13;
                    else if (item.Key == "Picn2.P14")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn2.P14;
                    else if (item.Key == "Picn2.P15")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn2.P15;
                    else if (item.Key == "Picn2.P16")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn2.P16;
                    else if (item.Key == "Picn2.P17")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn2.P17;
                    else if (item.Key == "Picn2.P18")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn2.P18;
                    else if (item.Key == "Picn2.P19")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn2.P19;
                    else if (item.Key == "Picn2.P110")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn2.P110;
                    else if (item.Key == "Picn2.P111")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Picn2.P111;

                    else if (item.Key == "Weigher.P21")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Weigher.P21;
                    else if (item.Key == "Weigher.P22")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Weigher.P22;
                    else if (item.Key == "Weigher.P23")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Weigher.P23;
                    else if (item.Key == "Weigher.P24")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Weigher.P24;
                    else if (item.Key == "Weigher.P25")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Weigher.P25;
                    else if (item.Key == "Temp.P31")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Temp.P31;
                    else if (item.Key == "Temp.P32")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Temp.P32;
                    else if (item.Key == "Temp.P33")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Temp.P33;
                    else if (item.Key == "Temp.P34")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Temp.P34;
                    else if (item.Key == "Temp.P35")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Temp.P35;
                    else if (item.Key == "Press.P41")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Press.P41;
                    else if (item.Key == "Press.P42")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Press.P42;
                    else if (item.Key == "Press.P43")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Press.P43;
                    else if (item.Key == "Press.P44")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Press.P44;
                    else if (item.Key == "Press.P45")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Press.P45;
                    else if (item.Key == "Delta_Picn")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.Delta_Picn;                    
                    else if (item.Key == "ServiceStaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.ServiceStaffData;
                    else if (item.Key == "DeliveryStaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.DeliveryStaffData;
                    else if (item.Key == "RecievingStaffData")
                        currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData.RecievingStaffData;                    
                    
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
            ARMData ad = new();
            TableKMH_MI2816Data row = new();
            GetViewDoc(data.DocID);

            ad = ((DataIVKDoc)Doc.Doc.DataIVK).TableKMH_MI2816.ARMData;

            if (data.Values.Exists(x => x.Key == "Picn1.P11"))
                ad.Picn1.P11 = data.Values.Where(x => x.Key == "Picn1.P11").Single().Value;
            if (data.Values.Exists(x => x.Key == "Picn1.P12")) 
                ad.Picn1.P12 = data.Values.Where(x => x.Key == "Picn1.P12").Single().Value;
            if (data.Values.Exists(x => x.Key == "Picn1.P13")) 
                ad.Picn1.P13 = data.Values.Where(x => x.Key == "Picn1.P13").Single().Value;
            if (data.Values.Exists(x => x.Key == "Picn1.P14")) 
                ad.Picn1.P14 = data.Values.Where(x => x.Key == "Picn1.P14").Single().Value;
            if (data.Values.Exists(x => x.Key == "Picn1.P15")) 
                ad.Picn1.P15 = data.Values.Where(x => x.Key == "Picn1.P15").Single().Value;
            if (data.Values.Exists(x => x.Key == "Picn1.P16")) 
                ad.Picn1.P16 = data.Values.Where(x => x.Key == "Picn1.P16").Single().Value;
            if (data.Values.Exists(x => x.Key == "Picn1.P17")) 
                ad.Picn1.P17 = data.Values.Where(x => x.Key == "Picn1.P17").Single().Value;
            if (data.Values.Exists(x => x.Key == "Picn1.P18")) 
                ad.Picn1.P18 = data.Values.Where(x => x.Key == "Picn1.P18").Single().Value;
            if (data.Values.Exists(x => x.Key == "Picn1.P19")) 
                ad.Picn1.P19 = data.Values.Where(x => x.Key == "Picn1.P19").Single().Value;
            if (data.Values.Exists(x => x.Key == "Picn1.P110")) 
                ad.Picn1.P110 = data.Values.Where(x => x.Key == "Picn1.P110").Single().Value;
            if (data.Values.Exists(x => x.Key == "Picn1.P111")) 
                ad.Picn1.P111 = data.Values.Where(x => x.Key == "Picn1.P111").Single().Value;

            if (data.Values.Exists(x => x.Key == "Picn2.P11")) 
                ad.Picn2.P11 = data.Values.Where(x => x.Key == "Picn2.P11").Single().Value;
            if (data.Values.Exists(x => x.Key == "Picn2.P12")) 
                ad.Picn2.P12 = data.Values.Where(x => x.Key == "Picn2.P12").Single().Value;
            if (data.Values.Exists(x => x.Key == "Picn2.P13")) 
                ad.Picn2.P13 = data.Values.Where(x => x.Key == "Picn2.P13").Single().Value;
            if (data.Values.Exists(x => x.Key == "Picn2.P14")) 
                ad.Picn2.P14 = data.Values.Where(x => x.Key == "Picn2.P14").Single().Value;
            if (data.Values.Exists(x => x.Key == "Picn2.P15")) 
                ad.Picn2.P15 = data.Values.Where(x => x.Key == "Picn2.P15").Single().Value;
            if (data.Values.Exists(x => x.Key == "Picn2.P16")) 
                ad.Picn2.P16 = data.Values.Where(x => x.Key == "Picn2.P16").Single().Value;
            if (data.Values.Exists(x => x.Key == "Picn2.P17")) 
                ad.Picn2.P17 = data.Values.Where(x => x.Key == "Picn2.P17").Single().Value;
            if (data.Values.Exists(x => x.Key == "Picn2.P18")) 
                ad.Picn2.P18 = data.Values.Where(x => x.Key == "Picn2.P18").Single().Value;
            if (data.Values.Exists(x => x.Key == "Picn2.P19")) 
                ad.Picn2.P19 = data.Values.Where(x => x.Key == "Picn2.P19").Single().Value;
            if (data.Values.Exists(x => x.Key == "Picn2.P110")) 
                ad.Picn2.P110 = data.Values.Where(x => x.Key == "Picn2.P110").Single().Value;
            if (data.Values.Exists(x => x.Key == "Picn2.P111")) 
                ad.Picn2.P111 = data.Values.Where(x => x.Key == "Picn2.P111").Single().Value;


            if (data.Values.Exists(x => x.Key == "Weigher.P21")) 
                ad.Weigher.P21 = data.Values.Where(x => x.Key == "Weigher.P21").Single().Value;
            if (data.Values.Exists(x => x.Key == "Weigher.P22")) 
                ad.Weigher.P22 = data.Values.Where(x => x.Key == "Weigher.P22").Single().Value;
            if (data.Values.Exists(x => x.Key == "Weigher.P23")) 
                ad.Weigher.P23 = data.Values.Where(x => x.Key == "Weigher.P23").Single().Value;
            if (data.Values.Exists(x => x.Key == "Weigher.P24")) 
                ad.Weigher.P24 = data.Values.Where(x => x.Key == "Weigher.P24").Single().Value;
            if (data.Values.Exists(x => x.Key == "Weigher.P25")) 
                ad.Weigher.P25 = data.Values.Where(x => x.Key == "Weigher.P25").Single().Value;

            if (data.Values.Exists(x => x.Key == "Temp.P31")) 
                ad.Temp.P31 = data.Values.Where(x => x.Key == "Temp.P31").Single().Value;
            if (data.Values.Exists(x => x.Key == "Temp.P32")) 
                ad.Temp.P32 = data.Values.Where(x => x.Key == "Temp.P32").Single().Value;
            if (data.Values.Exists(x => x.Key == "Temp.P33")) 
                ad.Temp.P33 = data.Values.Where(x => x.Key == "Temp.P33").Single().Value;
            if (data.Values.Exists(x => x.Key == "Temp.P34")) 
                ad.Temp.P34 = data.Values.Where(x => x.Key == "Temp.P34").Single().Value;
            if (data.Values.Exists(x => x.Key == "Temp.P35")) 
                ad.Temp.P35 = data.Values.Where(x => x.Key == "Temp.P35").Single().Value;

            if (data.Values.Exists(x => x.Key == "Press.P41")) 
                ad.Press.P41 = data.Values.Where(x => x.Key == "Press.P41").Single().Value;
            if (data.Values.Exists(x => x.Key == "Press.P42")) 
                ad.Press.P42 = data.Values.Where(x => x.Key == "Press.P42").Single().Value;
            if (data.Values.Exists(x => x.Key == "Press.P43")) 
                ad.Press.P43 = data.Values.Where(x => x.Key == "Press.P43").Single().Value;
            if (data.Values.Exists(x => x.Key == "Press.P44")) 
                ad.Press.P44 = data.Values.Where(x => x.Key == "Press.P44").Single().Value;
            if (data.Values.Exists(x => x.Key == "Press.P45")) 
                ad.Press.P45 = data.Values.Where(x => x.Key == "Press.P45").Single().Value;
            if (data.Values.Exists(x => x.Key == "Delta_Picn")) 
                ad.Delta_Picn = data.Values.Where(x => x.Key == "Delta_Picn").Single().Value;

            if (data.Values.Exists(x => x.Key == "ARM_KMH_Result1")) 
                ad.ARM_KMH_Result1 = data.Values.Where(x => x.Key == "ARM_KMH_Result1").Single().Value;
            if (data.Values.Exists(x => x.Key == "ARM_KMH_Result2")) 
                ad.ARM_KMH_Result2 = data.Values.Where(x => x.Key == "ARM_KMH_Result2").Single().Value;

            if (data.Values.Exists(x => x.Key == "ServiceStaffData")) 
                ad.ServiceStaffData = data.Values.Where(x => x.Key == "ServiceStaffData").Single().Value;
            if (data.Values.Exists(x => x.Key == "DeliveryStaffData")) 
                ad.DeliveryStaffData = data.Values.Where(x => x.Key == "DeliveryStaffData").Single().Value;
            if (data.Values.Exists(x => x.Key == "RecievingStaffData")) 
                ad.RecievingStaffData = data.Values.Where(x => x.Key == "RecievingStaffData").Single().Value;

            row.id = data.DocID;
            row.ARMData = JsonSerializeObject(ad).ToString();

            //DataDoc.Update(dd);

            this.Entry(row).Property(x => x.ARMData).IsModified = true;

            this.SaveChanges();

            return true;
        }
    }

    [Table("TableKMH_MI2816")]
    public class TableKMH_MI2816List
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int BIK { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public TableKMH_MI2816Data Data { get; set; }
    }

    [Table("TableKMH_MI2816")]
    public class TableKMH_MI2816Data
    {
        [Key]
        public int id { get; set; }
        public byte[] Protokol { get; set; }
        public byte[] AdditionalInfo { get; set; }
        public string ARMData { get; set; }
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
            ((DataIVKDoc)Doc.DataIVK).TableKMH_MI2816 = new TableKMH_MI2816();
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

    }

    #endregion

    #endregion

    #region Doc.DataIVK

    public class DataIVKDoc : DataIVK
    {
        public TableKMH_MI2816 TableKMH_MI2816 { get; set; }
    }
    public class TableKMH_MI2816
    {
        public int id { get; set; }
        public int BIK { get; set; }
        public Protokol Protokol { get; set; }
        public AdditionalInfo AdditionalInfo { get; set; }
        public ARMData ARMData { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
    }
    public class Protokol
    {
        public List<Table1> Table1 { get; set; }
        public List<Table1_1> Table1_1 { get; set; }
    }
    public class Table1
    {
        public string Q { get; set; }
        public string PP_t { get; set; }
        public string PP_P { get; set; }
        public string PP_Dens { get; set; }
        public string Picn_Avg { get; set; }
        public string Picn_Normolized { get; set; }
        public string Delta { get; set; }
    }
    public class Table1_1
    {
        public string Index { get; set; }
        public string W_p { get; set; }
        public string W_z { get; set; }
        public string t { get; set; }
        public string P { get; set; }
    }
    public class AdditionalInfo
    {
        public PP_AdditionalInfo PP1_AdditionalInfo { get; set; }
        public PP_AdditionalInfo PP2_AdditionalInfo { get; set; }
    }
    public class PP_AdditionalInfo
    {
        public int IsUsed { get; set; }
        public string SIKN_Name { get; set; }
        public string PSP_Name { get; set; }
        public double Delta_Picn { get; set; }
        public string DensSensor_Name { get; set; }
        public string DensSensor_FactoryNumber { get; set; }
        public string DensSensor_CheckDate { get; set; }
        public double Delta_PP { get; set; }
        public List<PicnInit> PicnInit { get; set; }
    }
    public class PicnInit
    {
        public double V { get; set; }
        public double Ft { get; set; }
        public double t0 { get; set; }
        public double FP { get; set; }
    }
    public class ARMData
    {
        public string ServiceStaffData { get; set; } = "";
        public string DeliveryStaffData { get; set; } = "";
        public string RecievingStaffData { get; set; } = "";
        public string ARM_KMH_Result1 { get; set; } = "";
        public string ARM_KMH_Result2 { get; set; } = "";
        public string Delta_Picn { get; set; } = "";
        public Picn Picn1 { get; set; } = new Picn();
        public Picn Picn2 { get; set; } = new Picn();
        public Weigher Weigher { get; set; } = new Weigher();
        public Temp Temp { get; set; } = new Temp();
        public Press Press { get; set; } = new Press();
    }
    public class Picn
    {
        public string P11 { get; set; } = "";
        public string P12 { get; set; } = "";
        public string P13 { get; set; } = "";
        public string P14 { get; set; } = "";
        public string P15 { get; set; } = "";
        public string P16 { get; set; } = "";
        public string P17 { get; set; } = "";
        public string P18 { get; set; } = "";
        public string P19 { get; set; } = "";
        public string P110 { get; set; } = "";
        public string P111 { get; set; } = "";
    }
    public class Weigher
    {
        public string P21 { get; set; } = "";
        public string P22 { get; set; } = "";
        public string P23 { get; set; } = "";
        public string P24 { get; set; } = "";
        public string P25 { get; set; } = "";
    }
    public class Temp
    {
        public string P31 { get; set; } = "";
        public string P32 { get; set; } = "";
        public string P33 { get; set; } = "";
        public string P34 { get; set; } = "";
        public string P35 { get; set; } = "";
    }
    public class Press
    {
        public string P41 { get; set; } = "";
        public string P42 { get; set; } = "";
        public string P43 { get; set; } = "";
        public string P44 { get; set; } = "";
        public string P45 { get; set; } = "";
    }

    #endregion


}
