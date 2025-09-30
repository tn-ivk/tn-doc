using System;
using NUnit.Framework;
using TN.Doc;

namespace Tests.Services;

[TestFixture]
public class DocGeneralTests
{
    [TestFixture]
    public class NormalizeDecimalStringTests
    {
        [Test]
        public void NormalizeDecimalString_NullInput_ReturnsNull()
        {
            // Arrange
            string input = null;

            // Act
            var result = DocGeneral.NormalizeDecimalString(input);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void NormalizeDecimalString_EmptyInput_ReturnsEmpty()
        {
            // Arrange
            var input = "";

            // Act
            var result = DocGeneral.NormalizeDecimalString(input);

            // Assert
            Assert.That(result, Is.EqualTo(""));
        }

        [Test]
        public void NormalizeDecimalString_DotToComma_Success()
        {
            // Arrange
            var input = "123.45";

            // Act
            var result = DocGeneral.NormalizeDecimalString(input, ',');

            // Assert
            Assert.That(result, Is.EqualTo("123,45"));
        }

        [Test]
        public void NormalizeDecimalString_CommaToDot_Success()
        {
            // Arrange
            var input = "123,45";

            // Act
            var result = DocGeneral.NormalizeDecimalString(input, '.');

            // Assert
            Assert.That(result, Is.EqualTo("123.45"));
        }

        [Test]
        public void NormalizeDecimalString_DefaultToComma_Success()
        {
            // Arrange
            var input = "123.45";

            // Act
            var result = DocGeneral.NormalizeDecimalString(input);

            // Assert
            Assert.That(result, Is.EqualTo("123,45"));
        }

        [Test]
        public void NormalizeDecimalString_NoDecimalSeparator_ReturnsUnchanged()
        {
            // Arrange
            var input = "12345";

            // Act
            var result = DocGeneral.NormalizeDecimalString(input);

            // Assert
            Assert.That(result, Is.EqualTo("12345"));
        }

        [Test]
        public void NormalizeDecimalString_MultipleSeparators_ReplacesAll()
        {
            // Arrange
            var input = "123.45.67";

            // Act
            var result = DocGeneral.NormalizeDecimalString(input, ',');

            // Assert
            Assert.That(result, Is.EqualTo("123,45,67"));
        }
    }

    [TestFixture]
    public class MapPropertiesByNameTests
    {
        private class SourceClass
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public double Value { get; set; }
            public bool IsActive { get; set; }
        }

        private class TargetClass
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public double Value { get; set; }
            public bool IsActive { get; set; }
            public string Email { get; set; } // Свойство только в целевом классе
        }

        private class PartialTargetClass
        {
            public string Name { get; set; }
            public int Age { get; set; }
            // Нет Value и IsActive
        }

        private class IncompatibleTargetClass
        {
            public string Name { get; set; }
            public string Age { get; set; } // Несовместимый тип
        }

        private class ReadOnlyTargetClass
        {
            public string Name { get; set; }
            public int Age { get; private set; } // Только для чтения
        }

