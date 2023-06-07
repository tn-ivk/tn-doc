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
    public class Poverka2816 : DocGeneral
    {
        public Poverka2816(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
        {
            IdDoc = IdDoc.Poverka2816;
            PathToDocConfigFile = GetPathConfigFile();
            PathToDocEditConfigFile = GetPathEditConfigFile();
            PathToDocTemplateFile = GetPathTemplateFile();
        }

        private DbSet<TablePoverka2816List> ListDoc { get; set; }
        private DbSet<TablePoverka2816Data> DataDoc { get; set; }

        private Doc Doc;
        private JObject jsonDoc;

        public override List<RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<RequestListDocs> docs = new();

            var list = (from item in ListDoc.AsNoTracking()
                        where item.DateTimeLong > UTBegin && item.DateTimeLong < UTEnd && item.id > 1
                        select item).ToList<TablePoverka2816List>();

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

            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka2816.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka2816.BIK = list.BIK;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka2816.Protokol = JsonDeserializeObject<Protokol>(ArrByteToString(list.Data.Protokol));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka2816.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka2816.DateTimeString = list.DateTimeString;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka2816.DateTimeLong = list.DateTimeLong;

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

            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka2816.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka2816.BIK = list.BIK;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka2816.Protokol = JsonDeserializeObject<Protokol>(ArrByteToString(list.Data.Protokol));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka2816.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka2816.DateTimeString = list.DateTimeString;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka2816.DateTimeLong = list.DateTimeLong;

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

                HtmlNode Input = HtmlNode.CreateNode("<input></input>");

                if (item.Key == "PP1_AdditionalInfo.Protokol_Number")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka2816.AdditionalInfo.PP1_AdditionalInfo.Protokol_Number;
                else if (item.Key == "PP1_AdditionalInfo.Factory_number")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka2816.AdditionalInfo.PP1_AdditionalInfo.Factory_number;
                else if (item.Key == "PP1_AdditionalInfo.SIKN_Factory")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka2816.AdditionalInfo.PP1_AdditionalInfo.SIKN_Factory;
                else if (item.Key == "PP1_AdditionalInfo.SIKN_PSP")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka2816.AdditionalInfo.PP1_AdditionalInfo.SIKN_PSP;
                else if (item.Key == "PP1_AdditionalInfo.StaffData")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka2816.AdditionalInfo.PP1_AdditionalInfo.StaffData;
                else if (item.Key == "PP2_AdditionalInfo.Protokol_Number")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka2816.AdditionalInfo.PP2_AdditionalInfo.Protokol_Number;
                else if (item.Key == "PP2_AdditionalInfo.Factory_number")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka2816.AdditionalInfo.PP2_AdditionalInfo.Factory_number;
                else if (item.Key == "PP2_AdditionalInfo.SIKN_Factory")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka2816.AdditionalInfo.PP2_AdditionalInfo.SIKN_Factory;
                else if (item.Key == "PP2_AdditionalInfo.SIKN_PSP")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka2816.AdditionalInfo.PP2_AdditionalInfo.SIKN_PSP;
                else if (item.Key == "PP2_AdditionalInfo.StaffData")
                    currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka2816.AdditionalInfo.PP2_AdditionalInfo.StaffData;

                Input.Id = item.Key;
                Input.Attributes.Add($"data-edit", $"1");
                Input.Attributes.Add($"data-key", $"{item.Key}");
                Input.Attributes.Add($"data-tag", $"AdditionalInfo");
                Input.Attributes.Add($"type", $"{item.Type}");
                Input.Attributes.Add($"value", $"{currentValue}");

                if (!item.Edit)
                    Input.Attributes.Add($"disabled", $"disabled");

                TableData.AppendChild(Input);

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
            TablePoverka2816Data row = new();
            GetViewDoc(data.DocID);

            ad = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka2816.AdditionalInfo;

            if (data.Values.Exists(x => x.Key == "PP1_AdditionalInfo.Protokol_Number"))
                ad.PP1_AdditionalInfo.Protokol_Number = data.Values.Where(x => x.Key == "PP1_AdditionalInfo.Protokol_Number").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP1_AdditionalInfo.StaffData"))
                ad.PP1_AdditionalInfo.StaffData = data.Values.Where(x => x.Key == "PP1_AdditionalInfo.StaffData").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP2_AdditionalInfo.Protokol_Number"))
                ad.PP2_AdditionalInfo.Protokol_Number = data.Values.Where(x => x.Key == "PP2_AdditionalInfo.Protokol_Number").Single().Value;
            if (data.Values.Exists(x => x.Key == "PP2_AdditionalInfo.StaffData"))
                ad.PP2_AdditionalInfo.StaffData = data.Values.Where(x => x.Key == "PP2_AdditionalInfo.StaffData").Single().Value;

            row.id = data.DocID;
            row.AdditionalInfo = StringToHexArrByte(ad);

            //DataDoc.Update(dd);

            this.Entry(row).Property(x => x.AdditionalInfo).IsModified = true;

            this.SaveChanges();

            return true;
        }


    }

    [Table("TablePoverka2816")]
    public class TablePoverka2816List
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int BIK { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public TablePoverka2816Data Data { get; set; }
    }

    [Table("TablePoverka2816")]
    public class TablePoverka2816Data
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
            ((DataIVKDoc)Doc.DataIVK).TablePoverka2816 = new TablePoverka2816();
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
        public TablePoverka2816 TablePoverka2816 { get; set; }
    }
    public class TablePoverka2816
    {
        public int id { get; set; }
        public int BIK { get; set; }
        public Protokol Protokol { get; set; }
        public AdditionalInfo AdditionalInfo { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
    }
    public class Protokol
    {
        public List<Table1> PP1_Table1 { get; set; }
        public List<Table1> PP2_Table1 { get; set; }
    }
    public class Table1
    {
        public string t_PP { get; set; }
        public string t_Pycn { get; set; }
        public string P_PP { get; set; }
        public string P_Pycn { get; set; }
        public string Dens_Pycn { get; set; }
        public string Dens_Pycn_Normalized { get; set; }
        public string Period { get; set; }
        public string Dens_PP { get; set; }
        public string Delta { get; set; }
    }
    public class AdditionalInfo
    {
        public PP_AdditionalInfo PP1_AdditionalInfo { get; set; }
        public PP_AdditionalInfo PP2_AdditionalInfo { get; set; }
    }
    public class PP_AdditionalInfo
    {
        public int IsUsed { get; set; }
        public string Protokol_Number { get; set; }
        public string Type_of_converter { get; set; }
        public string Factory_number { get; set; }
        public string SIKN_Factory { get; set; }
        public string SIKN_PSP { get; set; }
        public string Environment_temperature { get; set; }
        public string Atmosphere_pressure { get; set; }
        public KoefsSolartron KoefsSolartron { get; set; }
        public KoefsSarasota KoefsSarasota { get; set; }
        public string Koefs_CheckDate { get; set; }
        public string StaffData { get; set; }
    }
    public class KoefsSolartron
    {
        public int IsUsed { get; set; }
        public string K0 { get; set; }
        public string K1 { get; set; }
        public string K2 { get; set; }
        public string K18 { get; set; }
        public string K19 { get; set; }
        public string K20A { get; set; }
        public string K20B { get; set; }
        public string K21A { get; set; }
        public string K21B { get; set; }
    }
    public class KoefsSarasota
    {
        public int IsUsed { get; set; }
        public string D0 { get; set; }
        public string T0 { get; set; }
        public string TEMPCO { get; set; }
        public string Tcal { get; set; }
        public string PRESCO { get; set; }
        public string Pcal { get; set; }
        public string K { get; set; }
        public string Patm { get; set; }
    }
    #endregion


}
