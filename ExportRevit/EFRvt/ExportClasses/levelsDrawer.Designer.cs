namespace EFRvt
{
    partial class levelsDrawer
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
            this.NextFloor_Label = new System.Windows.Forms.Label();
            this.FraminLevel_Label = new System.Windows.Forms.Label();
            this.TopPlate_Label = new System.Windows.Forms.Label();
            this.BaseLevel_label = new System.Windows.Forms.Label();
            this.Sheathing_thick_label = new System.Windows.Forms.Label();
            this.Framing_thick_Label = new System.Windows.Forms.Label();
            this.Plate_Height_Label = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // NextFloor_Label
            // 
            this.NextFloor_Label.AutoSize = true;
            this.NextFloor_Label.Location = new System.Drawing.Point(4, 12);
            this.NextFloor_Label.Name = "NextFloor_Label";
            this.NextFloor_Label.Size = new System.Drawing.Size(13, 13);
            this.NextFloor_Label.TabIndex = 0;
            this.NextFloor_Label.Text = "0";
            // 
            // FraminLevel_Label
            // 
            this.FraminLevel_Label.AutoSize = true;
            this.FraminLevel_Label.Location = new System.Drawing.Point(4, 49);
            this.FraminLevel_Label.Name = "FraminLevel_Label";
            this.FraminLevel_Label.Size = new System.Drawing.Size(13, 13);
            this.FraminLevel_Label.TabIndex = 1;
            this.FraminLevel_Label.Text = "1";
            // 
            // TopPlate_Label
            // 
            this.TopPlate_Label.AutoSize = true;
            this.TopPlate_Label.Location = new System.Drawing.Point(7, 82);
            this.TopPlate_Label.Name = "TopPlate_Label";
            this.TopPlate_Label.Size = new System.Drawing.Size(13, 13);
            this.TopPlate_Label.TabIndex = 2;
            this.TopPlate_Label.Text = "2";
            // 
            // BaseLevel_label
            // 
            this.BaseLevel_label.AutoSize = true;
            this.BaseLevel_label.Location = new System.Drawing.Point(4, 123);
            this.BaseLevel_label.Name = "BaseLevel_label";
            this.BaseLevel_label.Size = new System.Drawing.Size(13, 13);
            this.BaseLevel_label.TabIndex = 3;
            this.BaseLevel_label.Text = "3";
            // 
            // Sheathing_thick_label
            // 
            this.Sheathing_thick_label.AutoSize = true;
            this.Sheathing_thick_label.Location = new System.Drawing.Point(84, 31);
            this.Sheathing_thick_label.Name = "Sheathing_thick_label";
            this.Sheathing_thick_label.Size = new System.Drawing.Size(19, 13);
            this.Sheathing_thick_label.TabIndex = 4;
            this.Sheathing_thick_label.Text = "01";
            // 
            // Framing_thick_Label
            // 
            this.Framing_thick_Label.AutoSize = true;
            this.Framing_thick_Label.Location = new System.Drawing.Point(87, 62);
            this.Framing_thick_Label.Name = "Framing_thick_Label";
            this.Framing_thick_Label.Size = new System.Drawing.Size(19, 13);
            this.Framing_thick_Label.TabIndex = 5;
            this.Framing_thick_Label.Text = "12";
            // 
            // Plate_Height_Label
            // 
            this.Plate_Height_Label.AutoSize = true;
            this.Plate_Height_Label.Location = new System.Drawing.Point(84, 102);
            this.Plate_Height_Label.Name = "Plate_Height_Label";
            this.Plate_Height_Label.Size = new System.Drawing.Size(19, 13);
            this.Plate_Height_Label.TabIndex = 6;
            this.Plate_Height_Label.Text = "23";
            // 
            // levelsDrawer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Plate_Height_Label);
            this.Controls.Add(this.Framing_thick_Label);
            this.Controls.Add(this.Sheathing_thick_label);
            this.Controls.Add(this.BaseLevel_label);
            this.Controls.Add(this.TopPlate_Label);
            this.Controls.Add(this.FraminLevel_Label);
            this.Controls.Add(this.NextFloor_Label);
            this.Name = "levelsDrawer";
            this.Size = new System.Drawing.Size(125, 185);
            this.Load += new System.EventHandler(this.LevelsDrawer_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.LevelsDrawer_Paint);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label NextFloor_Label;
        private System.Windows.Forms.Label FraminLevel_Label;
        private System.Windows.Forms.Label TopPlate_Label;
        private System.Windows.Forms.Label BaseLevel_label;
        private System.Windows.Forms.Label Sheathing_thick_label;
        private System.Windows.Forms.Label Framing_thick_Label;
        private System.Windows.Forms.Label Plate_Height_Label;
    }
}
