using System.Collections.Generic;

namespace TN_Doc.Class.Edit
{
    //public class CfgEditPassport
    //{
    //    public Correction Correction = new ();
    //    public List<Parameter> Parameters = new ();
    //    public List<Metod> Metods = new ();
    //    public List<AdditionalInfo> AdditionalInfo = new ();
    //}

    //public enum GUIDMetod
    //{

    //}

    //public class Parameter
    //{
    //    public int Id { get; set; }
    //    public string Key { get; set; }
    //    public string Name { get; set; }
    //    public bool Use { get; set; }
    //    public bool Edit { get; set; }
    //}
    //public class Metod
    //{
    //    public bool Use { get; set; }
    //    public int IdParameter { get; set; }
    //    public string Name { get; set; }
    //    public bool LimitValueActivate { get; set; }
    //    public float LimitValue { get; set; }
    //    public string LimitValueString { get; set; }
    //}
    //public class AdditionalInfo
    //{ 
    //    public bool Use { get; set; }
    //    public string Key { set; get; }
    //    public string Type { set; get; }
    //    public string Name { set; get; }
    //    public object Value { set; get; }
    //}

    //public class Correction
    //{
    //    public TempCorrection TempCorrection { get; set; } = new TempCorrection();
    //    public PressCorrection PressCorrection { get; set; } = new PressCorrection();
    //    public DensCorrection DensCorrection { get; set; } = new DensCorrection();
    //    public Dens15Correction Dens15Correction { get; set; } = new Dens15Correction();
    //    public Dens20Correction Dens20Correction { get; set; } = new Dens20Correction();
    //    public MassWaterFracCorrection MassWaterFracCorrection { get; set; } = new MassWaterFracCorrection();
    //    public string PassportID { get; set; } = "";
    //    public GOSTIndexCorrection GOSTIndexCorrection { get; set; } = new GOSTIndexCorrection();
    //    public Chloride_Salts Chloride_Salts { get; set; } = new Chloride_Salts();
    //    public Impurity Impurity { get; set; } = new Impurity();
    //    public SulfurCorrection SulfurCorrection { get; set; } = new SulfurCorrection();
    //    public DNP DNP { get; set; } = new DNP();
    //    public Yield_fraction_200 Yield_fraction_200 { get; set; } = new Yield_fraction_200();
    //    public Yield_fraction_300 Yield_fraction_300 { get; set; } = new Yield_fraction_300();
    //    public Yield_fraction_350 Yield_fraction_350 { get; set; } = new Yield_fraction_350();
    //    public Mass_fraction_of_paraffin Mass_fraction_of_paraffin { get; set; } = new Mass_fraction_of_paraffin();
    //    public Mass_fraction_of_hydrogen_sulfide Mass_fraction_of_hydrogen_sulfide { get; set; } = new Mass_fraction_of_hydrogen_sulfide();
    //    public Mass_fraction_of_methyl_and_ethyl_mercaptan Mass_fraction_of_methyl_and_ethyl_mercaptan { get; set; } = new Mass_fraction_of_methyl_and_ethyl_mercaptan();
    //    public Mass_fraction_of_organic_chlorides Mass_fraction_of_organic_chlorides { get; set; } = new Mass_fraction_of_organic_chlorides();
    //    public Data DensCorrectionValue { get; set; } = new Data();
    //    public ExtendGOSTIndexCorrection ExtendGOSTIndexCorrection { get; set; } = new ExtendGOSTIndexCorrection();
    //}
    //public class TempCorrection
    //{
    //    public Data Data { get; set; } = new Data();
    //    public string Desc { get; set; } = "";
    //}
    //public class PressCorrection
    //{
    //    public Data Data { get; set; } = new Data();
    //    public string Desc { get; set; } = "";
    //}
    //public class DensCorrection
    //{
    //    public Data Data { get; set; } = new Data();
    //    public string Desc { get; set; } = "";
    //}
    //public class Dens15Correction
    //{
    //    public Data Data { get; set; } = new Data();
    //    public string Desc { get; set; } = "";
    //}
    //public class Dens20Correction
    //{
    //    public Data Data { get; set; } = new Data();
    //    public string Desc { get; set; } = "";
    //}
    //public class MassWaterFracCorrection
    //{
    //    public Data Data { get; set; } = new Data();
    //    public string Desc { get; set; } = "";
    //}
    //public class GOSTIndexCorrection
    //{
    //    public int IsFilled { get; set; } = 0;
    //    public int IsExport { get; set; } = 0;
    //    public Data OilClass { get; set; } = new Data();
    //    public Data OilType { get; set; } = new Data();
    //    public Data OilGroup { get; set; } = new Data();
    //    public Data OilSort { get; set; } = new Data();
    //}
    //public class Chloride_Salts
    //{
    //    public Concentration Concentration { get; set; } = new Concentration();
    //    public MassFraction MassFraction { get; set; } = new MassFraction();
    //    public DensityOil DensityOil { get; set; } = new DensityOil();
    //}
    //public class Concentration
    //{
    //    public Data Data { get; set; } = new Data();
    //    public string Desc { get; set; } = "";
    //}
    //public class MassFraction
    //{
    //    public Data Data { get; set; } = new Data();
    //    public string Desc { get; set; } = "";
    //}
    //public class DensityOil
    //{
    //    public Data Data { get; set; } = new Data();
    //}
    //public class Impurity
    //{
    //    public Data Data { get; set; } = new Data();
    //    public string Desc { get; set; } = "";
    //}
    //public class SulfurCorrection
    //{
    //    public Data Data { get; set; } = new Data();
    //    public string Desc { get; set; } = "";
    //}
    //public class DNP
    //{
    //    public kPa kPa { get; set; } = new kPa();
    //    public mercury_mm mercury_mm { get; set; } = new mercury_mm();
    //}
    //public class kPa
    //{
    //    public Data Data { get; set; } = new Data();
    //    public string Desc { get; set; } = "";
    //}
    //public class mercury_mm
    //{
    //    public Data Data { get; set; } = new Data();
    //    public string Desc { get; set; } = "";
    //}
    //public class Yield_fraction_200
    //{
    //    public Data Data { get; set; } = new Data();
    //    public string Desc { get; set; } = "";
    //}
    //public class Yield_fraction_300
    //{
    //    public Data Data { get; set; } = new Data();
    //    public string Desc { get; set; } = "";
    //}
    //public class Yield_fraction_350
    //{
    //    public Data Data { get; set; } = new Data();
    //    public string Desc { get; set; } = "";
    //}
    //public class Mass_fraction_of_paraffin
    //{
    //    public Data Data { get; set; } = new Data();
    //    public string Desc { get; set; } = "";
    //}
    //public class Mass_fraction_of_hydrogen_sulfide
    //{
    //    public Data Data { get; set; } = new Data();
    //    public string Desc { get; set; } = "";
    //    public int Prop { get; set; } = 0;
    //}
    //public class Mass_fraction_of_methyl_and_ethyl_mercaptan
    //{
    //    public Data Data { get; set; } = new Data();
    //    public string Desc { get; set; } = "";
    //    public int Prop { get; set; } = 0;
    //}
    //public class Mass_fraction_of_organic_chlorides
    //{
    //    public Data Data { get; set; } = new Data();
    //    public string Desc { get; set; } = "";
    //    public int Prop { get; set; } = 0;
    //}
    //public class Data
    //{
    //    public float Value { get; set; } = 0;
    //    public int Legal { get; set; } = 0;
    //}
    //public class ExtendGOSTIndexCorrection
    //{
    //    public int IsFilled { get; set; } = 0;
    //    public int IsExport { get; set; } = 0;
    //    public Data OilClass { get; set; } = new Data();
    //    public Data OilType { get; set; } = new Data();
    //    public Data OilGroup { get; set; } = new Data();
    //    public Data OilSort { get; set; } = new Data();
    //}
    //public class AdditionalData
    //{
    //    public string DelivePoint { get; set; } = "-";
    //    public string AccrSertifNumber { get; set; } = "-";
    //    public string Laboratory { get; set; } = "-";
    //    public string SIKN_Number { get; set; } = "-";
    //    public string Laboratory_Post { get; set; } = "-";
    //    public string Laboratory_Factory { get; set; } = "-";
    //    public string Laboratory_IOF { get; set; } = "-";
    //    public string Delive_Post { get; set; } = "-";
    //    public string Delive_Factory { get; set; } = "-";
    //    public string Delive_IOF { get; set; } = "-";
    //    public string Receive_Post { get; set; } = "-";
    //    public string Receive_Factory { get; set; } = "-";
    //    public string Receive_IOF { get; set; } = "-";
    //    public PassportPeriodDT PassportPeriodDT { get; set; } = new PassportPeriodDT();
    //    public string TankerName { get; set; } = "-";
    //}
    //public class PassportPeriodDT
    //{
    //    public int Begin { get; set; } = 0;
    //    public int End { get; set; } = 0;
    //}
}
