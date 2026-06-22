using System.Drawing;
using CapillaryExercise.Models;
using CapillaryExercise.Services;

namespace CapillaryExercise.Forms;

/// <summary>
/// 领料界面：收集工单号/机台号（FR-01），调用 <see cref="PickupService"/> 执行领料，
/// 实时显示每一步进度（FR-02）并展示最终结果（FR-03）。
/// 窗体只负责交互——业务编排在 Service 层，进度文字的渲染在 <see cref="PickupProgressTracker"/>，
/// 二者均与 UI 解耦、可独立测试（见编码规范 §7/§8）。
/// </summary>
public partial class PickupForm : Form
{
    private readonly PickupService _pickupService;

    /// <summary>
    /// 通过构造函数注入领料服务，窗体不自行 new 业务依赖（见编码规范 §3）。
    /// </summary>
    /// <param name="pickupService">领料流程编排服务。</param>
    public PickupForm(PickupService pickupService)
    {
        _pickupService = pickupService;
        InitializeComponent();
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
