using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace TN_Doc.Models.Services
{
    /// <summary>
    /// Сервис взаимодействия со справочниками
    /// </summary>
    public sealed class DirectoryService : IDisposable
    {
        private readonly object _lock;
        private string _cacheDirectoriesJson;
        private bool _isValidCache;
        private FileSystemWatcher _fileWatcher;
        private readonly FileInfo _mainCfgFile;
        private readonly FileInfo _mainAppCfgFile;

        // ReSharper disable once NotAccessedField.Local
        private readonly ILogger<DirectoryService> _logger;

        /// <summary>
        /// Инициализация сервиса работы со словарями
        /// </summary>
        /// <param name="mainCfgFile">Путь до конфы приложения</param>
        /// <param name="mainAppCfgFile">Путь до главной конфы приложения</param>
        /// <param name="dirName">Название папки с конфигурациями</param>
        /// <param name="logger">Журнал логирования приложения</param>
        /// <exception cref="ArgumentNullException">При отсутствие пути до главной конфигурации приложения</exception>
        public DirectoryService(string mainCfgFile, string mainAppCfgFile, string dirName, ILogger<DirectoryService> logger = null)
        {
            if (string.IsNullOrEmpty(mainCfgFile))
                throw new ArgumentNullException(nameof(mainCfgFile), @"Отсутствует путь главной конфигурации");
            if (string.IsNullOrEmpty(mainAppCfgFile))
                throw new ArgumentNullException(nameof(mainAppCfgFile), @"Отсутствует путь главной конфигурации приложения");

            _mainCfgFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), dirName, mainCfgFile));
            _mainAppCfgFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), dirName, mainAppCfgFile));
            FileNotFoundThrowExceptionHelper(_mainCfgFile);
            FileNotFoundThrowExceptionHelper(_mainAppCfgFile);
            _cacheDirectoriesJson = null;
            _lock = new object();
            _fileWatcher = CreateCfgFileWatcher();
            _logger = logger;
        }

        /// <summary>
        /// Создание наблюдателя за файлом конифгурации
        /// </summary>
        /// <returns>Возвращает  наблюдателя за файлом конифгурации со словарями</returns>
        /// <remarks>
        /// На перезапись файла вешается событие инвалидации кеша
        /// </remarks>
        private FileSystemWatcher CreateCfgFileWatcher()
        {
            var fileWatcher = new FileSystemWatcher(_mainCfgFile.DirectoryName!, _mainCfgFile.Name);
            fileWatcher.IncludeSubdirectories = false;
            fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
            fileWatcher.EnableRaisingEvents = true;
            fileWatcher.Changed += InvalidateCache;
            return fileWatcher;
        }

        /// <summary>
        /// Событие инвалидация кеша
        /// </summary>
        /// <param name="sender">Источник сообщения</param>
        /// <param name="e"></param>
        private void InvalidateCache(object sender, FileSystemEventArgs e) => _isValidCache = false;

        /// <summary>
        /// Получения доступных словарей в формате JSON
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException">При отсутствие словарей на сервере</exception>
        public async Task<string> GetDirectoriesJsonAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    if (!string.IsNullOrEmpty(_cacheDirectoriesJson) && _isValidCache)
                        return _cacheDirectoriesJson;
                    FileNotFoundThrowExceptionHelper(_mainCfgFile);

                    var json = File.ReadAllText(_mainCfgFile.FullName);
                    var jObject = JObject.Parse(json);
                    _cacheDirectoriesJson = jObject["Doc"]?["Settings"]?["Dictionarys"]?.ToString();
                    _isValidCache = true;
                    if (string.IsNullOrEmpty(_cacheDirectoriesJson))
                        throw new InvalidDataException(message: "Отсутствует данные по справочникам (json is null)");
                    return (string)_cacheDirectoriesJson?.Clone() ?? string.Empty;
                }
            });
        }

        /// <summary>
        /// Установка нового значения словарей в формате JSON на сервере. Перезаписывается конфигурация приложения
        /// </summary>
        /// <param name="json">Новый JSON словарей</param>
        public async Task SetDirectoriesFromJsonAsync(string json)
        {
            await Task.Run(() =>
            {
                var modifJson = json;
                lock (_lock)
                {
                    FileNotFoundThrowExceptionHelper(_mainCfgFile);
                    WritePatchesToJson(modifJson);
                    _isValidCache = false;
                }
            });
        }

        /// <summary>
        /// Установка патча Паспортов качества 
        /// </summary>
        /// <param name="json">Json который необходимо применить</param>
        public async Task SetQpConfigFromJsonAsync(string json)
        {
            await Task.Run(() =>
            {
                var modifJson = json;
                lock (_lock)
                {
                    WritePatchesQpToJson(modifJson);
                }

            });
        }
        
        /// <summary>
        /// Получение конфигурации паспортов качества продуктов
        /// </summary>
        /// <returns>Конфигурации/справочников по паспортам качества</returns>
        public Task<string> GetQualityPassportConfigs() => Task.Run(() => ExtractQPMethodAndParameters(ExtractPassportNameForDeviceCfg()).ToString());
        
        /// <summary>
        /// Запись новых справочников в конфигурацию приложения
        /// </summary>
        /// <param name="modifJson">Модифицированный JSON для записи в файл</param>
        private void WritePatchesToJson(string modifJson)
        {
            string json = File.ReadAllText(_mainCfgFile.FullName);
            JObject jObject = JObject.Parse(json);
            JObject jObjectDir = JObject.Parse(modifJson);
            jObject["Doc"]?["Settings"]?["Dictionarys"]?.Replace(jObjectDir);
            File.WriteAllText(_mainCfgFile.FullName, jObject.ToString(), Encoding.Default);
        }

        /// <summary>
        ///  Запись патча паспортов качества
        /// </summary>
        /// <param name="modifQpJson">Модифицированный Json паспортов качества</param>
        /// <exception cref="FileNotFoundException">Гененерируется при отсутствие файла</exception>
        private void WritePatchesQpToJson(string modifQpJson)
        {
            JObject modifJo = JObject.Parse(modifQpJson);
            foreach (JToken qpi in modifJo["QpsInfo"]!)
            {
                FileInfo fileInfo = new(Directory.GetCurrentDirectory() + qpi["EditConfigFilePath"]!);
                FileNotFoundThrowExceptionHelper(fileInfo);
                JObject jObject = JObject.Parse(File.ReadAllText(fileInfo.FullName));
                jObject["Methods"]!.Replace(qpi["Methods"]!);
                jObject["Parameters"]!.Replace(qpi["Parameters"]!);
                File.WriteAllText(fileInfo.FullName, jObject.ToString(), Encoding.Default);
            }
        }
        
        /// <summary>
        /// Вспомогательный метод проверки наличия конфигурационного файла
        /// </summary>
        /// <param name="info">Информация о файле</param>
        /// <exception cref="FileNotFoundException">Гененерируется при отсутствие файла</exception>
        private void FileNotFoundThrowExceptionHelper(FileInfo info)
        {
            if (!info.Exists)
                throw new FileNotFoundException($"Отсутствует файл с главной конфигурацией: {info.FullName}");
        }

        /// <summary>
        /// Извлечение данных параметров и методов для каждого паспорта
        /// </summary>
        /// <param name="qpsInfo">Информация о девайсах</param>
        /// <returns>Инфомация о всех паспортах качества для каждого продукта</returns>
        private JObject ExtractQPMethodAndParameters(JObject qpsInfo)
        {

            foreach (JToken qps in qpsInfo["QpsInfo"])
            {
                FileInfo cfgFile = new FileInfo(Directory.GetCurrentDirectory()+ qps["EditConfigFilePath"]!);
                FileNotFoundThrowExceptionHelper(cfgFile);
                JObject fullPassCfgJson = JObject.Parse(File.ReadAllText(cfgFile.FullName));
                qps["Methods"] = fullPassCfgJson["Methods"];
                qps["Parameters"] = fullPassCfgJson["Parameters"];
            }
            return qpsInfo;
        }

        /// <summary>
        ///  Получение информации о паспортах на девайсах
        /// </summary>
        /// <returns>JObject с информацией об используемых паспортах на них</returns>
        /// <exception cref="FileNotFoundException">При отсутствие файла с информацией о девайсах</exception>
        private JObject ExtractPassportNameForDeviceCfg()
        {
            FileNotFoundThrowExceptionHelper(_mainAppCfgFile);
            var cfgAppJson = JObject.Parse(File.ReadAllText(_mainAppCfgFile.FullName));
            var passports = new JArray();
            var resultJo = new JObject();
            resultJo.Add("QpsInfo", passports);
            foreach (var deviceJson in cfgAppJson["Devices"]!)
            {
                foreach (var doc in deviceJson["Docs"]!.Where(item => item["Name"].ToString() == "Паспорта"))
                {
                    foreach (var d in doc["TemplateDocs"]!.Where(item => item["Use"].ToObject<bool>()))
                    {
                        if(passports.Any(passItem => passItem["EditConfigFilePath"].ToString() == d["PathToDocEditConfigFile"]!.ToString()))
                            continue;
                        passports.Add(
                            new JObject(
                                new JProperty("EditConfigFilePath",d["PathToDocEditConfigFile"]),
                                new JProperty("Name", d["Name"]))
                            );
                    }
                }
            }
            return resultJo;
        }

        /// <summary>
        /// Очистка ресурсов сервиса
        /// </summary>
        public void Dispose()
        {
            _fileWatcher.Changed -= InvalidateCache;
            _fileWatcher.Dispose();
        }
    }
}