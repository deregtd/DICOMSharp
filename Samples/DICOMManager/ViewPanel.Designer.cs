namespace DICOMManager
{
    partial class ViewPanel
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
            this.LoadProgress = new System.Windows.Forms.ProgressBar();
            this.simpleViewerPane1 = new DICOMSharpControls.Imaging.SimpleViewerPane();
            this.SuspendLayout();
            // 
            // LoadProgress
            // 
            this.LoadProgress.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.LoadProgress.Location = new System.Drawing.Point(0, 306);
            this.LoadProgress.Name = "LoadProgress";
            this.LoadProgress.Size = new System.Drawing.Size(362, 21);
            this.LoadProgress.TabIndex = 1;
            // 
            // simpleViewerPane1
            // 
            this.simpleViewerPane1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.simpleViewerPane1.Location = new System.Drawing.Point(0, 0);
            this.simpleViewerPane1.Name = "simpleViewerPane1";
            this.simpleViewerPane1.Size = new System.Drawing.Size(362, 306);
            this.simpleViewerPane1.TabIndex = 2;
            // 
            // ViewPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(362, 327);
            this.Controls.Add(this.simpleViewerPane1);
            this.Controls.Add(this.LoadProgress);
            this.Name = "ViewPanel";
            this.Text = "View Panel";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ViewPanel_FormClosed);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ProgressBar LoadProgress;
        private DICOMSharpControls.Imaging.SimpleViewerPane simpleViewerPane1;
    }
}