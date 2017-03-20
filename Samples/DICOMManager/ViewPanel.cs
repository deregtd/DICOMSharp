using System;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using DICOMSharp.Data;
using DICOMSharp.Data.Tags;


namespace DICOMManager
{
    public partial class ViewPanel : Form
    {
        public ViewPanel()
        {
            InitializeComponent();

            Assembly assem = Assembly.GetExecutingAssembly();
            StreamReader tr = new StreamReader(assem.GetManifestResourceStream("DICOMManager.Resources.ViewerConfig.xml"));
            string xmlcont = tr.ReadToEnd();
            //simpleViewerPane1.SetConfiguration(xmlcont);
        }

        public void SetProgress(int Count, int Max)
        {
            if (Count >= Max)
                LoadProgress.Visible = false;

            LoadProgress.Maximum = Max;
            LoadProgress.Value = Count;
            Application.DoEvents();
        }

        public void AttachImage(DICOMData indata)
        {
            try
            {
                if (indata.Elements.ContainsKey(DICOMTags.PixelData))
                {
                    simpleViewerPane1.AddImage(indata);
                }
            }
            catch (Exception)
            {
            }
            Application.DoEvents();
        }

        private void ViewPanel_FormClosed(object sender, FormClosedEventArgs e)
        {
        }
    }
}