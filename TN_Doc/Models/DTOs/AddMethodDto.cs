namespace TN_Doc.Models.DTOs;

/// <summary>
/// DTO для добавления метода испытаний в справочник
/// </summary>
public class AddMethodDto
{
    /// <summary>
    /// Относительный путь к файлу конфигурации (CfgEditPassport*.json)
    /// </summary>
    public string EditConfigFilePath { get; set; }

    /// <summary>
    /// ID параметра качества
    /// </summary>
    public int ParameterId { get; set; }

    /// <summary>
    /// Название метода испытаний
    /// </summary>
    public string MethodName { get; set; }

    /// <summary>
    /// Метод по умолчанию
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Активирован ли лимит
    /// </summary>
    public bool LimitValueActivate { get; set; }

    /// <summary>
    /// Пороговое значение
    /// </summary>
    public float? LimitValue { get; set; }

    /// <summary>
    /// Строка лимита
    /// </summary>
    public string LimitValueString { get; set; }
}
