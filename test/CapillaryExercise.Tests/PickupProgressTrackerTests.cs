using CapillaryExercise.Forms;

namespace CapillaryExercise.Tests;

/// <summary>
/// PickupProgressTracker 进度渲染逻辑单元测试。
/// 这是 PickupForm 里唯一含判断的展示逻辑（FR-02：每步显示状态标记），抽出来独立验证；
/// 窗体本身只是把渲染结果贴到控件上，无需 UI 测试。
/// </summary>
public class PickupProgressTrackerTests
{
    // ---- 进行中：最后一步标记为 ▶，之前的步骤标记为 ✓ ----

    [Fact]
    public void Render_WhileRunning_MarksLastStepPendingAndPriorStepsDone()
    {
        // Arrange
        var tracker = new PickupProgressTracker();
        tracker.Report("MES: 查询劈刀类型...");
        tracker.Report("查找库存: CAP-A...");

        // Act
        var text = tracker.Render();

        // Assert：前一步完成、当前步进行中。
        var lines = text.Split(Environment.NewLine);
        Assert.Equal("✓ MES: 查询劈刀类型...", lines[0]);
        Assert.Equal("▶ 查找库存: CAP-A...", lines[1]);
        Assert.False(tracker.IsFinished);
    }

    // ---- 成功收尾：最后一步标记为 ✓ ----

    [Fact]
    public void Render_AfterFinishSuccess_MarksLastStepDone()
    {
        // Arrange
        var tracker = new PickupProgressTracker();
        tracker.Report("PLC: 出料...");

        // Act
        tracker.Finish(succeeded: true);
        var text = tracker.Render();

        // Assert
        Assert.Equal("✓ PLC: 出料...", text);
        Assert.True(tracker.IsFinished);
    }

    // ---- 失败收尾：最后一步标记为 ✗，之前的步骤仍为 ✓ ----

    [Fact]
    public void Render_AfterFinishFailure_MarksLastStepFailed()
    {
        // Arrange
        var tracker = new PickupProgressTracker();
        tracker.Report("扫码器: 读码验证...");
        tracker.Report("读码失败，放回原位并锁定...");

        // Act
        tracker.Finish(succeeded: false);
        var text = tracker.Render();

        // Assert：失败发生在最后一步，前序步骤已完成。
        var lines = text.Split(Environment.NewLine);
        Assert.Equal("✓ 扫码器: 读码验证...", lines[0]);
        Assert.Equal("✗ 读码失败，放回原位并锁定...", lines[1]);
    }

    // ---- 无步骤时渲染为空字符串 ----

    [Fact]
    public void Render_NoSteps_ReturnsEmpty()
    {
        // Arrange
        var tracker = new PickupProgressTracker();

        // Act
        var text = tracker.Render();

        // Assert
        Assert.Equal(string.Empty, text);
    }

    // ---- 步骤按报告顺序保留 ----

    [Fact]
    public void Report_KeepsStepsInOrder()
    {
        // Arrange
        var tracker = new PickupProgressTracker();

        // Act
        tracker.Report("第一步");
        tracker.Report("第二步");
        tracker.Report("第三步");

        // Assert
        Assert.Equal(new[] { "第一步", "第二步", "第三步" }, tracker.Steps);
    }
}
