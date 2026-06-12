namespace INLO.Core.DataTable.Editor
{
    public interface IDataTableSourceReader<TRow>
    {
        DataTableImportResult<TRow> Read();
    }
}