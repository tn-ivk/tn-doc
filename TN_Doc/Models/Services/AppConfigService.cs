using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TN.DocData;
using TN.Utils.Helpers;

namespace TN_Doc.Models.Services
{
    public class AppConfigService :IAppConfigService
    {
        IConfiguration _configuration;
        ILogger<AppConfigService> _logger;

        readonly CfgApp _cfgApp;
        readonly LastUsedTemplateListCfg _lastUsedTemplateList;
        
        public AppConfigService(IConfiguration configuration, ILogger<AppConfigService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            var cfgDirName = configuration.GetValue<string>("CfgDirPath");
            var mainAppCfgFileName = configuration.GetValue<string>("RelCfgAppName");
            
            if (string.IsNullOrEmpty(mainAppCfgFileName))
            {
                throw new ArgumentNullException(nameof(mainAppCfgFileName), @"Отсутствует путь главной конфигурации приложения");
            }
            
            var mainAppCfgFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), cfgDirName, mainAppCfgFileName));
            if (!mainAppCfgFile.Exists)
                throw new FileNotFoundException($"Отсутствует файл с конфигурацией: {mainAppCfgFile.FullName}");
            
            _cfgApp = CfgFileRW.LoadCfg<CfgApp>(mainAppCfgFile.FullName);
            
            var cfgUserPreferenceDir = configuration.GetValue<string>("UserPreferenceDirPath");
            var lastUsedTemplateFileName = configuration.GetValue<string>("LastUsedTemplateListFileName");
            
            if (string.IsNullOrEmpty(lastUsedTemplateFileName))
            {
                throw new ArgumentNullException(nameof(lastUsedTemplateFileName), @"Отсутствует путь списка последних открытх шаблонов документов");
            }
            
            var lastUsedTemplateFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), cfgUserPreferenceDir, lastUsedTemplateFileName));
            if (!lastUsedTemplateFile.Exists)
                throw new FileNotFoundException($"Отсутствует файл с конфигурацией: {lastUsedTemplateFile.FullName}");
            
            _lastUsedTemplateList = CfgFileRW.LoadCfg<LastUsedTemplateListCfg>(lastUsedTemplateFile.FullName);
        }

        public CfgApp GetAppCfg() => _cfgApp;

        public LastUsedTemplateListCfg GetLastUsedTemplateList() => _lastUsedTemplateList;
    }
}