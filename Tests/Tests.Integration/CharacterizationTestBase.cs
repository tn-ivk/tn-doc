namespace Tests.Integration;

/// <summary>
/// Базовый класс для characterization-тестов, требующих live MySQL.
/// </summary>
public abstract class CharacterizationTestBase : IntegrationTestBase
{
    protected override bool UseLiveDb => true;
}

