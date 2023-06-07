using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Xml.Linq;
using TN.DocData;

namespace TN.Doc
{
    public class KMH_MPR_TPR : DocGeneral
    {
        public KMH_MPR_TPR(DbContextOptions<DocGeneral> options, string path, Device device) : base(options, path, device)
        {
            IdDoc = IdDoc.KMH_MPR_TPR;
            PathToDocConfigFile = GetPathConfigFile();
            PathToDocEditConfigFile = GetPathEditConfigFile();
            PathToDocTemplateFile = GetPathTemplateFile();
        }

        private DbSet<TableKMH_MPR_TPRList> ListDoc { get; set; }
        private DbSet<TableKMH_MPR_TPRData> DataDoc { get; set; }

        private Doc Doc;
        private JObject jsonDoc;

        public override List<RequestListDocs> GetList(long UTBegin, long UTEnd)
        {
            List<RequestListDocs> docs = new();

            var list = (from item in ListDoc
                        where item.DateTimeLong > UTBegin && item.DateTimeLong < UTEnd && item.id > 1
                        select item).ToList<TableKMH_MPR_TPRList>();

            if (list.Count != 0)
            {
                foreach (var item in list)
                {
                    docs.Add(new()
                    {
                        Id = item.id,
                        DT = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(item.DateTimeLong).ToString("dd.MM.yy HH:mm"),
                        Description = $"{ArrByteToString(item.IL_Name)}"
                    });
                }
            }

            return docs;
        }

        public override object GetViewDoc(int id)
        {
            var list = (from item in ListDoc
                        where item.id == id
                        select item).First();

            list.Data = (from item in DataDoc
                         where item.id == id
                         select item).First();

            Doc doc = new();

            LoadCfg(PathToDocConfigFile, ref doc);

            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_TPR.id = list.id;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_TPR.IL = list.IL;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_TPR.MI = list.MI;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_TPR.Protokol = JsonDeserializeObject<Protokol>(ArrByteToString(list.Data.Protokol));
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_TPR.AdditionalInfo = JsonDeserializeObject<AdditionalInfo>(ArrByteToString(list.Data.AdditionalInfo));
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_TPR.DateTimeString = list.DateTimeString;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_TPR.DateTimeLong = list.DateTimeLong;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_TPR.BIK_ID = list.BIK_ID;
            ((DataIVKDoc)doc.Doc.DataIVK).TableKMH_MPR_TPR.IL_Name = ArrByteToString(list.IL_Name);

            jsonDoc = JObject.Parse(JsonSerializeObject<Doc>(doc).ToString());
            Doc = doc;

            return JsonSerializeObject<Doc>(doc);
        }

        public override string GetEditDoc(int id)
        {
            GetViewDoc(id);

            return "";
        }

        public override bool SaveDoc(string jsonData)
        {
            return true;
        }
    }

    [Table("TableKMH_MPR_TPR")]
    public class TableKMH_MPR_TPRList
    {
        [Key, ForeignKey("Data")]
        public int id { get; set; }
        public int IL { get; set; }
        public int MI { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public int BIK_ID { get; set; }
        public byte[] IL_Name { get; set; }
        public TableKMH_MPR_TPRData Data { get; set; }
    }

    [Table("TableKMH_MPR_TPR")]
    public class TableKMH_MPR_TPRData
    {
        [Key]
        public int id { get; set; }
        public byte[] Protokol { get; set; }
        public byte[] AdditionalInfo { get; set; }
    }


    public class Doc : Root
    {
        public Doc()
        {
            Doc = new();

            Doc.Settings = new Settings();

            Doc.Settings.Header = new HeaderDoc();
            Doc.Settings.Data = new DataDoc();
            Doc.Settings.Footer = new FooterDoc();
            Doc.Settings.Dictionarys = new DictionarysDoc();
            Doc.DataIVK = new DataIVKDoc();
            ((DataIVKDoc)Doc.DataIVK).TableKMH_MPR_TPR = new TableKMH_MPR_TPR();
        }
    }

    #region Doc.Settings

    #region Doc.Settings.Header

    public class HeaderDoc : Header
    {
        public string FieldSIKN { get; set; }
    }

    #endregion

    #region Doc.Settings.Data

    public class DataDoc : Data
    {

    }

    #endregion

    #region Doc.Settings.Footer

    public class FooterDoc : Footer
    {

    }

    #endregion

    #region Doc.Settings.Dictionarys

    public class DictionarysDoc : Dictionarys
    {

    }

    #endregion

