using System;
using FastReport;

namespace TN_Doc.Models.Services;

/// <summary>
/// Потокобезопасный буфер хранения последнего отчёта и PDF-байтов.
/// </summary>
public sealed class ReportBuffer : IReportBuffer, IDisposable
{
	private readonly object _sync = new();
	private Report _preparedReport;
	private byte[] _pdfBytes;
	
	public byte[] GetPdfBytes()
	{
		lock (_sync)
		{
			return _pdfBytes;
		}
	}

	public void SetPdfBytes(byte[] bytes)
	{
		lock (_sync)
		{
			_pdfBytes = bytes;
		}
	}

	private void _disposeReportNoThrow(Report report)
	{
		try { report?.Dispose(); } catch { }
	}

	public void Dispose()
	{
		lock (_sync)
		{
			_disposeReportNoThrow(_preparedReport);
			_preparedReport = null;
			_pdfBytes = null;
		}
	}
}