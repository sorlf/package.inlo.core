using NUnit.Framework;
using INLO.Core.DataTable;
using RuntimeDataTable = INLO.Core.DataTable.DataTable;

namespace INLO.Core.Tests
{
    public sealed class DataTableRuntimeTests
    {
        [Test]
        public void CsvParser_BuildsSchemaAndRows_FromHeaderTypeAndDataRows()
        {
            RuntimeDataTable table = CsvDataTableParser.Parse(
                "id,key,name,hp,speed,isBoss\n" +
                "int,string,string,int,float,bool\n" +
                "1001,goblin,Goblin,50,3.5,false\n" +
                "1002,dragon_boss,Dragon Boss,500,1.25,yes");

            Assert.That(table.Schema.Columns, Has.Count.EqualTo(6));
            Assert.That(table.Schema.GetColumn("hp").Type, Is.EqualTo(DataTableValueType.Int));
            Assert.That(table.Rows, Has.Count.EqualTo(2));
            Assert.That(table.GetRow(1001).GetString("name"), Is.EqualTo("Goblin"));
            Assert.That(table.GetRow(1002).GetBool("isBoss"), Is.True);
        }

        [Test]
        public void CsvParser_HandlesQuotedCommasAndEscapedQuotes()
        {
            RuntimeDataTable table = CsvDataTableParser.Parse(
                "id,key,desc\n" +
                "int,string,string\n" +
                "1001,goblin,\"fast, small enemy\"\n" +
                "1002,dragon,\"boss says \"\"hello\"\"\"");

            Assert.That(table.GetRow(1001).GetString("desc"), Is.EqualTo("fast, small enemy"));
            Assert.That(table.GetRow(1002).GetString("desc"), Is.EqualTo("boss says \"hello\""));
        }

        [Test]
        public void CsvParser_RejectsDuplicateColumnsUnsupportedTypesAndEmptyNumericValues()
        {
            Assert.Throws<DataTableException>(
                () => CsvDataTableParser.Parse(
                    "id,id\n" +
                    "int,int\n" +
                    "1001,1001"));

            Assert.Throws<DataTableException>(
                () => CsvDataTableParser.Parse(
                    "id,color\n" +
                    "int,Color\n" +
                    "1001,red"));

            Assert.Throws<DataTableException>(
                () => CsvDataTableParser.Parse(
                    "id,hp\n" +
                    "int,int\n" +
                    "1001,"));
        }

        [Test]
        public void DataTable_IndexesRowsByNumericIdAndOptionalKey()
        {
            RuntimeDataTable table = CsvDataTableParser.Parse(
                "id,key,name\n" +
                "int,string,string\n" +
                "1001,goblin,Goblin");

            Assert.That(table.TryGetRow(1001, out DataTableRow row), Is.True);
            Assert.That(row.GetString("key"), Is.EqualTo("goblin"));
            Assert.That(table.TryGetRowByKey("goblin", out DataTableRow keyRow), Is.True);
            Assert.That(keyRow.Id, Is.EqualTo(1001));
        }

        [Test]
        public void DataTable_RejectsMissingAndDuplicateIds()
        {
            Assert.Throws<DataTableException>(
                () => CsvDataTableParser.Parse(
                    "key,name\n" +
                    "string,string\n" +
                    "goblin,Goblin"));

            Assert.Throws<DataTableException>(
                () => CsvDataTableParser.Parse(
                    "id,key\n" +
                    "int,string\n" +
                    "1001,goblin\n" +
                    "1001,goblin2"));
        }
    }
}
