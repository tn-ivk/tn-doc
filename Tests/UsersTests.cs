using System.Collections.Generic;
using NUnit.Framework;
using TN_DocGeneral.Dictionaries;
using TN.DocData;

namespace Tests
{
	[TestFixture(TestName = "Набор тестов проверки моделек  из файла  Data.cs -> Users")]
	public class UsersTests
	{
		/// <summary>
		/// Проверяет, что конструктор Users не выбрасывает исключения при инициализации по умолчанию.
		/// </summary>
		[TestCase(TestName = "#1 Инициализация модельки пользователя. Не должно генерироваться исключений")]
		public void InitUsersModel()
			=> Assert.DoesNotThrow(() =>
			{
				Users _ = new();
			});

		/// <summary>
		/// Проверяет значения свойств по умолчанию у Users после инициализации.
		/// </summary>
		[TestCase(TestName = "#2 Проверка значений по умолчанию")]
		public void DefaultUsersValueCheck() =>
			Assert.Multiple(() =>
			{
				Users u = new();
				Assert.That(u.UseFullNameSeparator, Is.True);
				Assert.That(u.UseFullNameWhiteSpace, Is.True);
				Assert.That(u.UseShortFullNameForm, Is.True);
			});

		/// <summary>
		/// Проверяет формирование короткой формы ФИО по умолчанию (с отчеством).
		/// </summary>
		[TestCase(TestName = "#3 Проверка формирования ФИО для подписи. Поведение  по умолчанию, без модификаций")]
		public void CheckUsersDefaultFIOAction()
		{
			Users u = new();
			u.F = "Сталин";
			u.I = "Иван";
			u.O = "Александрович";
			string sign = u.FIO;
			Assert.That(sign, Is.EqualTo("Сталин И.А."));
		}

		/// <summary>
		/// Проверяет формирование ФИО по умолчанию без отчества.
		/// </summary>
		[TestCase(TestName = "#4 Проверка формирования ФИО для подписи. Поведение  по умолчанию, без модификаций. Отсутствует отчество у человека")]
		public void CheckUsersDefaultFIOActionWithoutPatronymic()
		{
			Users u = new();
			u.F = "Сталин";
			u.I = "Иван";
			string sign = u.FIO;
			Assert.That(sign, Is.EqualTo("Сталин И."));
		}

		/// <summary>
		/// Проверяет влияние флага UseFullNameSeparator на формат ФИО (с отчеством).
		/// </summary>
		[TestCase(TestName = "#5 Проверка модификатора разделителей в ФИО. При его выключение должный убратся из ФИО.")]
		public void CheckUseFullNameSeparatorModificator()
		{
			Users u = new();
			u.F = "Сталин";
			u.I = "Иван";
			u.O = "Александрович";
			u.UseFullNameSeparator = false;
			Assert.That(u.FIO, Is.EqualTo("Сталин ИА"));
		}

		/// <summary>
		/// Проверяет влияние флага UseFullNameSeparator на формат ФИО без отчества.
		/// </summary>
		[TestCase(TestName = "#6 Проверка модификатора разделителей в ФИО. При его выключение должный убратся из ФИО.Отчество отсутствует")]
		public void CheckUseFullNameSeparatorModificatorWithoutPatronymic()
		{
			Users u = new();
			u.F = "Сталин";
			u.I = "Иван";
			u.UseFullNameSeparator = false;
			Assert.That(u.FIO, Is.EqualTo("Сталин И"));
		}

		/// <summary>
		/// Проверяет влияние флага UseFullNameWhiteSpace на формат ФИО (с отчеством).
		/// </summary>
		[TestCase(TestName = "#7 Проверка модификатора пробелов в ФИО. При его выключение должный убратся пробелы из ФИО.")]
		public void CheckUseFullNameWhiteSpaceModificator()
		{
			Users u = new();
			u.F = "Сталин";
			u.I = "Иван";
			u.O = "Александрович";
			u.UseFullNameWhiteSpace = false;
			Assert.That(u.FIO, Is.EqualTo("СталинИ.А."));
		}

