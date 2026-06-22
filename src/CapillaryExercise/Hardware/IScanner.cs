namespace CapillaryExercise.Hardware;

/// <summary>
/// 扫码器接口。封装触发扫码与条码读取。
/// 教学/演示用 <c>FakeScanner</c> 进程内模拟；生产可换串口实现，业务逻辑零改动。
/// </summary>
public interface IScanner
{
    /// <summary>
    /// 连接扫码器。预期内的连接失败用返回值表达，不抛异常。
    /// </summary>
    /// <returns>连接成功返回 true，失败返回 false。</returns>
    Task<bool> ConnectAsync();

    /// <summary>
    /// 断开扫码器连接。
    /// </summary>
    void Disconnect();

    /// <summary>当前是否已连接。</summary>
    bool IsConnected { get; }

    /// <summary>
    /// 触发一次扫码，返回读取到的条码。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>读取成功返回条码，读码失败返回 null。</returns>
    Task<string?> ScanAsync(CancellationToken ct = default);

    /// <summary>
    /// 被动推送模式事件：扫码器主动上报条码时触发（可选）。
    /// </summary>
    event Action<string>? OnBarcodeReceived;
}
