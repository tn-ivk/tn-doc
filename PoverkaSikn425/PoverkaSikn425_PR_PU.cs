using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using TN.DocData;

namespace TN.Doc;

public class PoverkaSikn425_PR_PU : DocGeneral
{
    public PoverkaSikn425_PR_PU(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
    {
        IdDoc = IdDoc.Poverka3312_PR_PU;
        PathToDocConfigFile = GetPathConfigFile();
        PathToDocEditConfigFile = GetPathEditConfigFile();
        PathToDocTemplateFile = GetPathTemplateFile();
    }
    
    private DbSet<TablePoverkaSikn425_PR_PUList> ListDoc { get; set; }
    private DbSet<TablePoverkaSikn425_PR_PUData> DataDoc { get; set; }

    private DocData.Doc Doc;
    private JObject jsonDoc;
    
    public override List<RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<RequestListDocs> docs = new();

            var list = (from item in ListDoc.AsNoTracking()
                        where item.DateTimeLong > UTBegin && item.DateTimeLong < UTEnd && item.id > 1
                        select item).ToList<TablePoverka3312_PR_PUList>();

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

        DocData.Doc doc = new();

        LoadCfg(PathToDocConfigFile, ref doc);

        doc.Doc.Settings.General.NefType = CfgGeneral.Doc.Settings.General.NefType;
        doc.Doc.Settings.General.ObjType = CfgGeneral.Doc.Settings.General.ObjType;
        doc.Doc.Settings.Dictionarys.UsersGroup = CfgGeneral.Doc.Settings.Dictionarys.UsersGroup;
        doc.Doc.Settings.Dictionarys.Users = CfgGeneral.Doc.Settings.Dictionarys.Users;
        doc.Doc.Settings.Dictionarys.Licenses = CfgGeneral.Doc.Settings.Dictionarys.Licenses;

        ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3312_PR_PU.id = list.id;
        ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3312_PR_PU.IL = list.IL;
        ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3312_PR_PU.UPR_LineIndx = list.UPR_LineIndx;
        ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3312_PR_PU.Protokol = JsonDeserializeObject<Protokol>(ArrByteToString(list.Data.Protokol));
        ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo = JsonDeserializeObject<DocData.AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));
        ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3312_PR_PU.DateTimeString = list.DateTimeString;
        ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3312_PR_PU.DateTimeLong = list.DateTimeLong;
        ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3312_PR_PU.BIK_ID = list.BIK_ID;
        ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3312_PR_PU.IL_Name = ArrByteToString(list.IL_Name);

        string fileName = string.Format("{0}_Поверка МИ 3312-2011 МПР по ПУ",
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3312_PR_PU.DateTimeString.Replace(":", "."));
        doc.Doc.Settings.General.FileNameForExportDoc = $"{fileName}";


        jsonDoc = JObject.Parse(JsonSerializeObject<DocData.Doc>(doc).ToString());
        Doc = doc;

        return JsonSerializeObject<DocData.Doc>(doc);
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

            if (item.Key == "Place_PSP")
                currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo.Place_PSP;
            else if (item.Key == "Place_SIKN")
                currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo.Place_SIKN;
            else if (item.Key == "Place_Factory")
                currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo.Place_Factory;
            else if (item.Key == "PR_Type")
                currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo.PR_Type;
            else if (item.Key == "PR_FactoryNumber")
                currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo.PR_FactoryNumber;
            else if (item.Key == "PR_LineName")
                currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo.PR_LineName;
            else if (item.Key == "MM_Sensor_Type")
                currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo.MM_Sensor_Type;
            else if (item.Key == "MM_Sensor_DU")
                currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo.MM_Sensor_DU;
            else if (item.Key == "MM_Sensor_FactoryNumber")
                currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo.MM_Sensor_FactoryNumber;
            else if (item.Key == "MM_PEP")
                currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo.MM_PEP;
            else if (item.Key == "MM_PEP_FactoryNumber")
                currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo.MM_PEP_FactoryNumber;
            else if (item.Key == "PU_Type")
                currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo.PU_Type;
            else if (item.Key == "PU_FactoryNumber")
                currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo.PU_FactoryNumber;
            else if (item.Key == "PP_Type")
                currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo.PP_Type;
            else if (item.Key == "PP_FactoryNumber")
                currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo.PP_FactoryNumber;
            else if (item.Key == "IVK_Type")
                currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo.IVK_Type;
            else if (item.Key == "IVK_FactoryNumber")
                currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo.IVK_FactoryNumber;
            else if (item.Key == "Oil_Type")
                currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo.Oil_Type;
            else if (item.Key == "ProtokolNum")
                currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo.ProtokolNum;
            else if (item.Key == "StaffData")
                currentValue = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo.StaffData;

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
        DocData.AdditionalInfo ad = new();
        TablePoverka3312_PR_PUData row = new();
        GetViewDoc(data.DocID);

        ad = ((DataIVKDoc)Doc.Doc.DataIVK).TablePoverka3312_PR_PU.AdditionalInfo;

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
        if (data.Values.Exists(x => x.Key == "MM_Sensor_Type"))
            ad.MM_Sensor_Type = data.Values.Where(x => x.Key == "MM_Sensor_Type").Single().Value;
        if (data.Values.Exists(x => x.Key == "MM_Sensor_DU"))
            ad.MM_Sensor_DU = data.Values.Where(x => x.Key == "MM_Sensor_DU").Single().Value;
        if (data.Values.Exists(x => x.Key == "MM_Sensor_FactoryNumber"))
            ad.MM_Sensor_FactoryNumber = data.Values.Where(x => x.Key == "MM_Sensor_FactoryNumber").Single().Value;
        if (data.Values.Exists(x => x.Key == "MM_PEP"))
            ad.MM_PEP = data.Values.Where(x => x.Key == "MM_PEP").Single().Value;
        if (data.Values.Exists(x => x.Key == "MM_PEP_FactoryNumber"))
            ad.MM_PEP_FactoryNumber = data.Values.Where(x => x.Key == "MM_PEP_FactoryNumber").Single().Value;
        if (data.Values.Exists(x => x.Key == "PU_Type"))
            ad.PU_Type = data.Values.Where(x => x.Key == "PU_Type").Single().Value;
        if (data.Values.Exists(x => x.Key == "PU_FactoryNumber"))
            ad.PU_FactoryNumber = data.Values.Where(x => x.Key == "PU_FactoryNumber").Single().Value;
        if (data.Values.Exists(x => x.Key == "PP_Type"))
            ad.PP_Type = data.Values.Where(x => x.Key == "PP_Type").Single().Value;
        if (data.Values.Exists(x => x.Key == "PP_FactoryNumber"))
            ad.PP_FactoryNumber = data.Values.Where(x => x.Key == "PP_FactoryNumber").Single().Value;
        if (data.Values.Exists(x => x.Key == "IVK_Type"))
            ad.IVK_Type = data.Values.Where(x => x.Key == "IVK_Type").Single().Value;
        if (data.Values.Exists(x => x.Key == "IVK_FactoryNumber"))
            ad.IVK_FactoryNumber = data.Values.Where(x => x.Key == "IVK_FactoryNumber").Single().Value;
        if (data.Values.Exists(x => x.Key == "Oil_Type"))
            ad.Oil_Type = data.Values.Where(x => x.Key == "Oil_Type").Single().Value;
        if (data.Values.Exists(x => x.Key == "ProtokolNum"))
            ad.ProtokolNum = data.Values.Where(x => x.Key == "ProtokolNum").Single().Value;
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

