using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TN_Doc.Models.Home
{
    public class Home
    {

    }

    public class ModelReport
    {
        public ModelReport()
        {
            tr = new List<tReport>();
            tr.Add(new tReport() { id = 1, Name = "Отчет за два часа", ShortName = "Отчет за 2ч" });
            tr.Add(new tReport() { id = 2, Name = "Отчет за смену", ShortName = "Отчет за см" });
            tr.Add(new tReport() { id = 3, Name = "Отчет за сутки", ShortName = "Отчет за сут" });
            tr.Add(new tReport() { id = 4, Name = "Отчет за месяц", ShortName = "Отчет за м" });
            tr.Add(new tReport() { id = 5, Name = "Отчет за время ведения ТКО", ShortName = "Отчет за ТКО" });
        }

        public List<tReport> tr;
        public List<TableReportList> TableReportList { get; set; }
        public TableReportData TableReportData { get; set; }
    }


    public struct tReport
    {
        public int id;
        public string Name;
        public string ShortName;
    }

    [Table("TableReport")]
    public class TableReportList
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public string strBegin { get; set; }
        public int Begin { get; set; }
        public string strEnd { get; set; }
        public int End { get; set; }
        public int ReportType { get; set; }
        public int ReportPeriod { get; set; }
        public int BIK_ID { get; set; }
        
        [NotMapped]
        public string strReportType { get; set; }
        
        [NotMapped]
        public string strDT { get; set; }

        public TableReportData Data { get; set; }
    }

    [Table("TableReport")]
    public class TableReportData
    {
        //[ForeignKey("List")]
        [Key]
        public int id { get; set; }
        public byte[] Report { get; set; }
        public byte[] ReportRaw { get; set; }
        public string DataARM { get; set; }
    }



    [Table("TableActAndPassportData")]
    public class FillingTableActAndPassport
    {
        public int id { get; set; }
        public int PassportID { get; set; }
        public byte[] Data { get; set; }
        public byte[] AdditionalData { get; set; }
    }


    [Table("TableActAndPassport")]
    public class TableActAndPassportList
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public string strBegin { get; set; }
        public int Begin { get; set; }
        public string strEnd { get; set; }
        public int End { get; set; }
        public int PeriodType { get; set; }
        public int Period { get; set; }
        public int BIK_ID { get; set; }
        public int IsFilled { get; set; }
        public long TimeStamp { get; set; }

        [NotMapped]
        public string strPeriodType { get; set; }

        [NotMapped]
        public string strDT { get; set; }

        public TableActAndPassportData Data { get; set; }
    }

    [Table("TableActAndPassport")]
    public class TableActAndPassportData
    {
        [Key]
        public int id { get; set; }
        
        public byte[] ActAndPassport { get; set; }
        public byte[] AdditionalData { get; set; }
        public byte[] PassportResult { get; set; }
        public string DataARM { get; set; }
    }



    [Table("TableResultActAndPassport")]
    public class TableResultActAndPassportList
    {
        [Key, ForeignKey("Data")]
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
        public long TimeStamp { get; set; }     
        public TableResultActAndPassportData Data { get; set; }
    }

    [Table("TableResultActAndPassport")]
    public class TableResultActAndPassportData
    {
        [Key]
        public int id { get; set; }
        public byte[] ResultActAndPassport { get; set; }
        public byte[] AdditionalInfo { get; set; }
    }



    [Table("TableMeasurementJornal")]
    public class TableMeasurementJornalList
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int BIK_ID { get; set; }
        public TableMeasurementJornalData Data { get; set; }
    }

    [Table("TableMeasurementJornal")]
    public class TableMeasurementJornalData
    {
        [Key]
        public int id { get; set; }

        public byte[] Additional { get; set; }
        public byte[] Data { get; set; }
        public string DataARM { get; set; }
    }



    [Table("TablePoverka3287")]
    public class TablePoverka3287List
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int IL { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public int BIK_ID { get; set; }
        public TablePoverka3287Data Data { get; set; }
        public byte[] IL_Name { get; set; }

    }

    [Table("TablePoverka3287")]
    public class TablePoverka3287Data
    {
        [Key]
        public int id { get; set; }
        public byte[] Protokol { get; set; }
        public byte[] AdditionalInfo { get; set; }
    }



    [Table("TableKMH_PP_Areom")]
    public class TableKMH_PP_AreomList
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int BIK { get; set; }
        public int KMH_TYPE { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public TableKMH_PP_AreomData Data { get; set; }

    }

    [Table("TableKMH_PP_Areom")]
    public class TableKMH_PP_AreomData
    {
        [Key]
        public int id { get; set; }
        public byte[] Protokol { get; set; }
        public byte[] AdditionalInfo { get; set; }
    }



    [Table("TableKMH_PR_PU")]
    public class TableKMH_PR_PUList
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int MI { get; set; }
        public int IL { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public int BIK_ID { get; set; }
        public byte[] IL_Name { get; set; }
        public TableKMH_PR_PUData Data { get; set; }

    }

    [Table("TableKMH_PR_PU")]
    public class TableKMH_PR_PUData
    {
        [Key]
        public int id { get; set; }
        public byte[] Protokol { get; set; }
        public byte[] AdditionalInfo { get; set; }
    }



    [Table("TableKMH_PW")]
    public class TableKMH_PWList
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int BIK { get; set; }
        public int PW { get; set; }
        public int KMH_TYPE { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public TableKMH_PWData Data { get; set; }

    }

    [Table("TableKMH_PW")]
    public class TableKMH_PWData
    {
        [Key]
        public int id { get; set; }
        public byte[] Protokol { get; set; }
        public byte[] AdditionalInfo { get; set; }
    }

}