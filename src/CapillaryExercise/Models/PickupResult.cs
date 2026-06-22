namespace CapillaryExercise.Models;

/// <summary>
/// 领料流程的执行结果，包含成功标志和说明文字。
/// 由 <c>PickupService</c> 返回，供 UI 层展示。
/// </summary>
public class PickupResult
{
    /// <summary>是否领料成功。</summary>
    public bool IsSuccess { get; }

    /// <summary>结果说明：成功时为提示信息，失败时为原因。</summary>
    public string Message { get; }

    private PickupResult(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }

    /// <summary>
    /// 创建一个成功结果。
    /// </summary>
    /// <param name="message">成功提示信息</param>
    public static PickupResult Success(string message) => new(true, message);

    /// <summary>
    /// 创建一个失败结果。
    /// </summary>
    /// <param name="message">失败原因</param>
    public static PickupResult Fail(string message) => new(false, message);
}
