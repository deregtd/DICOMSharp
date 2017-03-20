namespace ReallySimpleViewer
{
    partial class Form1
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.filterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.automaticToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.alwaysOnToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.alwaysOffToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.windowLevelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.imagesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.resetToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(628, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.openToolStripMenuItem.Text = "&Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(149, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.filterToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.optionsToolStripMenuItem.Text = "&Options";
            // 
            // filterToolStripMenuItem
            // 
            this.filterToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.automaticToolStripMenuItem,
            this.alwaysOnToolStripMenuItem,
            this.alwaysOffToolStripMenuItem});
            this.filterToolStripMenuItem.Name = "filterToolStripMenuItem";
            this.filterToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.filterToolStripMenuItem.Text = "&Filtering Mode";
            // 
            // automaticToolStripMenuItem
            // 
            this.automaticToolStripMenuItem.Checked = true;
            this.automaticToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.automaticToolStripMenuItem.Name = "automaticToolStripMenuItem";
            this.automaticToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.automaticToolStripMenuItem.Text = "&Automatic";
            this.automaticToolStripMenuItem.Click += new System.EventHandler(this.automaticToolStripMenuItem_Click);
            // 
            // alwaysOnToolStripMenuItem
            // 
            this.alwaysOnToolStripMenuItem.Name = "alwaysOnToolStripMenuItem";
            this.alwaysOnToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.alwaysOnToolStripMenuItem.Text = "Always &On";
            this.alwaysOnToolStripMenuItem.Click += new System.EventHandler(this.alwaysOnToolStripMenuItem_Click);
            // 
            // alwaysOffToolStripMenuItem
            // 
            this.alwaysOffToolStripMenuItem.Name = "alwaysOffToolStripMenuItem";
            this.alwaysOffToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.alwaysOffToolStripMenuItem.Text = "Always O&ff";
            this.alwaysOffToolStripMenuItem.Click += new System.EventHandler(this.alwaysOffToolStripMenuItem_Click);
            // 
            // resetToolStripMenuItem
            // 
            this.resetToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.windowLevelToolStripMenuItem,
            this.viewportToolStripMenuItem,
            this.imagesToolStripMenuItem});
            this.resetToolStripMenuItem.Name = "resetToolStripMenuItem";
            this.resetToolStripMenuItem.Size = new System.Drawing.Size(47, 20);
            this.resetToolStripMenuItem.Text = "&Reset";
            // 
            // windowLevelToolStripMenuItem
            // 
            this.windowLevelToolStripMenuItem.Name = "windowLevelToolStripMenuItem";
            this.windowLevelToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.windowLevelToolStripMenuItem.Text = "&Window/Level";
            this.windowLevelToolStripMenuItem.Click += new System.EventHandler(this.windowLevelToolStripMenuItem_Click);
            // 
            // viewportToolStripMenuItem
            // 
            this.viewportToolStripMenuItem.Name = "viewportToolStripMenuItem";
            this.viewportToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.viewportToolStripMenuItem.Text = "&Viewport";
            this.viewportToolStripMenuItem.Click += new System.EventHandler(this.viewportToolStripMenuItem_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.DefaultExt = "*.dcm";
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Filter = "DICOM files|*.dcm|All Files|*.*";
            this.openFileDialog1.Multiselect = true;
            // 
            // imagesToolStripMenuItem
            // 
            this.imagesToolStripMenuItem.Name = "imagesToolStripMenuItem";
            this.imagesToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.imagesToolStripMenuItem.Text = "&Images";
            this.imagesToolStripMenuItem.Click += new System.EventHandler(this.imagesToolStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(628, 477);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "Really Simple Viewer";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem resetToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem windowLevelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewportToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem filterToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem automaticToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem alwaysOnToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem alwaysOffToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem imagesToolStripMenuItem;
    }
}

