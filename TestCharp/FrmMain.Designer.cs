namespace TestCharp
{
    partial class FrmMain
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
            this.openDlg = new System.Windows.Forms.OpenFileDialog();
            this.lblModule = new System.Windows.Forms.Label();
            this.textPath = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.picImage = new System.Windows.Forms.PictureBox();
            this.radioBTN_Auto = new System.Windows.Forms.RadioButton();
            this.radioBTN_Manual = new System.Windows.Forms.RadioButton();
            this.btnRecog = new System.Windows.Forms.Button();
            this.textBoxPixel = new System.Windows.Forms.TextBox();
            this.textBoxDis = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.picImage)).BeginInit();
            this.SuspendLayout();
            // 
            // lblModule
            // 
            this.lblModule.AutoSize = true;
            this.lblModule.Location = new System.Drawing.Point(33, 30);
            this.lblModule.Name = "lblModule";
            this.lblModule.Size = new System.Drawing.Size(34, 13);
            this.lblModule.TabIndex = 0;
            this.lblModule.Text = "径路:";
            this.lblModule.Click += new System.EventHandler(this.lblModule_Click);
            // 
            // textPath
            // 
            this.textPath.Location = new System.Drawing.Point(117, 27);
            this.textPath.Name = "textPath";
            this.textPath.Size = new System.Drawing.Size(825, 20);
            this.textPath.TabIndex = 1;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(948, 24);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // picImage
            // 
            this.picImage.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.picImage.Location = new System.Drawing.Point(226, 63);
            this.picImage.Name = "picImage";
            this.picImage.Size = new System.Drawing.Size(797, 546);
            this.picImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picImage.TabIndex = 3;
            this.picImage.TabStop = false;
            // 
            // radioBTN_Auto
            // 
            this.radioBTN_Auto.AutoSize = true;
            this.radioBTN_Auto.Location = new System.Drawing.Point(36, 89);
            this.radioBTN_Auto.Name = "radioBTN_Auto";
            this.radioBTN_Auto.Size = new System.Drawing.Size(47, 17);
            this.radioBTN_Auto.TabIndex = 4;
            this.radioBTN_Auto.TabStop = true;
            this.radioBTN_Auto.Text = "Auto";
            this.radioBTN_Auto.UseVisualStyleBackColor = true;
            this.radioBTN_Auto.CheckedChanged += new System.EventHandler(this.radioBTN_Auto_CheckedChanged);
            // 
            // radioBTN_Manual
            // 
            this.radioBTN_Manual.AutoSize = true;
            this.radioBTN_Manual.Location = new System.Drawing.Point(36, 123);
            this.radioBTN_Manual.Name = "radioBTN_Manual";
            this.radioBTN_Manual.Size = new System.Drawing.Size(60, 17);
            this.radioBTN_Manual.TabIndex = 5;
            this.radioBTN_Manual.TabStop = true;
            this.radioBTN_Manual.Text = "Manual";
            this.radioBTN_Manual.UseVisualStyleBackColor = true;
            this.radioBTN_Manual.CheckedChanged += new System.EventHandler(this.radioBTN_Manual_CheckedChanged);
            // 
            // btnRecog
            // 
            this.btnRecog.Font = new System.Drawing.Font("SimSun", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRecog.Location = new System.Drawing.Point(28, 165);
            this.btnRecog.Name = "btnRecog";
            this.btnRecog.Size = new System.Drawing.Size(75, 23);
            this.btnRecog.TabIndex = 6;
            this.btnRecog.Text = "识别";
            this.btnRecog.UseVisualStyleBackColor = true;
            this.btnRecog.Click += new System.EventHandler(this.btnRecog_Click);
            // 
            // textBoxPixel
            // 
            this.textBoxPixel.Location = new System.Drawing.Point(120, 521);
            this.textBoxPixel.Name = "textBoxPixel";
            this.textBoxPixel.Size = new System.Drawing.Size(100, 20);
            this.textBoxPixel.TabIndex = 7;
            // 
            // textBoxDis
            // 
            this.textBoxDis.Location = new System.Drawing.Point(120, 559);
            this.textBoxDis.Name = "textBoxDis";
            this.textBoxDis.Size = new System.Drawing.Size(100, 20);
            this.textBoxDis.TabIndex = 8;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(30, 482);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 16);
            this.label1.TabIndex = 9;
            this.label1.Text = "结果";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(33, 524);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 13);
            this.label2.TabIndex = 10;
            this.label2.Text = "Pixels";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(25, 562);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(71, 13);
            this.label3.TabIndex = 11;
            this.label3.Text = "Distance(mm)";
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1048, 643);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxDis);
            this.Controls.Add(this.textBoxPixel);
            this.Controls.Add(this.btnRecog);
            this.Controls.Add(this.radioBTN_Manual);
            this.Controls.Add(this.radioBTN_Auto);
            this.Controls.Add(this.picImage);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.textPath);
            this.Controls.Add(this.lblModule);
            this.Name = "FrmMain";
            this.Text = "nnb ";
            ((System.ComponentModel.ISupportInitialize)(this.picImage)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblModule;
        private System.Windows.Forms.TextBox textPath;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.PictureBox picImage;
        private System.Windows.Forms.RadioButton radioBTN_Auto;
        private System.Windows.Forms.RadioButton radioBTN_Manual;
        private System.Windows.Forms.Button btnRecog;
        private System.Windows.Forms.OpenFileDialog openDlg;
        private System.Windows.Forms.TextBox textBoxPixel;
        private System.Windows.Forms.TextBox textBoxDis;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
    }
}

