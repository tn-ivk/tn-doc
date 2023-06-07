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
    public class Poverka3288 : DocGeneral
    {
        public Poverka3288(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
        {
            IdDoc = IdDoc.Poverka3288;
            PathToDocConfigFile = GetPathConfigFile();
            PathToDocEditConfigFile = GetPathEditConfigFile();
            PathToDocTemplateFile = GetPathTemplateFile();
        }

        private DbSet<TablePoverka3288List> ListDoc { get; set; }
        private DbSet<TablePoverka3288Data> DataDoc { get; set; }

        private Doc Doc;
        private JObject jsonDoc;

        public override List<RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<RequestListDocs> docs = new();

            var list = (from item in ListDoc
                        where item.DateTimeLong > UTBegin && item.DateTimeLong < UTEnd && item.id > 1
                        select item).ToList<TablePoverka3288List>();

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

            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3288.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3288.IL = list.IL;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3288.Mode = list.Mode;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3288.Protokol_MPR = JsonDeserializeObject<Protokol_MPR>(ArrByteToString(list.Data.Protokol_MPR));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3288.AdditionalInfo_MPR = JsonDeserializeObject<AdditionalInfo_MPR>(ArrByteToString(list.Data.AdditionalInfo_MPR));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3288.Protokol_TPR = JsonDeserializeObject<Protokol_TPR>(ArrByteToString(list.Data.Protokol_TPR));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3288.AdditionalInfo_TPR = JsonDeserializeObject<AdditionalInfo_TPR>(ArrByteToString(list.Data.AdditionalInfo_TPR));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3288.DateTimeString = list.DateTimeString;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3288.DateTimeLong = list.DateTimeLong;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3288.BIK_ID = list.BIK_ID;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3288.IL_Name = ArrByteToString(list.IL_Name);

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

    [Table("TablePoverka3288")]
    public class TablePoverka3288List
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int IL { get; set; }
        public int Mode { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public int BIK_ID { get; set; }
        public byte[] IL_Name { get; set; }
        public TablePoverka3288Data Data { get; set; }
    }

    [Table("TablePoverka3288")]
    public class TablePoverka3288Data
    {
        [Key]
        public int id { get; set; }
        public byte[] Protokol_MPR { get; set; }
        public byte[] AdditionalInfo_MPR { get; set; }
        public byte[] Protokol_TPR { get; set; }
        public byte[] AdditionalInfo_TPR { get; set; }
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
            ((DataIVKDoc)Doc.DataIVK).TablePoverka3288 = new TablePoverka3288();
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
        public TablePoverka3288 TablePoverka3288 { get; set; }
    }

    public class TablePoverka3288
    {
        public int id { get; set; }
        public int IL { get; set; }
        public int Mode { get; set; }
        public Protokol_MPR Protokol_MPR { get; set; }
        public AdditionalInfo_MPR AdditionalInfo_MPR { get; set; }
        public Protokol_TPR Protokol_TPR { get; set; }
        public AdditionalInfo_TPR AdditionalInfo_TPR { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public int BIK_ID { get; set; }
        public string IL_Name { get; set; }
    }

    public class Protokol_MPR
    {
        public string OilType { get; set; }
        public int CalcMode { get; set; }
        public List<Table1MPR> Table1 { get; set; }
        public List<Table2_Part1> Table2_part1 { get; set; }
        public List<Table2_Part2> Table2_part2 { get; set; }
        public List<Table3MPR> Table3 { get; set; }
        public List<Table4> Table4 { get; set; }
        public List<TableGH> TableGH { get; set; }
    }
    public class Table1MPR
    {
        public string DetName { get; set; }
        public string V0 { get; set; }
        public string m_D { get; set; }
        public string m_S { get; set; }
        public string m_E { get; set; }
        public string Alpha_k1 { get; set; }
        public string Alpha_D { get; set; }
        public string Teta_Sum_0 { get; set; }
        public string Teta_V_0 { get; set; }
        public string Delta_t_PU { get; set; }
        public string Delta_t_PP { get; set; }
        public string Delta_PP { get; set; }
        public string Delta_t_PR { get; set; }
        public string delta_PR { get; set; }
        public string delta_IVK_PU { get; set; }
        public string delta_IVK_PR { get; set; }
        public string K_PM_ust { get; set; }
        public string K_M_MF_ust { get; set; }
        public string Q_M_max { get; set; }
        public string ZS { get; set; }
        public string delta_t_dop { get; set; }
        public string Q_nom { get; set; }
        public string t_min { get; set; }
        public string t_max { get; set; }
        public string delta_P_dop { get; set; }
        public string P_min { get; set; }
        public string P_max { get; set; }
    }
    public class Table2_Part1
    {
        public int Activated { get; set; }
        public string Ser { get; set; }
        public string Row { get; set; }
        public string Q_ji { get; set; }
        public string T_ji { get; set; }
        public string t_PU_ji { get; set; }
        public string P_PU_ji { get; set; }
        public string t_D_ji { get; set; }
        public string Dens_PP_ji { get; set; }
        public string DensTemp_ji { get; set; }
        public string DensPress_ji { get; set; }
        public string Beta_ji { get; set; }
        public string N_ji { get; set; }
        public string M_0_ji { get; set; }
        public string M_ji { get; set; }
        public string K_MF_F_ji { get; set; }
        public string f_ji { get; set; }
        public string U_ji { get; set; }
        public string U_ji_ERROR { get; set; }
        public string CTL_PU_ji { get; set; }
        public string CPL_PU_ji { get; set; }
        public string CTL_PP_ji { get; set; }
        public string CPL_PP_ji { get; set; }
    }
    public class Table2_Part2
    {
        public int Activated { get; set; }
        public string Ser { get; set; }
        public string Row { get; set; }
        public string Q_ji { get; set; }
        public string T_ji { get; set; }
        public string Q_PR_ji { get; set; }
        public string t_PR_ji { get; set; }
        public string P_PR_ji { get; set; }
        public string K_PR_ji { get; set; }
        public string N_PR_ji { get; set; }
        public string Dens_PP_ji { get; set; }
        public string DensTemp_ji { get; set; }
        public string DensPress_ji { get; set; }
        public string Beta_ji { get; set; }
        public string N_ji { get; set; }
        public string M_0_ji { get; set; }
        public string M_ji { get; set; }
        public string K_MF_F_ji { get; set; }
        public string f_ji { get; set; }
        public string U_ji { get; set; }
        public string U_ji_ERROR { get; set; }
        public string CTL_PR_ji { get; set; }
        public string CPL_PR_ji { get; set; }
        public string CTL_PP_ji { get; set; }
        public string CPL_PP_ji { get; set; }
    }
    public class Table3MPR
    {
        public int Activated { get; set; }
        public string Point { get; set; }
        public string Q_j { get; set; }
        public string K_MF_F_j { get; set; }
        public string n_j { get; set; }
        public string S_j { get; set; }
        public string S_j_ERROR { get; set; }
        public string S_0_j { get; set; }
        public string t_095_j { get; set; }
        public string Epsilon_j { get; set; }
        public string f_j { get; set; }
        public string U_j { get; set; }
        public string U_j_ERROR { get; set; }
    }
    public class Table4
    {
        public string Q_min { get; set; }
        public string Q_max { get; set; }
        public string K_MF_F { get; set; }
        public string S_0 { get; set; }
        public string Epsilon { get; set; }
        public string Teta_A { get; set; }
        public string Teta_Z { get; set; }
        public string Teta_ro { get; set; }
        public string Teta_t_PU { get; set; }
        public string Teta_t_PR { get; set; }
        public string t_P { get; set; }
        public string Teta_Mt { get; set; }
        public string P_P { get; set; }
        public string Teta_MP { get; set; }
        public string Teta_Sum { get; set; }
        public string delta { get; set; }
        public string delta_ERROR { get; set; }
    }
    public class TableGH
    {
        public string Point { get; set; }
        public string f_j { get; set; }
        public string K_j { get; set; }
        public string Q_j { get; set; }
    }
    public class AdditionalInfo_MPR
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
        public string CP_Type { get; set; }
        public string CP_FactoryNumber { get; set; }
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

    public class Protokol_TPR
    {
        public string OilType { get; set; }
        public List<Table1TPR> Table1 { get; set; }
        public List<Table2> Table2 { get; set; }
        public Table3TPR Table3 { get; set; }
    }
    public class Table1TPR
    {
        public string DetName { get; set; }
        public string V0 { get; set; }
        public string m_D { get; set; }
        public string m_S { get; set; }
        public string m_E { get; set; }
        public string Alpha_k1 { get; set; }
        public string Alpha_D { get; set; }
        public string Teta_Sum_0 { get; set; }
        public string Teta_V_0 { get; set; }
        public string Delta_t_PU { get; set; }
        public string Delta_t_PR { get; set; }
        public string delta_IVK { get; set; }
    }
    public class Table2
    {
        public int Activated { get; set; }
        public string Ser { get; set; }
        public string Row { get; set; }
        public string Q_ji { get; set; }
        public string T_ji { get; set; }
        public string t_PU_ji { get; set; }
        public string P_PU_ji { get; set; }
        public string t_D_ji { get; set; }
        public string Dens_PP_ji { get; set; }
        public string DensTemp_ji { get; set; }
        public string DensPress_ji { get; set; }
        public string Beta_ji { get; set; }
        public string t_PR_ji { get; set; }
        public string P_PR_ji { get; set; }
        public string N_ji { get; set; }
        public string K_PR_ji { get; set; }
        public string U_ji { get; set; }
        public string U_ji_ERROR { get; set; }
        public string CTL_PU_ji { get; set; }
        public string CPL_PU_ji { get; set; }
        public string CTL_PR_ji { get; set; }
        public string CPL_PR_ji { get; set; }
    }
    public class Table3TPR
    {
        public List<Points> Point { get; set; }
        public string Teta_t { get; set; }
        public string Teta_Sum { get; set; }
        public string delta_PR { get; set; }
    }
    public class Points
    {
        public int Activated { get; set; }
        public string Point { get; set; }
        public string Q_j { get; set; }
        public string K_PR_j { get; set; }
        public string S_j { get; set; }
        public string S_j_ERROR { get; set; }
        public string n_j { get; set; }
        public string S_0_j { get; set; }
        public string t_095_j { get; set; }
        public string Epsilon_j { get; set; }
        public string delta_PR_j { get; set; }
        public string U_j { get; set; }
        public string U_j_ERROR { get; set; }
    }
    public class AdditionalInfo_TPR
    {
        public string ProtokolNum { get; set; }
        public string Place_PSP { get; set; }
        public string Place_Factory { get; set; }
        public string Place_SIKN_Name { get; set; }
        public string PR_Type { get; set; }
        public string PR_FactoryNumber { get; set; }
        public string CP_Type { get; set; }
        public string CP_FactoryNumber { get; set; }
        public string IVK_Type { get; set; }
        public string IVK_FactoryNumber { get; set; }
        public string Oil_Type { get; set; }
        public string ARM_StaffData_fio { get; set; }
        public int ARM_CheckDate { get; set; }
    }

    #endregion

}
