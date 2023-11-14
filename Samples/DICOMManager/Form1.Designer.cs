namespace DICOMManager
{
    public partial class Form1
    {
        private System.ComponentModel.IContainer components;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
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
            this.components = new System.ComponentModel.Container();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.IntPanel = new System.Windows.Forms.Panel();
            this.SendAllTo = new System.Windows.Forms.Button();
            this.SendAnon = new System.Windows.Forms.ComboBox();
            this.SendList = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.AEEdit = new System.Windows.Forms.Button();
            this.AEDel = new System.Windows.Forms.Button();
            this.AEAdd = new System.Windows.Forms.Button();
            this.SendCompression = new System.Windows.Forms.ComboBox();
            this.SendTo = new System.Windows.Forms.Button();
            this.TypeTab = new System.Windows.Forms.TabControl();
            this.CDROMTab = new System.Windows.Forms.TabPage();
            this.CDToMem = new System.Windows.Forms.Button();
            this.CDViewSeries = new System.Windows.Forms.Button();
            this.CDROMDriveDrop = new System.Windows.Forms.ComboBox();
            this.EjectCD = new System.Windows.Forms.Button();
            this.CDROMView = new CodersLab.Windows.Controls.TreeView();
            this.CDROMLoad = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.FilesTab = new System.Windows.Forms.TabPage();
            this.FilesToMem = new System.Windows.Forms.Button();
            this.AddFiles = new System.Windows.Forms.Button();
            this.FilesViewSel = new System.Windows.Forms.Button();
            this.ClearFiles = new System.Windows.Forms.Button();
            this.FilesView = new CodersLab.Windows.Controls.TreeView();
            this.MemoryTab = new System.Windows.Forms.TabPage();
            this.Mem_Demographics = new System.Windows.Forms.Button();
            this.Mem_Save = new System.Windows.Forms.Button();
            this.Mem_QR = new System.Windows.Forms.Button();
            this.Mem_View = new System.Windows.Forms.Button();
            this.Mem_Clear = new System.Windows.Forms.Button();
            this.MemoryFiles = new CodersLab.Windows.Controls.TreeView();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.DebugText = new System.Windows.Forms.TextBox();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.menuStrip1.SuspendLayout();
            this.IntPanel.SuspendLayout();
            this.TypeTab.SuspendLayout();
            this.CDROMTab.SuspendLayout();
            this.FilesTab.SuspendLayout();
            this.MemoryTab.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(628, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.menuItem2_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.optionsToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.toolsToolStripMenuItem.Text = "&File";
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.optionsToolStripMenuItem.Text = "&Options";
            this.optionsToolStripMenuItem.Click += new System.EventHandler(this.menuItem4_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.aboutToolStripMenuItem.Text = "&Options";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.menuItem6_Click);
            // 
            // IntPanel
            // 
            this.IntPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.IntPanel.Controls.Add(this.SendAllTo);
            this.IntPanel.Controls.Add(this.SendAnon);
            this.IntPanel.Controls.Add(this.SendList);
            this.IntPanel.Controls.Add(this.AEEdit);
            this.IntPanel.Controls.Add(this.AEDel);
            this.IntPanel.Controls.Add(this.AEAdd);
            this.IntPanel.Controls.Add(this.SendCompression);
            this.IntPanel.Controls.Add(this.SendTo);
            this.IntPanel.Controls.Add(this.TypeTab);
            this.IntPanel.Location = new System.Drawing.Point(0, 4);
            this.IntPanel.Name = "IntPanel";
            this.IntPanel.Size = new System.Drawing.Size(798, 548);
            this.IntPanel.TabIndex = 0;
            // 
            // SendAllTo
            // 
            this.SendAllTo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SendAllTo.Location = new System.Drawing.Point(643, 523);
            this.SendAllTo.Name = "SendAllTo";
            this.SendAllTo.Size = new System.Drawing.Size(153, 21);
            this.SendAllTo.TabIndex = 20;
            this.SendAllTo.Text = "Send All";
            this.SendAllTo.Click += new System.EventHandler(this.SendAllTo_Click);
            // 
            // SendAnon
            // 
            this.SendAnon.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SendAnon.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SendAnon.Items.AddRange(new object[] {
            "Leave Patient Data",
            "Anonymize Patient Info"});
            this.SendAnon.Location = new System.Drawing.Point(643, 443);
            this.SendAnon.Name = "SendAnon";
            this.SendAnon.Size = new System.Drawing.Size(153, 21);
            this.SendAnon.TabIndex = 19;
            this.SendAnon.SelectedIndexChanged += new System.EventHandler(this.SendCompression_SelectedIndexChanged);
            // 
            // SendList
            // 
            this.SendList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SendList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.SendList.FullRowSelect = true;
            this.SendList.HideSelection = false;
            this.SendList.Location = new System.Drawing.Point(643, 21);
            this.SendList.MultiSelect = false;
            this.SendList.Name = "SendList";
            this.SendList.Size = new System.Drawing.Size(153, 416);
            this.SendList.TabIndex = 18;
            this.SendList.UseCompatibleStateImageBehavior = false;
            this.SendList.View = System.Windows.Forms.View.Details;
            this.SendList.DoubleClick += new System.EventHandler(this.AEEdit_Click);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Name";
            this.columnHeader1.Width = 101;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "AE";
            this.columnHeader2.Width = 79;
            // 
            // AEEdit
            // 
            this.AEEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.AEEdit.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AEEdit.Location = new System.Drawing.Point(696, 0);
            this.AEEdit.Name = "AEEdit";
            this.AEEdit.Size = new System.Drawing.Size(47, 21);
            this.AEEdit.TabIndex = 17;
            this.AEEdit.Text = "Edit";
            this.AEEdit.Click += new System.EventHandler(this.AEEdit_Click);
            // 
            // AEDel
            // 
            this.AEDel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.AEDel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AEDel.Location = new System.Drawing.Point(770, 0);
            this.AEDel.Name = "AEDel";
            this.AEDel.Size = new System.Drawing.Size(20, 21);
            this.AEDel.TabIndex = 16;
            this.AEDel.Text = "-";
            this.AEDel.Click += new System.EventHandler(this.AEDel_Click);
            // 
            // AEAdd
            // 
            this.AEAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.AEAdd.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AEAdd.Location = new System.Drawing.Point(650, 0);
            this.AEAdd.Name = "AEAdd";
            this.AEAdd.Size = new System.Drawing.Size(20, 21);
            this.AEAdd.TabIndex = 15;
            this.AEAdd.Text = "+";
            this.AEAdd.Click += new System.EventHandler(this.AEAdd_Click);
            // 
            // SendCompression
            // 
            this.SendCompression.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SendCompression.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SendCompression.Items.AddRange(new object[] {
            "Leave Image Data",
            "Decompress All"});
            this.SendCompression.Location = new System.Drawing.Point(643, 469);
            this.SendCompression.Name = "SendCompression";
            this.SendCompression.Size = new System.Drawing.Size(153, 21);
            this.SendCompression.TabIndex = 14;
            this.SendCompression.SelectedIndexChanged += new System.EventHandler(this.SendCompression_SelectedIndexChanged);
            // 
            // SendTo
            // 
            this.SendTo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SendTo.Location = new System.Drawing.Point(643, 496);
            this.SendTo.Name = "SendTo";
            this.SendTo.Size = new System.Drawing.Size(153, 21);
            this.SendTo.TabIndex = 13;
            this.SendTo.Text = "Send Selected";
            this.SendTo.Click += new System.EventHandler(this.SendTo_Click);
            // 
            // TypeTab
            // 
            this.TypeTab.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TypeTab.Controls.Add(this.CDROMTab);
            this.TypeTab.Controls.Add(this.FilesTab);
            this.TypeTab.Controls.Add(this.MemoryTab);
            this.TypeTab.Controls.Add(this.tabPage3);
            this.TypeTab.Location = new System.Drawing.Point(0, 0);
            this.TypeTab.Name = "TypeTab";
            this.TypeTab.SelectedIndex = 0;
            this.TypeTab.Size = new System.Drawing.Size(636, 548);
            this.TypeTab.TabIndex = 12;
            // 
            // CDROMTab
            // 
            this.CDROMTab.Controls.Add(this.CDToMem);
            this.CDROMTab.Controls.Add(this.CDViewSeries);
            this.CDROMTab.Controls.Add(this.CDROMDriveDrop);
            this.CDROMTab.Controls.Add(this.EjectCD);
            this.CDROMTab.Controls.Add(this.CDROMView);
            this.CDROMTab.Controls.Add(this.CDROMLoad);
            this.CDROMTab.Controls.Add(this.label1);
            this.CDROMTab.Location = new System.Drawing.Point(4, 22);
            this.CDROMTab.Name = "CDROMTab";
            this.CDROMTab.Size = new System.Drawing.Size(628, 522);
            this.CDROMTab.TabIndex = 0;
            this.CDROMTab.Text = "CDROM";
            this.CDROMTab.UseVisualStyleBackColor = true;
            // 
            // CDToMem
            // 
            this.CDToMem.Location = new System.Drawing.Point(328, 7);
            this.CDToMem.Name = "CDToMem";
            this.CDToMem.Size = new System.Drawing.Size(75, 21);
            this.CDToMem.TabIndex = 6;
            this.CDToMem.Text = "To Memory";
            this.CDToMem.UseVisualStyleBackColor = true;
            this.CDToMem.Click += new System.EventHandler(this.CDToMem_Click);
            // 
            // CDViewSeries
            // 
            this.CDViewSeries.Location = new System.Drawing.Point(247, 7);
            this.CDViewSeries.Name = "CDViewSeries";
            this.CDViewSeries.Size = new System.Drawing.Size(75, 21);
            this.CDViewSeries.TabIndex = 5;
            this.CDViewSeries.Text = "View Series";
            this.CDViewSeries.UseVisualStyleBackColor = true;
            this.CDViewSeries.Click += new System.EventHandler(this.CDViewSeries_Click);
            // 
            // CDROMDriveDrop
            // 
            this.CDROMDriveDrop.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CDROMDriveDrop.Location = new System.Drawing.Point(41, 7);
            this.CDROMDriveDrop.Name = "CDROMDriveDrop";
            this.CDROMDriveDrop.Size = new System.Drawing.Size(53, 21);
            this.CDROMDriveDrop.TabIndex = 0;
            // 
            // EjectCD
            // 
            this.EjectCD.Location = new System.Drawing.Point(174, 7);
            this.EjectCD.Name = "EjectCD";
            this.EjectCD.Size = new System.Drawing.Size(67, 21);
            this.EjectCD.TabIndex = 4;
            this.EjectCD.Text = "Eject";
            this.EjectCD.Click += new System.EventHandler(this.EjectCD_Click);
            // 
            // CDROMView
            // 
            this.CDROMView.AllowDrop = true;
            this.CDROMView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.CDROMView.FullRowSelect = true;
            this.CDROMView.HideSelection = false;
            this.CDROMView.Location = new System.Drawing.Point(3, 32);
            this.CDROMView.Name = "CDROMView";
            this.CDROMView.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            this.CDROMView.SelectionMode = CodersLab.Windows.Controls.TreeViewSelectionMode.MultiSelectSameParent;
            this.CDROMView.Size = new System.Drawing.Size(625, 488);
            this.CDROMView.TabIndex = 3;
            this.CDROMView.DragDrop += new System.Windows.Forms.DragEventHandler(this.FilesView_DragDrop);
            this.CDROMView.DragEnter += new System.Windows.Forms.DragEventHandler(this.FilesView_DragEnter);
            // 
            // CDROMLoad
            // 
            this.CDROMLoad.Location = new System.Drawing.Point(101, 7);
            this.CDROMLoad.Name = "CDROMLoad";
            this.CDROMLoad.Size = new System.Drawing.Size(67, 21);
            this.CDROMLoad.TabIndex = 2;
            this.CDROMLoad.Text = "Load";
            this.CDROMLoad.Click += new System.EventHandler(this.CDROMLoad_Click);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(8, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(39, 21);
            this.label1.TabIndex = 1;
            this.label1.Text = "Drive:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // FilesTab
            // 
            this.FilesTab.Controls.Add(this.FilesToMem);
            this.FilesTab.Controls.Add(this.AddFiles);
            this.FilesTab.Controls.Add(this.FilesViewSel);
            this.FilesTab.Controls.Add(this.ClearFiles);
            this.FilesTab.Controls.Add(this.FilesView);
            this.FilesTab.Location = new System.Drawing.Point(4, 22);
            this.FilesTab.Name = "FilesTab";
            this.FilesTab.Size = new System.Drawing.Size(628, 522);
            this.FilesTab.TabIndex = 1;
            this.FilesTab.Text = "Files";
            this.FilesTab.UseVisualStyleBackColor = true;
            // 
            // FilesToMem
            // 
            this.FilesToMem.Location = new System.Drawing.Point(261, 7);
            this.FilesToMem.Name = "FilesToMem";
            this.FilesToMem.Size = new System.Drawing.Size(75, 21);
            this.FilesToMem.TabIndex = 8;
            this.FilesToMem.Text = "To Memory";
            this.FilesToMem.UseVisualStyleBackColor = true;
            this.FilesToMem.Click += new System.EventHandler(this.FilesToMem_Click);
            // 
            // AddFiles
            // 
            this.AddFiles.Location = new System.Drawing.Point(74, 7);
            this.AddFiles.Name = "AddFiles";
            this.AddFiles.Size = new System.Drawing.Size(84, 21);
            this.AddFiles.TabIndex = 7;
            this.AddFiles.Text = "Add Files";
            this.AddFiles.UseVisualStyleBackColor = true;
            this.AddFiles.Click += new System.EventHandler(this.AddFiles_Click);
            // 
            // FilesViewSel
            // 
            this.FilesViewSel.Location = new System.Drawing.Point(164, 7);
            this.FilesViewSel.Name = "FilesViewSel";
            this.FilesViewSel.Size = new System.Drawing.Size(91, 21);
            this.FilesViewSel.TabIndex = 6;
            this.FilesViewSel.Text = "View Selected";
            this.FilesViewSel.UseVisualStyleBackColor = true;
            this.FilesViewSel.Click += new System.EventHandler(this.FilesViewSel_Click);
            // 
            // ClearFiles
            // 
            this.ClearFiles.Location = new System.Drawing.Point(8, 7);
            this.ClearFiles.Name = "ClearFiles";
            this.ClearFiles.Size = new System.Drawing.Size(60, 21);
            this.ClearFiles.TabIndex = 1;
            this.ClearFiles.Text = "Clear";
            this.ClearFiles.Click += new System.EventHandler(this.ClearFiles_Click);
            // 
            // FilesView
            // 
            this.FilesView.AllowDrop = true;
            this.FilesView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.FilesView.FullRowSelect = true;
            this.FilesView.HideSelection = false;
            this.FilesView.Location = new System.Drawing.Point(3, 32);
            this.FilesView.Name = "FilesView";
            this.FilesView.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            this.FilesView.SelectionMode = CodersLab.Windows.Controls.TreeViewSelectionMode.MultiSelectSameParent;
            this.FilesView.Size = new System.Drawing.Size(625, 488);
            this.FilesView.TabIndex = 0;
            this.FilesView.DragDrop += new System.Windows.Forms.DragEventHandler(this.FilesView_DragDrop);
            this.FilesView.DragEnter += new System.Windows.Forms.DragEventHandler(this.FilesView_DragEnter);
            // 
            // MemoryTab
            // 
            this.MemoryTab.Controls.Add(this.Mem_Demographics);
            this.MemoryTab.Controls.Add(this.Mem_Save);
            this.MemoryTab.Controls.Add(this.Mem_QR);
            this.MemoryTab.Controls.Add(this.Mem_View);
            this.MemoryTab.Controls.Add(this.Mem_Clear);
            this.MemoryTab.Controls.Add(this.MemoryFiles);
            this.MemoryTab.Location = new System.Drawing.Point(4, 22);
            this.MemoryTab.Name = "MemoryTab";
            this.MemoryTab.Padding = new System.Windows.Forms.Padding(3);
            this.MemoryTab.Size = new System.Drawing.Size(628, 522);
            this.MemoryTab.TabIndex = 3;
            this.MemoryTab.Text = "Memory";
            this.MemoryTab.UseVisualStyleBackColor = true;
            // 
            // Mem_Demographics
            // 
            this.Mem_Demographics.Location = new System.Drawing.Point(365, 7);
            this.Mem_Demographics.Name = "Mem_Demographics";
            this.Mem_Demographics.Size = new System.Drawing.Size(104, 21);
            this.Mem_Demographics.TabIndex = 10;
            this.Mem_Demographics.Text = "Edit Demographics";
            this.Mem_Demographics.UseVisualStyleBackColor = true;
            this.Mem_Demographics.Click += new System.EventHandler(this.Mem_Demographics_Click);
            // 
            // Mem_Save
            // 
            this.Mem_Save.Location = new System.Drawing.Point(268, 7);
            this.Mem_Save.Name = "Mem_Save";
            this.Mem_Save.Size = new System.Drawing.Size(91, 21);
            this.Mem_Save.TabIndex = 9;
            this.Mem_Save.Text = "Save Files";
            this.Mem_Save.UseVisualStyleBackColor = true;
            this.Mem_Save.Click += new System.EventHandler(this.Mem_Save_Click);
            // 
            // Mem_QR
            // 
            this.Mem_QR.Location = new System.Drawing.Point(171, 7);
            this.Mem_QR.Name = "Mem_QR";
            this.Mem_QR.Size = new System.Drawing.Size(91, 21);
            this.Mem_QR.TabIndex = 8;
            this.Mem_QR.Text = "Query/Retrieve";
            this.Mem_QR.UseVisualStyleBackColor = true;
            this.Mem_QR.Click += new System.EventHandler(this.Mem_QR_Click);
            // 
            // Mem_View
            // 
            this.Mem_View.Location = new System.Drawing.Point(74, 7);
            this.Mem_View.Name = "Mem_View";
            this.Mem_View.Size = new System.Drawing.Size(91, 21);
            this.Mem_View.TabIndex = 7;
            this.Mem_View.Text = "View Selected";
            this.Mem_View.UseVisualStyleBackColor = true;
            this.Mem_View.Click += new System.EventHandler(this.Mem_View_Click);
            // 
            // Mem_Clear
            // 
            this.Mem_Clear.Location = new System.Drawing.Point(8, 7);
            this.Mem_Clear.Name = "Mem_Clear";
            this.Mem_Clear.Size = new System.Drawing.Size(60, 21);
            this.Mem_Clear.TabIndex = 2;
            this.Mem_Clear.Text = "Clear";
            this.Mem_Clear.UseVisualStyleBackColor = true;
            this.Mem_Clear.Click += new System.EventHandler(this.Mem_Clear_Click);
            // 
            // MemoryFiles
            // 
            this.MemoryFiles.AllowDrop = true;
            this.MemoryFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MemoryFiles.FullRowSelect = true;
            this.MemoryFiles.HideSelection = false;
            this.MemoryFiles.Location = new System.Drawing.Point(3, 32);
            this.MemoryFiles.Name = "MemoryFiles";
            this.MemoryFiles.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            this.MemoryFiles.SelectionMode = CodersLab.Windows.Controls.TreeViewSelectionMode.SingleSelect;
            this.MemoryFiles.Size = new System.Drawing.Size(625, 488);
            this.MemoryFiles.TabIndex = 1;
            this.MemoryFiles.DragDrop += new System.Windows.Forms.DragEventHandler(this.FilesView_DragDrop);
            this.MemoryFiles.DragEnter += new System.Windows.Forms.DragEventHandler(this.FilesView_DragEnter);
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.DebugText);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(628, 522);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Debug";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // DebugText
            // 
            this.DebugText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DebugText.Location = new System.Drawing.Point(0, 0);
            this.DebugText.Multiline = true;
            this.DebugText.Name = "DebugText";
            this.DebugText.ReadOnly = true;
            this.DebugText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.DebugText.Size = new System.Drawing.Size(628, 522);
            this.DebugText.TabIndex = 0;
            this.DebugText.DragDrop += new System.Windows.Forms.DragEventHandler(this.FilesView_DragDrop);
            this.DebugText.DragEnter += new System.Windows.Forms.DragEventHandler(this.FilesView_DragEnter);
            // 
            // folderBrowserDialog1
            // 
            this.folderBrowserDialog1.RootFolder = System.Environment.SpecialFolder.MyComputer;
            this.folderBrowserDialog1.ShowNewFolderButton = false;
            // 
            // Form1
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(798, 552);
            this.Controls.Add(this.IntPanel);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.MinimumSize = new System.Drawing.Size(353, 312);
            this.Name = "Form1";
            this.Text = "DICOM Manager";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.IntPanel.ResumeLayout(false);
            this.TypeTab.ResumeLayout(false);
            this.CDROMTab.ResumeLayout(false);
            this.FilesTab.ResumeLayout(false);
            this.MemoryTab.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.Panel IntPanel;
        private System.Windows.Forms.ComboBox SendAnon;
        private System.Windows.Forms.ListView SendList;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.Button AEEdit;
        private System.Windows.Forms.Button AEDel;
        private System.Windows.Forms.Button AEAdd;
        private System.Windows.Forms.ComboBox SendCompression;
        private System.Windows.Forms.Button SendTo;
        private System.Windows.Forms.TabControl TypeTab;
        private System.Windows.Forms.TabPage CDROMTab;
        private System.Windows.Forms.ComboBox CDROMDriveDrop;
        private System.Windows.Forms.Button EjectCD;
        private CodersLab.Windows.Controls.TreeView CDROMView;
        private System.Windows.Forms.Button CDROMLoad;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabPage FilesTab;
        private System.Windows.Forms.Button ClearFiles;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TextBox DebugText;
        private System.Windows.Forms.Button SendAllTo;
        private System.Windows.Forms.Button CDViewSeries;
        private System.Windows.Forms.Button FilesViewSel;
        private System.Windows.Forms.Button AddFiles;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.TabPage MemoryTab;
        private CodersLab.Windows.Controls.TreeView MemoryFiles;

        private System.Windows.Forms.Button CDToMem;
        private System.Windows.Forms.Button Mem_Clear;
        private System.Windows.Forms.Button FilesToMem;
        private CodersLab.Windows.Controls.TreeView FilesView;
        private System.Windows.Forms.Button Mem_View;
        private System.Windows.Forms.Button Mem_QR;
        private System.Windows.Forms.Button Mem_Save;
        private System.Windows.Forms.Button Mem_Demographics;
    }
}