    #endregion

    #region Doc.DataIVK

    public class DataIVKDoc : DataIVK
    {
        public TableKMH_MPR_TPR TableKMH_MPR_TPR { get; set; }
    }
    public class TableKMH_MPR_TPR
    {
        public int id { get; set; }
        public int IL { get; set; }
        public int MI { get; set; }
        public Protokol Protokol { get; set; }
        public AdditionalInfo AdditionalInfo { get; set; }
        public string DateTimeString { get; set; }
        public long DateTimeLong { get; set; }
        public int BIK_ID { get; set; }
        public string IL_Name { get; set; }
    }
    public class Protokol
    {
        public List<string> DetName { get; set; }
        public string OilType { get; set; }
        public string Delta_max { get; set; }
        public string Delta_max_ERROR { get; set; }
        public Visc Visc { get; set; }
        public Table1 Table1 { get; set; }
        public List<Table2_part1_Points> Table2_part1_Points { get; set; }
        public List<Table2_part1> Table2_part1 { get; set; }
        public List<Table2_part2_Points> Table2_part2_Points { get; set; }
        public List<Table2_part2> Table2_part2 { get; set; }
    }
    public class Visc
    {
        public string KinimVisc_min { get; set; }
        public string KinimVisc_middle { get; set; }
        public string KinimVisc_max { get; set; }
    }
    public class Table1
    {
        public List<string> DetName { get; set; }
        public List<string> V0 { get; set; }
        public string Delta_KP { get; set; }
        public string m_D { get; set; }
        public string m_s { get; set; }
        public string m_E { get; set; }
        public string Delta_t_KP { get; set; }
        public string Delta_PP { get; set; }
        public string Delta_t_PP { get; set; }
        public string Delta_k_UOI { get; set; }
        public string KF_konf { get; set; }
        public string ZS { get; set; }
        public string Alpha_cil_t { get; set; }
        public string Alpha_cil_t_kv { get; set; }
        public string Alpha_t_cm { get; set; }
    }
    public class Table2_part1
    {
        public int Activated { get; set; }
        public string Ser { get; set; }
        public string Row { get; set; }
        public string Q_ji { get; set; }
        public string N_TPR_ji { get; set; }
        public string t_TPR_ji { get; set; }
        public string P_TPR_ji { get; set; }
        public string t_KP_ji { get; set; }
        public string P_KP_ji { get; set; }
        public string t_cm_ji { get; set; }
        public string V_KP_PR_ji { get; set; }
        public string K_TPR_ji { get; set; }
        public string P_ji { get; set; }
    }
    public class Table2_part1_Points
    {
        public string K1_TPR_j { get; set; }
        public string K2_TPR_j { get; set; }
        public string Delta_k_j { get; set; }
        public string Repeatability_K_j { get; set; }
        public string Repeatability_K_j_ERROR { get; set; }
    }
    public class Table2_part2
    {
        public int Activated { get; set; }
        public string Ser { get; set; }
        public string Row { get; set; }
        public string Q_ji { get; set; }
        public string V_TPR_ji { get; set; }
        public string t_TPR_ji { get; set; }
        public string P_TPR_ji { get; set; }
        public string N_mas_ji { get; set; }
        public string Dens_ji { get; set; }
        public string DensTemp_ji { get; set; }
        public string DensPress_ji { get; set; }
        public string Dens_PP_PR_ji { get; set; }
        public string M_RA_ji { get; set; }
        public string M_mas_ji { get; set; }
        public string MF_ji { get; set; }
        public string KinimVisc_ji { get; set; }
        public string M_from_GH_ji { get; set; }
        public string Delta_KMH_ji { get; set; }
    }
    public class Table2_part2_Points
    {
        public string K_TPR_j { get; set; }
        public string N_TPR_zad_j { get; set; }
    }
    public class AdditionalInfo
    {
        public string ProtokolNum { get; set; }
        public string MPR_Type { get; set; }
        public string MPR_FactoryNum { get; set; }
        public string Place_PSP { get; set; }
        public string Place_Factory { get; set; }
        public string SIKN { get; set; }
        public string PU_Type { get; set; }
        public string PU_Factory { get; set; }
        public string PU_CheckDate { get; set; }
        public string TPR_Type { get; set; }
        public string TPR_FactoryNum { get; set; }
        public string ARM_KMH_Result { get; set; }
        public string ServiceStaffData { get; set; }
        public string DeliveryStaffData { get; set; }
        public string RecievingStaffData { get; set; }
    }
    #endregion

}