[Table("TablePoverkaSikn425_PR_PU")]
public class TablePoverkaSikn425_PR_PUList
{
    [Key, ForeignKey("Data")]
    public int id { get; set; }
    public int IL { get; set; }
    public int UPR_LineIndx { get; set; }
    public string DateTimeString { get; set; }
    public long DateTimeLong { get; set; }
    public int BIK_ID { get; set; }
    public byte[] IL_Name { get; set; }
    public TablePoverka3312_PR_PUData Data { get; set; }
}

[Table("TablePoverkaSikn425_PR_PU")]
public class TablePoverkaSikn425_PR_PUData
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
        ((DataIVKDoc)Doc.DataIVK).TablePoverka3312_PR_PU = new TablePoverka3312_PR_PU();
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
    public TablePoverka3312_PR_PU TablePoverka3312_PR_PU { get; set; }
}

public class TablePoverka3312_PR_PU
{
    public int id { get; set; }
    public int IL { get; set; }
    public int UPR_LineIndx { get; set; }
    public Protokol Protokol { get; set; }
    public DocData.AdditionalInfo AdditionalInfo { get; set; }
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
    public Table3 Table3 { get; set; }
}
public class Table1
{
    public List<string> DetName { get; set; }
    public List<string> V0 { get; set; }
    public string m_D { get; set; }
    public string m_S { get; set; }
    public string m_E { get; set; }
    public string Alpha_t { get; set; }
    public List<string> Teta_Sum_0 { get; set; }
    public List<string> Teta_V_0 { get; set; }
    public string Delta_t_PU { get; set; }
    public string Delta_t_PP { get; set; }
    public string Delta_Dens_PP { get; set; }
    public string Delta_IVK { get; set; }
    public string ZSk { get; set; }
}
public class Table2
{
    public string Ser { get; set; }
    public string Row { get; set; }
    public string Q_jik { get; set; }
    public string Detector { get; set; }
    public string T_jik { get; set; }
    public string t_PU_jik { get; set; }
    public string P_PU_jik { get; set; }
    public string Dens_jik { get; set; }
    public string DensTemp_jik { get; set; }
    public string DensPress_jik { get; set; }
    public string Beta_jik { get; set; }
    public string N_jik { get; set; }
    public string M_jik { get; set; }
    public string K_jik { get; set; }
    public string U_jik { get; set; }
    public string U_ERROR_jik { get; set; }
}
public class Table3
{
    public string Teta_tk { get; set; }
    public string Teta_pk { get; set; }
    public string Delta_k { get; set; }
    public List<Points> Point { get; set; }
}
public class Points
{
    public string Point { get; set; }
    public string Q_jk { get; set; }
    public string K_jk { get; set; }
    public string S_jk { get; set; }
    public string S_jk_ERROR { get; set; }
    public string n_jk { get; set; }
    public string S0_jk { get; set; }
    public string t095_jk { get; set; }
    public string e_jk { get; set; }
    public string Teta_Z_jk { get; set; }
    public string Teta_Sum_jk { get; set; }
    public string Delta_jk { get; set; }
    public string SK_jk { get; set; }
}
public class AdditionalInfo
{
    public string Place_PSP { get; set; }
    public string Place_SIKN { get; set; }
    public string Place_Factory { get; set; }
    public string PR_Type { get; set; }
    public string PR_FactoryNumber { get; set; }
    public string PR_LineName { get; set; }
    public string MM_Sensor_Type { get; set; }
    public string MM_Sensor_DU { get; set; }
    public string MM_Sensor_FactoryNumber { get; set; }
    public string MM_PEP { get; set; }
    public string MM_PEP_FactoryNumber { get; set; }
    public string PU_Type { get; set; }
    public string PU_FactoryNumber { get; set; }
    public string PP_Type { get; set; }
    public string PP_FactoryNumber { get; set; }
    public string IVK_Type { get; set; }
    public string IVK_FactoryNumber { get; set; }
    public string Oil_Type { get; set; }
    public string ProtokolNum { get; set; }
    public string StaffData { get; set; }
}

#endregion
