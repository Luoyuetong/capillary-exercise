using System.Text;

namespace CapillaryExercise.Forms;

/// <summary>
/// 领料进度的纯展示逻辑：把 <see cref="Services.PickupService"/> 通过
/// <see cref="IProgress{T}"/> 报告的步骤文字，连同最终结果，渲染成带状态标记的多行文本。
/// 抽成独立类是为了把"怎么显示进度"从窗体里剥离出来单元测试（对应 FR-02），
/// 窗体只负责把它的输出贴到控件上。
/// </summary>
public sealed class PickupProgressTracker
{
    /// <summary>进行中步骤的标记。</summary>
    public const string PendingMark = "▶";

    /// <summary>已完成步骤的标记。</summary>
    public const string DoneMark = "✓";

    /// <summary>失败步骤的标记。</summary>
    public const string FailedMark = "✗";

    private readonly List<string> _steps = new();
    private bool _finished;
    private bool _succeeded;

    /// <summary>已接收到的步骤文字，按报告顺序排列。</summary>
    public IReadOnlyList<string> Steps => _steps;

    /// <summary>是否已收到最终结果。</summary>
    public bool IsFinished => _finished;

    /// <summary>
    /// 记录一个新报告的步骤。最新步骤视为"进行中"，之前的步骤视为"已完成"。
    /// </summary>
    /// <param name="step">服务报告的步骤描述文字。</param>
    public void Report(string step) => _steps.Add(step);

    /// <summary>
    /// 标记流程结束。成功时最后一步显示完成，失败时最后一步显示失败。
    /// </summary>
    /// <param name="succeeded">领料是否成功。</param>
    public void Finish(bool succeeded)
    {
        _finished = true;
        _succeeded = succeeded;
    }

    /// <summary>
    /// 渲染当前进度为多行文本：每行一个步骤，前缀状态标记。
    /// 未结束时最后一步为"进行中"（▶）；结束后最后一步按成败显示 ✓ / ✗。
    /// </summary>
    /// <returns>逐行带状态标记的进度文本；无步骤时返回空字符串。</returns>
    public string Render()
    {
        var builder = new StringBuilder();
        for (var i = 0; i < _steps.Count; i++)
        {
            var isLast = i == _steps.Count - 1;
            builder.AppendLine($"{MarkFor(isLast)} {_steps[i]}");
        }
        return builder.ToString().TrimEnd();
    }

    /// <summary>
    /// 计算某一步的状态标记。非末步一律为"已完成"；末步在未结束时为"进行中"，
    /// 结束后按成败显示完成或失败。
    /// </summary>
    private string MarkFor(bool isLast)
    {
        if (!isLast)
        {
            return DoneMark;
        }
        if (!_finished)
        {
            return PendingMark;
        }
        return _succeeded ? DoneMark : FailedMark;
    }
}
