using System.Collections.Generic;

namespace TN_Doc.Class
{
    public class DocMeasurementJornal : Root
    {
        public DocMeasurementJornal()
        {
            Doc = new Doc();

            Doc.Settings = new Settings();

            Doc.Settings.Header = new HeaderMeasurementJornal();
            //Doc.Settings.Data = new DataMeasurementJornal();
            Doc.Settings.Footer = new FooterMeasurementJornal();
            Doc.Settings.Dictionarys = new DictionarysMeasurementJornal();
            Doc.DataIVK = new DataIVKMeasurementJornal();
            ((DataIVKMeasurementJornal)Doc.DataIVK).TableMeasurementJornal = new TableMeasurementJornal();
        }

        #region Doc.Settings

        #region Doc.Settings.Header
        public class HeaderMeasurementJornal : Header
        {
            public string Prefix_SIKN_Name { get; set; }
            public string NameIVK { get; set; }
        }

        #endregion

        #region Doc.Settings.Data
        public class DataMeasurementJornal : Data
        {

        }


        #endregion

        #region Doc.Settings.Footer

        public class FooterMeasurementJornal : Footer
        {

        }


        #endregion

        #region Doc.Settings.Dictionarys
        public class DictionarysMeasurementJornal : Dictionarys
        {

        }

        #endregion

        #endregion

        #region Doc.DataIVK

        public class DataIVKMeasurementJornal : DataIVK
        {
            public TableMeasurementJornal TableMeasurementJornal { get; set; }
        }
        public class TableMeasurementJornal
        {
            public int id { get; set; }
            public int Day { get; set; }
            public int Month { get; set; }
            public int Year { get; set; }
            public int BIK_ID { get; set; }
            public Additional AdditionalInfo { get; set; }
            public Data Data { get; set; }
            public DataARM DataARM { get; set; }
        }
        public class Additional
        {
            public string SIKN_Name { get; set; }
            public string Factory { get; set; }
            public int ShiftNum { get; set; }
        }
        public class Data
        {
            public List<Rows> Rows { get; set; }
            public List<string> Shift { get; set; }
            public string Day { get; set; }
        }
        public class Rows
        {
            public int Used { get; set; }
            public string strBegin { get; set; }
            public string strEnd { get; set; }
            public List<BIK> BIK { get; set; }
            public List<Line> Line { get; set; }
            public List<SIKN> SIKN { get; set; }
            public string TankerName { get; set; }
            public string ExtendAddInfo { get; set; }
        }
        public class BIK
        {
            public string Dens { get; set; }
            public string Dens15 { get; set; }
            public string Dens20 { get; set; }
            public string DynamicVisc { get; set; }
            public string KinemVisc { get; set; }
            public string Flow { get; set; }
            public string VolWater { get; set; }
            public string MasWater { get; set; }
            public string MasSulphur { get; set; }
            public string DensTemp { get; set; }
            public string DensPress { get; set; }
            public string Oil_Type { get; set; }
            public string ViscTemp { get; set; }
        }
        public class Line
        {
            public int Used { get; set; }
            public string Name { get; set; }
            public string Dens { get; set; }
            public string MassFlow { get; set; }
            public string VolFlow { get; set; }
            public string Vol { get; set; }
            public string Vol20 { get; set; }
            public string Vol15 { get; set; }
            public string Mass { get; set; }
            public string Temp { get; set; }
            public string Press { get; set; }
            public string BeginVol { get; set; }
            public string EndVol { get; set; }
            public string BeginVol15 { get; set; }
            public string EndVol15 { get; set; }
            public string BeginVol20 { get; set; }
            public string EndVol20 { get; set; }
            public string BeginMass { get; set; }
            public string EndMass { get; set; }
            public string BeginVolTotal { get; set; }
            public string EndVolTotal { get; set; }
            public string BeginVol15Total { get; set; }
            public string EndVol15Total { get; set; }
            public string BeginVol20Total { get; set; }
            public string EndVol20Total { get; set; }
            public string BeginMassTotal { get; set; }
            public string EndMassTotal { get; set; }
            public string CheckDeltaVol { get; set; }
            public string CheckDeltaVol20 { get; set; }
            public string CheckDeltaVol15 { get; set; }
            public string CheckDeltaMass { get; set; }
            public string VolTotal { get; set; }
            public string Vol20Total { get; set; }
            public string Vol15Total { get; set; }
            public string MassTotal { get; set; }
            public string Vol15Flow { get; set; }
            public string Vol20Flow { get; set; }
        }
        public class SIKN
        {
            public string Dens { get; set; }
            public string MassFlow { get; set; }
            public string VolFlow { get; set; }
            public string Vol { get; set; }
            public string Vol20 { get; set; }
            public string Vol15 { get; set; }
            public string Mass { get; set; }
            public string Temp { get; set; }
            public string Press { get; set; }
            public string BeginVol { get; set; }
            public string EndVol { get; set; }
            public string BeginVol15 { get; set; }
            public string EndVol15 { get; set; }
            public string BeginVol20 { get; set; }
            public string EndVol20 { get; set; }
            public string BeginMass { get; set; }
            public string EndMass { get; set; }
            public string BeginVolTotal { get; set; }
            public string EndVolTotal { get; set; }
            public string BeginVol15Total { get; set; }
            public string EndVol15Total { get; set; }
            public string BeginVol20Total { get; set; }
            public string EndVol20Total { get; set; }
            public string BeginMassTotal { get; set; }
            public string EndMassTotal { get; set; }
            public string CheckDeltaVol { get; set; }
            public string CheckDeltaVol20 { get; set; }
            public string CheckDeltaVol15 { get; set; }
            public string CheckDeltaMass { get; set; }
            public string VolTotal { get; set; }
            public string Vol20Total { get; set; }
            public string Vol15Total { get; set; }
            public string MassTotal { get; set; }
            public string Vol15Flow { get; set; }
            public string Vol20Flow { get; set; }
        }
        public class DataARM
        {
            public string DeliveryFIO1 { get; set; }
            public string DeliveryFIO2 { get; set; }
            public string ReceiveFIO1 { get; set; }
            public string ReceiveFIO2 { get; set; }
        }
        #endregion
    }
}
