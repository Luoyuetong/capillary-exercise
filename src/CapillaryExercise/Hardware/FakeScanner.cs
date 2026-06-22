namespace CapillaryExercise.Hardware;

/// <summary>
/// 进程内模拟扫码器，实现 <see cref="IScanner"/>，让 App 无需真实硬件即可运行并支撑端到端测试。
/// <see cref="ScanAsync"/> 返回预置条码 <see cref="NextBarcode"/>；将其设为 null 即模拟读码失败。
/// 生产环境可替换为串口实现，业务逻辑零改动。
/// </summary>
public class FakeScanner : IScanner
{
    /// <summary>连接是否成功。设为 false 模拟连接失败。默认 true。</summary>
    public bool ConnectShouldSucceed { get; set; } = true;

    /// <summary>下一次扫码返回的条码；设为 null 模拟读码失败。</summary>
    public string? NextBarcode { get; set; }

    /// <summary>
    /// 用预置条码构造。
    /// </summary>
    /// <param name="presetBarcode">扫码返回的条码；传 null 表示模拟读码失败。</param>
    public FakeScanner(string? presetBarcode = null) => NextBarcode = presetBarcode;

    /// <inheritdoc />
    public bool IsConnected { get; private set; }

    /// <inheritdoc />
    public event Action<string>? OnBarcodeReceived;

    /// <inheritdoc />
    public Task<bool> ConnectAsync()
    {
        IsConnected = ConnectShouldSucceed;
        return Task.FromResult(ConnectShouldSucceed);
    }

    /// <inheritdoc />
    public void Disconnect() => IsConnected = false;

    /// <inheritdoc />
    public Task<string?> ScanAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        // 读到条码时同时触发被动推送事件，模拟扫码器主动上报。
        if (NextBarcode is not null)
        {
            OnBarcodeReceived?.Invoke(NextBarcode);
        }
        return Task.FromResult(NextBarcode);
    }
}
