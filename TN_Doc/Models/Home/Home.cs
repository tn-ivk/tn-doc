using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace TN_Doc.Models.Home
{
    //public class CfgApp
    //{ 
    //    public List<Device> Devices { get; set; }
    //}
    //public class Device
    //{
    //    public bool Use { get; set; }
    //    public IdDevice GuidDevice { get; set; }
    //    public string Name { get; set; }
    //    public string Description { get; set; }
    //    public List<Document> Docs { get; set; }
    //    public List<DBConnectionString> DBConnectionStrings { get; set; }
    //}
    //public class DBConnectionString
    //{
    //    public bool Use { get; set; }
    //    public IdDevice GuidDevice { get; set; }
    //    public string Server { get; set; }
    //    public string Userid { get; set; }
    //    public string Password { get; set; }
    //    public string Database { get; set; }
    //    public int ConnectionTimeout { get; set; }
    //    public string GetConnectionString()
    //    {
    //        return $"Server={Server};Userid={Userid};Password={Password};Database={Database}; Connection Timeout={ConnectionTimeout};";
    //    }
    //}
    //public class Document
    //{
    //    public bool Use { get; set; }
    //    public IdDoc IdDoc { get; set; }
    //    public string Name { get; set; }
    //    public string Description { get; set; }
    //    public string PathToDocDll { get; set; }
    //    public string PathToDocConfigFile { get; set; }
    //    public string PathToDocEditConfigFile { get; set; }
    //    public string PathToDocTemplateFile { get; set; }
    //}
    
    
    
    //public class MySqlConnectionInterceptor : DbConnectionInterceptor
    //{
    //    private List<DBConnectionString> Ivk;
    //    private DBConnectionString CurrentIvk;
    //    private IdDevice GuidDevice;

    //    public MySqlConnectionInterceptor(IdDevice guidDevice, CfgApp ivk)
    //    {
    //        GuidDevice = guidDevice;

    //        var device = ivk.Devices.Single(x => x.GuidDevice == GuidDevice && x.Use);

    //        Ivk = device.DBConnectionStrings;
    //    }
    //    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    //    {
    //        if (Ivk != null)
    //        {
    //            //connection.ConnectionString = Ivk.First().GetConnectionString();
    //        }
    //        base.ConnectionOpened(connection, eventData);
    //    }
    //    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    //    {
    //        if (Ivk != null)
    //        {
    //            //connection.ConnectionString = Ivk.First().GetConnectionString();
    //        }
    //        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    //    }
    //    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    //    {
    //        if (Ivk != null)
    //        {
    //            if (CurrentIvk == null) CurrentIvk = Ivk.First();
    //            connection.ConnectionString = CurrentIvk.GetConnectionString();              
    //        }
    //        return await base.ConnectionOpeningAsync(connection, eventData, result, cancellationToken);
    //    }
    //    public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    //    {
    //        if (Ivk != null)
    //        {
    //            var listIVK = Ivk.Where(x => x.GuidDevice == GuidDevice).ToList();
    //            foreach (var item in listIVK)
    //            {
    //                connection.ConnectionString = item.GetConnectionString();
                    
    //                try
    //                {
    //                    connection.Open();
    //                }
    //                catch { }

    //                if (connection.State == System.Data.ConnectionState.Open)
    //                {
    //                    connection.Close();
    //                    break;
    //                }
    //            }
    //        }
    //        return base.ConnectionOpening(connection, eventData, result);
    //    }
    //    public override void ConnectionFailed(DbConnection connection, ConnectionErrorEventData eventData)
    //    {
    //        //CurrentIvk = Ivk.Where(x => x.NameDevice == CurrentIvk.NameDevice && x.Server != CurrentIvk.Server).Single();
    //        base.ConnectionFailed(connection, eventData);
    //    }        
    //    public override async Task ConnectionFailedAsync(DbConnection connection, ConnectionErrorEventData eventData, CancellationToken cancellationToken = default)
    //    {
    //        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    //    }
    //}
    //public class DBIVK : DbContext
    //{
    //    public static IdDevice GuidDevice = IdDevice.IVK1;

    //    CfgApp cfgApp = new();

    //    public void AddData<T>(IdDoc idDoc)
    //    {
            
    //    }
    //    public void GetData<T>(IdDoc idDoc)
    //    {
           
    //    }

    //    //public DbSet<TN.TableReportList> TRL { get; set; }
    //    //public DbSet<TN.TableReportData> TRD { get; set; }   

    //    //public DbSet<TableReportList> TableReportList { get; set; }
    //    //public DbSet<TableReportData> TableReportData { get; set; }
        
    //    //public DbSet<FillingTableActAndPassport> FillingTableActAndPassport { get; set; }
    //    //public DbSet<TableActAndPassportList> TableActAndPassportList { get; set; }
    //    //public DbSet<TableActAndPassportData> TableActAndPassportData { get; set; }

    //    //public DbSet<TableMeasurementJornalList> TableMeasurementJornalList { get; set; }
    //    //public DbSet<TableMeasurementJornalData> TableMeasurementJornalData { get; set; }

    //    //public DbSet<TableResultActAndPassportList> TableResultActAndPassportList { get; set; }
    //    //public DbSet<TableResultActAndPassportData> TableResultActAndPassportData { get; set; }

    //    //public DbSet<TablePoverka3287List> TablePoverka3287List { get; set; }
    //    //public DbSet<TablePoverka3287Data> TablePoverka3287Data { get; set; }

    //    //public DbSet<TableKMH_PP_AreomList> TableKMH_PP_AreomList { get; set; }
    //    //public DbSet<TableKMH_PP_AreomData> TableKMH_PP_AreomData { get; set; }

    //    //public DbSet<TableKMH_PR_PUList> TableKMH_PR_PUList { get; set; }
    //    //public DbSet<TableKMH_PR_PUData> TableKMH_PR_PUData { get; set; }

    //    //public DbSet<TableKMH_PWList> TableKMH_PWList { get; set; }
    //    //public DbSet<TableKMH_PWData> TableKMH_PWData { get; set; }

    //    public DBIVK(DbContextOptions<DBIVK> options) : base(options)
    //    {
    //        DocGeneral.LoadCfg<CfgApp>(Path.Combine(Directory.GetCurrentDirectory(), $"Cfg", $"CfgApp.json"), ref cfgApp);
    //    }
    //    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //    {
    //        optionsBuilder.AddInterceptors(new MySqlConnectionInterceptor(GuidDevice, cfgApp));
    //    }
    //}


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