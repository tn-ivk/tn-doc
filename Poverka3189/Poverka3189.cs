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
    public class Poverka3189 : DocGeneral
    {
        public Poverka3189(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
        {
            IdDoc = IdDoc.Poverka3189;
            PathToDocConfigFile = GetPathConfigFile();
            PathToDocEditConfigFile = GetPathEditConfigFile();
            PathToDocTemplateFile = GetPathTemplateFile();
        }

        private DbSet<TablePoverka3189List> ListDoc { get; set; }
        private DbSet<TablePoverka3189Data> DataDoc { get; set; }

        private Doc Doc;
        private JObject jsonDoc;

        public override List<RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<RequestListDocs> docs = new();

            var list = (from item in ListDoc
                        where item.DateTimeLong > UTBegin && item.DateTimeLong < UTEnd && item.id > 1
                        select item).ToList<TablePoverka3189List>();

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

            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3189.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3189.IL = list.IL;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3189.Protokol = JsonDeserializeObject<Protokol>(ArrByteToString(list.Data.Protokol));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3189.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3189.DateTimeString = list.DateTimeString;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3189.DateTimeLong = list.DateTimeLong;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3189.BIK_ID = list.BIK_ID;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3189.IL_Name = ArrByteToString(list.IL_Name);

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

    [Table("TablePoverka3189")]
    public class TablePoverka3189List
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int IL { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public int BIK_ID { get; set; }
        public byte[] IL_Name { get; set; }
        public TablePoverka3189Data Data { get; set; }
    }

    [Table("TablePoverka3189")]
    public class TablePoverka3189Data
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
            ((DataIVKDoc)Doc.DataIVK).TablePoverka3189 = new TablePoverka3189();
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
        public TablePoverka3189 TablePoverka3189 { get; set; }
    }
    public class TablePoverka3189
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
        public List<string> DetName { get; set; }
        public int Refrash { get; set; }
        public string OilType { get; set; }
        public Table1 Table1 { get; set; }
        public List<Table2> Table2 { get; set; }
        public List<Table3> Table3 { get; set; }
        public Table4 Table4 { get; set; }
        public List<TableGH> TableGH { get; set; }
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
        public string Alpha_t1 { get; set; }
        public string Teta_Sum_0 { get; set; }
        public string Teta_V_0 { get; set; }
        public string Delta_t_PU { get; set; }
        public string Delta_t_PP { get; set; }
        public string Delta_PP { get; set; }
        public string Delta_IVK { get; set; }
        public string K_PM { get; set; }
        public string K_M_ust { get; set; }
        public string MF_ust { get; set; }
        public string Q_type { get; set; }
        public string Q_nom_or_max { get; set; }
        public string ZS { get; set; }
        public string Delta_t_dop { get; set; }
        public string Delta_P_dop { get; set; }
        public string t_min { get; set; }
        public string t_max { get; set; }
        public string P_min { get; set; }
        public string P_max { get; set; }
        public string MetrologCharType { get; set; }
    }
    public class Table2
    {
        public int Activated { get; set; }
        public string Ser { get; set; }
        public string Row { get; set; }
        public string Q_ji { get; set; }
        public string Det { get; set; }
        public string T_ji { get; set; }
        public string t_PU_ji { get; set; }
        public string P_PU_ji { get; set; }
        public string Dens_ji { get; set; }
        public string DensTemp_ji { get; set; }
        public string DensPress_ji { get; set; }
        public string Betta_ji { get; set; }
        public string N_ji { get; set; }
        public string M_PU_ji { get; set; }
        public string M_ji { get; set; }
        public string K_M_MF_ji { get; set; }
        public string t_D_ji { get; set; }
        public string K_t_ji { get; set; }
        public string K_P_ji { get; set; }
        public string CTL_PU_ji { get; set; }
        public string CPL_PU_ji { get; set; }
        public string CTL_PP_ji { get; set; }
        public string CPL_PP_ji { get; set; }
    }
    public class Table3
    {
        public string Ser { get; set; }
        public string Q_j { get; set; }
        public string K_M_MF_j { get; set; }
        public string n_j { get; set; }
        public string S_j { get; set; }
        public string S_ERROR_j { get; set; }
        public string S_0_j { get; set; }
        public string t095_j { get; set; }
        public string Epsilon_j { get; set; }
    }
    public class Table4
    {
        public string Q_min { get; set; }
        public string Q_max { get; set; }
        public string K_M_MF { get; set; }
        public string S_0 { get; set; }
        public string Epsilon { get; set; }
        public string Teta_A { get; set; }
        public string Teta_Z { get; set; }
        public string Teta_Dens { get; set; }
        public string Teta_t { get; set; }
        public string t_P { get; set; }
        public string Teta_M_t { get; set; }
        public string P_P { get; set; }
        public string Teta_MP { get; set; }
        public string Teta { get; set; }
        public string Delta { get; set; }
        public string Delta_Error { get; set; }
        public string K { get; set; }
        public string SKO_SUM { get; set; }
        public string SKO_Teta { get; set; }
    }
    public class TableGH
    {
        public string Point { get; set; }
        public string Q_j { get; set; }
        public string f_j { get; set; }
        public string K_j { get; set; }
    }
    public class AdditionalInfo
    {
        public string ProtokolNum { get; set; }
        public string LineName { get; set; }
        public string Place_PSP { get; set; }
        public string Place_Factory { get; set; }
        public string Place_SIKN_Name { get; set; }
        public string MPR_Sensor_Type { get; set; }
        public string MPR_Sensor_FactoryNumber { get; set; }
        public string MPR_PEP_Type { get; set; }
        public string MPR_PEP_FactoryNumber { get; set; }
        public string PU_Type { get; set; }
        public string PU_FactoryNumber { get; set; }
        public string PP_Type { get; set; }
        public string PP_FactoryNumber { get; set; }
        public string PR_Type { get; set; }
        public string PR_FactoryNumber { get; set; }
        public string IVK_Type { get; set; }
        public string IVK_FactoryNumber { get; set; }
        public string Oil_Type { get; set; }
        public string ARM_Poverka_Result { get; set; }
        public string ARM_StaffData_fio { get; set; }
        public int ARM_CheckDate { get; set; }
    }

    #endregion

}
