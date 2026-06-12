using System;

namespace INLO.Core.DataTable
{
    /// <summary>
    /// DataTable 조회, 캐시 생성, 검증 과정에서 발생하는 예외입니다.
    /// </summary>
    public sealed class DataTableException : Exception
    {
        public DataTableException(string message) : base(message)
        {
        }
    }
}