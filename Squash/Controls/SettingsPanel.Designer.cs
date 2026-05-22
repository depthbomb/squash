namespace Squash.Controls
{
    partial class SettingsPanel
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
            c_SaveButton = new Button();
            c_EnableNotificationsCheckBox = new CheckBox();
            c_AllowMultipleInstancesCheckBox = new CheckBox();
            c_ResetButton = new Button();
            c_EnableAdditionalQualityPresetsCheckBox = new CheckBox();
            SuspendLayout();
            // 
            // c_SaveButton
            // 
            c_SaveButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            c_SaveButton.Location = new Point(6, 422);
            c_SaveButton.Name = "c_SaveButton";
            c_SaveButton.Size = new Size(75, 23);
            c_SaveButton.TabIndex = 1;
            c_SaveButton.Text = "&Save";
            c_SaveButton.TextImageRelation = TextImageRelation.ImageBeforeText;
            c_SaveButton.UseVisualStyleBackColor = true;
            // 
            // c_EnableNotificationsCheckBox
            // 
            c_EnableNotificationsCheckBox.AutoSize = true;
            c_EnableNotificationsCheckBox.Location = new Point(6, 6);
            c_EnableNotificationsCheckBox.Margin = new Padding(6);
            c_EnableNotificationsCheckBox.Name = "c_EnableNotificationsCheckBox";
            c_EnableNotificationsCheckBox.Size = new Size(130, 19);
            c_EnableNotificationsCheckBox.TabIndex = 2;
            c_EnableNotificationsCheckBox.Text = "Enable notifications";
            c_EnableNotificationsCheckBox.UseVisualStyleBackColor = true;
            // 
            // c_AllowMultipleInstancesCheckBox
            // 
            c_AllowMultipleInstancesCheckBox.AutoSize = true;
            c_AllowMultipleInstancesCheckBox.Location = new Point(6, 37);
            c_AllowMultipleInstancesCheckBox.Margin = new Padding(6);
            c_AllowMultipleInstancesCheckBox.Name = "c_AllowMultipleInstancesCheckBox";
            c_AllowMultipleInstancesCheckBox.Size = new Size(244, 19);
            c_AllowMultipleInstancesCheckBox.TabIndex = 3;
            c_AllowMultipleInstancesCheckBox.Text = "Allow multiple instances (requires restart)";
            c_AllowMultipleInstancesCheckBox.UseVisualStyleBackColor = true;
            // 
            // c_ResetButton
            // 
            c_ResetButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            c_ResetButton.Location = new Point(87, 422);
            c_ResetButton.Name = "c_ResetButton";
            c_ResetButton.Size = new Size(75, 23);
            c_ResetButton.TabIndex = 4;
            c_ResetButton.Text = "Reset";
            c_ResetButton.UseVisualStyleBackColor = true;
            // 
            // c_EnableAdditionalQualityPresetsCheckBox
            // 
            c_EnableAdditionalQualityPresetsCheckBox.AutoSize = true;
            c_EnableAdditionalQualityPresetsCheckBox.Location = new Point(6, 68);
            c_EnableAdditionalQualityPresetsCheckBox.Margin = new Padding(6);
            c_EnableAdditionalQualityPresetsCheckBox.Name = "c_EnableAdditionalQualityPresetsCheckBox";
            c_EnableAdditionalQualityPresetsCheckBox.Size = new Size(285, 19);
            c_EnableAdditionalQualityPresetsCheckBox.TabIndex = 5;
            c_EnableAdditionalQualityPresetsCheckBox.Text = "Enable additional quality presets (requires restart)";
            c_EnableAdditionalQualityPresetsCheckBox.UseVisualStyleBackColor = true;
            // 
            // SettingsPanel
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(c_EnableAdditionalQualityPresetsCheckBox);
            Controls.Add(c_ResetButton);
            Controls.Add(c_AllowMultipleInstancesCheckBox);
            Controls.Add(c_EnableNotificationsCheckBox);
            Controls.Add(c_SaveButton);
            Name = "SettingsPanel";
            Size = new Size(761, 448);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Button c_SaveButton;
        private CheckBox c_EnableNotificationsCheckBox;
        private CheckBox c_AllowMultipleInstancesCheckBox;
        private Button c_ResetButton;
        private CheckBox c_EnableAdditionalQualityPresetsCheckBox;
    }
}
