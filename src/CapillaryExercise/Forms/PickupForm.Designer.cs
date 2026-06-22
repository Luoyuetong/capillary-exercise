namespace CapillaryExercise.Forms;

partial class PickupForm
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>Clean up any resources being used.</summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this._lblWorkOrder = new System.Windows.Forms.Label();
        this._txtWorkOrder = new System.Windows.Forms.TextBox();
        this._lblMachineNo = new System.Windows.Forms.Label();
        this._txtMachineNo = new System.Windows.Forms.TextBox();
        this._btnStart = new System.Windows.Forms.Button();
        this._grpProgress = new System.Windows.Forms.GroupBox();
        this._txtProgress = new System.Windows.Forms.TextBox();
        this._lblResult = new System.Windows.Forms.Label();
        this._grpProgress.SuspendLayout();
        this.SuspendLayout();
        //
        // _lblWorkOrder
        //
        this._lblWorkOrder.AutoSize = true;
        this._lblWorkOrder.Location = new System.Drawing.Point(24, 27);
        this._lblWorkOrder.Name = "_lblWorkOrder";
        this._lblWorkOrder.Size = new System.Drawing.Size(58, 20);
        this._lblWorkOrder.TabIndex = 0;
        this._lblWorkOrder.Text = "工单号";
        //
        // _txtWorkOrder
        //
        this._txtWorkOrder.Location = new System.Drawing.Point(104, 24);
        this._txtWorkOrder.MaxLength = 50;
        this._txtWorkOrder.Name = "_txtWorkOrder";
        this._txtWorkOrder.Size = new System.Drawing.Size(200, 27);
        this._txtWorkOrder.TabIndex = 1;
        //
        // _lblMachineNo
        //
        this._lblMachineNo.AutoSize = true;
        this._lblMachineNo.Location = new System.Drawing.Point(24, 67);
        this._lblMachineNo.Name = "_lblMachineNo";
        this._lblMachineNo.Size = new System.Drawing.Size(58, 20);
        this._lblMachineNo.TabIndex = 2;
        this._lblMachineNo.Text = "机台号";
        //
        // _txtMachineNo
        //
        this._txtMachineNo.Location = new System.Drawing.Point(104, 64);
        this._txtMachineNo.MaxLength = 50;
        this._txtMachineNo.Name = "_txtMachineNo";
        this._txtMachineNo.Size = new System.Drawing.Size(200, 27);
        this._txtMachineNo.TabIndex = 3;
        //
        // _btnStart
        //
        this._btnStart.Location = new System.Drawing.Point(330, 24);
        this._btnStart.Name = "_btnStart";
        this._btnStart.Size = new System.Drawing.Size(120, 67);
        this._btnStart.TabIndex = 4;
        this._btnStart.Text = "开始领料";
        this._btnStart.UseVisualStyleBackColor = true;
        this._btnStart.Click += new System.EventHandler(this.OnStartClick);
        //
        // _grpProgress
        //
        this._grpProgress.Controls.Add(this._txtProgress);
        this._grpProgress.Location = new System.Drawing.Point(24, 107);
        this._grpProgress.Name = "_grpProgress";
        this._grpProgress.Size = new System.Drawing.Size(426, 230);
        this._grpProgress.TabIndex = 5;
        this._grpProgress.TabStop = false;
        this._grpProgress.Text = "领料进度";
        //
        // _txtProgress
        //
        this._txtProgress.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._txtProgress.Dock = System.Windows.Forms.DockStyle.Fill;
        this._txtProgress.Location = new System.Drawing.Point(3, 23);
        this._txtProgress.Multiline = true;
        this._txtProgress.Name = "_txtProgress";
        this._txtProgress.Padding = new System.Windows.Forms.Padding(6);
        this._txtProgress.ReadOnly = true;
        this._txtProgress.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this._txtProgress.Size = new System.Drawing.Size(420, 204);
        this._txtProgress.TabIndex = 0;
        this._txtProgress.TabStop = false;
        //
        // _lblResult
        //
        this._lblResult.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        this._lblResult.Location = new System.Drawing.Point(24, 349);
        this._lblResult.Name = "_lblResult";
        this._lblResult.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
        this._lblResult.Size = new System.Drawing.Size(426, 40);
        this._lblResult.TabIndex = 6;
        this._lblResult.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // PickupForm
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(474, 411);
        this.Controls.Add(this._lblResult);
        this.Controls.Add(this._grpProgress);
        this.Controls.Add(this._btnStart);
        this.Controls.Add(this._txtMachineNo);
        this.Controls.Add(this._lblMachineNo);
        this.Controls.Add(this._txtWorkOrder);
        this.Controls.Add(this._lblWorkOrder);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.Name = "PickupForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "劈刀领料";
        this._grpProgress.ResumeLayout(false);
        this._grpProgress.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Label _lblWorkOrder;
    private System.Windows.Forms.TextBox _txtWorkOrder;
    private System.Windows.Forms.Label _lblMachineNo;
    private System.Windows.Forms.TextBox _txtMachineNo;
    private System.Windows.Forms.Button _btnStart;
    private System.Windows.Forms.GroupBox _grpProgress;
    private System.Windows.Forms.TextBox _txtProgress;
    private System.Windows.Forms.Label _lblResult;
}
