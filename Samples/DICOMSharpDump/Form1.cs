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
        private System.Windows.Forms.MainMenu mainMenu1;
        private System.Windows.Forms.MenuItem menuItem1;
        private System.Windows.Forms.MenuItem menuItem2;
        private System.Windows.Forms.MenuItem menuItem3;
        private System.Windows.Forms.MenuItem menuItem4;
        private IContainer components;
        private System.Windows.Forms.OpenFileDialog oFD1;
        private System.Windows.Forms.MenuItem menuItem5;

        private System.Windows.Forms.SaveFileDialog sFD1;
        private System.Windows.Forms.MenuItem menuItem7;
        private System.Windows.Forms.MenuItem menuItem6;
        private MenuItem menuItem8;
        private MenuItem menuItem9;

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
            this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this.menuItem5 = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();
            this.menuItem4 = new System.Windows.Forms.MenuItem();
            this.menuItem7 = new System.Windows.Forms.MenuItem();
            this.menuItem6 = new System.Windows.Forms.MenuItem();
            this.oFD1 = new System.Windows.Forms.OpenFileDialog();
            this.sFD1 = new System.Windows.Forms.SaveFileDialog();
            this.menuItem8 = new System.Windows.Forms.MenuItem();
            this.menuItem9 = new System.Windows.Forms.MenuItem();
            this.SuspendLayout();
            // 
            // DICOMTree
            // 
            this.DICOMTree.AllowDrop = true;
            this.DICOMTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DICOMTree.Location = new System.Drawing.Point(0, 7);
            this.DICOMTree.Name = "DICOMTree";
            this.DICOMTree.Size = new System.Drawing.Size(657, 449);
            this.DICOMTree.TabIndex = 0;
            this.DICOMTree.DragDrop += new System.Windows.Forms.DragEventHandler(this.DICOMTree_DragDrop);
            this.DICOMTree.DragEnter += new System.Windows.Forms.DragEventHandler(this.DICOMTree_DragEnter);
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem1,
            this.menuItem7});
            // 
            // menuItem1
            // 
            this.menuItem1.Index = 0;
            this.menuItem1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem2,
            this.menuItem5,
            this.menuItem3,
            this.menuItem4});
            this.menuItem1.Text = "&File";
            // 
            // menuItem2
            // 
            this.menuItem2.Index = 0;
            this.menuItem2.Text = "&Open";
            this.menuItem2.Click += new System.EventHandler(this.menuItem2_Click);
            // 
            // menuItem5
            // 
            this.menuItem5.Index = 1;
            this.menuItem5.Text = "&Save";
            this.menuItem5.Click += new System.EventHandler(this.menuItem5_Click);
            // 
            // menuItem3
            // 
            this.menuItem3.Index = 2;
            this.menuItem3.Text = "-";
            // 
            // menuItem4
            // 
            this.menuItem4.Index = 3;
            this.menuItem4.Text = "E&xit";
            this.menuItem4.Click += new System.EventHandler(this.menuItem4_Click);
            // 
            // menuItem7
            // 
            this.menuItem7.Index = 1;
            this.menuItem7.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem8,
            this.menuItem6,
            this.menuItem9});
            this.menuItem7.Text = "&Tools";
            // 
            // menuItem6
            // 
            this.menuItem6.Index = 1;
            this.menuItem6.Text = "&Anonymize";
            this.menuItem6.Click += new System.EventHandler(this.menuItem6_Click);
            // 
            // menuItem8
            // 
            this.menuItem8.Index = 0;
            this.menuItem8.Text = "&Uncompress";
            this.menuItem8.Click += new System.EventHandler(this.menuItem8_Click);
            // 
            // menuItem9
            // 
            this.menuItem9.Index = 2;
            this.menuItem9.Text = "&Compress J2K";
            this.menuItem9.Click += new System.EventHandler(this.menuItem9_Click);
            // 
            // Form1
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(658, 458);
            this.Controls.Add(this.DICOMTree);
            this.Menu = this.mainMenu1;
            this.Name = "Form1";
            this.Text = "DICOM Dump";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);

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
