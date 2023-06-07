using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Xml.Linq;
using TN.DocData;

namespace TN.Doc
{
    public class Poverka3267 : DocGeneral
    {
        public Poverka3267(DbContextOptions<DocGeneral> options, string path, TN.DocData.Device device) : base(options, path, device)
        {
            IdDoc = TN.DocData.IdDoc.Poverka3287;
            PathToDocConfigFile = path + "/Cfg/CfgPoverka3287.json";
            PathToDocEditConfigFile = path + "/Cfg/CfgEditPoverka3287.json";
            PathToDocTemplateFile = path + "/Doc/10_Poverka3287_AnnexA_Release_version.frx";
        }

        private DbSet<TablePoverka3287List> ListDoc { get; set; }
        private DbSet<TablePoverka3287Data> DataDoc { get; set; }

        private Doc Doc;
        private JObject jsonDoc;

        public override List<TN.DocData.RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<TN.DocData.RequestListDocs> docs = new();

            var list = (from item in ListDoc
                        where item.DateTimeLong > UTBegin && item.DateTimeLong < UTEnd && item.id > 1
                        select item).ToList<TablePoverka3287List>();

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
            var list = (from item in ListDoc
                        where item.id == id
                        select item).First();

            list.Data = (from item in DataDoc
                         where item.id == id
                         select item).First();

            Doc doc = new();

            LoadCfg(PathToDocConfigFile, ref doc);

            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3287.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3287.IL = list.IL;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3287.Protokol = JsonDeserializeObject<Protokol>(ArrByteToString(list.Data.Protokol));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3287.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3287.DateTimeString = list.DateTimeString;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3287.DateTimeLong = list.DateTimeLong;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3287.BIK_ID = list.BIK_ID;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3287.IL_Name = ArrByteToString(list.IL_Name);

            jsonDoc = JObject.Parse(JsonSerializeObject<Doc>(doc).ToString());
            Doc = doc;

            return JsonSerializeObject<Doc>(doc);
        }

        public override string GetEditDoc(int id)
        {
            GetViewDoc(id);

            return "";
        }

