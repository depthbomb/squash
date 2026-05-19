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
        c_MainFormStatusStrip = new StatusStrip();
        c_NavigationView = new NavigationView();
        SuspendLayout();
        // 
        // c_MainFormStatusStrip
        // 
        c_MainFormStatusStrip.Location = new Point(0, 428);
        c_MainFormStatusStrip.Name = "c_MainFormStatusStrip";
        c_MainFormStatusStrip.Size = new Size(800, 22);
        c_MainFormStatusStrip.TabIndex = 0;
        // 
        // c_NavigationView
        // 
        c_NavigationView.Dock = DockStyle.Fill;
        c_NavigationView.Location = new Point(0, 0);
        c_NavigationView.Name = "c_NavigationView";
        c_NavigationView.NavigationWidth = 124;
        c_NavigationView.Size = new Size(800, 428);
        c_NavigationView.TabIndex = 1;
        // 
        // MainFormV2
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 450);
        Controls.Add(c_NavigationView);
        Controls.Add(c_MainFormStatusStrip);
        FormScreenCaptureMode = ScreenCaptureMode.HideContent;
        Icon = (Icon)resources.GetObject("$this.Icon");
        MinimumSize = new Size(620, 380);
        Name = "MainFormV2";
        SizeGripStyle = SizeGripStyle.Hide;
        Text = "Squash";
        ResumeLayout(false);
        PerformLayout();
    }
    #endregion

    private StatusStrip c_MainFormStatusStrip;
    private Controls.NavigationView c_NavigationView;
}

