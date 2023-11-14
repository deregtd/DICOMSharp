using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Linq;

using DICOMSharp.Data;
using DICOMSharp.Data.Elements;
using System.Collections.Generic;
using DICOMSharp.Data.Tags;
using System.IO;
using System.Text;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Logging;

namespace DICOMDump
{
    /// <summary>
    /// Summary description for Form1.
    /// </summary>
    public class Form1 : System.Windows.Forms.Form
    {
        private string CurFName;

        private System.Windows.Forms.TreeView DICOMTree;

        private IContainer components;
        private System.Windows.Forms.OpenFileDialog oFD1;
        private System.Windows.Forms.SaveFileDialog sFD1;

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem anonymizeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem uncompressToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem compressJ2kToolStripMenuItem;

        private DICOMData CurDCM;

        public Form1()
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
            this.DICOMTree = new System.Windows.Forms.TreeView();
            this.oFD1 = new System.Windows.Forms.OpenFileDialog();
            this.sFD1 = new System.Windows.Forms.SaveFileDialog();
         
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.anonymizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.uncompressToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.compressJ2kToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();

            this.SuspendLayout();
            // 
            // DICOMTree
            // 
            this.DICOMTree.AllowDrop = true;
            this.DICOMTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DICOMTree.Location = new System.Drawing.Point(0, 20);
            this.DICOMTree.Name = "DICOMTree";
            this.DICOMTree.Size = new System.Drawing.Size(657, 449);
            this.DICOMTree.TabIndex = 0;
            this.DICOMTree.DragDrop += new System.Windows.Forms.DragEventHandler(this.DICOMTree_DragDrop);
            this.DICOMTree.DragEnter += new System.Windows.Forms.DragEventHandler(this.DICOMTree_DragEnter);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.toolsToolStripMenuItem});
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
            this.openToolStripMenuItem.Click += new System.EventHandler(this.menuItem2_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.saveToolStripMenuItem.Text = "&Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.menuItem5_Click);
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
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.menuItem4_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.anonymizeToolStripMenuItem,
            uncompressToolStripMenuItem,
            compressJ2kToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.toolsToolStripMenuItem.Text = "&Tools";
            // 
            // anonymizeToolStripMenuItem
            // 
            this.anonymizeToolStripMenuItem.Name = "anonymizeToolStripMenuItem";
            this.anonymizeToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.anonymizeToolStripMenuItem.Text = "&Anonymize";
            this.anonymizeToolStripMenuItem.Click += new System.EventHandler(this.menuItem6_Click);
            // 
            // uncompressToolStripMenuItem
            // 
            this.uncompressToolStripMenuItem.Name = "uncompressToolStripMenuItem";
            this.uncompressToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.uncompressToolStripMenuItem.Text = "&Uncompress";
            this.uncompressToolStripMenuItem.Click += new System.EventHandler(this.menuItem8_Click);
            // 
            // compressJ2kToolStripMenuItem
            // 
            this.compressJ2kToolStripMenuItem.Name = "compressJ2kToolStripMenuItem";
            this.compressJ2kToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.compressJ2kToolStripMenuItem.Text = "&Compress J2k";
            this.compressJ2kToolStripMenuItem.Click += new System.EventHandler(this.menuItem9_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(628, 477);
            this.Controls.Add(this.DICOMTree);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "DICOM Dump";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.Run(new Form1());
        }

        private void menuItem4_Click(object sender, System.EventArgs e)
        {
            //File/Exit
            Application.Exit();
        }

        private void menuItem2_Click(object sender, System.EventArgs e)
        {
            //File/Open
            oFD1.ShowDialog();
            if (oFD1.FileName == "")
                return;

            LoadFile(oFD1.FileName);
        }

        private void AddElemToNode(DICOMElement elem, TreeNode BaseNode)
        {
            string VR = elem.VR;
            TreeNode newNode = BaseNode.Nodes.Add(elem.Dump());
            if (VR == "SQ")
            {
                List<SQItem> sqItems = (List<SQItem>)elem.Data;

                int iSQCnt = sqItems.Count;

                foreach (SQItem sqItem in sqItems)
                {
                    TreeNode SQNode2 = newNode.Nodes.Add("SQ Item:");

                    foreach (DICOMElement nElem in sqItem.Elements)
                        AddElemToNode(nElem, SQNode2);

                    if (sqItem.IsEncapsulatedImage)
                        SQNode2.Nodes.Add("Encapsulated Image Data: " + sqItem.EncapsulatedImageData.Length + " bytes");
                }
            }
        }

        private void Form1_Load(object sender, System.EventArgs ev)
        {
        }

        private void menuItem5_Click(object sender, System.EventArgs e)
        {
            //File/Save
            sFD1.ShowDialog();
            if (sFD1.FileName == "")
                return;

            CurDCM.WriteFile(sFD1.FileName, new NullLogger());
        }

        private void DICOMTree_DragEnter(object sender, System.Windows.Forms.DragEventArgs e)
        {
            // If the data is a file, display the copy cursor.
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void DICOMTree_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            string[] ToSend = (string[])((IDataObject)e.Data).GetData("FileDrop");

            LoadFile(ToSend[0]);
        }

        private void LoadFile(string FName)
        {
            CurFName = FName;
            CurDCM = new DICOMData();
            CurDCM.ParseFile(CurFName, true, new NullLogger());

            //StringBuilder builder = new StringBuilder();
            //builder.Append("[\r\n");
            //byte[] data = (byte[])CurDCM.Elements[DICOMTags.PixelData].Data;
            //for (int i=0; i<data.Length; i++)
            //{
            //    builder.Append(data[i]);
            //    if (i < data.Length-1) builder.Append(",");
            //    if ((i & 255) == 0) builder.Append("\r\n");
            //}
            //builder.Append("]\r\n");

            //File.WriteAllText(@"c:\src\test.json", builder.ToString());

            RefreshView();
        }

        private void RefreshView()
        {
            DICOMTree.Nodes.Clear();

            TreeNode BaseNode = DICOMTree.Nodes.Add(CurFName);
            foreach (DICOMElement elem in CurDCM.Elements.Values)
                AddElemToNode(elem, BaseNode);
            DICOMTree.ExpandAll();
            DICOMTree.TopNode = BaseNode;
        }

        private void menuItem8_Click(object sender, EventArgs e)
        {
            //Tools/uncompress

            CurDCM.Uncompress();

            RefreshView();
        }

        private void menuItem6_Click(object sender, System.EventArgs e)
        {
            //Tools/anonymize

            CurDCM.Anonymize();

            RefreshView();
        }

        private void menuItem9_Click(object sender, EventArgs e)
        {
            //Tools/Compress J2k
            CurDCM.ChangeTransferSyntax(TransferSyntaxes.JPEG2000ImageCompression);

            RefreshView();
        }
    }
}
