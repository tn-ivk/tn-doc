using System.Collections.Generic;

namespace TN_Doc.Class;

public class DocKMH_PV : Root
{
    public DocKMH_PV()
    {
        Doc = new Doc();

        Doc.Settings = new Settings();

        Doc.Settings.Header = new HeaderKMH_PV();
        Doc.Settings.Data = new DataKMH_PV();
        Doc.Settings.Footer = new FooterKMH_PV();
        Doc.Settings.Dictionarys = new DictionarysKMH_PV();
        Doc.DataIVK = new DataIVKKMH_PV();
        ((DataIVKKMH_PV)Doc.DataIVK).TableKMH_PV = new TableKMH_PV();
    }

    #region Doc.Settings

    #region Doc.Settings.Header

    public class HeaderKMH_PV : Header
    {
        public string FieldSIKN { get; set; }
    }

    #endregion

    #region Doc.Settings.Data
    public class DataKMH_PV : Data
    {

    }

    #endregion

    #region Doc.Settings.Footer

    public class FooterKMH_PV : Footer
    {

    }

    #endregion

    #region Doc.Settings.Dictionarys

    public class DictionarysKMH_PV : Dictionarys
    {
        public Dictionary<int, string> Parameter01Name { get; set; }
        public Dictionary<int, string> Parameter02Name { get; set; }
        public Dictionary<int, string> ControlConditions { get; set; }
        public Dictionary<int, string> UnitsMeasurement { get; set; }
        public Dictionary<int, string> OilTechParams { get; set; }
        public Dictionary<int, string> Dens { get; set; }
        public Dictionary<int, string> Visc_LAB { get; set; }
        public Dictionary<int, string> Delta { get; set; }
        public DictionarysKMH_PV()
        {
            Parameter01Name = new Dictionary<int, string>();
            Parameter02Name = new Dictionary<int, string>();
            ControlConditions = new Dictionary<int, string>();
            UnitsMeasurement = new Dictionary<int, string>();
            OilTechParams = new Dictionary<int, string>();
            Dens = new Dictionary<int, string>();
            Visc_LAB = new Dictionary<int, string>();
            Delta = new Dictionary<int, string>();
        }
    }

    #endregion

    #endregion

    #region Doc.DataIVK

    public class DataIVKKMH_PV : DataIVK
    {
        public TableKMH_PV TableKMH_PV { get; set; }
    }
    public class TableKMH_PV
    {
        public int id { get; set; }
        public int BIK { get; set; }
        public int PV { get; set; }
        public int KMH_TYPE { get; set; }
        public Protokol Protokol { get; set; }
        public AdditionalInfo AdditionalInfo { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
    }
    public class Protokol
    {
        public string KMX_Condition { get; set; }
        public Table1 Table1 { get; set; }
        public Table2 Table2 { get; set; }
        public string Error_PV { get; set; }
    }
    public class Table1
    {
        public string UpLimitMeas_PV { get; set; }
        public string Delta_PV { get; set; }
        public string UpLimitMeas_LAB { get; set; }
        public string Delta_LAB { get; set; }
    }
    public class Table2
    {
        public string Q { get; set; }
        public string t { get; set; }
        public string P { get; set; }
        public string Dens { get; set; }
        public string Visc { get; set; }
        public string Visc_LAB { get; set; }
        public string t_LAB { get; set; }
        public string Delta { get; set; }
    }
    public class AdditionalInfo
    {
        public string Place_PSP { get; set; }
        public string Place_Factory { get; set; }
        public string Place_SIKN_Name { get; set; }
        public string Laboratory_Name { get; set; }
        public string LAB_Type { get; set; }
        public string LAB_ManufNum { get; set; }
        public string LAB_CheckDate { get; set; }
        public string ServiceStaffData { get; set; }
        public string DeliveryStaffData { get; set; }
        public string RecievingStaffData { get; set; }
        public List<PVAddInfo> PV_AddInfo { get; set; }
    }
    public class PVAddInfo
    {
        public string ProtokolNum { get; set; }
        public string PV_Type { get; set; }
        public string PV_ManufNum { get; set; }
        public string PV_CheckDate { get; set; }
        public string ARM_KMH_Result { get; set; }
    }

    #endregion
}