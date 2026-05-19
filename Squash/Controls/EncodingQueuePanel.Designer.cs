namespace Squash.Controls
{
    partial class EncodingQueuePanel
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            _controlsTable = new TableLayoutPanel();
            c_OutputFileTextBox = new TextBox();
            label3 = new Label();
            c_InputFileBrowseButton = new Button();
            c_OutputFileBrowseButton = new Button();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            c_QualityPresetComboBox = new ComboBox();
            c_InputFileTextBox = new TextBox();
            c_TargetSizeInput = new NumericUpDownEx();
            c_ToleranceInput = new NumericUpDownEx();
            c_MaxIterationsInput = new NumericUpDownEx();
            label1 = new Label();
            label2 = new Label();
            c_MainButton = new Button();
            _controlsTable.SuspendLayout();
            ((ISupportInitialize)c_TargetSizeInput).BeginInit();
            ((ISupportInitialize)c_ToleranceInput).BeginInit();
            ((ISupportInitialize)c_MaxIterationsInput).BeginInit();
            SuspendLayout();
            // 
            // _controlsTable
            // 
            _controlsTable.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _controlsTable.ColumnCount = 3;
            _controlsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23.7536659F));
            _controlsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 76.24633F));
            _controlsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
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
            _controlsTable.Location = new Point(3, 3);
            _controlsTable.Name = "_controlsTable";
            _controlsTable.RowCount = 6;
            _controlsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666641F));
            _controlsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666641F));
            _controlsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666641F));
            _controlsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666641F));
            _controlsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666641F));
            _controlsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666641F));
            _controlsTable.Size = new Size(807, 288);
            _controlsTable.TabIndex = 9;
            // 
            // c_OutputFileTextBox
            // 
            c_OutputFileTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            c_OutputFileTextBox.Enabled = false;
            c_OutputFileTextBox.Location = new Point(170, 59);
            c_OutputFileTextBox.Name = "c_OutputFileTextBox";
            c_OutputFileTextBox.Size = new Size(533, 23);
            c_OutputFileTextBox.TabIndex = 11;
            c_OutputFileTextBox.WordWrap = false;
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            label3.AutoSize = true;
            label3.BackColor = Color.Transparent;
            label3.Location = new Point(3, 110);
            label3.Name = "label3";
            label3.Size = new Size(161, 15);
            label3.TabIndex = 3;
            label3.Text = "Target size";
            label3.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // c_InputFileBrowseButton
            // 
            c_InputFileBrowseButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            c_InputFileBrowseButton.Location = new Point(709, 11);
            c_InputFileBrowseButton.Name = "c_InputFileBrowseButton";
            c_InputFileBrowseButton.Size = new Size(95, 24);
            c_InputFileBrowseButton.TabIndex = 4;
            c_InputFileBrowseButton.Tag = "toggleable";
            c_InputFileBrowseButton.Text = "Browse...";
            c_InputFileBrowseButton.UseVisualStyleBackColor = true;
            // 
            // c_OutputFileBrowseButton
            // 
            c_OutputFileBrowseButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            c_OutputFileBrowseButton.Enabled = false;
            c_OutputFileBrowseButton.Location = new Point(709, 58);
            c_OutputFileBrowseButton.Name = "c_OutputFileBrowseButton";
            c_OutputFileBrowseButton.Size = new Size(95, 24);
            c_OutputFileBrowseButton.TabIndex = 5;
            c_OutputFileBrowseButton.Tag = "toggleable";
            c_OutputFileBrowseButton.Text = "Browse...";
            c_OutputFileBrowseButton.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            label4.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            label4.AutoSize = true;
            label4.BackColor = Color.Transparent;
            label4.Location = new Point(3, 157);
            label4.Name = "label4";
            label4.Size = new Size(161, 15);
            label4.TabIndex = 6;
            label4.Text = "Tolerance";
            label4.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label5
            // 
            label5.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            label5.AutoSize = true;
            label5.BackColor = Color.Transparent;
            label5.Location = new Point(3, 204);
            label5.Name = "label5";
            label5.Size = new Size(161, 15);
            label5.TabIndex = 7;
            label5.Text = "Max iterations";
            label5.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label6
            // 
            label6.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            label6.AutoSize = true;
            label6.BackColor = Color.Transparent;
            label6.Location = new Point(3, 254);
            label6.Name = "label6";
            label6.Size = new Size(161, 15);
            label6.TabIndex = 8;
            label6.Text = "Quality preset";
            label6.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // c_QualityPresetComboBox
            // 
            c_QualityPresetComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            c_QualityPresetComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            c_QualityPresetComboBox.FlatStyle = FlatStyle.System;
            c_QualityPresetComboBox.FormattingEnabled = true;
            c_QualityPresetComboBox.Items.AddRange(new object[] { "1. Fast, decent quality", "2. Slow, better quality (recommended)", "3. Very slow, better quality", "4. Absurdly slow, better quality" });
            c_QualityPresetComboBox.Location = new Point(170, 250);
            c_QualityPresetComboBox.Name = "c_QualityPresetComboBox";
            c_QualityPresetComboBox.Size = new Size(533, 23);
            c_QualityPresetComboBox.TabIndex = 9;
            c_QualityPresetComboBox.Tag = "toggleable";
            // 
            // c_InputFileTextBox
            // 
            c_InputFileTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            c_InputFileTextBox.Enabled = false;
            c_InputFileTextBox.Location = new Point(170, 12);
            c_InputFileTextBox.Name = "c_InputFileTextBox";
            c_InputFileTextBox.PlaceholderText = "Click browse or drag a video file into this window";
            c_InputFileTextBox.Size = new Size(533, 23);
            c_InputFileTextBox.TabIndex = 10;
            c_InputFileTextBox.WordWrap = false;
            // 
            // c_TargetSizeInput
            // 
            c_TargetSizeInput.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            c_TargetSizeInput.BorderStyle = BorderStyle.FixedSingle;
            c_TargetSizeInput.Location = new Point(170, 106);
            c_TargetSizeInput.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            c_TargetSizeInput.Name = "c_TargetSizeInput";
            c_TargetSizeInput.Size = new Size(533, 23);
            c_TargetSizeInput.Suffix = " MB";
            c_TargetSizeInput.TabIndex = 12;
            c_TargetSizeInput.Tag = "toggleable";
            c_TargetSizeInput.TextAlign = HorizontalAlignment.Right;
            c_TargetSizeInput.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // c_ToleranceInput
            // 
            c_ToleranceInput.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            c_ToleranceInput.BorderStyle = BorderStyle.FixedSingle;
            c_ToleranceInput.DecimalPlaces = 1;
            c_ToleranceInput.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            c_ToleranceInput.Location = new Point(170, 153);
            c_ToleranceInput.Name = "c_ToleranceInput";
            c_ToleranceInput.Size = new Size(533, 23);
            c_ToleranceInput.Suffix = "%";
            c_ToleranceInput.TabIndex = 13;
            c_ToleranceInput.Tag = "toggleable";
            c_ToleranceInput.TextAlign = HorizontalAlignment.Right;
            c_ToleranceInput.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // c_MaxIterationsInput
            // 
            c_MaxIterationsInput.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            c_MaxIterationsInput.BorderStyle = BorderStyle.FixedSingle;
            c_MaxIterationsInput.Location = new Point(170, 200);
            c_MaxIterationsInput.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            c_MaxIterationsInput.Name = "c_MaxIterationsInput";
            c_MaxIterationsInput.Size = new Size(533, 23);
            c_MaxIterationsInput.TabIndex = 14;
            c_MaxIterationsInput.Tag = "toggleable";
            c_MaxIterationsInput.TextAlign = HorizontalAlignment.Right;
            c_MaxIterationsInput.Value = new decimal(new int[] { 15, 0, 0, 0 });
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            label1.AutoSize = true;
            label1.BackColor = Color.Transparent;
            label1.Location = new Point(3, 63);
            label1.Name = "label1";
            label1.Size = new Size(161, 15);
            label1.TabIndex = 2;
            label1.Text = "Output file";
            label1.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            label2.AutoSize = true;
            label2.BackColor = Color.Transparent;
            label2.Location = new Point(3, 16);
            label2.Name = "label2";
            label2.Size = new Size(161, 15);
            label2.TabIndex = 15;
            label2.Text = "Input file";
            label2.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // c_MainButton
            // 
            c_MainButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            c_MainButton.Location = new Point(3, 453);
            c_MainButton.Name = "c_MainButton";
            c_MainButton.Size = new Size(807, 32);
            c_MainButton.TabIndex = 10;
            c_MainButton.Text = "&Squash it!";
            c_MainButton.UseVisualStyleBackColor = true;
            // 
            // EncodingQueuePanel
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(c_MainButton);
            Controls.Add(_controlsTable);
            Name = "EncodingQueuePanel";
            Size = new Size(813, 488);
            _controlsTable.ResumeLayout(false);
            _controlsTable.PerformLayout();
            ((ISupportInitialize)c_TargetSizeInput).EndInit();
            ((ISupportInitialize)c_ToleranceInput).EndInit();
            ((ISupportInitialize)c_MaxIterationsInput).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel _controlsTable;
        private TextBox c_OutputFileTextBox;
        private Label label3;
        private Button c_InputFileBrowseButton;
        private Button c_OutputFileBrowseButton;
        private Label label4;
        private Label label5;
        private Label label6;
        private ComboBox c_QualityPresetComboBox;
        private TextBox c_InputFileTextBox;
        private NumericUpDownEx c_TargetSizeInput;
        private NumericUpDownEx c_ToleranceInput;
        private NumericUpDownEx c_MaxIterationsInput;
        private Label label1;
        private Label label2;
        private Button c_MainButton;
    }
}
