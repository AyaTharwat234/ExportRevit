namespace EFRvt
{
    partial class ElevationSelector
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
            this.Lvls_comboBox = new System.Windows.Forms.ComboBox();
            this.Input_Lb = new System.Windows.Forms.Label();
            this.BaseElvation_TextBox = new System.Windows.Forms.TextBox();
            this.outputLevel_LB = new System.Windows.Forms.Label();
            this.Lvls_txtBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // Lvls_comboBox
            // 
            this.Lvls_comboBox.FormattingEnabled = true;
            this.Lvls_comboBox.Location = new System.Drawing.Point(125, 25);
            this.Lvls_comboBox.Name = "Lvls_comboBox";
            this.Lvls_comboBox.Size = new System.Drawing.Size(100, 21);
            this.Lvls_comboBox.TabIndex = 9;
            this.Lvls_comboBox.SelectedIndexChanged += new System.EventHandler(this.Lvls_comboBox_SelectedIndexChanged);
            // 
            // Input_Lb
            // 
            this.Input_Lb.AutoSize = true;
            this.Input_Lb.Location = new System.Drawing.Point(10, 28);
            this.Input_Lb.Name = "Input_Lb";
            this.Input_Lb.Size = new System.Drawing.Size(81, 13);
            this.Input_Lb.TabIndex = 8;
            this.Input_Lb.Text = " Base Elevation";
            // 
            // BaseElvation_TextBox
            // 
            this.BaseElvation_TextBox.Location = new System.Drawing.Point(125, 25);
            this.BaseElvation_TextBox.Name = "BaseElvation_TextBox";
            this.BaseElvation_TextBox.Size = new System.Drawing.Size(100, 20);
            this.BaseElvation_TextBox.TabIndex = 7;
            this.BaseElvation_TextBox.TextChanged += new System.EventHandler(this.BaseElvation_TextBox_TextChanged);
            // 
            // outputLevel_LB
            // 
            this.outputLevel_LB.AutoSize = true;
            this.outputLevel_LB.Location = new System.Drawing.Point(10, 29);
            this.outputLevel_LB.Name = "outputLevel_LB";
            this.outputLevel_LB.Size = new System.Drawing.Size(0, 13);
            this.outputLevel_LB.TabIndex = 6;
            // 
            // Lvls_txtBox
            // 
            this.Lvls_txtBox.AutoSize = true;
            this.Lvls_txtBox.Location = new System.Drawing.Point(13, 3);
            this.Lvls_txtBox.Name = "Lvls_txtBox";
            this.Lvls_txtBox.Size = new System.Drawing.Size(111, 17);
            this.Lvls_txtBox.TabIndex = 5;
            this.Lvls_txtBox.Text = "Use Model Levels";
            this.Lvls_txtBox.UseVisualStyleBackColor = true;
            this.Lvls_txtBox.CheckedChanged += new System.EventHandler(this.Lvls_txtBox_CheckedChanged);
            // 
            // ElevationSelector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Lvls_comboBox);
            this.Controls.Add(this.Input_Lb);
            this.Controls.Add(this.BaseElvation_TextBox);
            this.Controls.Add(this.outputLevel_LB);
            this.Controls.Add(this.Lvls_txtBox);
            this.Name = "ElevationSelector";
            this.Size = new System.Drawing.Size(245, 51);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox Lvls_comboBox;
        private System.Windows.Forms.Label Input_Lb;
        private System.Windows.Forms.TextBox BaseElvation_TextBox;
        private System.Windows.Forms.Label outputLevel_LB;
        private System.Windows.Forms.CheckBox Lvls_txtBox;
    }
}
