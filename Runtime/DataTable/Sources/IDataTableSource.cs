namespace INLO.Core.DataTable
{
    public interface IDataTableSource
    {
        string Name { get; }
        string ReadText();
    }
}
