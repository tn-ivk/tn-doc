namespace TN_Doc.Class;

public enum GUIDDEVICE
{ 
    IVK1,
    IVK2
}
public enum GUIDDOC
{
    Report,
    Passport,
    Act,
    Jornal,
    Poverka3287,
    KMH_PP_Areom,
    KMH_PR_PU,
    KMH_PW
}

public abstract class Root
{
    public Doc Doc { get; set; }
}
public class Doc
{
    public string Version { get; set; }
    public GUIDDOC GUID { get; set; }
    public Settings Settings { get; set; }
    public DataIVK DataIVK { get; set; }
}
public class Settings
{
    public General General { get; set; }
    public Header Header { get; set; }
    public Data Data { get; set; }
    public Footer Footer { get; set; }
    public Dictionarys Dictionarys { get; set; }
}
public class General
{
    public int ObjType { get; set; }
    public int NefType { get; set; }
    public PageSettings PageSettings { get; set; }
    public int ProtocolNumber { get; set; }
}
public class PageSettings
{
    public int PaperWidth { get; set; }
    public int PaperHeight { get; set; }
    public int TopMargin { get; set; }
    public int BottomMargin { get; set; }
    public int LeftMargin { get; set; }
    public int RightMargin { get; set; }
}
public class Header { }
public class Data { }
public class Footer { }
public class Dictionarys { }
public class DataIVK { }