namespace CapillaryExercise.Hardware;

/// <summary>
/// 进程内模拟 PLC，实现 <see cref="IPlcController"/>，让 App 无需真实硬件即可运行并支撑端到端测试。
/// 默认所有动作返回成功；可通过各 <c>*ShouldSucceed</c> 开关配置为模拟失败，覆盖业务的失败分支。
/// 生产环境可替换为 FINS 协议实现，业务逻辑零改动。
/// </summary>
public class FakePlcController : IPlcController
{
    /// <summary>连接是否成功。设为 false 模拟连接失败。默认 true。</summary>
    public bool ConnectShouldSucceed { get; set; } = true;

    /// <summary>取料是否成功。设为 false 模拟取料失败。默认 true。</summary>
    public bool FetchShouldSucceed { get; set; } = true;

    /// <summary>出料是否成功。设为 false 模拟出料失败。默认 true。</summary>
    public bool OutputShouldSucceed { get; set; } = true;

    /// <summary>放回是否成功。设为 false 模拟放回失败。默认 true。</summary>
    public bool ReturnShouldSucceed { get; set; } = true;

    /// <inheritdoc />
    public bool IsConnected { get; private set; }

    /// <inheritdoc />
    public event Action<string>? OnStatusChanged;

    /// <inheritdoc />
    public Task<bool> ConnectAsync()
    {
        IsConnected = ConnectShouldSucceed;
        Report(IsConnected ? "PLC 已连接" : "PLC 连接失败");
        return Task.FromResult(ConnectShouldSucceed);
    }

    /// <inheritdoc />
    public void Disconnect()
    {
        IsConnected = false;
        Report("PLC 已断开");
    }

    /// <inheritdoc />
    public Task<bool> FetchFromSlotAsync(string face, int x, int y)
    {
        Report(FetchShouldSucceed
            ? $"取料：{face}({x},{y}) → 读码位"
            : $"取料失败：{face}({x},{y})");
        return Task.FromResult(FetchShouldSucceed);
    }

    /// <inheritdoc />
    public Task<bool> OutputToPickupPortAsync()
    {
        Report(OutputShouldSucceed ? "出料至出料口" : "出料失败");
        return Task.FromResult(OutputShouldSucceed);
    }

    /// <inheritdoc />
    public Task<bool> ReturnToSlotAsync(string face, int x, int y)
    {
        Report(ReturnShouldSucceed
            ? $"放回：{face}({x},{y})"
            : $"放回失败：{face}({x},{y})");
        return Task.FromResult(ReturnShouldSucceed);
    }

    /// <summary>
    /// 触发状态变化事件，供 UI 订阅显示。
    /// </summary>
    private void Report(string status) => OnStatusChanged?.Invoke(status);
}
