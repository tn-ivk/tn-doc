using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using TN.DocData;
using Newtonsoft.Json.Linq;
using TN_DocGeneral.Services;
using TN.Doc;

namespace TN_Doc.Controllers;

public class ExportController : Controller
{
    private readonly ILogger<ExportController> _logger;
    private readonly IAppConfigService _appConfig;

    public ExportController(ILogger<ExportController> logger, IAppConfigService appConfig)
    {
        _logger = logger;
        _appConfig = appConfig;
    }

    public List<string> GetListFormats()
    {
        return ["pdf", "excel", "ods", "xml"];
    }

    private DocGeneral LoadDocsModule(int idDevice, IdDoc idDoc)
    {
        _logger.LogDebug($"Загрузка DLL документа {idDoc} устройства {_appConfig.GetDeviceName(idDevice)}");
        return new DocGeneral();
    }

    public string ExportDoc(int IdDevice, IdDoc IdDoc, int id, string format, int protocolNumber)
    {         
        _logger.LogDebug($"Экспорт документа {IdDoc} c ИД: {id}, номер протокола {protocolNumber} для устройства {_appConfig.GetDeviceName(IdDevice)}");
        try
        {
            var doc = LoadDocsModule(IdDevice, IdDoc);
            if (doc is null)
            {
                _logger.LogError($"Не удалось загрузить DLL для документа {IdDoc}");
                return string.Empty;
            }

            var exportDirPath = Path.Combine(_appConfig.GetAppCfg().ExportDoc.Path, _appConfig.GetDocCfg(IdDevice, IdDoc).Name);
            if (!Directory.Exists(exportDirPath))
                Directory.CreateDirectory(exportDirPath);

            using var report = new FastReport.Report();
            report.Load(doc.GetPathTemplateFile());
            var jsonDoc = doc.GetViewDoc(id, protocolNumber);
            if (jsonDoc == null)
            {
                _logger.LogError($"Метод GetViewDoc вернул null для документа {IdDoc} с id: {id}, номер протокола {protocolNumber}");
                return string.Empty;
            }

            report.SetParameterValue("JsonDoc", jsonDoc);
            report.Prepare();
            var exportFileName = JObject.Parse(doc.GetViewDoc(id).ToString() ?? string.Empty)["Doc"]?["Settings"]?["General"]?["FileNameForExportDoc"]?.ToString();
            if (string.IsNullOrEmpty(exportFileName))
            {
                _logger.LogError("Невозможно определить имя для экспортируемого файла");
                exportFileName = "undefined";
            }

            var exportFilePath = Path.Combine(exportDirPath, exportFileName);

            switch (format)
            {
                case "pdf":
                    report.Export(new FastReport.Export.Pdf.PDFExport() { ShowProgress = false }, exportFilePath += ".pdf");
                    break;
                case "excel":
                    report.Export(new FastReport.Export.OoXML.Excel2007Export() { ShowProgress = false }, exportFilePath += ".xlsx");
                    break;
                case "ods":
                    report.Export(new FastReport.Export.Odf.ODSExport() { ShowProgress = false }, exportFilePath += ".ods");
                    break;
                case "xml":
                    report.Export(new FastReport.Export.Xml.XMLExport() { ShowProgress = false }, exportFilePath += ".xml");
                    break;
                default:
                    throw new NotSupportedException($"Формат {format} не поддерживается");
            }

            return exportFilePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка экспорта документа {IdDoc}");
            return string.Empty;
        }
    }
}