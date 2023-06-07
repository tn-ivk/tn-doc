using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;
using TN_CfgFile;
using TN.DocData;

namespace TN.Doc
{
    public class DocGeneral : DbContext
    {
        public DocGeneral()
        {
        }
        public DocGeneral(DbContextOptions<DocGeneral> options, string path) : base(options)
        {
        }
        public DocGeneral(DbContextOptions<DocGeneral> options, string path, Device device) : base(options)
        {
            CurrentCfgDevice = device;
            PathToRootDirectory = path;

            Root dic = new Root();
            LoadCfg(path + "/Cfg/Cfg.json", ref dic);
            DictionarysDoc = dic.Doc.Settings.Dictionarys;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var list = CurrentCfgDevice.DBConnectionStrings.Where(x => x.Use).ToList();
            optionsBuilder.UseMySql(list.First().GetConnectionString(), MySqlServerVersion.LatestSupportedServerVersion,
                Builder => Builder.EnableRetryOnFailure());

            optionsBuilder.AddInterceptors(new MySqlConnectionInterceptor(list));
        }

        /// <summary>
        /// Общие справичники для документа
        /// </summary>
        public static Dictionarys DictionarysDoc
        {
            get;
            set;
        }

        /// <summary>
        /// Конфигурация выбранного устройства.
        /// </summary>
        public static Device CurrentCfgDevice { get; set; }

        /// <summary>
        /// Идентификатор выбранного устройства.
        /// </summary>
        public static int CurrentIdDevice { get; set; }

        /// <summary>
        /// Идентификатор документа.
        /// </summary>
        public IdDoc IdDoc { get; set; }

        /// <summary>
        /// Путь к корневой директории приложения.
        /// </summary>
        public string PathToRootDirectory { get; set; }

        /// <summary>
        /// Путь к файлу конфигурации документа.
        /// </summary>
        public string PathToDocConfigFile { get; set; }

        /// <summary>
        /// Путь к файлу конфигурации для редактирования документа.
        /// </summary>
        public string PathToDocEditConfigFile { get; set; }

        /// <summary>
        /// Путь к файлу шаблону-документа.
        /// </summary>
        public string PathToDocTemplateFile { get; set; }

        public virtual List<RequestListDocs> GetList(long UTBegin, long UTEnd) { return new List<RequestListDocs>(); }

        public virtual object GetViewDoc(int id) 
        { 
            return new DataIVK(); 
        }

        public virtual object GetViewDoc(int id, int protocolNumber)
        {
            return new DataIVK();
        }

        public virtual string GetEditDoc(int id) { return ""; }

        public virtual bool SaveDoc(string jsonData) { return true; }

        public static string ArrByteToString(object arrByte)
        {
            if (string.IsNullOrEmpty(arrByte.ToString()))
                return "";

            return Encoding.UTF8.GetString((byte[])arrByte);
        }

        public static byte[] StringToHexArrByte(object obj)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
        }

        public static object JsonSerializeObject<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static T JsonDeserializeObject<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All });
        }

        public static void LoadCfg<T>(string fileName, ref T obj)
        {
            CfgFile.LoadCfg(Path.Combine(Directory.GetCurrentDirectory(), $"Cfg", fileName), ref obj);
        }

        public static DateTime UnixTimestampToDatetime(long UnixTimestamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(UnixTimestamp);
        }

        public static int DatetimeToUnixTimestamp(DateTime dateTime)
        {
            return (int)(dateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        public void GetPropertyValue(object obj, string key, ref object result)
        {
            if (obj is null) return;

            if (obj is IEnumerable)
            {
                IEnumerable enumerable = (IEnumerable)obj;
                foreach (object item in enumerable)
                    GetPropertyValue(item, key, ref result);
            }
            else
            {
                Type objType = obj.GetType();
                PropertyInfo[] properties = objType.GetProperties();

                foreach (var property in properties)
                {
                    object propValue = property.GetValue(obj, null);

                    if (property.PropertyType.IsPrimitive)
                    {
                        //Debug.WriteLine(property.Name);
                        if (property.Name == key)
                        {
                            result = property.GetValue(obj);
                        }
                    }
                    else if (property.PropertyType.IsClass)
                    {
                        //Debug.WriteLine(property.Name);
                        if (propValue is string)
                        {
                            if (property.Name == key)
                            {
                                result = property.GetValue(obj);
                            }
                        }
                        else if (property.PropertyType.IsArray || property.PropertyType.IsSerializable)
                        {
                            IEnumerable enumerable = (IEnumerable)propValue;
                            foreach (object item in enumerable)
                                GetPropertyValue(item, key, ref result);
                        }
                        else
                        {
                            if (property.Name == key)
                            {
                                result = property.GetValue(obj);
                            }
                            else
                            {
                                GetPropertyValue(propValue, key, ref result);
                            }
                        }
                    }
                }
            }
        }

        public float StringToFloat(string value)
        {
            return float.Parse(value.Replace('.', ','));
        }

        public string GetPathConfigFile()
        {
            var doc = CurrentCfgDevice.Docs.Single(x => x.IdDoc == IdDoc);
            return PathToRootDirectory + doc.PathToDocConfigFile;
        }
        public string GetPathEditConfigFile()
        {
            var doc = CurrentCfgDevice.Docs.Single(x => x.IdDoc == IdDoc);
            return PathToRootDirectory + doc.TemplateDocs.Single(x => x.Id == doc.LastUsedTemplateId).PathToDocEditConfigFile;
        }
        public string GetPathTemplateFile()
        {
            var doc = CurrentCfgDevice.Docs.Single(x => x.IdDoc == IdDoc);
            return PathToRootDirectory + doc.TemplateDocs.Single(x => x.Id == doc.LastUsedTemplateId).PathToDocTemplateFile;
        }
    }

    public class MySqlConnectionInterceptor : DbConnectionInterceptor
    {
        private List<DBConnectionString> ConnectionStrings;

        public MySqlConnectionInterceptor(List<DBConnectionString> connectionStrings)
        {
            ConnectionStrings = connectionStrings;
        }
        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            base.ConnectionOpened(connection, eventData);
        }
        public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
        }
        public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            return await base.ConnectionOpeningAsync(connection, eventData, result, cancellationToken);
        }
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        {
            if (ConnectionStrings != null)
            {
                foreach (var item in ConnectionStrings)
                {
                    connection.ConnectionString = item.GetConnectionString();

                    try
                    {
                        connection.Open();
                    }
                    catch { }

                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        connection.Close();
                        break;
                    }
                }
            }
            return base.ConnectionOpening(connection, eventData, result);
        }
        public override void ConnectionFailed(DbConnection connection, ConnectionErrorEventData eventData)
        {
            base.ConnectionFailed(connection, eventData);
        }
        public override async Task ConnectionFailedAsync(DbConnection connection, ConnectionErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
        }
    }
}