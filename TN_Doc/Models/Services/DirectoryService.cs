using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace TN_Doc.Models.Services
{
    /// <summary>
    /// Сервис взаимодействия со справочниками
    /// </summary>
    public sealed class DirectoryService
    {
        private readonly FileInfo _mainCfgFile;
        private readonly object _lock;
        private string _cacheDirectoriesJson;
        private bool _isValidCache;
        
        /// <summary>
        /// Инициализация сервиса работы со словарями
        /// </summary>
        /// <param name="mainCfgFilePath">Путь до главной конфы приложения</param>
        /// <param name="logger">Журнал логирования приложения</param>
        /// <exception cref="ArgumentNullException">При отсутствие пути до главной конфигурации приложения</exception>
        public DirectoryService(string mainCfgFilePath, ILogger<DirectoryService> logger = null)
        {
            if (string.IsNullOrEmpty(mainCfgFilePath))
                throw new ArgumentNullException(nameof(mainCfgFilePath), "Отсутствует путь главной конфигурации");
            _mainCfgFile = new FileInfo(Path.Combine(AppContext.BaseDirectory,mainCfgFilePath));
            FileNotFoundThrowExceptionHelper(_mainCfgFile);
            _cacheDirectoriesJson = null;
            _lock = new object();
        }
        
        /// <summary>
        /// Получения доступных словарей в формате JSON
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException">При отсутствие словарей на сервере</exception>
        public  async Task<string> GetDirectoriesJson()
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
                    return (string)_cacheDirectoriesJson?.Clone()??string.Empty;
                }
            });
        }
        
        /// <summary>
        /// Установка нового значения словарей в формате JSON на сервере. Перезаписывается конфигурация приложения
        /// </summary>
        /// <param name="json">Новый JSON словарей</param>
        public async Task SetDirectoriesFromJson(string json)
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
        /// Запись новых справочников в конфигурацию приложения
        /// </summary>
        /// <param name="modifJson">Модифицированный JSON для записи в файл</param>
        private void WritePatchesToJson(string modifJson)
        {
            var json = File.ReadAllText(_mainCfgFile.FullName);
            var jObject = JObject.Parse(json);
            var jObjectDir = JObject.Parse(modifJson);
            jObject["Doc"]?["Settings"]?["Dictionarys"]?.Replace(jObjectDir);
            File.WriteAllText(_mainCfgFile.FullName, jObject.ToString(), Encoding.Default);
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
    }
}