        public override bool SaveDoc(string jsonData)
        {
            return true;
        }


    }

    [Table("TablePoverka3287")]
    public class TablePoverka3287List
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int IL { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public int BIK_ID { get; set; }
        public byte[] IL_Name { get; set; }
        public TablePoverka3287Data Data { get; set; }
    }

    [Table("TablePoverka3287")]
    public class TablePoverka3287Data
    {
        [Key]
        public int id { get; set; }
        public byte[] Protokol { get; set; }
        public byte[] AdditionalInfo { get; set; }
    }


    public class Doc : TN.DocData.Root
    {
        public Doc()
        {
            Doc = new();

            Doc.Settings = new TN.DocData.Settings();

            Doc.Settings.Header = new HeaderDoc();
            Doc.Settings.Data = new DataDoc();
            Doc.Settings.Footer = new FooterDoc();
            Doc.Settings.Dictionarys = new DictionarysDoc();
            Doc.DataIVK = new DataIVKDoc();
            ((DataIVKDoc)Doc.DataIVK).TablePoverka3287 = new TablePoverka3287();
        }
    }

    #region Doc.Settings

    #region Doc.Settings.Header

    public class HeaderDoc : TN.DocData.Header
    {
        public string FieldSIKN { get; set; }
    }

    #endregion

    #region Doc.Settings.Data

    public class DataDoc : TN.DocData.Data
    {

    }

    #endregion

    #region Doc.Settings.Footer

    public class FooterDoc : TN.DocData.Footer
    {

    }

    #endregion

    #region Doc.Settings.Dictionarys

    public class DictionarysDoc : TN.DocData.Dictionarys
    {

    }

    #endregion

    #endregion

    #region Doc.DataIVK

    public class DataIVKDoc : TN.DocData.DataIVK
    {
        public TablePoverka3287 TablePoverka3287 { get; set; }
    }
    public class TablePoverka3287
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
        public List<Table0> Table0 { get; set; }
        public List<Table1> Table1 { get; set; }
        public List<Table1_1> Table1_1 { get; set; }
        public List<Table2> Table2 { get; set; }
        public List<Table3> Table3 { get; set; }
        public List<Table3_Control> Table3_Control { get; set; }
        public List<Table3_Control_Element> Table3_Control_Element { get; set; }
        public List<Table4> Table4 { get; set; }
        public List<ActivatedTable> ActivatedTables { get; set; }
    }

    public class Table0
    {
        public int UseMF { get; set; }
        public int UseManualVisc { get; set; }
        public string Visc { get; set; }
        public string OilType { get; set; }
    }
    public class Table1
    {
        public string m_D { get; set; }
        public string m_S { get; set; }
        public string m_E { get; set; }
        public string Alpha_t { get; set; }
        public string Alpha_k1 { get; set; }
        public string Alpha_D { get; set; }
        public string Delta_t_PU { get; set; }
        public string Delta_t_PR { get; set; }
        public string Delta_IVK { get; set; }
        public string Delta_Visc { get; set; }
        public string KF { get; set; }
    }
    public class Table1_1
    {
        public string DetName { get; set; }
        public string V0 { get; set; }
        public string Teta_Sum_0 { get; set; }
        public string Teta_V_0 { get; set; }
    }
    public class Table2
    {
        public int Activated { get; set; }
        public string Ser { get; set; }
        public string Row { get; set; }
        public string Q_ji { get; set; }
        public string Detector { get; set; }
        public string T_ji { get; set; }
        public string t_PU_ji { get; set; }
        public string P_PU_ji { get; set; }
        public string t_D_ji { get; set; }
        public string Dens_ji { get; set; }
        public string DensTemp_ji { get; set; }
        public string DensPress_ji { get; set; }
        public string Beta_ji { get; set; }
        public string v_ji { get; set; }
        public string t_PR_ji { get; set; }
        public string P_PR_ji { get; set; }
        public string f_ji { get; set; }
        public string N_ji { get; set; }
        public string K_MF_ji { get; set; }
        public string U_ji { get; set; }
        public string U_ERROR_ji { get; set; }
    }
    public class Table3
    {
        public string Point { get; set; }
        public string Q_j { get; set; }
        public string f_j { get; set; }
        public string K_MF_j { get; set; }
        public string S_j { get; set; }
        public string S_j_ERROR { get; set; }
        public string n_j { get; set; }
        public string S0_j { get; set; }
        public string t095_j { get; set; }
        public string e_j { get; set; }
        public string SK_j { get; set; }
    }
    public class Table3_Control
    {
        public string v_min { get; set; }
        public string v_max { get; set; }
        public string Teta_t { get; set; }
        public string Teta_Sum { get; set; }
    }
    public class Table3_Control_Element
    {
        public string Point { get; set; }
        public string Q_j { get; set; }
        public string f_j { get; set; }
        public string K_MF_j { get; set; }
        public string S_j { get; set; }
        public string S_j_ERROR { get; set; }
        public string n_j { get; set; }
        public string S0_j { get; set; }
        public string t095_j { get; set; }
        public string e_j { get; set; }
        public string Delta_j { get; set; }
        public string Delta_j_ERROR { get; set; }
        public string SK_j { get; set; }
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
    public class ActivatedTable
    {
        public int Table0 { get; set; }
        public int Table1 { get; set; }
        public int Table1_1 { get; set; }
        public int Table2 { get; set; }
        public int Table3 { get; set; }
        public int Table3_Control { get; set; }
        public int Table3_Control_Element { get; set; }
        public int Table4 { get; set; }
    }
    public class AdditionalInfo
    {
        public string Place { get; set; }
        public string Place_SIKN { get; set; }
        public string Place_Factory { get; set; }
        public string PR_Type { get; set; }
        public string PR_FactoryNumber { get; set; }
        public string PR_LineName { get; set; }
        public string PU_Type { get; set; }
        public string PU_FactoryNumber { get; set; }
        public string IVK_Type { get; set; }
        public string IVK_FactoryNumber { get; set; }
        public string ProtokolNum { get; set; }
        public string ARM_Poverka_Result { get; set; }
        public string StaffData { get; set; }
    }

    #endregion

}
