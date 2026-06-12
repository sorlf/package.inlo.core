namespace INLO.Core.DataTable
{
    /// <summary>
    /// DataTable에 들어가는 모든 Row가 가져야 하는 최소 규칙입니다.
    /// 
    /// 각 Row는 반드시 고유한 Id를 가져야 하며,
    /// DataTable은 이 Id를 기준으로 Row를 조회합니다.
    /// </summary>
    public interface IDataTableRow
    {
        string Id { get; }
    }
}