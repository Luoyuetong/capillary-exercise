namespace CapillaryExercise.Demo;

/// <summary>
/// 一个内置演示案例：选中后自动把工单号 / 机台号填入界面，并给出预期结果提示。
/// 用于让演示者无需记忆"魔法工单号"（且工单号大小写敏感），点选即可复现各分支。
/// </summary>
/// <param name="Name">下拉框显示的案例名（含预期结果，便于一眼看懂）。</param>
/// <param name="WorkOrder">该案例自动填入的工单号。</param>
/// <param name="MachineNo">该案例自动填入的机台号。</param>
/// <param name="Expectation">预期结果说明，显示在提示行。</param>
public sealed record DemoScenario(string Name, string WorkOrder, string MachineNo, string Expectation)
{
    /// <summary>下拉框默认按 ToString 渲染，返回案例名。</summary>
    public override string ToString() => Name;
}
