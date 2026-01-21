using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using TN.DocData;

namespace Tests.Services;

public class CfgAppSyncTests
{
    [Test]
    public void CfgApp_DevicesConfigs_AreSynchronized()
    {
        // Arrange
        var repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        var cfgPath = Path.Combine(repoRoot, "TN_Doc", "Cfg", "CfgApp.json");
        Assert.That(File.Exists(cfgPath), Is.True, $"Файл конфигурации не найден: {cfgPath}");

        var json = File.ReadAllText(cfgPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var cfgApp = JsonSerializer.Deserialize<CfgApp>(json, options);
        Assert.That(cfgApp, Is.Not.Null, "Не удалось десериализовать CfgApp.json");
        Assert.That(cfgApp!.Devices, Is.Not.Null.And.Not.Empty, "Список устройств пуст");
        var devices = cfgApp.Devices.ToList();
        Assert.That(devices.Count, Is.GreaterThanOrEqualTo(2), "Для проверки синхронности нужно минимум 2 устройства в конфигурации");

        // Act: нормализуем конфигурации устройств (исключая девайс-специфичные поля)
        var normalized = devices
            .Select(d => new
            {
                Docs = (d.Docs ?? new List<TN.DocData.Document>())
                    .Select(doc => new
                    {
                        doc.Use,
                        doc.IdDoc,
                        doc.Name,
                        doc.PathToDocDll,
                        doc.PathToDocConfigFile,
                        doc.PathToDocEditConfigFile,
                        TemplateDocs = (doc.TemplateDocs ?? new List<TN.DocData.TemplateDoc>())
                            .Select(t => new
                            {
                                t.Use,
                                t.Id,
                                t.Name,
                                t.PathToDocTemplateFile,
                                t.PathToDocEditConfigFile
                            })
                            .OrderBy(t => t.Id)
                            .ToList()
                    })
                    .OrderBy(doc => doc.IdDoc)
                    .ToList()
            })
            .Select(obj => JsonSerializer.Serialize(obj))
            .ToList();

        // Assert: все нормализованные представления должны совпадать
        var baseline = normalized.First();
        var differences = devices
            .Select((dev, idx) => new { Device = dev, Index = idx, Payload = normalized[idx] })
            .Where(x => x.Payload != baseline)
            .Select(x => $"IdDevice={x.Device.IdDevice}, Name={x.Device.Name}")
            .ToList();

        Assert.That(differences, Is.Empty,
            () => "Конфигурации устройств в CfgApp.json не синхронизированы по разделу Docs/TemplateDocs. Отличаются устройства: " + string.Join(", ", differences));
    }
}



