extern alias PassportLib;

using System.Collections.Generic;
using System.Linq;
using NLog;
using NUnit.Framework;
using PassportLibModels = PassportLib::TN.DocEditor.Passport;
using PassportCore = PassportLib::TN.Doc;
using PassportEdit = PassportLib::TN.Doc.Edit;
namespace Tests.Services.Passport;

[TestFixture]
public class PassportQualityStrategyTests
{
	private static ILogger CreateLogger() => LogManager.CreateNullLogger();

	[Test]
	public void BuildQualityParameterSchema_ElisOff_KeepsLegacyBehavior()
	{
		// Arrange
		var strategy = new PassportCore.PassportQualityStrategy(CreateLogger());
		var parameter = new PassportEdit.Parameter
		{
			Id = 1,
			Key = "TempCorrection",
			Name = "Коррекция температуры",
			IsBallast = true,
			Edit = true
		};

		var schema = new PassportLibModels.QualityParameterSchema
		{
			Id = 1,
			Key = parameter.Key,
			Name = parameter.Name
		};

		var descriptors = new List<PassportCore.MethodDescriptor>
		{
			new(new PassportEdit.Metod { Id = -1, IdParameter = 1, Name = string.Empty }, PassportCore.PassportMethodSource.Config, 0),
			new(new PassportEdit.Metod { Id = 10, IdParameter = 1, Name = "ASTM D1298", Use = true, IsDefault = true }, PassportCore.PassportMethodSource.Config, 1)
		};

		var context = new PassportCore.QualityParameterSchemaContext(parameter, null, descriptors, schema, false);

		// Act
		var result = strategy.BuildQualityParameterSchema(context);

		// Assert
		Assert.That(result.IsBallast, Is.True);
		Assert.That(result.ResultEditMode, Is.Null);
		Assert.That(result.MethodSource, Is.EqualTo("config"));
		Assert.That(result.MethodOptions.Select(o => o.Source), Is.EqualTo(new[] { "config", "config" }));
	}

	[Test]
	public void BuildQualityParameterSchema_ElisOnBallast_UsesReadonlyMode()
	{
		// Arrange
		var strategy = new PassportCore.PassportQualityStrategyElis(CreateLogger());
		var parameter = new PassportEdit.Parameter
		{
			Id = 2,
			Key = "PressCorrection",
			Name = "Коррекция давления",
			IsBallast = true,
			Edit = true
		};

		var labInfo = new PassportCore.LabInfo
		{
			ParameterKey = parameter.Key,
			Metod = new PassportEdit.Metod { Id = 20, IdParameter = 2, Name = "Lab method" },
			ElisFilled = true
		};

		var schema = new PassportLibModels.QualityParameterSchema
		{
			Id = 2,
			Key = parameter.Key,
			Name = parameter.Name
		};

		var descriptors = new List<PassportCore.MethodDescriptor>
		{
			new(new PassportEdit.Metod { Id = -1, IdParameter = 2, Name = string.Empty }, PassportCore.PassportMethodSource.Config, 0),
			new(new PassportEdit.Metod { Id = 21, IdParameter = 2, Name = "Config method" }, PassportCore.PassportMethodSource.Config, 1),
			new(labInfo.Metod, PassportCore.PassportMethodSource.Lab, 2)
		};

		var ballastContext = new PassportCore.QualityParameterSchemaContext(parameter, labInfo, descriptors, schema, true);

		// Act
		var ballastResult = strategy.BuildQualityParameterSchema(ballastContext);

		// Assert
		Assert.That(ballastResult.ResultEditMode, Is.EqualTo(PassportCore.ResultEditModes.Readonly));
		Assert.That(ballastResult.MethodSource, Is.EqualTo("lab"));

		// Non-ballast parameter uses modal mode
		var nonBallast = new PassportEdit.Parameter
		{
			Id = 3,
			Key = "Yield_fraction_200",
			Name = "Выход до 200",
			IsBallast = false,
			Edit = true
		};

		var nonBallastSchema = new PassportLibModels.QualityParameterSchema
		{
			Id = 3,
			Key = nonBallast.Key,
			Name = nonBallast.Name
		};

		var nonBallastContext = new PassportCore.QualityParameterSchemaContext(nonBallast, labInfo, descriptors, nonBallastSchema, true);
		var nonBallastResult = strategy.BuildQualityParameterSchema(nonBallastContext);
		Assert.That(nonBallastResult.ResultEditMode, Is.EqualTo(PassportCore.ResultEditModes.Modal));
	}

	[Test]
	public void BuildParameterMethod_OrdersOptions_ConfigLabManual()
	{
		// Arrange
		var strategy = new PassportCore.PassportQualityStrategyElis(CreateLogger());
		var parameter = new PassportEdit.Parameter { Id = 4, Key = "DensCorrection", Name = "Плотность" };
		var labMethod = new PassportEdit.Metod { Id = 30, IdParameter = 4, Name = "Manual override", Use = true };
		var descriptors = new List<PassportCore.MethodDescriptor>
		{
			new(new PassportEdit.Metod { Id = -1, IdParameter = 4, Name = string.Empty }, PassportCore.PassportMethodSource.Config, 0),
			new(new PassportEdit.Metod { Id = 10, IdParameter = 4, Name = "Config method" }, PassportCore.PassportMethodSource.Config, 1),
			new(new PassportEdit.Metod { Id = 20, IdParameter = 4, Name = "Lab method" }, PassportCore.PassportMethodSource.Lab, 2),
			new(labMethod, PassportCore.PassportMethodSource.Manual, 3)
		};
		var labInfo = new PassportCore.LabInfo
		{
			ParameterKey = parameter.Key,
			Metod = labMethod,
			ElisFilled = false
		};
		var context = new PassportCore.ParameterMethodContext(parameter.Key, string.Empty, labInfo, true, descriptors);

		// Act
		var method = strategy.BuildParameterMethod(context);

		// Assert
		Assert.That(method.Options.Select(o => o.Source), Is.EqualTo(new[] { "config", "config", "lab", "manual" }));
		Assert.That(method.Source, Is.EqualTo("manual"));
	}
}

