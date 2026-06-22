using System.Drawing;
using CapillaryExercise.Demo;
using CapillaryExercise.Models;
using CapillaryExercise.Services;

namespace CapillaryExercise.Forms;

/// <summary>
/// 领料界面：收集工单号/机台号（FR-01），调用 <see cref="PickupService"/> 执行领料，
/// 实时显示每一步进度（FR-02）并展示最终结果（FR-03）。
/// 窗体只负责交互——业务编排在 Service 层，进度文字的渲染在 <see cref="PickupProgressTracker"/>，
/// 二者均与 UI 解耦、可独立测试（见编码规范 §7/§8）。
/// 顶部内置演示案例下拉与"重置演示数据"按钮，便于教学演示各分支（点选自动填参、重置使会改库的场景可重复）。
/// </summary>
public partial class PickupForm : Form
{
    private readonly PickupService _pickupService;
    private readonly IReadOnlyList<DemoScenario> _scenarios;
    private readonly Action _resetDemoData;

    /// <summary>
    /// 通过构造函数注入领料服务与演示配套，窗体不自行 new 业务依赖（见编码规范 §3）。
    /// </summary>
    /// <param name="pickupService">领料流程编排服务。</param>
    /// <param name="scenarios">内置演示案例列表，填充到顶部下拉框。</param>
    /// <param name="resetDemoData">重置演示数据的回调（清库重置），供"重置"按钮调用。</param>
    public PickupForm(
        PickupService pickupService,
        IReadOnlyList<DemoScenario> scenarios,
        Action resetDemoData)
    {
        _pickupService = pickupService;
        _scenarios = scenarios;
        _resetDemoData = resetDemoData;
        InitializeComponent();
        PopulateScenarios();
    }

    /// <summary>
    /// 把演示案例填入下拉框并默认选中第一个（自动填好工单/机台）。
    /// </summary>
    private void PopulateScenarios()
    {
        foreach (var scenario in _scenarios)
        {
            _cmbScenario.Items.Add(scenario);
        }
        if (_cmbScenario.Items.Count > 0)
        {
            _cmbScenario.SelectedIndex = 0;  // 触发 OnScenarioChanged，自动填入第一个案例
        }
    }

    /// <summary>
    /// 选中演示案例时，把对应工单号/机台号填入输入框，并显示预期结果提示。
    /// </summary>
    private void OnScenarioChanged(object? sender, EventArgs e)
    {
        if (_cmbScenario.SelectedItem is not DemoScenario scenario)
        {
            return;
        }
        _txtWorkOrder.Text = scenario.WorkOrder;
        _txtMachineNo.Text = scenario.MachineNo;
        _lblHint.Text = $"预期：{scenario.Expectation}";
    }

    /// <summary>
    /// "重置演示数据"按钮点击：清库重置，让成功/锁仓等会改库的场景可从干净状态重演。
    /// </summary>
    private void OnResetClick(object? sender, EventArgs e)
    {
        _resetDemoData();
        ClearDisplay();
        ShowResult("演示数据已重置，可重新演示各场景", success: true);
    }

    /// <summary>
    /// "开始领料"按钮点击：校验输入后异步执行领料流程，过程中刷新进度，结束后展示结果。
    /// 事件处理器用 async void——这是 WinForms 事件的惯例用法。
    /// </summary>
    private async void OnStartClick(object? sender, EventArgs e)
    {
        var workOrder = _txtWorkOrder.Text.Trim();
        var machineNo = _txtMachineNo.Text.Trim();

        // FR-01：工单号、机台号必填。
        if (workOrder.Length == 0 || machineNo.Length == 0)
        {
            ShowResult("请输入工单号和机台号", success: false);
            return;
        }

        // 执行期间禁用按钮，避免重复触发；用 try/finally 保证一定恢复。
        _btnStart.Enabled = false;
        ClearDisplay();
        var tracker = new PickupProgressTracker();

        // Progress 在 UI 线程创建，回调自动 marshal 回 UI 线程，可安全更新控件。
        var progress = new Progress<string>(step =>
        {
            tracker.Report(step);
            _txtProgress.Text = tracker.Render();
        });

        try
        {
            var result = await _pickupService.ExecuteAsync(workOrder, machineNo, progress);

            // ExecuteAsync 对 Fake 多为同步完成，排队的 progress 回调此刻可能尚未执行；
            // Yield 一次让消息队列把它们排空，确保 tracker 已收到全部步骤再收尾。
            await Task.Yield();

            tracker.Finish(result.IsSuccess);
            _txtProgress.Text = tracker.Render();
            ShowResult(BuildResultText(result), result.IsSuccess);
        }
        finally
        {
            _btnStart.Enabled = true;
        }
    }

    /// <summary>
    /// 组织结果展示文字：成功时附带条码/类型/原仓位（FR-03），失败时给出原因。
    /// </summary>
    private static string BuildResultText(PickupResult result) => result.Message;

    /// <summary>清空上一次领料的进度与结果显示。</summary>
    private void ClearDisplay()
    {
        _txtProgress.Clear();
        _lblResult.Text = string.Empty;
        _lblResult.BackColor = SystemColors.Control;
        _lblResult.ForeColor = SystemColors.ControlText;
    }

    /// <summary>
    /// 在结果栏显示一条消息，按成败着色（成功绿、失败红）。
    /// </summary>
    /// <param name="message">展示文字。</param>
    /// <param name="success">是否为成功结果。</param>
    private void ShowResult(string message, bool success)
    {
        _lblResult.Text = message;
        _lblResult.BackColor = success ? Color.FromArgb(223, 240, 216) : Color.FromArgb(242, 222, 222);
        _lblResult.ForeColor = success ? Color.FromArgb(60, 118, 61) : Color.FromArgb(169, 68, 66);
    }
}
