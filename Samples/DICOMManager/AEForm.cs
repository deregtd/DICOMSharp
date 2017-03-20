using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace DICOMManager
{
	/// <summary>
	/// Summary description for AEForm.
	/// </summary>
	public class AEForm : System.Windows.Forms.Form
	{
		private bool Closable = false;

		private System.Windows.Forms.Label label1;
		public System.Windows.Forms.TextBox AETitle;
		public System.Windows.Forms.TextBox Address;
		private System.Windows.Forms.Label label2;
		public System.Windows.Forms.TextBox Port;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button OKBut;
		private System.Windows.Forms.Button CancelBut;
		public System.Windows.Forms.TextBox EntName;
        private Label label5;
        public RadioButton Queue_Unlim;
        public RadioButton Queue_Lim;
        public TextBox Queue_Len;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public AEForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
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
            this.label1 = new System.Windows.Forms.Label();
            this.AETitle = new System.Windows.Forms.TextBox();
            this.Address = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.Port = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.EntName = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.OKBut = new System.Windows.Forms.Button();
            this.CancelBut = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.Queue_Unlim = new System.Windows.Forms.RadioButton();
            this.Queue_Lim = new System.Windows.Forms.RadioButton();
            this.Queue_Len = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(7, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(66, 21);
            this.label1.TabIndex = 0;
            this.label1.Text = "AE Title:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // AETitle
            // 
            this.AETitle.Location = new System.Drawing.Point(73, 28);
            this.AETitle.MaxLength = 16;
            this.AETitle.Name = "AETitle";
            this.AETitle.Size = new System.Drawing.Size(134, 20);
            this.AETitle.TabIndex = 2;
            // 
            // Address
            // 
            this.Address.Location = new System.Drawing.Point(73, 49);
            this.Address.MaxLength = 64;
            this.Address.Name = "Address";
            this.Address.Size = new System.Drawing.Size(134, 20);
            this.Address.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(7, 49);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(66, 20);
            this.label2.TabIndex = 3;
            this.label2.Text = "Address:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Port
            // 
            this.Port.Location = new System.Drawing.Point(73, 69);
            this.Port.MaxLength = 5;
            this.Port.Name = "Port";
            this.Port.Size = new System.Drawing.Size(54, 20);
            this.Port.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(7, 69);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(66, 21);
            this.label3.TabIndex = 5;
            this.label3.Text = "Port:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // EntName
            // 
            this.EntName.Location = new System.Drawing.Point(73, 7);
            this.EntName.MaxLength = 32;
            this.EntName.Name = "EntName";
            this.EntName.Size = new System.Drawing.Size(134, 20);
            this.EntName.TabIndex = 1;
            // 
            // label4
            // 
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(7, 7);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(66, 21);
            this.label4.TabIndex = 7;
            this.label4.Text = "Name:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // OKBut
            // 
            this.OKBut.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKBut.Location = new System.Drawing.Point(133, 115);
            this.OKBut.Name = "OKBut";
            this.OKBut.Size = new System.Drawing.Size(74, 21);
            this.OKBut.TabIndex = 5;
            this.OKBut.Text = "OK";
            this.OKBut.Click += new System.EventHandler(this.OKBut_Click);
            // 
            // CancelBut
            // 
            this.CancelBut.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBut.Location = new System.Drawing.Point(12, 115);
            this.CancelBut.Name = "CancelBut";
            this.CancelBut.Size = new System.Drawing.Size(74, 21);
            this.CancelBut.TabIndex = 6;
            this.CancelBut.Text = "Cancel";
            this.CancelBut.Click += new System.EventHandler(this.CancelBut_Click);
            // 
            // label5
            // 
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(7, 90);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(66, 17);
            this.label5.TabIndex = 8;
            this.label5.Text = "Max Queue:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Queue_Unlim
            // 
            this.Queue_Unlim.AutoSize = true;
            this.Queue_Unlim.Checked = true;
            this.Queue_Unlim.Location = new System.Drawing.Point(73, 90);
            this.Queue_Unlim.Name = "Queue_Unlim";
            this.Queue_Unlim.Size = new System.Drawing.Size(68, 17);
            this.Queue_Unlim.TabIndex = 9;
            this.Queue_Unlim.TabStop = true;
            this.Queue_Unlim.Text = "Unlimited";
            this.Queue_Unlim.UseVisualStyleBackColor = true;
            this.Queue_Unlim.CheckedChanged += new System.EventHandler(this.Queue_Unlim_CheckedChanged);
            // 
            // Queue_Lim
            // 
            this.Queue_Lim.AutoSize = true;
            this.Queue_Lim.Location = new System.Drawing.Point(145, 92);
            this.Queue_Lim.Name = "Queue_Lim";
            this.Queue_Lim.Size = new System.Drawing.Size(14, 13);
            this.Queue_Lim.TabIndex = 10;
            this.Queue_Lim.UseVisualStyleBackColor = true;
            this.Queue_Lim.CheckedChanged += new System.EventHandler(this.Queue_Unlim_CheckedChanged);
            // 
            // Queue_Len
            // 
            this.Queue_Len.Enabled = false;
            this.Queue_Len.Location = new System.Drawing.Point(162, 89);
            this.Queue_Len.Name = "Queue_Len";
            this.Queue_Len.Size = new System.Drawing.Size(45, 20);
            this.Queue_Len.TabIndex = 11;
            this.Queue_Len.Text = "100";
            // 
            // AEForm
            // 
            this.AcceptButton = this.OKBut;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.CancelButton = this.CancelBut;
            this.ClientSize = new System.Drawing.Size(219, 143);
            this.ControlBox = false;
            this.Controls.Add(this.Queue_Len);
            this.Controls.Add(this.Queue_Lim);
            this.Controls.Add(this.Queue_Unlim);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.CancelBut);
            this.Controls.Add(this.OKBut);
            this.Controls.Add(this.EntName);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.Port);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.Address);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.AETitle);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AEForm";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add/Edit Entity";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.AEForm_Closing);
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		private void OKBut_Click(object sender, System.EventArgs e)
		{
		}

		private void AEForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (Closable) return;

/*			int iPort;
			try { iPort = int.Parse(Port.Text); } 
			catch { e.Cancel = true; }*/

			if ((Port.Text.Length < 1) || (Port.Text[0] < '0') || (Port.Text[0] > '9'))
			{
				MessageBox.Show("Invalid Port");
				e.Cancel = true;
			}
			else if (AETitle.Text == "")
			{
				MessageBox.Show("Invalid AE Title");
				e.Cancel = true;
			}
			else if (EntName.Text == "")
			{
				MessageBox.Show("Invalid Entity Name");
				e.Cancel = true;
			}
			else if (Address.Text == "")
			{
				MessageBox.Show("Invalid Address");
				e.Cancel = true;
			}
            
            if (Queue_Lim.Checked)
            {
                try {
                    int.Parse(Queue_Len.Text);
                } catch {
    				MessageBox.Show("Max Queue Length Not A Number");
                    e.Cancel = true;
                }
            }
		}

		private void CancelBut_Click(object sender, System.EventArgs e)
		{
			Closable = true;
		}

        private void Queue_Unlim_CheckedChanged(object sender, EventArgs e)
        {
            if (Queue_Unlim.Checked)
            {
                Queue_Len.Enabled = false;
                Queue_Lim.Checked = false;
                Queue_Unlim.Checked = true;
            }
            else
            {
                Queue_Len.Enabled = true;
                Queue_Unlim.Checked = false;
                Queue_Lim.Checked = true;
            }
        }
	}
}
