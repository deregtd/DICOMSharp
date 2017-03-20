using System;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DICOMManager
{
	/// <summary>
	/// Summary description for Options.
	/// </summary>
	public class Options : System.Windows.Forms.Form
	{
		private bool Closable = false;

		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.Button OKBut;
		private System.Windows.Forms.Button CancelBut;
		public System.Windows.Forms.TextBox DICOM_AETitle;
		private System.Windows.Forms.Label label1;
		public System.Windows.Forms.RadioButton DICOM_CustomAE;
		public System.Windows.Forms.RadioButton DICOM_eFilmAE;
        private GroupBox groupBox2;
        public TextBox DICOM_Port;
        private Label label2;
        private GroupBox groupBox1;
        private TabPage tabPage2;
        private Button button3;
        private Button button2;
        private Button button1;
        private ListBox Anon_List;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public Options()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.DICOM_Port = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.DICOM_eFilmAE = new System.Windows.Forms.RadioButton();
            this.DICOM_CustomAE = new System.Windows.Forms.RadioButton();
            this.DICOM_AETitle = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.OKBut = new System.Windows.Forms.Button();
            this.CancelBut = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.Anon_List = new System.Windows.Forms.ListBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(373, 196);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.groupBox2);
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Size = new System.Drawing.Size(365, 201);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "DICOM";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.DICOM_Port);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Location = new System.Drawing.Point(8, 77);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(238, 45);
            this.groupBox2.TabIndex = 9;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "DICOM Port";
            // 
            // DICOM_Port
            // 
            this.DICOM_Port.Location = new System.Drawing.Point(70, 16);
            this.DICOM_Port.MaxLength = 16;
            this.DICOM_Port.Name = "DICOM_Port";
            this.DICOM_Port.Size = new System.Drawing.Size(153, 20);
            this.DICOM_Port.TabIndex = 9;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(16, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(47, 20);
            this.label2.TabIndex = 8;
            this.label2.Text = "Port:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.DICOM_eFilmAE);
            this.groupBox1.Controls.Add(this.DICOM_CustomAE);
            this.groupBox1.Controls.Add(this.DICOM_AETitle);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(8, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(239, 68);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "DICOM AE Title";
            // 
            // DICOM_eFilmAE
            // 
            this.DICOM_eFilmAE.Location = new System.Drawing.Point(122, 14);
            this.DICOM_eFilmAE.Name = "DICOM_eFilmAE";
            this.DICOM_eFilmAE.Size = new System.Drawing.Size(111, 21);
            this.DICOM_eFilmAE.TabIndex = 6;
            this.DICOM_eFilmAE.Text = "Copy from eFilm";
            this.DICOM_eFilmAE.CheckedChanged += new System.EventHandler(this.DICOM_eFilmAE_CheckedChanged);
            // 
            // DICOM_CustomAE
            // 
            this.DICOM_CustomAE.Location = new System.Drawing.Point(17, 14);
            this.DICOM_CustomAE.Name = "DICOM_CustomAE";
            this.DICOM_CustomAE.Size = new System.Drawing.Size(100, 21);
            this.DICOM_CustomAE.TabIndex = 5;
            this.DICOM_CustomAE.Text = "Custom";
            this.DICOM_CustomAE.CheckedChanged += new System.EventHandler(this.DICOM_CustomAE_CheckedChanged);
            // 
            // DICOM_AETitle
            // 
            this.DICOM_AETitle.Location = new System.Drawing.Point(70, 38);
            this.DICOM_AETitle.MaxLength = 16;
            this.DICOM_AETitle.Name = "DICOM_AETitle";
            this.DICOM_AETitle.Size = new System.Drawing.Size(153, 20);
            this.DICOM_AETitle.TabIndex = 7;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(16, 38);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(47, 20);
            this.label1.TabIndex = 3;
            this.label1.Text = "AE Title:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // OKBut
            // 
            this.OKBut.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OKBut.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKBut.Location = new System.Drawing.Point(286, 203);
            this.OKBut.Name = "OKBut";
            this.OKBut.Size = new System.Drawing.Size(80, 21);
            this.OKBut.TabIndex = 8;
            this.OKBut.Text = "OK";
            this.OKBut.Click += new System.EventHandler(this.OKBut_Click);
            // 
            // CancelBut
            // 
            this.CancelBut.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CancelBut.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBut.Location = new System.Drawing.Point(193, 203);
            this.CancelBut.Name = "CancelBut";
            this.CancelBut.Size = new System.Drawing.Size(80, 21);
            this.CancelBut.TabIndex = 9;
            this.CancelBut.Text = "Cancel";
            this.CancelBut.Click += new System.EventHandler(this.CancelBut_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.button3);
            this.tabPage2.Controls.Add(this.button2);
            this.tabPage2.Controls.Add(this.button1);
            this.tabPage2.Controls.Add(this.Anon_List);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(365, 170);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Anonymization";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // Anon_List
            // 
            this.Anon_List.FormattingEnabled = true;
            this.Anon_List.Location = new System.Drawing.Point(0, 21);
            this.Anon_List.Name = "Anon_List";
            this.Anon_List.Size = new System.Drawing.Size(164, 147);
            this.Anon_List.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(0, 0);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(54, 22);
            this.button1.TabIndex = 1;
            this.button1.Text = "Add";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(110, 0);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(54, 22);
            this.button2.TabIndex = 2;
            this.button2.Text = "Default";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(55, 0);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(54, 22);
            this.button3.TabIndex = 3;
            this.button3.Text = "Remove";
            this.button3.UseVisualStyleBackColor = true;
            // 
            // Options
            // 
            this.AcceptButton = this.OKBut;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.CancelButton = this.CancelBut;
            this.ClientSize = new System.Drawing.Size(375, 232);
            this.ControlBox = false;
            this.Controls.Add(this.CancelBut);
            this.Controls.Add(this.OKBut);
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Options";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Options";
            this.Load += new System.EventHandler(this.Options_Load);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.Options_Closing);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.ResumeLayout(false);

		}
		#endregion

		private void Options_Load(object sender, System.EventArgs e)
		{
			//DICOM Page
			DICOM_CustomAE.Checked = (((Form1) Owner).AEType == Form1.eAEType.Custom);
			DICOM_eFilmAE.Checked = (((Form1) Owner).AEType == Form1.eAEType.eFilm);
			DICOM_AETitle.ReadOnly = !DICOM_CustomAE.Checked;

			DICOM_AETitle.Text = ((Form1) Owner).AETitle;

            int iPrt = ((Form1) Owner).DICOMPort;
            DICOM_Port.Text = iPrt.ToString();
		}

		private void DICOM_eFilmAE_CheckedChanged(object sender, System.EventArgs e)
		{
			DICOM_eFilmAE.Checked = !DICOM_CustomAE.Checked;
			DICOM_AETitle.ReadOnly = !DICOM_CustomAE.Checked;

			if (DICOM_eFilmAE.Checked)
			{
				RegistryKey rkey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\eFilm Medical\\eFilm\\Settings");
				if (rkey != null)
				{
					DICOM_AETitle.Text = (string) rkey.GetValue("AE Title");
					rkey.Close();
				}
			}
		}

		private void DICOM_CustomAE_CheckedChanged(object sender, System.EventArgs e)
		{
			DICOM_CustomAE.Checked = !DICOM_eFilmAE.Checked;
			DICOM_AETitle.ReadOnly = !DICOM_CustomAE.Checked;
		}

		private void OKBut_Click(object sender, System.EventArgs e)
		{
		}

		private void Options_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (Closable) return;

            if (DICOM_AETitle.Text == "")
            {
                MessageBox.Show("AE Title must not be blank!");
                e.Cancel = true;
            }

            int tpi;
            if ((DICOM_Port.Text == "") || (int.TryParse(DICOM_Port.Text, out tpi) == false))
            {
                MessageBox.Show("DICOM Port must be valid (between 1 and 65535)!");
                e.Cancel = true;
            }
        }

		private void CancelBut_Click(object sender, System.EventArgs e)
		{
			Closable = true;
		}
	}
}
