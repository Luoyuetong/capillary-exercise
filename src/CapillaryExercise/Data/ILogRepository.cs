using CapillaryExercise.Models;

namespace CapillaryExercise.Data;

/// <summary>
/// 操作日志记录接口。
/// </summary>
public interface ILogRepository
{
    /// <summary>
    /// 写入一条操作日志。
    /// </summary>
    /// <param name="log">待写入的日志记录。</param>
    void Insert(OperationLog log);
}
