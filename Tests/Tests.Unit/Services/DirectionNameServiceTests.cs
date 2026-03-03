using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using TN.Doc;
using TN.DocData;
using TN_DocGeneral.Dictionaries;
using TN_DocGeneral.Interfaces;
using TN_DocGeneral.Services;
using DocDataBik = TN.DocData.BIK;

namespace Tests.Unit.Services;

[TestFixture]
public class DirectionNameServiceTests
{
    private sealed class DirectionItem : IDirectionItem
    {
        public int BIK_ID { get; init; }
        public int? DIR_ID { get; init; }
        public byte[] SiknName { get; init; }
        public byte[] DirName { get; init; }
    }

    [Test]
    public void GetDirectionName_WhenSourceIsNone_ReturnsEmpty()
    {
        var item = new DirectionItem
        {
            BIK_ID = 1,
            DIR_ID = 1,
            SiknName = Encoding.UTF8.GetBytes("Sikn A"),
            DirName = Encoding.UTF8.GetBytes("Dir A")
        };

        var result = DirectionNameService.GetDirectionName(item, DirectionNameSource.None, new Dictionarys());

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetDirectionName_WhenSourceIsDatabaseAndDirectionExists_ReturnsSiknAndDirection()
    {
        var item = new DirectionItem
        {
            BIK_ID = 10,
            DIR_ID = 20,
            SiknName = Encoding.UTF8.GetBytes("Sikn A"),
            DirName = Encoding.UTF8.GetBytes("Dir A")
        };

        var result = DirectionNameService.GetDirectionName(item, DirectionNameSource.Database, new Dictionarys());

        Assert.That(result, Is.EqualTo("<br><span class=\"direction-caption\">Sikn A. Dir A</span>"));
    }

    [Test]
    public void GetDirectionName_WhenSourceIsDatabaseAndDirectionMissing_ReturnsOnlySikn()
    {
        var item = new DirectionItem
        {
            BIK_ID = 10,
            DIR_ID = 0,
            SiknName = Encoding.UTF8.GetBytes("Sikn A"),
            DirName = null
        };

        var result = DirectionNameService.GetDirectionName(item, DirectionNameSource.Database, new Dictionarys());

        Assert.That(result, Is.EqualTo("<br><span class=\"direction-caption\">Sikn A</span>"));
    }

    [Test]
    public void GetDirectionName_WhenSourceIsConfigAndDirectionNotFound_ReturnsOnlySiknFromDictionary()
    {
        var item = new DirectionItem
        {
            BIK_ID = 2,
            DIR_ID = 999,
            SiknName = Array.Empty<byte>(),
            DirName = Array.Empty<byte>()
        };

        var dictionaries = new Dictionarys
        {
            BIKs =
            [
                new DocDataBik { Id = 1, Name = "Sikn A" },
                new DocDataBik { Id = 2, Name = "Sikn B" }
            ],
            Directions =
            [
                new Direction { Id = 10, Name = "Dir X" },
                new Direction { Id = 11, Name = "Dir Y" }
            ]
        };

        var result = DirectionNameService.GetDirectionName(item, DirectionNameSource.Config, dictionaries);

        Assert.That(result, Is.EqualTo("<br><span class=\"direction-caption\">Sikn B</span>"));
    }

    [Test]
    public void BuildIncompleteList_CreatesCartesianProductAndKeepsDirId()
    {
        var docReport = (DocReport)RuntimeHelpers.GetUninitializedObject(typeof(DocReport));
        var method = typeof(DocReport).GetMethod("BuildIncompleteList", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, "Private method BuildIncompleteList should exist");

        var sikns = new List<DocDataBik>
        {
            new() { Id = 100, Name = "Sikn A" },
            new() { Id = 200, Name = "Sikn B" }
        };
        var directions = new List<Direction>
        {
            new() { Id = 10, Name = "Dir X" },
            new() { Id = 20, Name = "Dir Y" }
        };
        var reportTypes = new List<ReportTypeCfg>
        {
            new() { Id = 1, Name = "Incomplete report", ShortName = "IR", Use = true }
        };

        var docs = (List<RequestListDocs>)method.Invoke(docReport, [sikns, directions, reportTypes]);

        Assert.That(docs, Is.Not.Null);
        Assert.That(docs.Count, Is.EqualTo(4));

        var pairs = docs.Select(x => $"{GetPropertyValue(x, "BIKId")}:{GetPropertyValue(x, "DirId")}").ToList();
        Assert.That(pairs, Is.EquivalentTo(new[] { "100:10", "100:20", "200:10", "200:20" }));
        Assert.That(docs.All(x => x.Description.Contains("Incomplete report<br><span class=\"direction-caption\">")), Is.True);
    }

    private static int GetPropertyValue(RequestListDocs doc, string key)
    {
        var value = doc.AdvancedProperties.Single(x => x.Key == key).Value;
        return Convert.ToInt32(value);
    }
}
