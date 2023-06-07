using System.Collections.Generic;

namespace TN_DocReport
{
    public class PageSettings
    {
        public int PaperWidth { get; set; }
        public int PaperHeight { get; set; }
        public int TopMargin { get; set; }
        public int BottomMargin { get; set; }
        public int LeftMargin { get; set; }
        public int RightMargin { get; set; }
    }

    public class General
    {
        public int ObjType { get; set; }
        public int NefType { get; set; }
        public int NumberDensitometers { get; set; }
        public int NumberViscometers { get; set; }
        public int NumberWatermetrs { get; set; }
        public PageSettings PageSettings { get; set; }
        public string PathToTemplateFile { get; set; }
    }

    public class Header
    {
        public string Prefix_SIKN_Name { get; set; }
        public string NameIVK { get; set; }
    }

    public class Parameters
    {
        public bool Visible { get; set; }
        public string Name { get; set; }
        public string SI { get; set; }
        public string Key { get; set; }
    }

    public class TableBIK
    {
        public bool Visible { get; set; }
        public List<Parameters> Parameters { get; set; }
    }

    public class TableLine
    {
        public bool Visible { get; set; }
        public int ShowNumberColumns { get; set; }
        public double ColumnsWidth { get; set; }
        public List<Parameters> Parameters { get; set; }
        public ColumnSIKN ColumnSIKN { get; set; }

    }

    public class ColumnSIKN
    {
        public bool Visible { get; set; }
        public string Name { get; set; }
        public double Width { get; set; }
    }

    public class Data
    {
        public TableBIK TableBIK { get; set; }
        public TableLine TableLine { get; set; }
        public ColumnSIKN ColumnSIKN { get; set; }
    }

    public class ShowSigner
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Visible { get; set; }
    }

    public class Signers
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<ShowSigner> ShowSigner { get; set; }
    }

    public class Footer
    {
        public List<Signers> Signers { get; set; }
    }

    public class Dictionarys
    {
    }

    public class Settings
    {
        public General General { get; set; }
        public Header Header { get; set; }
        public Data Data { get; set; }
        public Footer Footer { get; set; }
        public Dictionarys Dictionarys { get; set; }
    }

    public class Doc
    {
        public string Version { get; set; }
        public Settings Settings { get; set; }
        public DataIVK DataIVK { get; set; }
    }

    public class Additional
    {
        public string SIKN_Name { get; set; }
        public string Factory { get; set; }
        public int NumOfUsedDens { get; set; }
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

    public class Obj
    {
        public List<Additional> Additional { get; set; }
        public List<BIK> BIK { get; set; }
        public List<Line> Line { get; set; }
        public List<SIKN> SIKN { get; set; }
    }

    public class Report
    {
        public Obj Obj { get; set; }
    }

    public class TableReport
    {
        public int id { get; set; }
        public string strBegin { get; set; }
        public int Begin { get; set; }
        public string strEnd { get; set; }
        public int End { get; set; }
        public int ReportType { get; set; }
        public int ReportPeriod { get; set; }
        public int BIK_ID { get; set; }
        public Report Report { get; set; }
    }

    public class DataIVK
    {
        public TableReport TableReport { get; set; }
    }

    public class Root
    {
        public Doc Doc { get; set; }
    }
}
