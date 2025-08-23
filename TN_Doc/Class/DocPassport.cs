using System.Collections.Generic;

namespace TN_Doc.Class;

public class DocPassport : Root
{
    public DocPassport()
    {
        Doc = new Doc();

        Doc.Settings = new Settings();

        Doc.Settings.Header = new HeaderPassport();
        Doc.Settings.Data = new DataPassport();
        Doc.Settings.Footer = new FooterPassport();
        Doc.Settings.Dictionarys = new DictionarysPassport();
        Doc.DataIVK = new DataIVKPassport();
        Doc.DataIVK = new DataIVKPassport();
        ((DataIVKPassport)Doc.DataIVK).TablePassport = new TablePassport();
    }

    #region Doc.Settings

    #region Doc.Settings.Header
    public class HeaderPassport : Header
    {
        public string FieldSIKN { get; set; }
    }

    #endregion

    #region Doc.Settings.Data
    public class DataPassport : Data
    {
        public List<Parameter> Parameters { get; set; }
    }
    public partial class Parameter
    {
        public bool Use { get; set; }

        public bool Edit { get; set; }

        public string Name { get; set; }

        public string SI { get; set; }

        public string KeyPassportResult { get; set; }
    }

    #endregion

    #region Doc.Settings.Footer

    public class FooterPassport : Footer
    {
    }


    #endregion

    #region Doc.Settings.Dictionarys
    public class DictionarysPassport : Dictionarys
    {
    }

    #endregion

    #endregion

    #region Doc.DataIVK

    public class DataIVKPassport : DataIVK
    {
        public TablePassport TablePassport { get; set; }
    }
    public partial class TablePassport
    {
        public long id { get; set; }
        public string StrBegin { get; set; }
        public long Begin { get; set; }
        public string strEnd { get; set; }
        public long End { get; set; }
        public long PeriodType { get; set; }
        public long Period { get; set; }
        public long BIK_ID { get; set; }
        public long IsFilled { get; set; }
        public TableActAndPassport TableActAndPassport { get; set; }
        public AdditionalData AdditionalData { get; set; }
        public PassportResult PassportResult { get; set; }
        public long TimeStamp { get; set; }
        public string DataArm { get; set; }
    }
    public partial class TableActAndPassport
    {
        public Passport Passport { get; set; }
    }
    public partial class Passport
    {
        public ParameterProperties TempResult { get; set; }
        public ParameterProperties PressResult { get; set; }
        public ParameterProperties DensResult { get; set; }
        public ParameterProperties Dens15Result { get; set; }
        public ParameterProperties Dens20Result { get; set; }
        public ParameterProperties MassWaterFracResult { get; set; }
        public string PassportId { get; set; }
        public PassportChloride_Salts Chloride_Salts { get; set; }
        public ParameterProperties Impurity { get; set; }
        public ParameterProperties SulfurResult { get; set; }
        public PassportDNP DNP { get; set; }
        public ParameterProperties Yield_fraction_200 { get; set; }
        public ParameterProperties Yield_fraction_300 { get; set; }
        public ParameterProperties Yield_fraction_350 { get; set; }
        public ParameterProperties Mass_fraction_of_paraffin { get; set; }
        public ParameterProperties Mass_fraction_of_hydrogen_sulfide { get; set; }
        public ParameterProperties Mass_fraction_of_methyl_and_ethyl_mercaptan { get; set; }
        public ParameterProperties Mass_fraction_of_organic_chlorides { get; set; }
        public long Oil_Type { get; set; }
    }
    public partial class ParameterProperties
    {
        public string Desc { get; set; }
    }
    public partial class PassportChloride_Salts
    {
        public ParameterProperties Concentration { get; set; }
        public ParameterProperties MassFraction { get; set; }
    }
    public partial class PassportDNP
    {
        public ParameterProperties kPa { get; set; }
        public ParameterProperties mercury_mm { get; set; }
    }
    public partial class AdditionalData
    {
        public string DelivePoint { get; set; }
        public string AccrSertifNumber { get; set; }
        public string Laboratory { get; set; }
        public string SIKN_Number { get; set; }
        public string Laboratory_Post { get; set; }
        public string Laboratory_Factory { get; set; }
        public string Laboratory_IOF { get; set; }
        public string Delive_Post { get; set; }
        public string Delive_Factory { get; set; }
        public string Delive_IOF { get; set; }
        public string Receive_Post { get; set; }
        public string Receive_Factory { get; set; }
        public string Receive_IOF { get; set; }
        public PassportPeriodDt PassportPeriodDt { get; set; }
        public string TankerName { get; set; }
    }
    public partial class PassportPeriodDt
    {
        public long Begin { get; set; }
        public long End { get; set; }
    }
    public partial class PassportResult
    {
        public string TempResult { get; set; }
        public string PressResult { get; set; }
        public string DensResult { get; set; }
        public string Dens15Result { get; set; }
        public string Dens20Result { get; set; }
        public string MassWaterFracResult { get; set; }
        public string PassportId { get; set; }
        public PassportResultChloride_Salts Chloride_Salts { get; set; }
        public string Impurity { get; set; }
        public string SulfurResult { get; set; }
        public PassportResultDNP DNP { get; set; }
        public string Yield_fraction_200 { get; set; }
        public string Yield_fraction_300 { get; set; }
        public string Yield_fraction_350 { get; set; }
        public string Mass_fraction_of_paraffin { get; set; }
        public string Mass_fraction_of_hydrogen_sulfide { get; set; }
        public string Mass_fraction_of_methyl_and_ethyl_mercaptan { get; set; }
        public string Mass_fraction_of_organic_chlorides { get; set; }
        public string Oil_Type { get; set; }
        public string GostIndexResult { get; set; }
        public string ExtendGostIndexResult { get; set; }
    }
    public partial class PassportResultChloride_Salts
    {
        public string Concentration { get; set; }
        public string MassFraction { get; set; }
    }
    public partial class PassportResultDNP
    {
        public string kPa { get; set; }
        public string mercury_mm { get; set; }
    }

    #endregion


}