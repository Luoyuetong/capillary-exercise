namespace CapillaryExercise.Hardware;

/// <summary>
/// PLC 控制接口。封装取料、出料、放回等机械动作。
/// 教学/演示用 <c>FakePlcController</c> 进程内模拟；生产可换 FINS 协议实现，业务逻辑零改动。
/// </summary>
public interface IPlcController
{
    /// <summary>
    /// 连接 PLC。预期内的连接失败用返回值表达，不抛异常。
    /// </summary>
    /// <returns>连接成功返回 true，失败返回 false。</returns>
    Task<bool> ConnectAsync();

    /// <summary>
    /// 断开 PLC 连接。
    /// </summary>
    void Disconnect();

    /// <summary>当前是否已连接。</summary>
    bool IsConnected { get; }

    /// <summary>
    /// 从指定仓位取出劈刀至读码位。
    /// </summary>
    /// <param name="face">仓位面（A/B/C）。</param>
    /// <param name="x">X 坐标（1-18）。</param>
    /// <param name="y">Y 坐标（1-18）。</param>
    /// <returns>取料成功返回 true，失败返回 false。</returns>
    Task<bool> FetchFromSlotAsync(string face, int x, int y);

    /// <summary>
    /// 将劈刀放入出料口，完成领料。
    /// </summary>
    /// <returns>出料成功返回 true，失败返回 false。</returns>
    Task<bool> OutputToPickupPortAsync();

    /// <summary>
    /// 将劈刀放回原仓位（读码失败或 MES 拒绝时回退）。
    /// </summary>
    /// <param name="face">仓位面（A/B/C）。</param>
    /// <param name="x">X 坐标（1-18）。</param>
    /// <param name="y">Y 坐标（1-18）。</param>
    /// <returns>放回成功返回 true，失败返回 false。</returns>
    Task<bool> ReturnToSlotAsync(string face, int x, int y);

    /// <summary>
    /// PLC 状态变化事件，参数为状态描述文字。
    /// </summary>
    event Action<string>? OnStatusChanged;
}
