using System.Collections.Generic;

namespace TN_Doc.Class
{
    public class DocKMH_PR_PU : Root
    {
        public DocKMH_PR_PU()
        {
            Doc = new Doc();

            Doc.Settings = new Settings();

            Doc.Settings.Header = new HeaderKMH_PR_PU();
            Doc.Settings.Data = new DataKMH_PR_PU();
            Doc.Settings.Footer = new FooterKMH_PR_PU();
            Doc.Settings.Dictionarys = new DictionarysKMH_PR_PU();
            Doc.DataIVK = new DataIVKKMH_PR_PU();
            ((DataIVKKMH_PR_PU)Doc.DataIVK).TableKMH_PR_PU = new TableKMH_PR_PU();
        }

        #region Doc.Settings

        #region Doc.Settings.Header
        public class HeaderKMH_PR_PU : Header
        {
            public string FieldSIKN { get; set; }
        }

        #endregion

        #region Doc.Settings.Data
        public class DataKMH_PR_PU : Data
        {

        }

        #endregion

        #region Doc.Settings.Footer

        public class FooterKMH_PR_PU : Footer
        {

        }

        #endregion

        #region Doc.Settings.Dictionarys
        public class DictionarysKMH_PR_PU : Dictionarys
        {

        }

        #endregion

        #endregion

        #region Doc.DataIVK

        public class DataIVKKMH_PR_PU : DataIVK
        {
            public TableKMH_PR_PU TableKMH_PR_PU { get; set; }
        }
        public class TableKMH_PR_PU
        {
            public int id { get; set; }
            public int MI { get; set; }
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
            public int IsUSR { get; set; }
            public string Visc { get; set; }
            public string Delta_KMH_Max { get; set; }
            public string Delta_KMH_Max_ERROR { get; set; }
            public string OilType { get; set; }
            public int Use_fv { get; set; }
            public Table1 Table1 { get; set; }
            public Table2 Table2 { get; set; }
            public Table2_1 Table2_1 { get; set; }
        }
        public class Table1
        {
            public List<string> DetName { get; set; }
            public List<string> V0 { get; set; }
            public string m_D { get; set; }
            public string m_S { get; set; }
            public string m_E { get; set; }
            public string Alpha_t { get; set; }
            public string Alpha_D { get; set; }
            public List<string> Q { get; set; }
        }
        public class Table2
        {
            public List<Obj> Obj { get; set; }
        }
        public class Table2_1
        {
            public List<Obj> Obj { get; set; }
        }
        public class Obj
        {
            public string Ser { get; set; }
            public string Row { get; set; }
            public string Det { get; set; }
            public string Q_ji { get; set; }
            public string f_ji { get; set; }
            public string fv_ji { get; set; }
            public string t_PU_ji { get; set; }
            public string P_PU_ji { get; set; }
            public string t_PR_ji { get; set; }
            public string P_PR_ji { get; set; }
            public string t_cm_ji { get; set; }
            public string k_tp_ji { get; set; }
            public string V_ji { get; set; }
            public string N_ji { get; set; }
            public string K_ji { get; set; }
            public string Point { get; set; }
            public string K_j { get; set; }
            public string K_rasch_j { get; set; }
            public string Delta_j { get; set; }
            public string Delta_j_ERROR { get; set; }
            public string SKO_j { get; set; }
            public string SKO_j_ERROR { get; set; }
        }
        public class AdditionalInfo
        {
            public string Place { get; set; }
            public string Place_SIKN { get; set; }
            public string Place_Factory { get; set; }
            public string PR_Name { get; set; }
            public string PR_Type { get; set; }
            public string PR_FactoryNumber { get; set; }
            public string PU_Type { get; set; }
            public string PU_FactoryNumber { get; set; }
            public string PU_CheckDate { get; set; }
            public string ServiceStaffData { get; set; }
            public string DeliveryStaffData { get; set; }
            public string RecievingStaffData { get; set; }
            public string ARM_KMH_Result { get; set; }
            public string ProtokolNum { get; set; }
        }

        #endregion
    }
}
