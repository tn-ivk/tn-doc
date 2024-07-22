namespace TN_Doc.Models
{
	/// <summary>
	/// Провайдер данных для предосталении информации о приложение
	/// </summary>
	public class AppInfoProvider
	{
		/// <summary>
		/// Инициализация провайдера
		/// </summary>
		/// <param name="version">Версия приложения</param>
		public AppInfoProvider(string version)
		{
			Version = version;
		}
		
		/// <summary>
		/// Версия приложения
		/// </summary>
		public string Version { get; init; }
	}
}