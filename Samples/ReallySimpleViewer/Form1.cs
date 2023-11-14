using System;
using System.Drawing;
using System.Windows.Forms;
using DICOMSharp.Data;
using DICOMSharp.Imaging;
using DICOMSharpControls.Imaging;
using DICOMSharp.Data.Tags;
using DICOMSharp.Logging;

namespace ReallySimpleViewer
{
    public partial class Form1 : Form
    {
        private SimpleViewerPane viewer;

        public Form1()
        {
            InitializeComponent();

            viewer = new SimpleViewerPane();
            viewer.Location = new Point(0, menuStrip1.Height);
            viewer.Size = new Size(ClientSize.Width, ClientSize.Height - menuStrip1.Height);
            viewer.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(viewer);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DICOMData loadedData = new DICOMData();
                loadedData.ParseFile(openFileDialog1.FileName, true, new NullLogger());
                loadedData.Uncompress();

                if (loadedData.Elements.ContainsKey(DICOMTags.PixelData))
                {
                    viewer.AddImage(loadedData);
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }


        private void windowLevelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            viewer.ResetWindowLevel();
        }

        private void viewportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            viewer.ResetView();
        }

        private void alwaysOnToolStripMenuItem_Click(object sender, EventArgs e)
        {
            viewer.FilteringMode = SimpleViewerPane.FilteringModeType.AlwaysFilter;
            alwaysOffToolStripMenuItem.Checked = false;
            alwaysOnToolStripMenuItem.Checked = true;
            automaticToolStripMenuItem.Checked = false;
        }

        private void automaticToolStripMenuItem_Click(object sender, EventArgs e)
        {
            viewer.FilteringMode = SimpleViewerPane.FilteringModeType.AutoFilter;
            alwaysOffToolStripMenuItem.Checked = false;
            alwaysOnToolStripMenuItem.Checked = false;
            automaticToolStripMenuItem.Checked = true;
        }

        private void alwaysOffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            viewer.FilteringMode = SimpleViewerPane.FilteringModeType.NeverFilter;
            alwaysOffToolStripMenuItem.Checked = true;
            alwaysOnToolStripMenuItem.Checked = false;
            automaticToolStripMenuItem.Checked = false;
        }

        private void imagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            viewer.ClearImages();
        }
    }
}