		/// <summary>
		/// Проверяет влияние флага UseFullNameWhiteSpace на формат ФИО без отчества.
		/// </summary>
		[TestCase(TestName = "#8 Проверка модификатора пробелов в ФИО." +
		                     "При его выключение должный убратся пробелы из ФИО.Отчество отсутствует")]
		public void CheckUseFullNameWhiteSpaceModificatorWithoutPatronymic()
		{
			Users u = new();
			u.F = "Сталин";
			u.I = "Иван";
			u.UseFullNameWhiteSpace = false;
			Assert.That(u.FIO, Is.EqualTo("СталинИ."));
		}

		/// <summary>
		/// Проверяет влияние флага UseShortFullNameForm на формат ФИО (полная форма с отчеством).
		/// </summary>
		[TestCase(TestName = "#9 Проверка модификатора использования короткой формы ФИО в ФИО. " +
		                     "При его выключение должный применяться полная форма из ФИО.")]
		public void CheckUseShortFullNameFormModificator()
		{
			Users u = new();
			u.F = "Сталин";
			u.I = "Иван";
			u.O = "Александрович";
			u.UseShortFullNameForm = false;
			Assert.That(u.FIO, Is.EqualTo("Сталин Иван.Александрович."));
		}

		/// <summary>
		/// Проверяет влияние флага UseShortFullNameForm на формат ФИО без отчества (полная форма).
		/// </summary>
		[TestCase(TestName
			= "#10 Проверка модификатора использования короткой формы ФИО в ФИО. " +
			  "При его выключение должный применяться полная форма из ФИО. Отчество отсутствует")]
		public void CheckUseShortFullNameFormModificatorWithoutPatronymic()
		{
			Users u = new();
			u.F = "Сталин";
			u.I = "Иван";
			u.UseShortFullNameForm = false;
			Assert.That(u.FIO, Is.EqualTo("Сталин Иван."));
		}

		/// <summary>
		/// Проверяет формирование ИОФ по умолчанию (короткая форма с инициализированным отчеством).
		/// </summary>
		[TestCase(TestName = "#11 Проверка формирования ИОФ для подписи." +
		                     "Поведение  по умолчанию, без модификаций")]
		public void CheckUsersDefaultIOFAction()
		{
			Users u = new();
			u.F = "Сталин";
			u.I = "Иван";
			u.O = "Александрович";
			Assert.That(u.IOF, Is.EqualTo("И.А. Сталин"));
		}

		/// <summary>
		/// Проверяет формирование ИОФ по умолчанию без отчества.
		/// </summary>
		[TestCase(TestName = "#12 Проверка формирования ИОФ для подписи. " +
		                     "Поведение  по умолчанию, без модификаций. Отсутствует отчество у человека")]
		public void CheckUsersDefaultIOFActionWithoutPatronymic()
		{
			Users u = new();
			u.F = "Сталин";
			u.I = "Иван";
			Assert.That(u.IOF, Is.EqualTo("И. Сталин"));
		}

		/// <summary>
		/// Проверяет влияние флага UseFullNameSeparator на ИОФ (с отчеством).
		/// </summary>
		[TestCase(TestName = "#13 Проверка модификатора разделителей в ИОФ. " +
		                     "При его выключение должный убратся из ИОФ.")]
		public void CheckIOFUseFullNameSeparatorModificator()
		{
			Users u = new();
			u.F = "Сталин";
			u.I = "Иван";
			u.O = "Александрович";
			u.UseFullNameSeparator = false;
			Assert.That(u.IOF, Is.EqualTo("ИА Сталин"));
		}

