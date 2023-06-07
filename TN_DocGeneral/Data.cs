using System;
using System.Collections.Generic;

namespace TN.DocData
{
    public class Root
    {
        public Doc Doc { get; set; }
    }
    public class Doc
    {
        public string Version { get; set; }
        //public IdDoc IdDoc { get; set; }
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
    public class Header
    {

    }
    public class Data
    {

    }
    public class Footer
    {

    }
    public class Dictionarys
    {
        public List<Users> Users { get; set; }
        public List<UsersGroup> UsersGroup { get; set; }
        public List<License> Licenses { get; set; }
    }
    public class DataIVK
    {

    }

    public class ResponseListDocs
    {
        public int Id { get; set; }
        public string DT { get; set; }
        public string Description { get; set; }
    }
    public class RequestListDocs
    {
        public int Id { get; set; }
        public string DT { get; set; }
        public string Description { get; set; }
    }


    //GetEditDoc
    public class CfgEdit
    {
        public List<AdditionalInfo> AdditionalInfo = new();
    }
    public class AdditionalInfo
    {
        public int Id { get; set; }
        public bool Use { get; set; }
        public bool Edit { get; set; }
        public string Key { set; get; }
        public string Type { set; get; }
        public string Name { set; get; }
    }

    //SaveDoc
    public class CorrectionData
    {
        public int DocID { get; set; }
        public List<EditData> Values { get; set; }
    }
    public class EditData
    {
        public string Key { get; set; }
        public string Tag { get; set; }
        public string Value { get; set; }
    }

    public enum IdDoc
    {
        Report,
        Passport,
        Act,
        Jornal,
        Poverka3287,
        KMH_PP_Areom,
        KMH_PR_PU,
        KMH_PW,
        KMH_PV,
        KMH_PP,
        KMH_MI2816,
        Poverka2816,
        KMH_PR_PR,
        KMH_MPR_MPR,
        Poverka3380,
        Poverka3265_PR_PU,
        Poverka3265_UPR_PU,
        Poverka3265_UPR_PR,
        KMH3265_PR_PU,
        KMH3265_UPR_PR,
        Poverka3312_PR_PU,
        Poverka3312_UPR_PR,
        KMH3312_PR_PU,
        KMH3312_UPR_PR,
        Poverka1974,
        Poverka3151,
        KMH_MPR_PU,
        Poverka3288,
        KMH3288_MPR_TPR,
        Poverka3272,
        KMH_MPR_TPR,
        Poverka3189,
        ReportIncomplete
    }

    public class Users
    {
        public bool Use { get; set; }
        public int Id { get; set; }
        public int IdGroup { get; set; }
        public string F { get; set; }
        public string I { get; set; }
        public string O { get; set; }
        public string Factory { get; set; }
        public string Post { get; set; }
        public string FIO => GetFIO();
        public string IOF => GetIOF();

        private string GetFIO()
        {
            return $"{F.Trim()} {I.Trim().Substring(0, 1)}.{O.Trim().Substring(0, 1)}.";
        }
        private string GetIOF()
        {
            return $"{I.Trim().Substring(0, 1)}.{O.Trim().Substring(0, 1)}. {F.Trim()}";
        }
    }
    public class UsersGroup
    {
        public bool Use { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class License
    {
        public bool Use { get; set; }
        public int IdUser { get; set; }
        public string LicensesNumber { get; set; }
        public string LicensesDate { get; set; }
    }

    public class CfgApp
    {
        public List<Device> Devices { get; set; } = new List<Device>();
    }
    public class Device
    {
        public bool Use { get; set; }
        public int IdDevice { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Document> Docs { get; set; } = new List<Document>();
        public List<DBConnectionString> DBConnectionStrings { get; set; } = new List<DBConnectionString>();
    }
    public class DBConnectionString
    {
        public bool Use { get; set; }
        public int GuidDevice { get; set; }
        public string Server { get; set; }
        public string Userid { get; set; }
        public string Password { get; set; }
        public string Database { get; set; }
        public int ConnectionTimeout { get; set; }
        public string GetConnectionString()
        {
            return $"Server={Server};Userid={Userid};Password={Password};Database={Database}; Connection Timeout={ConnectionTimeout};";
        }
    }
    public class Document
    {
        public bool Use { get; set; }
        public IdDoc IdDoc { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string PathToDocDll { get; set; }
        public string PathToDocConfigFile { get; set; }
        public string PathToDocEditConfigFile { get; set; }
        public int LastUsedTemplateId { get; set; }
        public List<TemplateDoc> TemplateDocs { get; set; }
    }
    public class TemplateDoc
    {
        public bool Use { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string PathToDocTemplateFile { get; set; }
        public string PathToDocEditConfigFile { get; set; }
    }

    public class PrintSettings
    {
        public string LastSelectedPrinter { get; set; }
    }
}
