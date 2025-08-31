
namespace TN_Doc.Models.Services;

/// <summary>
/// Буфер последнего подготовленного отчёта и его PDF-байтов в памяти.
/// Используется для просмотра и печати без записи на диск.
/// </summary>
public interface IReportBuffer
{
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