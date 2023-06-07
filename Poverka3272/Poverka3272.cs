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
    public class Poverka3272 : DocGeneral
    {
        public Poverka3272(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
        {
            IdDoc = IdDoc.Poverka3272;
            PathToDocConfigFile = GetPathConfigFile();
            PathToDocEditConfigFile = GetPathEditConfigFile();
            PathToDocTemplateFile = GetPathTemplateFile();
        }

        private DbSet<TablePoverka3272List> ListDoc { get; set; }
        private DbSet<TablePoverka3272Data> DataDoc { get; set; }

        private Doc Doc;
        private JObject jsonDoc;

        public override List<RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<RequestListDocs> docs = new();

            var list = (from item in ListDoc
                        where item.DateTimeLong > UTBegin && item.DateTimeLong < UTEnd && item.id > 1
                        select item).ToList<TablePoverka3272List>();

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

            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3272.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3272.IL = list.IL;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3272.Protokol = JsonDeserializeObject<Protokol>(ArrByteToString(list.Data.Protokol));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3272.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3272.DateTimeString = list.DateTimeString;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3272.DateTimeLong = list.DateTimeLong;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3272.BIK_ID = list.BIK_ID;
            ((DataIVKDoc)doc.Doc.DataIVK).TablePoverka3272.IL_Name = ArrByteToString(list.IL_Name);

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

    [Table("TablePoverka3272")]
    public class TablePoverka3272List
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int IL { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public int BIK_ID { get; set; }
        public byte[] IL_Name { get; set; }
        public TablePoverka3272Data Data { get; set; }
    }

    [Table("TablePoverka3272")]
    public class TablePoverka3272Data
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
            ((DataIVKDoc)Doc.DataIVK).TablePoverka3272 = new TablePoverka3272();
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
        public TablePoverka3272 TablePoverka3272 { get; set; }
    }
    public class TablePoverka3272
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
        public string OilType { get; set; }
        public int Refrash { get; set; }
        public Table1 Table1 { get; set; }
        public List<Table2> Table2 { get; set; }
        public List<Table2_part1> Table2_part1 { get; set; }
        public List<Table2_part1_Points> Table2_part1_Points { get; set; }
        public List<Table2_part2> Table2_part2 { get; set; }
        public List<Table2_part2_Points> Table2_part2_Points { get; set; }
        public Table3 Table3 { get; set; }
        public List<Table3_1> Table3_1 { get; set; }
        public List<Table4_1> Table4_1 { get; set; }
        public Table4_1_Subr Table4_1_Subr { get; set; }
        public List<Table4_2> Table4_2 { get; set; }
        public Table4_2_Subr Table4_2_Subr { get; set; }
        public List<Table4_3> Table4_3 { get; set; }
        public List<Table4_3_Subr> Table4_3_Subr { get; set; }
        public ActivatedTables ActivatedTables { get; set; }
        public List<TableGH> TableGH { get; set; }
    }
    public class Table1
    {
        public string V_0_KP { get; set; }
        public string Delta_KP { get; set; }
        public string m_D { get; set; }
        public string m_s { get; set; }
        public string m_E { get; set; }
        public string Delta_t_KP { get; set; }
        public string Delta_PP { get; set; }
        public string Delta_t_PP { get; set; }
        public string Delta_k_UOI { get; set; }
        public string KF_konf { get; set; }
        public string ZS { get; set; }
    }
    public class Table2
    {
        public int Activated { get; set; }
        public string Ser { get; set; }
        public string Row { get; set; }
        public string Q_ji { get; set; }
        public string N_mas_ji { get; set; }
        public string t_KP_ji { get; set; }
        public string P_KP_ji { get; set; }
        public string Dens_PP_ji { get; set; }
        public string DensTemp_ji { get; set; }
        public string DensPress_ji { get; set; }
        public string V_KP_PR_ji { get; set; }
        public string Dens_PP_PR_ji { get; set; }
        public string M_RA_ji { get; set; }
        public string M_mas_ji { get; set; }
        public string MF_ji { get; set; }
        public string T_ji { get; set; }
        public string t_cm_ji { get; set; }
        public string Beta_ji { get; set; }
        public string Gamma_ji { get; set; }
        public string KF_ji { get; set; }
    }
    public class Table2_part1
    {
        public int Activated { get; set; }
        public string Ser { get; set; }
        public string Row { get; set; }
        public string Q_ji { get; set; }
        public string N_TPR_ji { get; set; }
        public string t_TPR_ji { get; set; }
        public string P_TPR_ji { get; set; }
        public string t_KP_ji { get; set; }
        public string P_KP_ji { get; set; }
        public string t_cm_ji { get; set; }
        public string V_KP_PR_ji { get; set; }
        public string K_TPR_ji { get; set; }
        public string T_ji { get; set; }
        public string Dens_ji { get; set; }
        public string Dens_PP_PR_ji { get; set; }
        public string DensTemp_ji { get; set; }
        public string DensPress_ji { get; set; }
        public string Beta_ji { get; set; }
        public string Gamma_ji { get; set; }
    }
    public class Table2_part1_Points
    {
        public string K1_TPR_j { get; set; }
        public string K2_TPR_j { get; set; }
        public string Delta_k_j { get; set; }
        public string Repeatability_K_j { get; set; }
        public string Repeatability_K_j_ERROR { get; set; }
    }
    public class Table2_part2
    {
        public int Activated { get; set; }
        public string Ser { get; set; }
        public string Row { get; set; }
        public string Q_ji { get; set; }
        public string V_TPR_ji { get; set; }
        public string t_TPR_ji { get; set; }
        public string P_TPR_ji { get; set; }
        public string N_mas_ji { get; set; }
        public string Dens_ji { get; set; }
        public string DensTemp_ji { get; set; }
        public string DensPress_ji { get; set; }
        public string Dens_PP_PR_ji { get; set; }
        public string M_RA_ji { get; set; }
        public string M_mas_ji { get; set; }
        public string MF_ji { get; set; }
        public string T_ji { get; set; }
        public string N_TPR_ji { get; set; }
        public string Beta_ji { get; set; }
        public string Gamma_ji { get; set; }
        public string KF_ji { get; set; }
    }
    public class Table2_part2_Points
    {
        public string K_TPR_j { get; set; }
        public string N_TPR_zad_j { get; set; }
    }
    public class Table3
    {
        public string Alpha_cil_t { get; set; }
        public string Alpha_cil_t_kv { get; set; }
        public string Alpha_t_cm { get; set; }
        public string tPn { get; set; }
        public string Zp { get; set; }
    }
    public class Table3_1
    {
        public string Alpha_cil_t { get; set; }
        public string Alpha_cil_t_kv { get; set; }
        public string Alpha_t_cm { get; set; }
        public string tPn { get; set; }
        public string Zp { get; set; }
    }
    public class Table4_1
    {
        public string Point { get; set; }
        public string Q_j { get; set; }
        public string MF_j { get; set; }
    }
    public class Table4_1_Subr
    {
        public string SKO_MF { get; set; }
        public string SKO_MF_ERROR { get; set; }
        public string Delta_0_mas { get; set; }
        public string MF { get; set; }
        public string K_GR { get; set; }
        public string Epsilon { get; set; }
        public string Teta_Sum { get; set; }
        public string Delta { get; set; }
        public string Delta_ERROR { get; set; }
        public string Teta_t { get; set; }
        public string Teta_MF { get; set; }
    }
    public class Table4_2
    {
        public string Point { get; set; }
        public string Q_j { get; set; }
        public string KF_j { get; set; }
    }
    public class Table4_2_Subr
    {
        public string SKO_KF { get; set; }
        public string SKO_KF_ERROR { get; set; }
        public string Delta_0_mas { get; set; }
        public string KF { get; set; }
        public string Teta_KF { get; set; }
        public string Epsilon { get; set; }
        public string Teta_Sum { get; set; }
        public string Delta { get; set; }
        public string Delta_ERROR { get; set; }
        public string Teta_t { get; set; }
    }
    public class Table4_3
    {
        public string Point { get; set; }
        public string Q_j { get; set; }
        public string KF_j { get; set; }
    }
    public class Table4_3_Subr
    {
        public string Point { get; set; }
        public string Q_min { get; set; }
        public string Q_max { get; set; }
        public string SKO_KF { get; set; }
        public string SKO_KF_ERROR { get; set; }
        public string Delta_0_mas { get; set; }
        public string Teta_KF { get; set; }
        public string Epsilon { get; set; }
        public string Teta_Sum { get; set; }
        public string Delta { get; set; }
        public string Delta_ERROR { get; set; }
        public string Teta_t { get; set; }
    }
    public class ActivatedTables
    {
        public int Table1 { get; set; }
        public int Table2 { get; set; }
        public int Table2_Part1 { get; set; }
        public int Table2_Part1_Points { get; set; }
        public int Table2_Part2 { get; set; }
        public int Table2_Part2_Points { get; set; }
        public int Table3 { get; set; }
        public int Table3_1 { get; set; }
        public int Table4_1 { get; set; }
        public int Table4_1_Subr { get; set; }
        public int Table4_2 { get; set; }
        public int Table4_2_Subr { get; set; }
        public int Table4_3 { get; set; }
        public int Table4_3_Subr { get; set; }
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
        public string PR_Type { get; set; }
        public string Place_PSP { get; set; }
        public string Place_Factory { get; set; }
        public string PR_Sensor_Type { get; set; }
        public string PR_Sensor_DU { get; set; }
        public string PR_Sensor_FactoryNumber { get; set; }
        public string PR_PEP { get; set; }
        public string PR_PEP_FactoryNumber { get; set; }
        public string SIKN { get; set; }
        public string CP_Name { get; set; }
        public string CP_Category { get; set; }
        public string CP_FactoryNumber { get; set; }
        public string CP_CheckDate { get; set; }
        public string TPR_Name { get; set; }
        public string TPR_FactoryNumber { get; set; }
        public string TPR_Range { get; set; }
        public string PP_Name { get; set; }
        public string PP_FactoryNumber { get; set; }
        public string PP_CheckDate { get; set; }
        public string ARM_Poverka_Result { get; set; }
        public string ARM_Result_Type { get; set; }
        public string Certificate_Num { get; set; }
        public string Certificate_Date { get; set; }
        public string StaffData_company { get; set; }
        public string StaffData_fio { get; set; }
        public string Oil_Type { get; set; }
    }

    #endregion

}