        [Test]
        public void MapPropertiesByName_NullSource_ReturnsDefault()
        {
            // Act
            var result = DocGeneral.MapPropertiesByName<TargetClass>(null);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void MapPropertiesByName_ValidSource_MapsAllProperties()
        {
            // Arrange
            var source = new SourceClass
            {
                Name = "Test",
                Age = 25,
                Value = 123.45,
                IsActive = true
            };

            // Act
            var result = DocGeneral.MapPropertiesByName<TargetClass>(source);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Test"));
            Assert.That(result.Age, Is.EqualTo(25));
            Assert.That(result.Value, Is.EqualTo(123.45));
            Assert.That(result.IsActive, Is.True);
            Assert.That(result.Email, Is.Null); // Свойство только в целевом классе
        }

        [Test]
        public void MapPropertiesByName_PartialMatch_MapsOnlyMatchingProperties()
        {
            // Arrange
            var source = new SourceClass
            {
                Name = "Test",
                Age = 25,
                Value = 123.45,
                IsActive = true
            };

            // Act
            var result = DocGeneral.MapPropertiesByName<PartialTargetClass>(source);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Test"));
            Assert.That(result.Age, Is.EqualTo(25));
        }

        [Test]
        public void MapPropertiesByName_IncompatibleTypes_SkipsIncompatibleProperties()
        {
            // Arrange
            var source = new SourceClass
            {
                Name = "Test",
                Age = 25,
                Value = 123.45,
                IsActive = true
            };

            // Act
            var result = DocGeneral.MapPropertiesByName<IncompatibleTargetClass>(source);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Test"));
            Assert.That(result.Age, Is.Null); // Несовместимый тип, не скопировано
        }

        [Test]
        public void MapPropertiesByName_ReadOnlyProperty_SkipsReadOnlyProperties()
        {
            // Arrange
            var source = new SourceClass
            {
                Name = "Test",
                Age = 25,
                Value = 123.45,
                IsActive = true
            };

            // Act
            var result = DocGeneral.MapPropertiesByName<ReadOnlyTargetClass>(source);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Test"));
            Assert.That(result.Age, Is.EqualTo(0)); // Свойство только для чтения, не скопировано
        }

        [Test]
        public void MapPropertiesByName_NullValues_CopiesNullValues()
        {
            // Arrange
            var source = new SourceClass
            {
                Name = null,
                Age = 25,
                Value = 123.45,
                IsActive = true
            };

            // Act
            var result = DocGeneral.MapPropertiesByName<TargetClass>(source);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.Null);
            Assert.That(result.Age, Is.EqualTo(25));
            Assert.That(result.Value, Is.EqualTo(123.45));
            Assert.That(result.IsActive, Is.True);
        }
    }

    [TestFixture]
    public class TN_ToolsFastReportCompatibilityTests
    {
        private TN_ToolsFastReport.Tools _tools;

        [SetUp]
        public void Setup()
        {
            _tools = new TN_ToolsFastReport.Tools();
        }

        [Test]
        public void UnixTimestampToDatetime_ValidTimestamp_ReturnsCorrectDateTime()
        {
            // Arrange
            long timestamp = 1609459200; // 2021-01-01 00:00:00 UTC

            // Act
            var result = _tools.UnixTimestampToDatetime(timestamp);

            // Assert
            Assert.That(result, Is.EqualTo(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        }

        [Test]
        public void GetPropertyValue_ValidProperty_ReturnsCorrectValue()
        {
            // Arrange
            var testObj = new { Name = "Test", Age = 25 };
            object result = null;

            // Act
            _tools.GetPropertyValue(testObj, "Name", ref result);

            // Assert
            Assert.That(result, Is.EqualTo("Test"));
        }

        [Test]
        public void GetNormalizedValue_DotSeparator_ReplacesWithComma()
        {
            // Arrange
            var input = "123.45";

            // Act
            var result = _tools.GetNormalizedValue(input);

            // Assert
            Assert.That(result, Is.EqualTo("123,45"));
        }

        [Test]
        public void GetNormalizedValue_NullInput_ReturnsEmptyString()
        {
            // Act
            var result = _tools.GetNormalizedValue(null);

            // Assert
            Assert.That(result, Is.EqualTo(""));
        }

        [Test]
        public void Cast_ValidObject_MapsPropertiesCorrectly()
        {
            // Arrange
            var source = new { Name = "Test", Age = 25 };

            // Act
            var result = _tools.Cast<TargetClass>(source);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Test"));
            Assert.That(result.Age, Is.EqualTo(25));
        }

        [Test]
        public void Cast_NullInput_ReturnsDefault()
        {
            // Act
            var result = _tools.Cast<TargetClass>(null);

            // Assert
            Assert.That(result, Is.Null);
        }

        private class TargetClass
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }
    }
}