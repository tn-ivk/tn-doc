using System.Collections.Generic;

namespace TN_Doc.Class
{
    public class DocKMH_PW : Root
    {
        public DocKMH_PW()
        {
            Doc = new Doc();

            Doc.Settings = new Settings();

            Doc.Settings.Header = new HeaderKMH_PW();
            Doc.Settings.Data = new DataKMH_PW();
            Doc.Settings.Footer = new FooterKMH_PW();
            Doc.Settings.Dictionarys = new DictionarysKMH_PW();
            Doc.DataIVK = new DataIVKKMH_PW();
            ((DataIVKKMH_PW)Doc.DataIVK).TableKMH_PW = new TableKMH_PW();
        }

        #region Doc.Settings

        #region Doc.Settings.Header
        public class HeaderKMH_PW : Header
        {
            public string FieldSIKN { get; set; }
        }

        #endregion

        #region Doc.Settings.Data
        public class DataKMH_PW : Data
        {

        }

        #endregion

        #region Doc.Settings.Footer

        public class FooterKMH_PW : Footer
        {

        }

        #endregion

        #region Doc.Settings.Dictionarys
        public class DictionarysKMH_PW : Dictionarys
        {
            public Dictionary<int, string> ParameterName { get; set; }
            public Dictionary<int, string> FlowMeterColHeader { get; set; }
            public Dictionary<int, string> LabMethodColHeader { get; set; }
            public Dictionary<int, string> ControlConditions { get; set; }
            public Dictionary<int, string> FlowMeterUnitMeasurement { get; set; }
            public Dictionary<int, string> LabMethodUnitMeasurement { get; set; }
            public Dictionary<int, string> ControlResult { get; set; }
            public Dictionary<int, string> OilTechParams { get; set; }

            public DictionarysKMH_PW()
            {
                ParameterName = new Dictionary<int, string>();
                FlowMeterColHeader = new Dictionary<int, string>();
                LabMethodColHeader = new Dictionary<int, string>();
                ControlConditions = new Dictionary<int, string>();
                FlowMeterUnitMeasurement = new Dictionary<int, string>();
                LabMethodUnitMeasurement = new Dictionary<int, string>();
                ControlResult = new Dictionary<int, string>();
                OilTechParams = new Dictionary<int, string>();
            }

        }

        #endregion

        #endregion

        #region Doc.DataIVK

        public class DataIVKKMH_PW : DataIVK
        {
            public TableKMH_PW TableKMH_PW { get; set; }
        }
        public class TableKMH_PW
        {
            public int id { get; set; }
            public int BIK { get; set; }
            public int PW { get; set; }
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
            public string Error_PW { get; set; }
        }
        public class Table1
        {
            public string Delta_PW { get; set; }
            public string Delta_LAB { get; set; }
        }

        public class Table2
        {
            public string Q { get; set; }
            public string t { get; set; }
            public string P { get; set; }
            public string Dens { get; set; }
            public string Water { get; set; }
            public string Water_LAB { get; set; }
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
            public List<PWAddInfo> PW_AddInfo { get; set; }
        }
        public class PWAddInfo
        {
            public string ProtokolNum { get; set; }
            public string PW_Type { get; set; }
            public string PW_ManufNum { get; set; }
            public string PW_CheckDate { get; set; }
            public string ARM_KMH_Result { get; set; }
        }

        #endregion
    }
}
