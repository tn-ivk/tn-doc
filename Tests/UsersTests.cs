using System.Collections.Generic;
using NUnit.Framework;
using TN_DocGeneral.Dictionaries;
using TN.DocData;

namespace Tests
{
	[TestFixture(TestName = "Набор тестов проверки моделек  из файла  Data.cs -> Users")]
	public class UsersTests
	{
		[TestCase(TestName = "#1 Инициализация модельки пользователя. Не должно генерироваться исключений")]
		public void InitUsersModel()
			=> Assert.DoesNotThrow(() =>
			{
				Users _ = new();
			});

		[TestCase(TestName = "#2 Проверка значений по умолчанию")]
		public void DefaultUsersValueCheck() =>
			Assert.Multiple(() =>
			{
				Users u = new();
				Assert.That(u.UseFullNameSeparator, Is.True);
				Assert.That(u.UseFullNameWhiteSpace, Is.True);
				Assert.That(u.UseShortFullNameForm, Is.True);
			});

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

		[TestCase(TestName = "#4 Проверка формирования ФИО для подписи. Поведение  по умолчанию, без модификаций. Отсутствует отчество у человека")]
		public void CheckUsersDefaultFIOActionWithoutPatronymic()
		{
			Users u = new();
			u.F = "Сталин";
			u.I = "Иван";
			string sign = u.FIO;
			Assert.That(sign, Is.EqualTo("Сталин И."));
		}

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

		[TestCase(TestName = "#6 Проверка модификатора разделителей в ФИО. При его выключение должный убратся из ФИО.Отчество отсутствует")]
		public void CheckUseFullNameSeparatorModificatorWithoutPatronymic()
		{
			Users u = new();
			u.F = "Сталин";
			u.I = "Иван";
			u.UseFullNameSeparator = false;
			Assert.That(u.FIO, Is.EqualTo("Сталин И"));
		}


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

		[TestCase(TestName = "#12 Проверка формирования ИОФ для подписи. " +
		                     "Поведение  по умолчанию, без модификаций. Отсутствует отчество у человека")]
		public void CheckUsersDefaultIOFActionWithoutPatronymic()
		{
			Users u = new();
			u.F = "Сталин";
			u.I = "Иван";
			Assert.That(u.IOF, Is.EqualTo("И. Сталин"));
		}

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