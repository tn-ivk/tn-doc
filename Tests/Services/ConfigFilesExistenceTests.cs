using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Tests.Services
{
    [TestFixture]
    public class ConfigFilesExistenceTests
    {
        private static readonly string SolutionRoot = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..");
        private static readonly string ConfigDir = Path.Combine(SolutionRoot, "TN_Doc", "Cfg");
        private static readonly string[] ConfigFiles = { "CfgApp.Development.json", "CfgApp.json" };
        private static readonly string[] FileExtensions = { ".json", ".xml", ".frx", ".dll", ".exe", ".txt", ".csv", ".md", ".config" };

        [Test, TestCaseSource(nameof(ConfigFiles))]
        public void AllReferencedFilesExist(string configFileName)
        {
            var configPath = Path.Combine(ConfigDir, configFileName);
            Assert.That(File.Exists(configPath), $"Файл конфигурации не найден: {configPath}");

            var json = File.ReadAllText(configPath);
            var jToken = JToken.Parse(json);
            var filePaths = new List<string>();
            FindFilePaths(jToken, filePaths);

            foreach (var filePath in filePaths)
            {
                // Приведение к абсолютному пути относительно SolutionRoot
                var corFilePath = (filePath.StartsWith("/") ? filePath[1..] : filePath).Replace("/", Path.DirectorySeparatorChar.ToString());
                var absPath = Path.Combine(SolutionRoot, "TN_Doc", corFilePath);
                Assert.That(File.Exists(absPath), $"В конфиге {configFileName} указан несуществующий файл: {filePath} (ожидался по пути: {absPath})");
            }
        }

        private static void FindFilePaths(JToken token, List<string> filePaths)
        {
            if (token.Type == JTokenType.Object)
            {
                foreach (var property in ((JObject)token).Properties())
                {
                    FindFilePaths(property.Value, filePaths);
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var item in (JArray)token)
                {
                    FindFilePaths(item, filePaths);
                }
            }
            else if (token.Type == JTokenType.String)
            {
                var value = token.Value<string>();
                if (!string.IsNullOrWhiteSpace(value) && FileExtensions.Any(ext => value.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    filePaths.Add(value);
                }
            }
        }
    }
} 