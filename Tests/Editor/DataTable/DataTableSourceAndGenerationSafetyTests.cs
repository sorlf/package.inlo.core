using INLO.Core.DataTable.Editor;
using NUnit.Framework;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace INLO.Core.Editor.Tests
{
    public sealed class DataTableSourceAndGenerationSafetyTests
    {
        [TestCase("http://docs.google.com/spreadsheets/d/e/test/pub?output=csv")]
        [TestCase("https://example.com/file.csv")]
        [TestCase("https://docs.google.com/login")]
        public void GoogleSourcePolicy_RejectsUnsafeOrUnsupportedUrls(string url)
        {
            Assert.That(GooglePublishedCsvGridReader.IsSupportedUrl(url), Is.False);
        }

        [Test]
        public void GoogleSourcePolicy_AcceptsPublishedHttpsCsv()
        {
            Assert.That(
                GooglePublishedCsvGridReader.IsSupportedUrl(
                    "https://docs.google.com/spreadsheets/d/e/test/pub?output=csv"),
                Is.True);
        }

        [Test]
        public void CodeGenerationPlan_RejectsPackageAndMissingOutputFolders()
        {
            DataTableGrid grid = new();
            grid.Headers.Add("id");
            grid.Rows.Add(new DataTableGridRow(
                2,
                new System.Collections.Generic.List<string> { "1001" }));

            DataTableCodeGenerationPlan packagePlan = DataTableCodeGenerator.Prepare(
                "Monster",
                "Game.DataTable",
                "Packages/com.inlo.core",
                grid);

            DataTableCodeGenerationPlan missingFolderPlan = DataTableCodeGenerator.Prepare(
                "Monster",
                "Game.DataTable",
                "Assets/FolderThatMustNotExist",
                grid);

            Assert.That(packagePlan.CanApply, Is.False);
            Assert.That(missingFolderPlan.CanApply, Is.False);
        }

        [Test]
        public void CodeGenerationPlan_PreparesNewRowAndTableFromSourceGrid()
        {
            DataTableGrid grid = new();
            grid.Headers.Add("id");
            grid.Headers.Add("displayName");
            grid.Headers.Add("hp");
            grid.Rows.Add(new DataTableGridRow(
                2,
                new System.Collections.Generic.List<string> { "1001", "Slime", "30" }));

            DataTableCodeGenerationPlan plan = DataTableCodeGenerator.Prepare(
                "SchemaGenerationSafetySample",
                "Game.DataTable",
                "Assets",
                grid);

            Assert.That(plan.CanApply, Is.True, string.Join("\n", plan.Errors));
            Assert.That(plan.AssemblyName, Is.EqualTo("Assembly-CSharp"));
            Assert.That(plan.RowPath, Is.EqualTo("Assets/SchemaGenerationSafetySampleRow.cs"));
            Assert.That(plan.TablePath, Is.EqualTo("Assets/SchemaGenerationSafetySampleTable.cs"));
            Assert.That(plan.RowCode, Does.Contain("public string id;"));
            Assert.That(plan.RowCode, Does.Contain("public string displayName;"));
            Assert.That(plan.RowCode, Does.Contain("public int hp;"));
            Assert.That(plan.TableCode, Does.Contain(
                "DataTableAsset<SchemaGenerationSafetySampleRow>"));
        }

        [Test]
        public void CreationWorkflowPlan_UsesSeparateScriptAndAssetFolders()
        {
            DataTableGrid grid = new();
            grid.Headers.Add("id");
            grid.Headers.Add("displayName");
            grid.Rows.Add(new DataTableGridRow(
                2,
                new System.Collections.Generic.List<string> { "1001", "Slime" }));

            DataTableCreationWorkflowPlan plan = DataTableCreationWorkflow.Prepare(
                "OneClickSafetySample",
                DataTableCreationWorkflow.DefaultNamespace,
                "Assets",
                "Assets/INLO/DataTable",
                "Assets/source.xlsx",
                "Monsters",
                grid);

            Assert.That(plan.CanApply, Is.True, string.Join("\n", plan.Errors));
            Assert.That(plan.TableTypeName, Is.EqualTo("Game.DataTable.OneClickSafetySampleTable"));
            Assert.That(
                plan.CodePlan.RowPath,
                Is.EqualTo("Assets/OneClickSafetySampleRow.cs"));
            Assert.That(
                plan.CodePlan.TablePath,
                Is.EqualTo("Assets/OneClickSafetySampleTable.cs"));
            Assert.That(
                plan.AssetPath,
                Is.EqualTo("Assets/INLO/DataTable/OneClickSafetySampleTable.asset"));
            Assert.That(plan.Source, Is.EqualTo("Assets/source.xlsx"));
            Assert.That(plan.Sheet, Is.EqualTo("Monsters"));
        }

        [Test]
        public void XlsxReader_RejectsFormulaCellsInsteadOfUsingCachedValues()
        {
            string path = Path.Combine(Path.GetTempPath(), "inlo-formula-test.xlsx");

            try
            {
                using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create))
                {
                    WriteEntry(
                        archive,
                        "xl/workbook.xml",
                        "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                    WriteEntry(
                        archive,
                        "xl/_rels/workbook.xml.rels",
                        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Target=\"worksheets/sheet1.xml\" Type=\"worksheet\"/></Relationships>");
                    WriteEntry(
                        archive,
                        "xl/worksheets/sheet1.xml",
                        "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>id</t></is></c></row><row r=\"2\"><c r=\"A2\"><f>1+1</f><v>2</v></c></row></sheetData></worksheet>");
                }

                Assert.Throws<InvalidDataException>(() => XlsxTableGridReader.ReadFirstSheet(path));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Test]
        public void XlsxReader_AllowsStyledPlainNumberCells()
        {
            string path = Path.Combine(Path.GetTempPath(), "inlo-styled-number-test.xlsx");

            try
            {
                using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create))
                {
                    WriteEntry(
                        archive,
                        "xl/workbook.xml",
                        "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                    WriteEntry(
                        archive,
                        "xl/_rels/workbook.xml.rels",
                        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Target=\"worksheets/sheet1.xml\" Type=\"worksheet\"/></Relationships>");
                    WriteEntry(
                        archive,
                        "xl/worksheets/sheet1.xml",
                        "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>id</t></is></c></row><row r=\"2\"><c r=\"A2\" s=\"1\"><v>1001</v></c></row></sheetData></worksheet>");
                }

                DataTableGrid grid = XlsxTableGridReader.ReadFirstSheet(path);

                Assert.That(grid.Rows, Has.Count.EqualTo(1));
                Assert.That(grid.Rows[0].GetCell(0), Is.EqualTo("1001"));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private static void WriteEntry(ZipArchive archive, string name, string contents)
        {
            ZipArchiveEntry entry = archive.CreateEntry(name);
            using Stream stream = entry.Open();
            using StreamWriter writer = new(stream, new UTF8Encoding(false));
            writer.Write(contents);
        }
    }
}
