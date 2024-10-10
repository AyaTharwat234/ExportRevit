namespace EFRvt
{
    partial class frmMain
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnExit = new System.Windows.Forms.Button();
            this.btnBrowseFileName = new System.Windows.Forms.Button();
            this.txtFileName = new System.Windows.Forms.TextBox();
            this.lblFileName = new System.Windows.Forms.Label();
            this.btnRunEfr = new System.Windows.Forms.Button();
            this.txtEframerFile = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtRevitFile = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtVersion = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtAvailabe = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(431, 150);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(53, 23);
            this.btnExit.TabIndex = 12;
            this.btnExit.Text = "E&xit";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // btnBrowseFileName
            // 
            this.btnBrowseFileName.Location = new System.Drawing.Point(416, 69);
            this.btnBrowseFileName.Name = "btnBrowseFileName";
            this.btnBrowseFileName.Size = new System.Drawing.Size(68, 23);
            this.btnBrowseFileName.TabIndex = 10;
            this.btnBrowseFileName.Text = "&Browse";
            this.btnBrowseFileName.UseVisualStyleBackColor = true;
            this.btnBrowseFileName.Click += new System.EventHandler(this.btnBrowseFileName_Click);
            // 
            // txtFileName
            // 
            this.txtFileName.Location = new System.Drawing.Point(162, 71);
            this.txtFileName.Name = "txtFileName";
            this.txtFileName.ReadOnly = true;
            this.txtFileName.Size = new System.Drawing.Size(250, 20);
            this.txtFileName.TabIndex = 9;
            // 
            // lblFileName
            // 
            this.lblFileName.AutoSize = true;
            this.lblFileName.Location = new System.Drawing.Point(8, 74);
            this.lblFileName.Name = "lblFileName";
            this.lblFileName.Size = new System.Drawing.Size(124, 13);
            this.lblFileName.TabIndex = 8;
            this.lblFileName.Text = "eFramer Model File (efx)";
            // 
            // btnRunEfr
            // 
            this.btnRunEfr.Location = new System.Drawing.Point(342, 150);
            this.btnRunEfr.Name = "btnRunEfr";
            this.btnRunEfr.Size = new System.Drawing.Size(83, 23);
            this.btnRunEfr.TabIndex = 7;
            this.btnRunEfr.Text = "&Run eFramer";
            this.btnRunEfr.UseVisualStyleBackColor = true;
            this.btnRunEfr.Click += new System.EventHandler(this.btnRunEfr_Click);
            // 
            // txtEframerFile
            // 
            this.txtEframerFile.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtEframerFile.ForeColor = System.Drawing.Color.Red;
            this.txtEframerFile.Location = new System.Drawing.Point(162, 97);
            this.txtEframerFile.Name = "txtEframerFile";
            this.txtEframerFile.ReadOnly = true;
            this.txtEframerFile.Size = new System.Drawing.Size(322, 21);
            this.txtEframerFile.TabIndex = 15;
            this.txtEframerFile.Visible = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 100);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(122, 13);
            this.label1.TabIndex = 14;
            this.label1.Text = "eFramer Model File (efr)";
            this.label1.Visible = false;
            // 
            // txtRevitFile
            // 
            this.txtRevitFile.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtRevitFile.ForeColor = System.Drawing.Color.Red;
            this.txtRevitFile.Location = new System.Drawing.Point(162, 123);
            this.txtRevitFile.Name = "txtRevitFile";
            this.txtRevitFile.ReadOnly = true;
            this.txtRevitFile.Size = new System.Drawing.Size(322, 21);
            this.txtRevitFile.TabIndex = 17;
            this.txtRevitFile.Visible = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 126);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(107, 13);
            this.label2.TabIndex = 16;
            this.label2.Text = "Revit Model File (rvt)";
            this.label2.Visible = false;
            // 
            // txtVersion
            // 
            this.txtVersion.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtVersion.ForeColor = System.Drawing.Color.Red;
            this.txtVersion.Location = new System.Drawing.Point(162, 42);
            this.txtVersion.Name = "txtVersion";
            this.txtVersion.ReadOnly = true;
            this.txtVersion.Size = new System.Drawing.Size(250, 21);
            this.txtVersion.TabIndex = 21;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 45);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(85, 13);
            this.label3.TabIndex = 20;
            this.label3.Text = "eFramer Version";
            // 
            // txtAvailabe
            // 
            this.txtAvailabe.Location = new System.Drawing.Point(162, 16);
            this.txtAvailabe.Name = "txtAvailabe";
            this.txtAvailabe.ReadOnly = true;
            this.txtAvailabe.Size = new System.Drawing.Size(250, 20);
            this.txtAvailabe.TabIndex = 19;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 19);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(91, 13);
            this.label4.TabIndex = 18;
            this.label4.Text = "eFramer Availabe";
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(503, 186);
            this.Controls.Add(this.txtVersion);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtAvailabe);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtRevitFile);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtEframerFile);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.btnBrowseFileName);
            this.Controls.Add(this.txtFileName);
            this.Controls.Add(this.lblFileName);
            this.Controls.Add(this.btnRunEfr);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "eFramer Application Launcher";
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Button btnBrowseFileName;
        private System.Windows.Forms.TextBox txtFileName;
        private System.Windows.Forms.Label lblFileName;
        private System.Windows.Forms.Button btnRunEfr;
        private System.Windows.Forms.TextBox txtEframerFile;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtRevitFile;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtVersion;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtAvailabe;
        private System.Windows.Forms.Label label4;
    }
}

