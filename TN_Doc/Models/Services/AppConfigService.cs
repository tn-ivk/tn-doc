using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TN.DocData;
using TN.Utils.Helpers;

namespace TN_Doc.Models.Services
{
    public class AppConfigService :IAppConfigService
    {
        ILogger<AppConfigService> _logger;

        readonly CfgApp _cfgApp;
        readonly LastUsedTemplateListCfg _lastUsedTemplateList;
        
        public AppConfigService(IConfiguration configuration, ILogger<AppConfigService> logger)
        {
            _logger = logger;
            
            var cfgDirName = configuration.GetValue<string>("CfgDirPath");
            var mainAppCfgFileName = configuration.GetValue<string>("RelCfgAppName");

            _cfgApp = LoadAppCfg(cfgDirName, mainAppCfgFileName);
            
            var cfgUserPreferenceDir = configuration.GetValue<string>("UserPreferenceDirPath");
            var lastUsedTemplateFileName = configuration.GetValue<string>("LastUsedTemplateListFileName");

            _lastUsedTemplateList = LoadLastUsedTemplateList(cfgUserPreferenceDir, lastUsedTemplateFileName, _cfgApp);
        }

        public CfgApp GetAppCfg() => _cfgApp;
        public LastUsedTemplateListCfg GetLastUsedTemplateList() => _lastUsedTemplateList;


        private CfgApp LoadAppCfg(string dirName, string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException(nameof(fileName), @"Отсутствует путь главной конфигурации приложения");
            }
            
            var mainAppCfgFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), dirName, fileName));
            if (!mainAppCfgFile.Exists)
                throw new FileNotFoundException($"Отсутствует файл с конфигурацией: {mainAppCfgFile.FullName}");
            
            return CfgFileRW.LoadCfg<CfgApp>(mainAppCfgFile.FullName);
        }
        
        private LastUsedTemplateListCfg LoadLastUsedTemplateList(string dirName, string fileName, CfgApp cfg)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                _logger.LogError(@"Отсутствует путь списка последних просматриваемых шаблонов документов");
                return null;
            }
            
            var lastUsedTemplateFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), dirName, fileName));
            if (lastUsedTemplateFile.Exists)
            {
                _logger.LogDebug($"Считывание списка идентификаторов последних просматриваемых шаблонов документов из файла {lastUsedTemplateFile.FullName}");
                return CfgFileRW.LoadCfg<LastUsedTemplateListCfg>(lastUsedTemplateFile.FullName);
            }
            
            _logger.LogWarning($"Файл {lastUsedTemplateFile.FullName} не существует");
            if (_cfgApp is not null)
            {
                _logger.LogDebug(
                    "Восстановление списка идентификаторов последних просматриваемых шаблонов документов из файла настроек приложения");
                var result = new LastUsedTemplateListCfg()
                {
                    Devices = _cfgApp.Devices.Select(device => new LastUsedTemplateList()
                    {
                        IdDevice = device.IdDevice,
                        LastTemplateList = device.Docs.Select(doc => new LastUsedTemplate()
                        {
                            IdDoc = doc.IdDoc,
                            LastTemplateId = doc.LastUsedTemplateId
                        }).ToList()
                    }).ToList()
                };
                return result;
            }
                    
            _logger.LogError("Невозможно восстановить список идентификаторов последних просматриваемых шаблонов документов");
            return null;
        }
    }
}