		/// <summary>
		/// Проверяет влияние флага UseFullNameSeparator на ИОФ без отчества.
		/// </summary>
		[TestCase(TestName = "#14 Проверка модификатора разделителей в ФИО. " +
		                     "При его выключение должный убратся из ФИО.Отчество отсутствует")]
		public void CheckIOFUseFullNameSeparatorModificatorWithoutPatronymic()
		{
			Users u = new();
			u.F = "Сталин";
			u.I = "Иван";
			u.UseFullNameSeparator = false;
			Assert.That(u.IOF, Is.EqualTo("И Сталин"));
		}

		/// <summary>
		/// Проверяет влияние флага UseFullNameWhiteSpace на ИОФ (с отчеством).
		/// </summary>
		[TestCase(TestName = "#15 Проверка модификатора пробелов в ИОФ. " +
		                     "При его выключение должный убратся пробелы из ИОФ.")]
		public void CheckIOFUseFullNameWhiteSpaceModificator()
		{
			Users u = new();
			u.F = "Сталин";
			u.I = "Иван";
			u.O = "Александрович";
			u.UseFullNameWhiteSpace = false;
			Assert.That(u.IOF, Is.EqualTo("И.А.Сталин"));
		}

		/// <summary>
		/// Проверяет влияние флага UseFullNameWhiteSpace на ИОФ без отчества.
		/// </summary>
		[TestCase(TestName = "#16 Проверка модификатора пробелов в ИОФ. " +
		                     "При его выключение должный убратся пробелы из ИОФ.Отчество отсутствует")]
		public void CheckIOFUseFullNameWhiteSpaceModificatorWithoutPatronymic()
		{
			Users u = new();
			u.F = "Сталин";
			u.I = "Иван";
			u.UseFullNameWhiteSpace = false;
			Assert.That(u.IOF, Is.EqualTo("И.Сталин"));
		}

		/// <summary>
		/// Проверяет влияние флага UseShortFullNameForm на ИОФ (полная форма с отчеством).
		/// </summary>
		[TestCase(TestName = "#17 Проверка модификатора использования короткой формы ИОФ в ИОФ. " +
		                     "При его выключение должный применяться полная форма из ИОФ.")]
		public void CheckIOFUseShortFullNameFormModificator()
		{
			Users u = new();
			u.F = "Сталин";
			u.I = "Иван";
			u.O = "Александрович";
			u.UseShortFullNameForm = false;
			Assert.That(u.IOF, Is.EqualTo("Иван.Александрович. Сталин"));
		}

		/// <summary>
		/// Проверяет влияние флага UseShortFullNameForm на ИОФ без отчества (полная форма).
		/// </summary>
		[TestCase(TestName = "#18 Проверка модификатора использования короткой формы ИОФ в ИОФ. " +
		                     "При его выключение должный применяться полная форма из ИОФ. Отчество отсутствует")]
		public void CheckIOFUseShortFullNameFormModificatorWithoutPatronymic()
		{
			Users u = new();
			u.F = "Сталин";
			u.I = "Иван";
			u.UseShortFullNameForm = false;
			Assert.That(u.IOF, Is.EqualTo("Иван. Сталин"));
		}

		/// <summary>
		/// Проверяет корректность обработки китайских имён и влияние флагов форматирования ФИО/ИОФ.
		/// </summary>
		[TestCase(TestName = "#19 Проверка работы модификаторов на китайских ФИО ")]
		public void ChinaTest()
		{
			Users u = new();
			u.F = "张";
			u.I = "美玲";
			Assert.That(u.FIO, Is.EqualTo("张 美."));
			Assert.That(u.IOF, Is.EqualTo("美. 张"));

			u.UseFullNameSeparator = false;
			Assert.That(u.FIO, Is.EqualTo("张 美"));
			Assert.That(u.IOF, Is.EqualTo("美 张"));

			u.UseFullNameWhiteSpace = false;
			Assert.That(u.FIO, Is.EqualTo("张美"));
			Assert.That(u.IOF, Is.EqualTo("美张"));

			u.UseShortFullNameForm = false;
			Assert.That(u.FIO, Is.EqualTo("张美玲"));
			Assert.That(u.IOF, Is.EqualTo("美玲张"));
		}
	}
}