using System.ComponentModel;

namespace Squash.Forms;

partial class MainFormV2
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
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
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        ComponentResourceManager resources = new ComponentResourceManager(typeof(MainFormV2));
        c_StatusStrip = new StatusStrip();
        c_StatusLabel = new ToolStripStatusLabel();
        c_NavigationView = new NavigationView();
        c_StatusStrip.SuspendLayout();
        SuspendLayout();
        // 
        // c_StatusStrip
        // 
        c_StatusStrip.GripStyle = ToolStripGripStyle.Visible;
        c_StatusStrip.Items.AddRange(new ToolStripItem[] { c_StatusLabel });
        c_StatusStrip.Location = new Point(0, 314);
        c_StatusStrip.Name = "c_StatusStrip";
        c_StatusStrip.Size = new Size(784, 22);
        c_StatusStrip.TabIndex = 0;
        // 
        // c_StatusLabel
        // 
        c_StatusLabel.Name = "c_StatusLabel";
        c_StatusLabel.Size = new Size(48, 17);
        c_StatusLabel.Text = "Waiting";
        // 
        // c_NavigationView
        // 
        c_NavigationView.Dock = DockStyle.Fill;
        c_NavigationView.Location = new Point(0, 0);
        c_NavigationView.Name = "c_NavigationView";
        c_NavigationView.NavigationWidth = 110;
        c_NavigationView.Size = new Size(784, 314);
        c_NavigationView.TabIndex = 1;
        // 
        // MainFormV2
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(784, 336);
        Controls.Add(c_NavigationView);
        Controls.Add(c_StatusStrip);
        Icon = (Icon)resources.GetObject("$this.Icon");
        MinimumSize = new Size(800, 375);
        Name = "MainFormV2";
        SizeGripStyle = SizeGripStyle.Hide;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Squash";
        c_StatusStrip.ResumeLayout(false);
        c_StatusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
    private StatusStrip c_StatusStrip;
    private Controls.NavigationView c_NavigationView;
    private ToolStripStatusLabel c_StatusLabel;
}

