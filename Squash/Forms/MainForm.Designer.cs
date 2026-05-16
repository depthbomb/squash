using Squash.Controls;

namespace Squash.Forms;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
        c_MainButton             = new System.Windows.Forms.Button();
        _controlsTable           = new System.Windows.Forms.TableLayoutPanel();
        c_OutputFileTextBox      = new System.Windows.Forms.TextBox();
        label3                   = new System.Windows.Forms.Label();
        c_InputFileBrowseButton  = new System.Windows.Forms.Button();
        c_OutputFileBrowseButton = new System.Windows.Forms.Button();
        label4                   = new System.Windows.Forms.Label();
        label5                   = new System.Windows.Forms.Label();
        label6                   = new System.Windows.Forms.Label();
        c_QualityPresetComboBox  = new System.Windows.Forms.ComboBox();
        c_InputFileTextBox       = new System.Windows.Forms.TextBox();
        c_TargetSizeInput        = new Squash.Controls.NumericUpDownEx();
        c_ToleranceInput         = new Squash.Controls.NumericUpDownEx();
        c_MaxIterationsInput     = new Squash.Controls.NumericUpDownEx();
        label1                   = new System.Windows.Forms.Label();
        label2                   = new System.Windows.Forms.Label();
        c_StatusStrip            = new System.Windows.Forms.StatusStrip();
        c_StatusStripAboutLink   = new System.Windows.Forms.ToolStripStatusLabel();
        _controlsTable.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)c_TargetSizeInput).BeginInit();
        ((System.ComponentModel.ISupportInitialize)c_ToleranceInput).BeginInit();
        ((System.ComponentModel.ISupportInitialize)c_MaxIterationsInput).BeginInit();
        c_StatusStrip.SuspendLayout();
        SuspendLayout();
        // 
        // c_MainButton
        // 
        c_MainButton.Anchor                  = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
        c_MainButton.Enabled                 = false;
        c_MainButton.Location                = new System.Drawing.Point(12, 205);
        c_MainButton.Name                    = "c_MainButton";
        c_MainButton.Size                    = new System.Drawing.Size(529, 24);
        c_MainButton.TabIndex                = 7;
        c_MainButton.Text                    = "&Squash it!";
        c_MainButton.UseVisualStyleBackColor = true;
        // 
        // _controlsTable
        // 
        _controlsTable.Anchor      = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
        _controlsTable.ColumnCount = 3;
        _controlsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 23.753666F));
        _controlsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 76.24633F));
        _controlsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
        _controlsTable.Controls.Add(c_OutputFileTextBox, 1, 1);
        _controlsTable.Controls.Add(label3, 0, 2);
        _controlsTable.Controls.Add(c_InputFileBrowseButton, 2, 0);
        _controlsTable.Controls.Add(c_OutputFileBrowseButton, 2, 1);
        _controlsTable.Controls.Add(label4, 0, 3);
        _controlsTable.Controls.Add(label5, 0, 4);
        _controlsTable.Controls.Add(label6, 0, 5);
        _controlsTable.Controls.Add(c_QualityPresetComboBox, 1, 5);
        _controlsTable.Controls.Add(c_InputFileTextBox, 1, 0);
        _controlsTable.Controls.Add(c_TargetSizeInput, 1, 2);
        _controlsTable.Controls.Add(c_ToleranceInput, 1, 3);
        _controlsTable.Controls.Add(c_MaxIterationsInput, 1, 4);
        _controlsTable.Controls.Add(label1, 0, 1);
        _controlsTable.Controls.Add(label2, 0, 0);
        _controlsTable.Location = new System.Drawing.Point(12, 12);
        _controlsTable.Name     = "_controlsTable";
        _controlsTable.RowCount = 6;
        _controlsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.666664F));
        _controlsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.666664F));
        _controlsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.666664F));
        _controlsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.666664F));
        _controlsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.666664F));
        _controlsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.666664F));
        _controlsTable.Size     = new System.Drawing.Size(529, 187);
        _controlsTable.TabIndex = 8;
        // 
        // c_OutputFileTextBox
        // 
        c_OutputFileTextBox.Anchor   = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
        c_OutputFileTextBox.Enabled  = false;
        c_OutputFileTextBox.Location = new System.Drawing.Point(104, 35);
        c_OutputFileTextBox.Name     = "c_OutputFileTextBox";
        c_OutputFileTextBox.Size     = new System.Drawing.Size(321, 23);
        c_OutputFileTextBox.TabIndex = 11;
        c_OutputFileTextBox.WordWrap = false;
        // 
        // label3
        // 
        label3.Anchor    = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
        label3.AutoSize  = true;
        label3.BackColor = System.Drawing.Color.Transparent;
        label3.Location  = new System.Drawing.Point(3, 70);
        label3.Name      = "label3";
        label3.Size      = new System.Drawing.Size(95, 15);
        label3.TabIndex  = 3;
        label3.Text      = "Target size";
        label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // c_InputFileBrowseButton
        // 
        c_InputFileBrowseButton.Anchor                  = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
        c_InputFileBrowseButton.Location                = new System.Drawing.Point(431, 3);
        c_InputFileBrowseButton.Name                    = "c_InputFileBrowseButton";
        c_InputFileBrowseButton.Size                    = new System.Drawing.Size(95, 24);
        c_InputFileBrowseButton.TabIndex                = 4;
        c_InputFileBrowseButton.Tag                     = "toggleable";
        c_InputFileBrowseButton.Text                    = "Browse...";
        c_InputFileBrowseButton.UseVisualStyleBackColor = true;
        // 
        // c_OutputFileBrowseButton
        // 
        c_OutputFileBrowseButton.Anchor                  = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
        c_OutputFileBrowseButton.Enabled                 = false;
        c_OutputFileBrowseButton.Location                = new System.Drawing.Point(431, 34);
        c_OutputFileBrowseButton.Name                    = "c_OutputFileBrowseButton";
        c_OutputFileBrowseButton.Size                    = new System.Drawing.Size(95, 24);
        c_OutputFileBrowseButton.TabIndex                = 5;
        c_OutputFileBrowseButton.Tag                     = "toggleable";
        c_OutputFileBrowseButton.Text                    = "Browse...";
        c_OutputFileBrowseButton.UseVisualStyleBackColor = true;
        // 
        // label4
        // 
        label4.Anchor    = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
        label4.AutoSize  = true;
        label4.BackColor = System.Drawing.Color.Transparent;
        label4.Location  = new System.Drawing.Point(3, 101);
        label4.Name      = "label4";
        label4.Size      = new System.Drawing.Size(95, 15);
        label4.TabIndex  = 6;
        label4.Text      = "Tolerance";
        label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // label5
        // 
        label5.Anchor    = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
        label5.AutoSize  = true;
        label5.BackColor = System.Drawing.Color.Transparent;
        label5.Location  = new System.Drawing.Point(3, 132);
        label5.Name      = "label5";
        label5.Size      = new System.Drawing.Size(95, 15);
        label5.TabIndex  = 7;
        label5.Text      = "Max iterations";
        label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // label6
        // 
        label6.Anchor    = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
        label6.AutoSize  = true;
        label6.BackColor = System.Drawing.Color.Transparent;
        label6.Location  = new System.Drawing.Point(3, 163);
        label6.Name      = "label6";
        label6.Size      = new System.Drawing.Size(95, 15);
        label6.TabIndex  = 8;
        label6.Text      = "Quality preset";
        label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // c_QualityPresetComboBox
        // 
        c_QualityPresetComboBox.Anchor            = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
        c_QualityPresetComboBox.DropDownStyle     = System.Windows.Forms.ComboBoxStyle.DropDownList;
        c_QualityPresetComboBox.FlatStyle         = System.Windows.Forms.FlatStyle.System;
        c_QualityPresetComboBox.FormattingEnabled = true;
        c_QualityPresetComboBox.Items.AddRange(new object[] { "1. Fast, decent quality", "2. Slow, better quality (recommended)", "3. Very slow, better quality", "4. Absurdly slow, better quality" });
        c_QualityPresetComboBox.Location = new System.Drawing.Point(104, 159);
        c_QualityPresetComboBox.Name     = "c_QualityPresetComboBox";
        c_QualityPresetComboBox.Size     = new System.Drawing.Size(321, 23);
        c_QualityPresetComboBox.TabIndex = 9;
        c_QualityPresetComboBox.Tag      = "toggleable";
        // 
        // c_InputFileTextBox
        // 
        c_InputFileTextBox.Anchor          = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
        c_InputFileTextBox.Enabled         = false;
        c_InputFileTextBox.Location        = new System.Drawing.Point(104, 4);
        c_InputFileTextBox.Name            = "c_InputFileTextBox";
        c_InputFileTextBox.PlaceholderText = "Click browse or drag a video file into this window";
        c_InputFileTextBox.Size            = new System.Drawing.Size(321, 23);
        c_InputFileTextBox.TabIndex        = 10;
        c_InputFileTextBox.WordWrap        = false;
        // 
        // c_TargetSizeInput
        // 
        c_TargetSizeInput.Anchor      = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
        c_TargetSizeInput.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        c_TargetSizeInput.Location    = new System.Drawing.Point(104, 66);
        c_TargetSizeInput.Minimum     = new decimal(new int[] { 1, 0, 0, 0 });
        c_TargetSizeInput.Name        = "c_TargetSizeInput";
        c_TargetSizeInput.Size        = new System.Drawing.Size(321, 23);
        c_TargetSizeInput.Suffix      = " MB";
        c_TargetSizeInput.TabIndex    = 12;
        c_TargetSizeInput.Tag         = "toggleable";
        c_TargetSizeInput.TextAlign   = System.Windows.Forms.HorizontalAlignment.Right;
        c_TargetSizeInput.Value       = new decimal(new int[] { 10, 0, 0, 0 });
        // 
        // c_ToleranceInput
        // 
        c_ToleranceInput.Anchor        = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
        c_ToleranceInput.BorderStyle   = System.Windows.Forms.BorderStyle.FixedSingle;
        c_ToleranceInput.DecimalPlaces = 1;
        c_ToleranceInput.Increment     = new decimal(new int[] { 5, 0, 0, 65536 });
        c_ToleranceInput.Location      = new System.Drawing.Point(104, 97);
        c_ToleranceInput.Name          = "c_ToleranceInput";
        c_ToleranceInput.Size          = new System.Drawing.Size(321, 23);
        c_ToleranceInput.Suffix        = "%";
        c_ToleranceInput.TabIndex      = 13;
        c_ToleranceInput.Tag           = "toggleable";
        c_ToleranceInput.TextAlign     = System.Windows.Forms.HorizontalAlignment.Right;
        c_ToleranceInput.Value         = new decimal(new int[] { 2, 0, 0, 0 });
        // 
        // c_MaxIterationsInput
        // 
        c_MaxIterationsInput.Anchor      = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
        c_MaxIterationsInput.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        c_MaxIterationsInput.Location    = new System.Drawing.Point(104, 128);
        c_MaxIterationsInput.Minimum     = new decimal(new int[] { 1, 0, 0, 0 });
        c_MaxIterationsInput.Name        = "c_MaxIterationsInput";
        c_MaxIterationsInput.Size        = new System.Drawing.Size(321, 23);
        c_MaxIterationsInput.TabIndex    = 14;
        c_MaxIterationsInput.Tag         = "toggleable";
        c_MaxIterationsInput.TextAlign   = System.Windows.Forms.HorizontalAlignment.Right;
        c_MaxIterationsInput.Value       = new decimal(new int[] { 15, 0, 0, 0 });
        // 
        // label1
        // 
        label1.Anchor    = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
        label1.AutoSize  = true;
        label1.BackColor = System.Drawing.Color.Transparent;
        label1.Location  = new System.Drawing.Point(3, 39);
        label1.Name      = "label1";
        label1.Size      = new System.Drawing.Size(95, 15);
        label1.TabIndex  = 2;
        label1.Text      = "Output file";
        label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // label2
        // 
        label2.Anchor    = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
        label2.AutoSize  = true;
        label2.BackColor = System.Drawing.Color.Transparent;
        label2.Location  = new System.Drawing.Point(3, 8);
        label2.Name      = "label2";
        label2.Size      = new System.Drawing.Size(95, 15);
        label2.TabIndex  = 15;
        label2.Text      = "Input file";
        label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // c_StatusStrip
        // 
        c_StatusStrip.GripMargin = new System.Windows.Forms.Padding(0);
        c_StatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { c_StatusStripAboutLink });
        c_StatusStrip.Location   = new System.Drawing.Point(0, 236);
        c_StatusStrip.Name       = "c_StatusStrip";
        c_StatusStrip.Size       = new System.Drawing.Size(553, 22);
        c_StatusStrip.SizingGrip = false;
        c_StatusStrip.TabIndex   = 9;
        // 
        // c_StatusStripAboutLink
        // 
        c_StatusStripAboutLink.IsLink       = true;
        c_StatusStripAboutLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
        c_StatusStripAboutLink.Name         = "c_StatusStripAboutLink";
        c_StatusStripAboutLink.Size         = new System.Drawing.Size(40, 17);
        c_StatusStripAboutLink.Text         = "About";
        c_StatusStripAboutLink.TextAlign    = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // MainForm
        // 
        AllowDrop           = true;
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize          = new System.Drawing.Size(553, 258);
        Controls.Add(c_StatusStrip);
        Controls.Add(_controlsTable);
        Controls.Add(c_MainButton);
        FormBorderStyle       =  System.Windows.Forms.FormBorderStyle.FixedSingle;
        FormScreenCaptureMode =  System.Windows.Forms.ScreenCaptureMode.HideWindow;
        Icon                  =  ((System.Drawing.Icon)resources.GetObject("$this.Icon"));
        MaximizeBox           =  false;
        SizeGripStyle         =  System.Windows.Forms.SizeGripStyle.Hide;
        StartPosition         =  System.Windows.Forms.FormStartPosition.CenterScreen;
        Text                  =  "Squash";
        DragDrop              += OnDragDrop;
        DragEnter             += OnDragEnter;
        _controlsTable.ResumeLayout(false);
        _controlsTable.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)c_TargetSizeInput).EndInit();
        ((System.ComponentModel.ISupportInitialize)c_ToleranceInput).EndInit();
        ((System.ComponentModel.ISupportInitialize)c_MaxIterationsInput).EndInit();
        c_StatusStrip.ResumeLayout(false);
        c_StatusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.ToolStripStatusLabel c_StatusStripAboutLink;

    private System.Windows.Forms.StatusStrip c_StatusStrip;

    private System.Windows.Forms.Button c_MainButton;
    #endregion

    private System.Windows.Forms.TableLayoutPanel _controlsTable;
    private System.Windows.Forms.Label            label2;
    private System.Windows.Forms.Label            label1;
    private System.Windows.Forms.Label            label3;
    private System.Windows.Forms.Button           c_InputFileBrowseButton;
    private System.Windows.Forms.Button           c_OutputFileBrowseButton;
    private System.Windows.Forms.Label            label4;
    private System.Windows.Forms.Label            label5;
    private System.Windows.Forms.Label            label6;
    private System.Windows.Forms.ComboBox         c_QualityPresetComboBox;
    private System.Windows.Forms.TextBox          c_InputFileTextBox;
    private System.Windows.Forms.TextBox          c_OutputFileTextBox;
    private Squash.Controls.NumericUpDownEx       c_TargetSizeInput;
    private Squash.Controls.NumericUpDownEx       c_ToleranceInput;
    private Squash.Controls.NumericUpDownEx       c_MaxIterationsInput;
}
