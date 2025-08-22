using System;
using FastReport;

namespace TN_Doc.Models.Services
{
	/// <summary>
	/// Буфер последнего подготовленного отчёта и его PDF-байтов в памяти.
	/// Используется для просмотра и печати без записи на диск.
	/// </summary>
	public interface IReportBuffer
	{
		/// <summary>
		/// Получить последний подготовленный отчёт.
		/// </summary>
		Report GetPreparedReport();

		/// <summary>
		/// Установить подготовленный отчёт. Освобождает предыдущий отчёт, если он есть.
		/// </summary>
		/// <param name="report">Подготовленный отчёт (после Prepare). Владение переходит буферу.</param>
		void SetPreparedReport(Report report);

		/// <summary>
		/// Получить последние PDF-байты.
		/// </summary>
		byte[] GetPdfBytes();

		/// <summary>
		/// Установить PDF-байты.
		/// </summary>
		/// <param name="bytes">PDF содержимое.</param>
		void SetPdfBytes(byte[] bytes);
	}
}


