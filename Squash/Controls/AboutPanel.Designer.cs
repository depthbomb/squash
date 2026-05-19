using System.ComponentModel;

namespace Squash.Controls;

partial class AboutPanel
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

    #region Component Designer generated code
    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        c_HeadingLabel = new Label();
        c_LogoPictureBox = new PictureBox();
        c_VersionLabel = new Label();
        c_HeadingTableLayout = new TableLayoutPanel();
        c_ContentTableLayout = new TableLayoutPanel();
        c_InformationGroupBox = new GroupBox();
        c_InformationTableLayout = new TableLayoutPanel();
        c_RuntimeIdentifierLabel = new Label();
        c_RuntimeDescriptionLabel = new Label();
        c_HostArchLabel = new Label();
        c_HostOsLabel = new Label();
        c_CopyrightLabel = new Label();
        c_LicenseTextBox = new TextBox();
        ((ISupportInitialize)c_LogoPictureBox).BeginInit();
        c_HeadingTableLayout.SuspendLayout();
        c_ContentTableLayout.SuspendLayout();
        c_InformationGroupBox.SuspendLayout();
        c_InformationTableLayout.SuspendLayout();
        SuspendLayout();
        // 
        // c_HeadingLabel
        // 
        c_HeadingLabel.AutoSize = true;
        c_HeadingLabel.Dock = DockStyle.Fill;
        c_HeadingLabel.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        c_HeadingLabel.Location = new Point(6, 6);
        c_HeadingLabel.Margin = new Padding(6);
        c_HeadingLabel.Name = "c_HeadingLabel";
        c_HeadingLabel.Size = new Size(410, 36);
        c_HeadingLabel.TabIndex = 0;
        c_HeadingLabel.Text = "About Squash";
        c_HeadingLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // c_LogoPictureBox
        // 
        c_LogoPictureBox.Image = Resources.Images.logo;
        c_LogoPictureBox.Location = new Point(0, 54);
        c_LogoPictureBox.Margin = new Padding(6);
        c_LogoPictureBox.Name = "c_LogoPictureBox";
        c_LogoPictureBox.Size = new Size(100, 100);
        c_LogoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        c_LogoPictureBox.TabIndex = 1;
        c_LogoPictureBox.TabStop = false;
        // 
        // c_VersionLabel
        // 
        c_VersionLabel.AutoSize = true;
        c_VersionLabel.Dock = DockStyle.Fill;
        c_VersionLabel.ForeColor = SystemColors.ControlDarkDark;
        c_VersionLabel.Location = new Point(425, 0);
        c_VersionLabel.Name = "c_VersionLabel";
        c_VersionLabel.Size = new Size(417, 48);
        c_VersionLabel.TabIndex = 3;
        c_VersionLabel.Text = "3.1.0.0";
        c_VersionLabel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // c_HeadingTableLayout
        // 
        c_HeadingTableLayout.ColumnCount = 2;
        c_HeadingTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        c_HeadingTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        c_HeadingTableLayout.Controls.Add(c_VersionLabel, 1, 0);
        c_HeadingTableLayout.Controls.Add(c_HeadingLabel, 0, 0);
        c_HeadingTableLayout.Dock = DockStyle.Top;
        c_HeadingTableLayout.Location = new Point(0, 0);
        c_HeadingTableLayout.Margin = new Padding(0);
        c_HeadingTableLayout.Name = "c_HeadingTableLayout";
        c_HeadingTableLayout.RowCount = 1;
        c_HeadingTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        c_HeadingTableLayout.Size = new Size(845, 48);
        c_HeadingTableLayout.TabIndex = 3;
        // 
        // c_ContentTableLayout
        // 
        c_ContentTableLayout.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        c_ContentTableLayout.ColumnCount = 1;
        c_ContentTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        c_ContentTableLayout.Controls.Add(c_InformationGroupBox, 0, 0);
        c_ContentTableLayout.Controls.Add(c_CopyrightLabel, 0, 2);
        c_ContentTableLayout.Controls.Add(c_LicenseTextBox, 0, 1);
        c_ContentTableLayout.Location = new Point(109, 54);
        c_ContentTableLayout.Name = "c_ContentTableLayout";
        c_ContentTableLayout.RowCount = 3;
        c_ContentTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
        c_ContentTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        c_ContentTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        c_ContentTableLayout.Size = new Size(733, 499);
        c_ContentTableLayout.TabIndex = 4;
        // 
        // c_InformationGroupBox
        // 
        c_InformationGroupBox.Controls.Add(c_InformationTableLayout);
        c_InformationGroupBox.Dock = DockStyle.Fill;
        c_InformationGroupBox.Location = new Point(3, 3);
        c_InformationGroupBox.Name = "c_InformationGroupBox";
        c_InformationGroupBox.Size = new Size(727, 144);
        c_InformationGroupBox.TabIndex = 0;
        c_InformationGroupBox.TabStop = false;
        c_InformationGroupBox.Text = "Information";
        // 
        // c_InformationTableLayout
        // 
        c_InformationTableLayout.ColumnCount = 1;
        c_InformationTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        c_InformationTableLayout.Controls.Add(c_RuntimeIdentifierLabel, 0, 3);
        c_InformationTableLayout.Controls.Add(c_RuntimeDescriptionLabel, 0, 2);
        c_InformationTableLayout.Controls.Add(c_HostArchLabel, 0, 1);
        c_InformationTableLayout.Controls.Add(c_HostOsLabel, 0, 0);
        c_InformationTableLayout.Dock = DockStyle.Fill;
        c_InformationTableLayout.Location = new Point(3, 19);
        c_InformationTableLayout.Margin = new Padding(0);
        c_InformationTableLayout.Name = "c_InformationTableLayout";
        c_InformationTableLayout.RowCount = 4;
        c_InformationTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        c_InformationTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        c_InformationTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        c_InformationTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        c_InformationTableLayout.Size = new Size(721, 122);
        c_InformationTableLayout.TabIndex = 0;
        // 
        // c_RuntimeIdentifierLabel
        // 
        c_RuntimeIdentifierLabel.AutoSize = true;
        c_RuntimeIdentifierLabel.Dock = DockStyle.Fill;
        c_RuntimeIdentifierLabel.Location = new Point(3, 90);
        c_RuntimeIdentifierLabel.Name = "c_RuntimeIdentifierLabel";
        c_RuntimeIdentifierLabel.Size = new Size(715, 32);
        c_RuntimeIdentifierLabel.TabIndex = 3;
        c_RuntimeIdentifierLabel.Text = "Runtime identifier: ...";
        c_RuntimeIdentifierLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // c_RuntimeDescriptionLabel
        // 
        c_RuntimeDescriptionLabel.AutoSize = true;
        c_RuntimeDescriptionLabel.Dock = DockStyle.Fill;
        c_RuntimeDescriptionLabel.Location = new Point(3, 60);
        c_RuntimeDescriptionLabel.Name = "c_RuntimeDescriptionLabel";
        c_RuntimeDescriptionLabel.Size = new Size(715, 30);
        c_RuntimeDescriptionLabel.TabIndex = 2;
        c_RuntimeDescriptionLabel.Text = "Runtime description: ...";
        c_RuntimeDescriptionLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // c_HostArchLabel
        // 
        c_HostArchLabel.AutoSize = true;
        c_HostArchLabel.Dock = DockStyle.Fill;
        c_HostArchLabel.Location = new Point(3, 30);
        c_HostArchLabel.Name = "c_HostArchLabel";
        c_HostArchLabel.Size = new Size(715, 30);
        c_HostArchLabel.TabIndex = 1;
        c_HostArchLabel.Text = "Architecture: ...";
        c_HostArchLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // c_HostOsLabel
        // 
        c_HostOsLabel.AutoSize = true;
        c_HostOsLabel.Dock = DockStyle.Fill;
        c_HostOsLabel.Location = new Point(3, 0);
        c_HostOsLabel.Name = "c_HostOsLabel";
        c_HostOsLabel.Size = new Size(715, 30);
        c_HostOsLabel.TabIndex = 0;
        c_HostOsLabel.Text = "Operating system: ...";
        c_HostOsLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // c_CopyrightLabel
        // 
        c_CopyrightLabel.AutoSize = true;
        c_CopyrightLabel.Dock = DockStyle.Fill;
        c_CopyrightLabel.ForeColor = SystemColors.ControlDark;
        c_CopyrightLabel.Location = new Point(3, 475);
        c_CopyrightLabel.Name = "c_CopyrightLabel";
        c_CopyrightLabel.Size = new Size(727, 24);
        c_CopyrightLabel.TabIndex = 2;
        c_CopyrightLabel.Text = "Copyright © 2026 Caprine Logic";
        c_CopyrightLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // c_LicenseTextBox
        // 
        c_LicenseTextBox.Dock = DockStyle.Fill;
        c_LicenseTextBox.Location = new Point(3, 153);
        c_LicenseTextBox.Multiline = true;
        c_LicenseTextBox.Name = "c_LicenseTextBox";
        c_LicenseTextBox.ReadOnly = true;
        c_LicenseTextBox.ScrollBars = ScrollBars.Vertical;
        c_LicenseTextBox.Size = new Size(727, 319);
        c_LicenseTextBox.TabIndex = 3;
        // 
        // AboutPanel
        // 
        BackColor = SystemColors.Control;
        Controls.Add(c_ContentTableLayout);
        Controls.Add(c_HeadingTableLayout);
        Controls.Add(c_LogoPictureBox);
        Margin = new Padding(0);
        Name = "AboutPanel";
        Size = new Size(845, 556);
        ((ISupportInitialize)c_LogoPictureBox).EndInit();
        c_HeadingTableLayout.ResumeLayout(false);
        c_HeadingTableLayout.PerformLayout();
        c_ContentTableLayout.ResumeLayout(false);
        c_ContentTableLayout.PerformLayout();
        c_InformationGroupBox.ResumeLayout(false);
        c_InformationTableLayout.ResumeLayout(false);
        c_InformationTableLayout.PerformLayout();
        ResumeLayout(false);
    }

    private System.Windows.Forms.TableLayoutPanel c_HeadingTableLayout;

    private System.Windows.Forms.Label c_VersionLabel;

    private System.Windows.Forms.PictureBox c_LogoPictureBox;

    private System.Windows.Forms.Label c_HeadingLabel;
    #endregion

    private System.Windows.Forms.TableLayoutPanel c_ContentTableLayout;
    private System.Windows.Forms.GroupBox         c_InformationGroupBox;
    private System.Windows.Forms.Label            c_CopyrightLabel;
    private TextBox c_LicenseTextBox;
    private TableLayoutPanel c_InformationTableLayout;
    private Label c_HostOsLabel;
    private Label c_HostArchLabel;
    private Label c_RuntimeDescriptionLabel;
    private Label c_RuntimeIdentifierLabel;
}

