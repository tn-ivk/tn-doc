using System.Collections.Generic;

namespace TN_Doc.Class
{
    public class DocPoverka3287 : Root
    {
        public DocPoverka3287()
        {
            Doc = new Doc();

            Doc.Settings = new Settings();

            Doc.Settings.Header = new HeaderPoverka3287();
            Doc.Settings.Data = new DataPoverka3287();
            Doc.Settings.Footer = new FooterPoverka3287();
            Doc.Settings.Dictionarys = new DictionarysPoverka3287();
            Doc.DataIVK = new DataIVKPoverka3287();
            ((DataIVKPoverka3287)Doc.DataIVK).TablePoverka3287 = new TablePoverka3287();
        }

        #region Doc.Settings

        #region Doc.Settings.Header
        public class HeaderPoverka3287 : Header
        {
            public string FieldSIKN { get; set; }
        }

        #endregion

        #region Doc.Settings.Data
        public class DataPoverka3287 : Data
        {

        }

        #endregion

        #region Doc.Settings.Footer

        public class FooterPoverka3287 : Footer
        {

        }

        #endregion

        #region Doc.Settings.Dictionarys
        public class DictionarysPoverka3287 : Dictionarys
        {

        }

        #endregion

        #endregion

        #region Doc.DataIVK

        public class DataIVKPoverka3287 : DataIVK
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
}

