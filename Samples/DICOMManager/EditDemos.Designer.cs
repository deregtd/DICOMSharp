namespace DICOMManager
{
    partial class EditDemos
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
            this.EDSave = new System.Windows.Forms.Button();
            this.EDCancel = new System.Windows.Forms.Button();
            this.PropGrid = new System.Windows.Forms.PropertyGrid();
            this.SuspendLayout();
            // 
            // EDSave
            // 
            this.EDSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.EDSave.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.EDSave.Location = new System.Drawing.Point(244, 228);
            this.EDSave.Name = "EDSave";
            this.EDSave.Size = new System.Drawing.Size(89, 22);
            this.EDSave.TabIndex = 1;
            this.EDSave.Text = "Save";
            this.EDSave.UseVisualStyleBackColor = true;
            // 
            // EDCancel
            // 
            this.EDCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.EDCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.EDCancel.Location = new System.Drawing.Point(149, 228);
            this.EDCancel.Name = "EDCancel";
            this.EDCancel.Size = new System.Drawing.Size(89, 22);
            this.EDCancel.TabIndex = 2;
            this.EDCancel.Text = "Cancel";
            this.EDCancel.UseVisualStyleBackColor = true;
            // 
            // PropGrid
            // 
            this.PropGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.PropGrid.CommandsVisibleIfAvailable = false;
            this.PropGrid.HelpVisible = false;
            this.PropGrid.Location = new System.Drawing.Point(1, 1);
            this.PropGrid.Name = "PropGrid";
            this.PropGrid.PropertySort = System.Windows.Forms.PropertySort.NoSort;
            this.PropGrid.Size = new System.Drawing.Size(332, 156);
            this.PropGrid.TabIndex = 0;
            this.PropGrid.ToolbarVisible = false;
            // 
            // EditDemos
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(333, 250);
            this.Controls.Add(this.EDCancel);
            this.Controls.Add(this.EDSave);
            this.Controls.Add(this.PropGrid);
            this.Name = "EditDemos";
            this.Text = "EditDemos";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button EDSave;
        private System.Windows.Forms.Button EDCancel;
        public System.Windows.Forms.PropertyGrid PropGrid;
    }
}