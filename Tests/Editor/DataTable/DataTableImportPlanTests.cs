using INLO.Core.DataTable;
using INLO.Core.DataTable.Editor;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace INLO.Core.Editor.Tests
{
    public sealed class DataTableImportPlanTests
    {
        [Test]
        public void Prepare_ConvertsAllValuesAndPreservesStringWhitespace()
        {
            TestTable target = ScriptableObject.CreateInstance<TestTable>();
            DataTableGrid grid = CreateGrid(
                new[] { "id", "name", "count" },
                new[] { " 1001 ", "  Keep Me  ", "3" });

            DataTableImportPlan plan = DataTableImportPlanService.PrepareGrid(
                "Assets/test.xlsx",
                "Sheet1",
                target,
                grid);

            Assert.That(plan.CanApply, Is.True);
            Assert.That(plan.Rows, Has.Count.EqualTo(1));

            TestRow row = (TestRow)plan.Rows[0];
            Assert.That(row.id, Is.EqualTo("1001"));
            Assert.That(row.name, Is.EqualTo("  Keep Me  "));
            Assert.That(row.count, Is.EqualTo(3));
        }

        [Test]
        public void Prepare_RejectsInvalidValueAndCanonicalDuplicateId()
        {
            TestTable target = ScriptableObject.CreateInstance<TestTable>();
            DataTableGrid invalidValue = CreateGrid(
                new[] { "id", "name", "count" },
                new[] { "1001", "A", "not-number" });

            DataTableImportPlan invalidPlan = DataTableImportPlanService.PrepareGrid(
                "Assets/test.xlsx",
                "Sheet1",
                target,
                invalidValue);

            Assert.That(invalidPlan.CanApply, Is.False);

            DataTableGrid duplicate = CreateGrid(
                new[] { "id", "name", "count" },
                new[] { "1001", "A", "1" },
                new[] { " 1001 ", "B", "2" });

            DataTableImportPlan duplicatePlan = DataTableImportPlanService.PrepareGrid(
                "Assets/test.xlsx",
                "Sheet1",
                target,
                duplicate);

            Assert.That(duplicatePlan.CanApply, Is.False);
            Assert.That(duplicatePlan.Errors, Has.Some.Matches<DataTableValidationError>(
                error => error.Type == DataTableValidationErrorType.DuplicateId));
        }

        [Test]
        public void AtomicApply_RestoresEarlierTargetsWhenLaterApplyFails()
        {
            TestTable first = ScriptableObject.CreateInstance<TestTable>();
            TestTable second = ScriptableObject.CreateInstance<TestTable>();
            first.Editor_SetRows(new List<TestRow> { new TestRow { id = "old-first", count = 1 } });
            second.Editor_SetRows(new List<TestRow> { new TestRow { id = "old-second", count = 2 } });

            List<DataTableValidationError> schemaErrors = new();
            Assert.That(
                DataTableImportSchema.TryCreate(typeof(TestRow), schemaErrors, out DataTableImportSchema schema),
                Is.True);

            DataTableImportPlan validPlan = new(
                first,
                "Assets/first.xlsx",
                "Sheet1",
                typeof(TestRow),
                new DataTableGrid(),
                new List<TestRow> { new TestRow { id = "new-first", count = 10 } },
                schema,
                new List<DataTableValidationError>());

            DataTableImportPlan invalidApplyPlan = new(
                second,
                "Assets/second.xlsx",
                "Sheet1",
                typeof(TestRow),
                new DataTableGrid(),
                new List<WrongRow> { new WrongRow { id = "wrong-type" } },
                schema,
                new List<DataTableValidationError>());

            bool success = DataTableAssetGenerator.ApplyImportPlans(
                new[] { validPlan, invalidApplyPlan },
                out _);

            Assert.That(success, Is.False);
            Assert.That(first.TryGet("old-first", out _), Is.True);
            Assert.That(first.TryGet("new-first", out _), Is.False);
            Assert.That(second.TryGet("old-second", out _), Is.True);
        }

        private static DataTableGrid CreateGrid(string[] headers, params string[][] rows)
        {
            DataTableGrid grid = new();
            grid.Headers.AddRange(headers);

            for (int i = 0; i < rows.Length; i++)
                grid.Rows.Add(new DataTableGridRow(i + 2, new System.Collections.Generic.List<string>(rows[i])));

            return grid;
        }

        [Serializable]
        public sealed class TestRow : IDataTableRow
        {
            public string id;
            public string name;
            public int count;
            public string Id => id;
        }

        public sealed class TestTable : DataTableAsset<TestRow>
        {
        }

        [Serializable]
        public sealed class WrongRow : IDataTableRow
        {
            public string id;
            public string Id => id;
        }
    }
}
