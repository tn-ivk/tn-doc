using System.Collections.Generic;

namespace TN_Doc.Class;

public class DocAct : Root
{
    public DocAct()
    {
        Doc = new Doc();

        Doc.Settings = new Settings();

        Doc.Settings.Header = new HeaderAct();
        Doc.Settings.Data = new DataAct();
        Doc.Settings.Footer = new FooterAct();
        Doc.Settings.Dictionarys = new DictionarysAct();
        Doc.DataIVK = new DataIVKAct();
        ((DataIVKAct)Doc.DataIVK).TableResultActAndPassport = new TableResultActAndPassport();
    }

    #region Doc.Settings

    #region Doc.Settings.Header
    public class HeaderAct : Header
    {

    }

    #endregion

    #region Doc.Settings.Data
    public class DataAct : Data
    {

    }

    #endregion

    #region Doc.Settings.Footer

    public class FooterAct : Footer
    {

    }

    #endregion

    #region Doc.Settings.Dictionarys

    public class DictionarysAct : Dictionarys
    {

    }

    #endregion

    #endregion

    #region Doc.DataIVK

    public class DataIVKAct : DataIVK
    {
        public TableResultActAndPassport TableResultActAndPassport { get; set; }
    }
    public class TableResultActAndPassport
    {
        public int id { get; set; }
        public string strBegin { get; set; }
        public int Begin { get; set; }
        public string strEnd { get; set; }
        public int End { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int PeriodType { get; set; }
        public int PassportID { get; set; }
        public int BIK_ID { get; set; }
        public int IsReady { get; set; }
        public ResultActAndPassport ResultActAndPassport { get; set; }
        public AdditionalInfo AdditionalInfo { get; set; }

        public long TimeStamp { get; set; }
    }
    public class ResultActAndPassport
    {
        public List<Act> Act { get; set; }
        public List<Passport> Passport { get; set; }
        public List<PassportPeriodDT> PassportPeriodDT { get; set; }
        public Common Common { get; set; }
    }
    public class Act
    {
        public string Vol_LastShift { get; set; }
        public string Mass_LastShift { get; set; }
        public string Vol_CurrShift { get; set; }
        public string Mass_CurrShift { get; set; }
        public string Vol_Gross { get; set; }
        public string Mass_Gross { get; set; }
        public string Temp { get; set; }
        public string Press { get; set; }
        public string Density { get; set; }
        public string Density_correction { get; set; }
        public string PassportID { get; set; }
        public Balast Balast { get; set; }
        public string Balast_Mass_Frac { get; set; }
        public string Sulfur { get; set; }
        public string Chloride_Salts { get; set; }
        public string Balast_Mass { get; set; }
        public string Net_Mass { get; set; }
    }
    public class Balast
    {
        public string Water { get; set; }
        public string Chlorides { get; set; }
        public string Impurity { get; set; }
    }
    public class Passport
    {
        public string TempResult { get; set; }
        public string PressResult { get; set; }
        public string DensResult { get; set; }
        public string Dens15Result { get; set; }
        public string Dens20Result { get; set; }
        public string MassWaterFracResult { get; set; }
        public string PassportID { get; set; }
        public ChlorideSalts Chloride_Salts { get; set; }
        public string Impurity { get; set; }
        public string SulfurResult { get; set; }
        public DNP DNP { get; set; }
        public string Yield_fraction_200 { get; set; }
        public string Yield_fraction_300 { get; set; }
        public string Yield_fraction_350 { get; set; }
        public string Mass_fraction_of_paraffin { get; set; }
        public string Mass_fraction_of_hydrogen_sulfide { get; set; }
        public string Mass_fraction_of_methyl_and_ethyl_mercaptan { get; set; }
        public string Mass_fraction_of_organic_chlorides { get; set; }
        public string Oil_Type { get; set; }
        public string GOSTIndexResult { get; set; }
        public string ExtendGOSTIndexResult { get; set; }
    }
    public class ChlorideSalts
    {
        public string Concentration { get; set; }
        public string MassFraction { get; set; }
    }
    public class DNP
    {
        public string kPa { get; set; }
        public string mercury_mm { get; set; }
    }
    public class PassportPeriodDT
    {
        public int Begin { get; set; }
        public int End { get; set; }
    }
    public class Common
    {
        public string Begin_Mass { get; set; }
        public string End_Mass { get; set; }
        public string Net_Mass { get; set; }
        public string Balast_Mass { get; set; }
        public string Gross_Mass { get; set; }
        public string Gross_Vol { get; set; }
        public ValueInWords ValueInWords { get; set; }
        public string UsedShift { get; set; }
        public List<string> GostIndex51858 { get; set; }
        public List<string> ExtendGostIndex51858 { get; set; }
    }
    public class ValueInWords
    {
        public string WholePart { get; set; }
        public string Fraction { get; set; }
    }
    public class AdditionalInfo
    {
        public string ActNumber { get; set; }
        public string DelivePoint { get; set; }
        public string Factory { get; set; }
        public string SIKN_Number { get; set; }
        public string Delive_Factory { get; set; }
        public string Delive_FIO { get; set; }
        public string Delive_IOF { get; set; }
        public string Delive_Lic_Date { get; set; }
        public string Delive_Lic_Number { get; set; }
        public string Receive_Factory { get; set; }
        public string Receive_FIO { get; set; }
        public string Receive_IOF { get; set; }
        public string Receive_Lic_Date { get; set; }
        public string Receive_Lic_Number { get; set; }
        public string Oil_Name { get; set; }
        public string Contract { get; set; }
        public string Route_Telegram { get; set; }
        public string Refinery_Plant { get; set; }
        public string Consignor { get; set; }
        public string Consignee { get; set; }
        public string Destination { get; set; }
        public string Exporter_Importer { get; set; }
        public string Declaration { get; set; }
        public string TankerName { get; set; }
        public string TimeSchedule_Position { get; set; }
    }

    #endregion
}