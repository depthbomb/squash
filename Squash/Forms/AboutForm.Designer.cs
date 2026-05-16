using System.ComponentModel;

namespace Squash.Forms;

partial class AboutForm
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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
        c_LogoPictureBox          = new System.Windows.Forms.PictureBox();
        c_HeaderLabel             = new System.Windows.Forms.Label();
        c_HostInfoGroupBox        = new System.Windows.Forms.GroupBox();
        c_HostInfoLayout          = new System.Windows.Forms.TableLayoutPanel();
        c_HostArchitectureLabel   = new System.Windows.Forms.Label();
        c_HostOsLabel             = new System.Windows.Forms.Label();
        c_Layout                  = new System.Windows.Forms.TableLayoutPanel();
        c_RuntimeInfoGroupBox     = new System.Windows.Forms.GroupBox();
        c_RuntimeInfoLayout       = new System.Windows.Forms.TableLayoutPanel();
        c_RuntimeIdentifierLabel  = new System.Windows.Forms.Label();
        c_RuntimeDescriptionLabel = new System.Windows.Forms.Label();
        c_CopyrightLinkLabel      = new System.Windows.Forms.LinkLabel();
        ((System.ComponentModel.ISupportInitialize)c_LogoPictureBox).BeginInit();
        c_HostInfoGroupBox.SuspendLayout();
        c_HostInfoLayout.SuspendLayout();
        c_Layout.SuspendLayout();
        c_RuntimeInfoGroupBox.SuspendLayout();
        c_RuntimeInfoLayout.SuspendLayout();
        SuspendLayout();
        // 
        // c_LogoPictureBox
        // 
        c_LogoPictureBox.Image        = ((System.Drawing.Image)resources.GetObject("c_LogoPictureBox.Image"));
        c_LogoPictureBox.InitialImage = null;
        c_LogoPictureBox.Location     = new System.Drawing.Point(12, 12);
        c_LogoPictureBox.Name         = "c_LogoPictureBox";
        c_LogoPictureBox.Size         = new System.Drawing.Size(72, 72);
        c_LogoPictureBox.SizeMode     = System.Windows.Forms.PictureBoxSizeMode.Zoom;
        c_LogoPictureBox.TabIndex     = 0;
        c_LogoPictureBox.TabStop      = false;
        // 
        // c_HeaderLabel
        // 
        c_HeaderLabel.AutoSize  = true;
        c_HeaderLabel.Font      = new System.Drawing.Font("Segoe UI Semibold", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)0));
        c_HeaderLabel.Location  = new System.Drawing.Point(90, 12);
        c_HeaderLabel.Margin    = new System.Windows.Forms.Padding(0, 0, 0, 6);
        c_HeaderLabel.Name      = "c_HeaderLabel";
        c_HeaderLabel.Size      = new System.Drawing.Size(134, 25);
        c_HeaderLabel.TabIndex  = 0;
        c_HeaderLabel.Text      = "Squash 3.0.1.0";
        c_HeaderLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // c_HostInfoGroupBox
        // 
        c_HostInfoGroupBox.Controls.Add(c_HostInfoLayout);
        c_HostInfoGroupBox.Dock     = System.Windows.Forms.DockStyle.Fill;
        c_HostInfoGroupBox.Location = new System.Drawing.Point(0, 3);
        c_HostInfoGroupBox.Margin   = new System.Windows.Forms.Padding(0, 3, 0, 3);
        c_HostInfoGroupBox.Name     = "c_HostInfoGroupBox";
        c_HostInfoGroupBox.Size     = new System.Drawing.Size(311, 77);
        c_HostInfoGroupBox.TabIndex = 2;
        c_HostInfoGroupBox.TabStop  = false;
        c_HostInfoGroupBox.Text     = "Host Info";
        // 
        // c_HostInfoLayout
        // 
        c_HostInfoLayout.ColumnCount = 1;
        c_HostInfoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 128F));
        c_HostInfoLayout.Controls.Add(c_HostArchitectureLabel, 0, 1);
        c_HostInfoLayout.Controls.Add(c_HostOsLabel, 0, 0);
        c_HostInfoLayout.Dock     = System.Windows.Forms.DockStyle.Fill;
        c_HostInfoLayout.Location = new System.Drawing.Point(3, 19);
        c_HostInfoLayout.Name     = "c_HostInfoLayout";
        c_HostInfoLayout.RowCount = 2;
        c_HostInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
        c_HostInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
        c_HostInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
        c_HostInfoLayout.Size     = new System.Drawing.Size(305, 55);
        c_HostInfoLayout.TabIndex = 0;
        // 
        // c_HostArchitectureLabel
        // 
        c_HostArchitectureLabel.Dock      = System.Windows.Forms.DockStyle.Fill;
        c_HostArchitectureLabel.Location  = new System.Drawing.Point(3, 27);
        c_HostArchitectureLabel.Name      = "c_HostArchitectureLabel";
        c_HostArchitectureLabel.Size      = new System.Drawing.Size(299, 28);
        c_HostArchitectureLabel.TabIndex  = 1;
        c_HostArchitectureLabel.Text      = "Architecture: ...";
        c_HostArchitectureLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // c_HostOsLabel
        // 
        c_HostOsLabel.Dock      = System.Windows.Forms.DockStyle.Fill;
        c_HostOsLabel.Location  = new System.Drawing.Point(3, 0);
        c_HostOsLabel.Name      = "c_HostOsLabel";
        c_HostOsLabel.Size      = new System.Drawing.Size(299, 27);
        c_HostOsLabel.TabIndex  = 0;
        c_HostOsLabel.Text      = "Operating System: ...";
        c_HostOsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // c_Layout
        // 
        c_Layout.Anchor      = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
        c_Layout.ColumnCount = 1;
        c_Layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        c_Layout.Controls.Add(c_HostInfoGroupBox, 0, 0);
        c_Layout.Controls.Add(c_RuntimeInfoGroupBox, 0, 1);
        c_Layout.Controls.Add(c_CopyrightLinkLabel, 0, 2);
        c_Layout.Location = new System.Drawing.Point(90, 46);
        c_Layout.Name     = "c_Layout";
        c_Layout.RowCount = 3;
        c_Layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
        c_Layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
        c_Layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
        c_Layout.Size     = new System.Drawing.Size(311, 190);
        c_Layout.TabIndex = 3;
        // 
        // c_RuntimeInfoGroupBox
        // 
        c_RuntimeInfoGroupBox.Controls.Add(c_RuntimeInfoLayout);
        c_RuntimeInfoGroupBox.Dock     = System.Windows.Forms.DockStyle.Fill;
        c_RuntimeInfoGroupBox.Location = new System.Drawing.Point(0, 86);
        c_RuntimeInfoGroupBox.Margin   = new System.Windows.Forms.Padding(0, 3, 0, 3);
        c_RuntimeInfoGroupBox.Name     = "c_RuntimeInfoGroupBox";
        c_RuntimeInfoGroupBox.Size     = new System.Drawing.Size(311, 77);
        c_RuntimeInfoGroupBox.TabIndex = 3;
        c_RuntimeInfoGroupBox.TabStop  = false;
        c_RuntimeInfoGroupBox.Text     = "Runtime Info";
        // 
        // c_RuntimeInfoLayout
        // 
        c_RuntimeInfoLayout.ColumnCount = 1;
        c_RuntimeInfoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        c_RuntimeInfoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
        c_RuntimeInfoLayout.Controls.Add(c_RuntimeIdentifierLabel, 0, 1);
        c_RuntimeInfoLayout.Controls.Add(c_RuntimeDescriptionLabel, 0, 0);
        c_RuntimeInfoLayout.Dock     = System.Windows.Forms.DockStyle.Fill;
        c_RuntimeInfoLayout.Location = new System.Drawing.Point(3, 19);
        c_RuntimeInfoLayout.Name     = "c_RuntimeInfoLayout";
        c_RuntimeInfoLayout.RowCount = 2;
        c_RuntimeInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
        c_RuntimeInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
        c_RuntimeInfoLayout.Size     = new System.Drawing.Size(305, 55);
        c_RuntimeInfoLayout.TabIndex = 0;
        // 
        // c_RuntimeIdentifierLabel
        // 
        c_RuntimeIdentifierLabel.Dock      = System.Windows.Forms.DockStyle.Fill;
        c_RuntimeIdentifierLabel.Location  = new System.Drawing.Point(3, 27);
        c_RuntimeIdentifierLabel.Name      = "c_RuntimeIdentifierLabel";
        c_RuntimeIdentifierLabel.Size      = new System.Drawing.Size(299, 28);
        c_RuntimeIdentifierLabel.TabIndex  = 2;
        c_RuntimeIdentifierLabel.Text      = "Identifier: ...";
        c_RuntimeIdentifierLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // c_RuntimeDescriptionLabel
        // 
        c_RuntimeDescriptionLabel.Dock      = System.Windows.Forms.DockStyle.Fill;
        c_RuntimeDescriptionLabel.Location  = new System.Drawing.Point(3, 0);
        c_RuntimeDescriptionLabel.Name      = "c_RuntimeDescriptionLabel";
        c_RuntimeDescriptionLabel.Size      = new System.Drawing.Size(299, 27);
        c_RuntimeDescriptionLabel.TabIndex  = 1;
        c_RuntimeDescriptionLabel.Text      = "Description: ...";
        c_RuntimeDescriptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // c_CopyrightLinkLabel
        // 
        c_CopyrightLinkLabel.Dock                       = System.Windows.Forms.DockStyle.Fill;
        c_CopyrightLinkLabel.LinkArea                   = new System.Windows.Forms.LinkArea(17, 13);
        c_CopyrightLinkLabel.LinkBehavior               = System.Windows.Forms.LinkBehavior.HoverUnderline;
        c_CopyrightLinkLabel.Location                   = new System.Drawing.Point(3, 166);
        c_CopyrightLinkLabel.Name                       = "c_CopyrightLinkLabel";
        c_CopyrightLinkLabel.Size                       = new System.Drawing.Size(305, 24);
        c_CopyrightLinkLabel.TabIndex                   = 4;
        c_CopyrightLinkLabel.TabStop                    = true;
        c_CopyrightLinkLabel.Text                       = "Copyright © 2026 Caprine Logic";
        c_CopyrightLinkLabel.TextAlign                  = System.Drawing.ContentAlignment.MiddleLeft;
        c_CopyrightLinkLabel.UseCompatibleTextRendering = true;
        // 
        // AboutForm
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize          = new System.Drawing.Size(413, 248);
        Controls.Add(c_Layout);
        Controls.Add(c_HeaderLabel);
        Controls.Add(c_LogoPictureBox);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
        MaximizeBox     = false;
        MinimizeBox     = false;
        ShowIcon        = false;
        ShowInTaskbar   = false;
        StartPosition   = System.Windows.Forms.FormStartPosition.CenterParent;
        Text            = "About Squash";
        ((System.ComponentModel.ISupportInitialize)c_LogoPictureBox).EndInit();
        c_HostInfoGroupBox.ResumeLayout(false);
        c_HostInfoLayout.ResumeLayout(false);
        c_Layout.ResumeLayout(false);
        c_RuntimeInfoGroupBox.ResumeLayout(false);
        c_RuntimeInfoLayout.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.Label c_RuntimeDescriptionLabel;
    private System.Windows.Forms.Label c_RuntimeIdentifierLabel;

    private System.Windows.Forms.TableLayoutPanel c_RuntimeInfoLayout;

    private System.Windows.Forms.Label c_HostArchitectureLabel;

    private System.Windows.Forms.Label c_HostOsLabel;

    private System.Windows.Forms.TableLayoutPanel c_HostInfoLayout;

    private System.Windows.Forms.LinkLabel c_CopyrightLinkLabel;

    private System.Windows.Forms.GroupBox c_RuntimeInfoGroupBox;

    private System.Windows.Forms.TableLayoutPanel c_Layout;

    private System.Windows.Forms.GroupBox c_HostInfoGroupBox;

    private System.Windows.Forms.Label c_HeaderLabel;

    private System.Windows.Forms.PictureBox c_LogoPictureBox;
    #endregion
}

