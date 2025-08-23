using System.Collections.Generic;

namespace TN_Doc.Class;

public class DocKMH_PP_Areom : Root
{
    public DocKMH_PP_Areom()
    {
        Doc = new Doc();

        Doc.Settings = new Settings();

        Doc.Settings.Header = new HeaderKMH_PP_Areom();
        Doc.Settings.Data = new DataKMH_PP_Areom();
        Doc.Settings.Footer = new FooterKMH_PP_Areom();
        Doc.Settings.Dictionarys = new DictionarysKMH_PP_Areom();
        Doc.DataIVK = new DataIVKKMH_PP_Areom();
        ((DataIVKKMH_PP_Areom)Doc.DataIVK).TableKMH_PP_Areom = new TableKMH_PP_Areom();
    }

    #region Doc.Settings

    #region Doc.Settings.Header
    public class HeaderKMH_PP_Areom : Header
    {
        public string FieldSIKN { get; set; }
    }

    #endregion

    #region Doc.Settings.Data
    public class DataKMH_PP_Areom : Data
    {

    }

    #endregion

    #region Doc.Settings.Footer

    public class FooterKMH_PP_Areom : Footer
    {

    }

    #endregion

    #region Doc.Settings.Dictionarys
    public class DictionarysKMH_PP_Areom : Dictionarys
    {

    }

    #endregion

    #endregion

    #region Doc.DataIVK

    public class DataIVKKMH_PP_Areom : DataIVK
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
        public string SIKN_Num { get; set; }
        public string PSP_Name { get; set; }
        public string SensName_Oper { get; set; }
        public string ManufNum_Oper { get; set; }
        public string CheckDate_Oper { get; set; }
        public string SensName_Areom { get; set; }
        public string ManufNum_Areom { get; set; }
        public string CheckDate_Areom { get; set; }
        public string AbsError_Areom { get; set; }
        public string SensName_Areom2 { get; set; }
        public string ManufNum_Areom2 { get; set; }
        public string CheckDate_Areom2 { get; set; }
        public string AbsError_Areom2 { get; set; }
        public string ServiceStaffData { get; set; }
        public string DeliveryStaffData { get; set; }
        public string RecievingStaffData { get; set; }
        public string ARM_KMH_Result { get; set; }
        public string Error { get; set; }
    }

    #endregion
}