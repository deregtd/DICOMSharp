using System;
using System.Drawing;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Data;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Reflection;
using System.Threading;
using Microsoft.Win32;
using DICOMSharp.Network.Connections;
using DICOMSharp.Network.Workers;
using DICOMSharp.Logging;
using DICOMSharp.Data;
using DICOMSharp.Data.Tags;
using DICOMSharp.Data.Elements;
using System.Threading.Tasks;
using CodersLab.Windows.Controls;
using DICOMSharp.Network.QueryRetrieve;

namespace DICOMManager
{
    /// <summary>
    /// Summary description for Form1.
    /// </summary>
    public partial class Form1 : System.Windows.Forms.Form
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int GetDriveTypeA(string nDrive);
        [DllImport("winmm.dll", EntryPoint = "mciSendStringA")]
        static extern void mciSendStringA(string lpstrCommand, string lpstrReturnString, long uReturnLength, long hwndCallback);

        private static Form1 TForm;
        private bool Loaded = false, LoadingCD = false;
        private bool Listening = false;
        public struct stDICOMEntity
        {
            public stDICOMEntity(string AET, string Addy, int nPort, string nName, int nQueueLen)
            {
                AETitle = AET;
                Address = Addy;
                Port = nPort;
                Name = nName;
                QueueLen = nQueueLen;
            }
            public string AETitle;
            public string Address;
            public int Port;
            public string Name;
            public int QueueLen;

            internal ApplicationEntity ToAE()
            {
                return new ApplicationEntity(AETitle, Address, (ushort)Port);
            }
        };
        private SortedList Entities = new SortedList();

        public class cTransfer
        {
            public cTransfer(DICOMConnection conn, stDICOMEntity nTarget, Array nSendFiles, string[] nSendIDs)
            {
                Conn = conn;
                Target = nTarget;
                SendFiles = nSendFiles;
                SendIDs = nSendIDs;
                Sent = 0;
                Send = true;
            }
            public cTransfer(DICOMConnection conn, stDICOMEntity nSource)
            {
                Conn = conn;
                Target = nSource;
                Sent = 0;
                Send = false;
            }

            public DICOMConnection Conn;
            public stDICOMEntity Target;
            public Array SendFiles;
            public string[] SendIDs;
            public int Sent;
            public bool Send;
            public StatusBar bar;
        };

        private Dictionary<DICOMConnection, cTransfer> IncomingStores = new Dictionary<DICOMConnection, cTransfer>();

        private Hashtable ImageTransIDs = new Hashtable();

        public enum eAEType { eFilm = 1, Custom = 99 };
        public eAEType AEType = eAEType.Custom;
        public string AETitle = "DICOM";
        public int DICOMPort = 4006;

        private Hashtable BarList = new Hashtable();

        private StatusStrip LoadStrip = null;
        public static ArrayList ViewWindows = new ArrayList();

        private SubscribableListener logger;
        private DICOMListener listener;

        public Form1()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            logger = new SubscribableListener();
            logger.MessageLogged += new SubscribableListener.LoggedMessageHandler(logger_MessageLogged);

            listener = new DICOMListener(logger, false);
            listener.AssociationRequest += new DICOMListener.BasicConnectionHandler(listener_AssociationRequest);
            listener.ConnectionClosed += new DICOMListener.BasicConnectionHandler(listener_ConnectionClosed);
            listener.StoreRequest += new DICOMListener.StoreRequestHandler(listener_StoreRequest);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] Args)
        {
            Application.Run(new Form1());
        }

        private void menuItem2_Click(object sender, System.EventArgs e)
        {
            //File/Exit
            Application.Exit();
        }

        private void menuItem4_Click(object sender, System.EventArgs e)
        {
            //Tools/Options
            Options of = new Options();
            DialogResult dres = of.ShowDialog(this);
            if (dres != DialogResult.OK)
                return;

            if (of.DICOM_CustomAE.Checked) AEType = eAEType.Custom;
            if (of.DICOM_eFilmAE.Checked) AEType = eAEType.eFilm;
            AETitle = of.DICOM_AETitle.Text;
            DICOMPort = int.Parse(of.DICOM_Port.Text);

            SaveOptions();
            LoadOptions();
        }

        private void Form1_Load(object sender, System.EventArgs e)
        {
            TForm = this;

            //Setup defaults
            SendCompression.SelectedIndex = 0;
            SendAnon.SelectedIndex = 0;

            //Find all CD Drives
            for (char i = 'A'; i <= 'Z'; i++)
            {
                string drivename = i + ":\\";
                if (GetDriveTypeA(drivename) == 5)
                    CDROMDriveDrop.Items.Add(drivename);
            }
            if (CDROMDriveDrop.Items.Count > 0)
                CDROMDriveDrop.SelectedIndex = 0;

            //Load options
            LoadOptions();
        }

        void listener_AssociationRequest(DICOMConnection conn)
        {
            if (!IncomingStores.ContainsKey(conn))
            {
                //make new conn

                if (conn.CalledAE.Trim() != this.AETitle)
                {
                    conn.SendAssociateRJ(AssociateRJResults.RejectedPermanent, AssociateRJSources.DICOMULServiceProviderPresentation, AssociateRJReasons.CalledAENotRecognized);
                    return;
                }

                string RemoteAE = conn.CallingAE.Trim();
                if (Entities.ContainsKey(RemoteAE))
                {
                    stDICOMEntity nSource = (stDICOMEntity)Entities[RemoteAE];
                    conn.SendAssociateAC();

                    cTransfer NewTrans = new cTransfer(conn, nSource);
                    IncomingStores.Add(conn, NewTrans);

                    this.Invoke((MethodInvoker)delegate
                    {
                        StatusStrip nstrip = new StatusStrip();
                        nstrip.Items.Add("Receiving Images from " + RemoteAE + "...");
                        nstrip.SizingGrip = false;
                        IntPanel.Height -= nstrip.Height;
                        this.Height += nstrip.Height;
                        nstrip.Parent = this;
                        BarList.Add(NewTrans.Conn, nstrip);
                    });
                }
                else
                {
                    conn.SendAssociateRJ(AssociateRJResults.RejectedPermanent, AssociateRJSources.DICOMULServiceProviderPresentation, AssociateRJReasons.CallingAENotRecognized);
                }
            }
        }

        void listener_StoreRequest(DICOMConnection conn, DICOMData data)
        {
            this.Invoke((MethodInvoker)delegate
            {
                TForm.AddObjToMemory(data, true);

                cTransfer transfer = IncomingStores[conn];
                transfer.Sent++;

                StatusStrip strip = (StatusStrip)BarList[conn];
                strip.Items[0].Text = "Received " + transfer.Sent + " Images from " + transfer.Target.AETitle + "...";
            });
            conn.SendCSTORERSP(CommandStatus.Success);
        }

        void listener_ConnectionClosed(DICOMConnection conn)
        {
            if (IncomingStores.ContainsKey(conn))
            {
                IncomingStores.Remove(conn);

                this.Invoke((MethodInvoker)delegate
                {
                    StatusStrip strip = (StatusStrip)BarList[conn];
                    strip.Parent = null;
                    IntPanel.Height += strip.Height;
                    this.Height -= strip.Height;
                });

                BarList.Remove(conn);
            }
        }

        void logger_MessageLogged(LogLevel level, string message)
        {
            Task.Run(() =>
            {
                this.Invoke((MethodInvoker)delegate
                {
                    Debug.WriteLine(message);

                    DebugText.Text = message + "\r\n" + DebugText.Text;
                    if (DebugText.Text.Length > 15000)
                        DebugText.Text = DebugText.Text.Substring(0, 10000);
                });
            });
        }

        private void LoadOptions()
        {
            RegistryKey rkey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\PACSsoft\\DICOMManager");

            if (rkey != null)
            {
                //Make entities list
                Entities.Clear();
                SendList.Items.Clear();

                RegistryKey entkey = rkey.OpenSubKey("Entities");
                if (entkey != null)
                {
                    foreach (string Ent in entkey.GetSubKeyNames())
                    {
                        RegistryKey entk = entkey.OpenSubKey(Ent);
                        Entities.Add(Ent, new stDICOMEntity(Ent, (string)entk.GetValue("Address"), (int)entk.GetValue("Port"), (string)entk.GetValue("Name"), (int)entk.GetValue("QueueLen", -1)));

                        ListViewItem lvi = new ListViewItem(new string[] { (string)entk.GetValue("Name"), Ent });
                        lvi.Tag = Ent;
                        SendList.Items.Add(lvi);
                        entk.Close();
                    }
                    entkey.Close();
                }
                if ((SendList.SelectedIndices.Count == 0) && (SendList.Items.Count > 0))
                    SendList.Items[0].Selected = true;

                SendCompression.SelectedIndex = (int)rkey.GetValue("Compression", 0);
                SendAnon.SelectedIndex = (int)rkey.GetValue("Anonymization", 0);

                _uncompressImages = (SendCompression.SelectedIndex == 1);
                _anonymizeImages = (SendAnon.SelectedIndex == 1);

                AEType = (eAEType)rkey.GetValue("AEType");
                AETitle = (string)rkey.GetValue("AETitle");
                if (AEType == eAEType.eFilm)
                {
                    RegistryKey efkey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\eFilm Medical\\eFilm\\Settings");
                    if (efkey != null)
                    {
                        AETitle = (string)efkey.GetValue("AE Title");
                        efkey.Close();
                    }
                }

                DICOMPort = (int)rkey.GetValue("DICOMPort", (int)4006);
                if (Listening)
                {
                    listener.StopListening();
                    Listening = false;
                }

                try
                {
                    listener.StartListening((ushort)DICOMPort);
                    Listening = true;
                }
                catch
                { }

                rkey.Close();
            }
            Loaded = true;
        }

        private bool _uncompressImages;
        private bool _anonymizeImages;

        public void SaveOptions()
        {
            if (!Loaded) return;

            RegistryKey rkey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\PACSsoft\\DICOMManager");

            //Delete old entities
            if (rkey.OpenSubKey("Entities") != null)
                rkey.DeleteSubKeyTree("Entities");

            //Make entities list
            RegistryKey entkey = rkey.CreateSubKey("Entities");
            foreach (stDICOMEntity Ent in Entities.Values)
            {
                RegistryKey entk = entkey.CreateSubKey(Ent.AETitle);
                entk.SetValue("Address", Ent.Address);
                entk.SetValue("Port", Ent.Port);
                entk.SetValue("Name", Ent.Name);
                entk.SetValue("QueueLen", Ent.QueueLen);
                entk.Close();
            }
            entkey.Close();

            rkey.SetValue("Compression", (int)SendCompression.SelectedIndex);
            rkey.SetValue("Anonymization", (int)SendAnon.SelectedIndex);

            _uncompressImages = (SendCompression.SelectedIndex == 1);
            _anonymizeImages = (SendAnon.SelectedIndex == 1);

            rkey.SetValue("AEType", (int)AEType);
            rkey.SetValue("AETitle", AETitle);
            rkey.SetValue("DICOMPort", DICOMPort);

            rkey.Close();
        }

        private void SendCompression_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            SaveOptions();
        }

        private void AEAdd_Click(object sender, System.EventArgs e)
        {
            AEForm aef = new AEForm();
            DialogResult dres = aef.ShowDialog(this);
            if (dres != DialogResult.OK)
                return;

            int QL = -1;
            if (aef.Queue_Lim.Checked) QL = int.Parse(aef.Queue_Len.Text);

            Entities.Add(aef.AETitle.Text, new stDICOMEntity(aef.AETitle.Text, aef.Address.Text, int.Parse(aef.Port.Text), aef.EntName.Text, QL));
            SaveOptions();
            LoadOptions();
        }

        private void AEEdit_Click(object sender, System.EventArgs e)
        {
            if (SendList.SelectedIndices.Count < 1)
                return;

            stDICOMEntity de = (stDICOMEntity)Entities[(string)SendList.SelectedItems[0].Tag];

            AEForm aef = new AEForm();

            aef.AETitle.Text = de.AETitle;
            aef.Address.Text = de.Address;
            aef.Port.Text = de.Port.ToString();
            aef.EntName.Text = de.Name;
            if (de.QueueLen == -1) { aef.Queue_Unlim.Checked = true; aef.Queue_Lim.Checked = false; aef.Queue_Len.Text = "100"; }
            if (de.QueueLen >= 0) { aef.Queue_Unlim.Checked = false; aef.Queue_Lim.Checked = true; aef.Queue_Len.Text = de.QueueLen.ToString(); }

            DialogResult dres = aef.ShowDialog(this);
            if (dres != DialogResult.OK)
                return;

            Entities.Remove(de.AETitle);
            de.AETitle = aef.AETitle.Text;
            de.Address = aef.Address.Text;
            de.Port = int.Parse(aef.Port.Text);
            de.Name = aef.EntName.Text;
            if (aef.Queue_Unlim.Checked) de.QueueLen = -1;
            else de.QueueLen = int.Parse(aef.Queue_Len.Text);
            Entities.Add(de.AETitle, de);

            SaveOptions();
            LoadOptions();
        }

        private void AEDel_Click(object sender, System.EventArgs e)
        {
            if (SendList.SelectedIndices.Count < 1)
                return;

            if (MessageBox.Show("Are you sure you want to delete this item?", "Sure?", MessageBoxButtons.YesNo) == DialogResult.No)
                return;

            string AE = (string)SendList.SelectedItems[0].Tag;
            Entities.Remove(AE);
            SaveOptions();
            LoadOptions();
        }

        private delegate string loadCDHelper1(SQItem item, uint tag, string def);
        private void CDROMLoad_Click(object sender, System.EventArgs e)
        {
            if (LoadingCD)
            {
                MessageBox.Show("Error: Already Loading CD, please wait for it to finish before parsing another one!", "Error!");
                return;
            }

            CDROMView.Nodes.Clear();

            string BaseDir = CDROMDriveDrop.Text; //@"C:\Users\David\Desktop\";//
            FileInfo test = new FileInfo(BaseDir + "DICOMDIR");
            if (!test.Exists)
            {
                MessageBox.Show("Error: " + BaseDir + "DICOMDIR does not exist!", "Error!");
                return;
            }

            LoadStrip = new StatusStrip();
            LoadStrip.SizingGrip = false;
            LoadStrip.Items.Add("Loading CD...");
            IntPanel.Height -= LoadStrip.Height;
            this.Height += LoadStrip.Height;
            LoadStrip.Parent = this;

            LoadingCD = true;
            Application.DoEvents();

            DICOMData DCM = new DICOMData();
            DCM.ParseFile(BaseDir + "DICOMDIR", false, logger);

            ImageTransIDs.Clear();

            TreeNode CurPatNode = new TreeNode(""), CurStuNode = new TreeNode(""), LastSerNode = new TreeNode("");

            List<object> PatItems = null;
            List<object> StuItems = null;
            List<object> SerItems = null;
            int imcnt = 0;

            ToolStripProgressBar npb = new ToolStripProgressBar();
            npb.Style = ProgressBarStyle.Continuous;
            npb.AutoSize = false;
            npb.Width = 300;
            LoadStrip.Items.Add(npb);

            DICOMElement elem = DCM[DICOMTags.DirectoryRecordSequence];
            List<SQItem> dirList = (List<SQItem>)elem.Data;
            int iCount = dirList.Count;

            LoadStrip.Items[0].Text = "Parsing DICOMDIR...";

            Application.DoEvents();

            loadCDHelper1 getDisplayOrDefault = delegate(SQItem item, uint tag, string def)
            {
                if (item.ElementsLookup.ContainsKey(tag))
                    return item.ElementsLookup[tag].Display;
                return def;
            };

            npb.Maximum = iCount;
            for (int i = 0; i < iCount; i++)
            {
                npb.Value = i;

                SQItem DCM2 = dirList[i];
                string recordType = DCM2.ElementsLookup[DICOMTags.DirectoryRecordType].Display.Trim();
                if (recordType == "PATIENT")
                {
                    CurPatNode = CDROMView.Nodes.Add("ID: " + DCM2.ElementsLookup[DICOMTags.PatientID].Display + ", Name: " + DCM2.ElementsLookup[DICOMTags.PatientName].Display);

                    PatItems = new List<object>();
                    CurPatNode.Tag = PatItems;
                }
                else if (recordType == "STUDY")
                {
                    CurStuNode = CurPatNode.Nodes.Add(FormDate(DCM2.ElementsLookup[DICOMTags.StudyDate].Display) + " - " + DCM2.ElementsLookup[DICOMTags.StudyDescription].Display + " - Acc: " + getDisplayOrDefault(DCM2, DICOMTags.AccessionNumber, "?"));

                    StuItems = new List<object>();
                    CurStuNode.Tag = StuItems;
                }
                else if (recordType == "SERIES")
                {
                    LastSerNode.Text += " - " + imcnt.ToString() + " Image(s)";
                    imcnt = 0;

                    LastSerNode = CurStuNode.Nodes.Add(getDisplayOrDefault(DCM2, DICOMTags.Modality, "??") + " - " + getDisplayOrDefault(DCM2, DICOMTags.SeriesNumber, "?") + " - " + getDisplayOrDefault(DCM2, DICOMTags.SeriesDescription, "?"));

                    LastSerNode.Tag = new List<object>();

                    SerItems = new List<object>();
                    PatItems.Add(SerItems);
                    StuItems.Add(SerItems);
                    ((List<object>)LastSerNode.Tag).Add(SerItems);
                }
                else if ((recordType == "IMAGE") || (recordType == "PRIVATE"))
                {
                    imcnt++;
                    SerItems.Add(BaseDir + DCM2.ElementsLookup[DICOMTags.ReferencedFileID].Display);
                    ImageTransIDs[DCM2.ElementsLookup[DICOMTags.ReferencedSOPClassUIDInFile].Display] = 1;
                }
            }
            LastSerNode.Text += " - " + imcnt.ToString() + " Image(s)";

            CDROMView.SelectedNodes.Add(CDROMView.Nodes[0]);

            LoadStrip.Parent = null;
            IntPanel.Height += LoadStrip.Height;
            this.Height -= LoadStrip.Height;
            LoadStrip.Dispose();
            LoadStrip = null;
            LoadingCD = false;
        }

        private string FormDate(string instr)
        {
            string y = instr.Substring(0, 4), m = instr.Substring(4, 2), d = instr.Substring(6, 2);
            return m + "/" + d + "/" + y;
        }

        private void FilesView_DragEnter(object sender, System.Windows.Forms.DragEventArgs e)
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

        private void FilesView_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            TypeTab.SelectedTab = FilesTab;

            string[] DropItems = (string[])((System.Windows.Forms.IDataObject)e.Data).GetData("FileDrop");

            foreach (string item in DropItems)
                AddToQueue(null, item);

            if (FilesView.SelectedNodes.Count == 0)
                FilesView.SelectedNodes.Add(FilesView.Nodes[0]);
        }

        private void AddToQueue(TreeNode outNode, string Path)
        {
            FileInfo finfo = new FileInfo(Path);
            if (finfo.Exists)
            {
                if (finfo.Extension.ToLower() != ".jpg")
                {
                    //File
                    if (outNode == null) (FilesView.Nodes.Add(Path)).Tag = 1;
                    else (outNode.Nodes.Add(Path)).Tag = 1;
                }
            }
            else
            {
                //Adding Directory
                DirectoryInfo dinfo = new DirectoryInfo(Path);

                TreeNode tpn;
                if (outNode == null) tpn = FilesView.Nodes.Add((string)dinfo.FullName);
                else tpn = outNode.Nodes.Add(dinfo.Name);
                tpn.Tag = 0;

                //add files
                FileInfo[] fcontents = dinfo.GetFiles();
                foreach (FileInfo finfo2 in fcontents)
                {
                    if (finfo2.Extension.ToLower() == ".jpg") continue;

                    (tpn.Nodes.Add(finfo2.FullName)).Tag = 1;
                }

                //walk subdirs
                DirectoryInfo[] subdirs = dinfo.GetDirectories();
                foreach (DirectoryInfo dinfo2 in subdirs)
                {
                    AddToQueue(tpn, dinfo2.FullName);
                }
            }
        }

        private void ClearFiles_Click(object sender, System.EventArgs e)
        {
            FilesView.Nodes.Clear();
        }

        private Queue<SendableImage> GetCurFiles(bool SelOnly, bool NeedSyntaxes)
        {
            Queue<SendableImage> SendFiles = new Queue<SendableImage>();

            var fileNames = new List<string>();

            if (TypeTab.SelectedTab == CDROMTab)
            {
                //CDROM
                foreach (TreeNode tn in (SelOnly ? CDROMView.SelectedNodes as IEnumerable<TreeNode> : CDROMView.Nodes as IEnumerable<TreeNode>))
                {
                    var stuList = tn.Tag as List<List<string>>;
                    foreach (var sers in stuList)
                    {
                        fileNames.AddRange(sers);
                    }
                }
            }
            else if (TypeTab.SelectedTab == FilesTab)
            {
                //Files

                if (SelOnly)
                {
                    foreach (TreeNode tn in FilesView.SelectedNodes)
                        SendHack(ref fileNames, tn);
                }
                else
                {
                    foreach (TreeNode tn in FilesView.Nodes)
                        SendHack(ref fileNames, tn);
                }
            }
            else if (TypeTab.SelectedTab == MemoryTab)
            {
                if (SelOnly)
                {
                    foreach (TreeNode tn in MemoryFiles.SelectedNodes)
                        MemorySendHack(ref SendFiles, tn);
                }
                else
                {
                    foreach (TreeNode tn in MemoryFiles.Nodes)
                        MemorySendHack(ref SendFiles, tn);
                }
            }

            foreach (string fname in fileNames)
            {
                if (NeedSyntaxes)
                {
                    var dicomData = new DICOMData();
                    if (dicomData.ParseFile(fname, false, logger))
                    {
                        SendFiles.Enqueue(new SendableImage
                        {
                            FilePath = fname,
                            AbstractSyntax = DICOMSender.ExtractAbstractSyntaxFromDicomData(dicomData),
                            TransferSyntax = dicomData.TransferSyntax
                        });
                    }
                }
                else
                {
                    SendFiles.Enqueue(new SendableImage
                    {
                        FilePath = fname
                    });
                }
            }

            return SendFiles;
        }

        private void SendHack(ref List<string> fileList, TreeNode node)
        {
            if ((int)node.Tag == 1)
                fileList.Add(node.Text);
            else
            {
                foreach (TreeNode nnode in node.Nodes)
                    SendHack(ref fileList, nnode);
            }
        }

        private void MemorySendHack(ref Queue<SendableImage> outlist, TreeNode node)
        {
            if (node.Nodes.Count > 0)
            {
                foreach (TreeNode nnode in node.Nodes)
                    MemorySendHack(ref outlist, nnode);
            }
            else
            {
                string PatID = ((string)node.Parent.Parent.Tag).Substring(3);
                string StuID = ((string)node.Parent.Tag).Substring(3);
                string SerID = ((string)node.Tag).Substring(3);

                foreach (var dicomData in (List<DICOMData>)((Hashtable)((Hashtable)MemPatItems[PatID])[StuID])[SerID])
                    outlist.Enqueue(new SendableImage { DicomData = dicomData });
            }
        }

        private void GenSendList(bool SelOnly)
        {
            Queue<SendableImage> SendFiles = GetCurFiles(SelOnly, true);
            if (SendFiles == null || SendFiles.Count == 0)
                return;

            Hashtable SendIDs = new Hashtable();
            if (TypeTab.SelectedTab == CDROMTab)
                SendIDs = ImageTransIDs;

            StartSend(SendFiles);
        }

        private void SendTo_Click(object sender, System.EventArgs e)
        {
            GenSendList(true);
        }

        private void SendAllTo_Click(object sender, System.EventArgs e)
        {
            GenSendList(false);
        }

        private void StartSend(Queue<SendableImage> SendFiles)
        {
            //Figure out who we're sending to
            if (SendList.SelectedItems.Count < 1)
                return;

            string AESend = (string)SendList.SelectedItems[0].Tag;
            AddSend(AESend, SendFiles);
        }

        private void AddSend(string AESend, Queue<SendableImage> SendFiles)
        {
            //make sending arrays of files work

            stDICOMEntity destEntity = (stDICOMEntity)Entities[(string)AESend];
            DICOMSender sender = new DICOMSender(logger, "Sender", false);
            sender.PreSendImage += new DICOMSender.PreSendImageHandler(sender_PreSendImage);
            sender.SendUpdate += new DICOMSender.SendUpdateHandler(sender_SendUpdate);

            StatusStrip nstrip = new StatusStrip();
            nstrip.Items.Add("Sending " + SendFiles.Count.ToString() + " to " + AESend + "...");
            nstrip.SizingGrip = false;
            ToolStripProgressBar npb = new ToolStripProgressBar();
            npb.Style = ProgressBarStyle.Continuous;
            npb.Width = 300;
            npb.AutoSize = false;
            npb.Value = 0;
            npb.Maximum = SendFiles.Count;
            nstrip.Items.Add(npb);
            IntPanel.Height -= nstrip.Height;
            this.Height += nstrip.Height;
            nstrip.Parent = this;

            sender.Send(new ApplicationEntity(AETitle), destEntity.ToAE(), SendFiles, null, (ushort)0);
            BarList.Add(sender, nstrip);
        }

        private void sender_PreSendImage(DICOMSender sender, DICOMData imageAboutToSend)
        {
            if (this._uncompressImages)
            {
                // Decompress All
                imageAboutToSend.Uncompress();
            }

            if (this._anonymizeImages)
            {
                // Anonymize Patient Info
                imageAboutToSend.Anonymize();
            }
        }

        void sender_SendUpdate(DICOMSender sender, ushort remaining, ushort completed, ushort warned, ushort failed)
        {
            this.Invoke((MethodInvoker)delegate
            {
                StatusStrip nstrip = (StatusStrip)BarList[sender];
                if (nstrip == null)
                    return;

                if (remaining == 0)
                {
                    //nuke
                    nstrip.Parent = null;
                    IntPanel.Height += nstrip.Height;
                    this.Height -= nstrip.Height;
                    BarList.Remove(sender);
                }
                else
                {
                    ToolStripProgressBar pb = (ToolStripProgressBar)nstrip.Items[1];
                    pb.Value = completed + failed;
                    nstrip.Items[0].Text = "Sent " + completed.ToString() + (failed > 0 ? " (" + failed + " failed)" : "") + "/" + pb.Maximum.ToString() +
                        " to " + sender.TargetAE + "...";
                }
            });
        }

        private void EjectCD_Click(object sender, System.EventArgs e)
        {
            string BaseDir = CDROMDriveDrop.Text;
            string rt = "";
            mciSendStringA("set CDAudio!" + BaseDir + " door open", rt, 0, 0);
        }

        private void menuItem6_Click(object sender, EventArgs e)
        {
            //Help/About

            AboutBox1 tpab = new AboutBox1();
            tpab.ShowDialog();
        }

        private void CDViewSeries_Click(object sender, EventArgs e)
        {
            ShowSeries(GetCurFiles(true, false));
        }

        private void FilesViewSel_Click(object sender, EventArgs e)
        {
            ShowSeries(GetCurFiles(true, false));
        }

        private void Mem_View_Click(object sender, EventArgs e)
        {
            ShowSeries(GetCurFiles(true, false));
        }

        private void ShowSeries(Queue<SendableImage> ShowFiles)
        {
            if (ShowFiles == null || ShowFiles.Count == 0)
            {
                MessageBox.Show("You must select something before attempting to view it.  Make sure you have a CD or local files loaded first.");
                return;
            }

            ViewPanel vp = new ViewPanel();
            vp.SetProgress(0, ShowFiles.Count);
            vp.Visible = true;
            ViewWindows.Add(vp);

            string errorList = "";

            int cnt = 0;
            foreach (var sf in ShowFiles)
            {
                if (sf.DicomData != null)
                {
                    vp.AttachImage(sf.DicomData);
                }
                else
                {
                    DICOMData tpd = new DICOMData();
                    if (tpd.ParseFile(sf.FilePath, true, logger))
                        vp.AttachImage(tpd);
                    else
                        errorList += sf.FilePath + "\r\n";
                }

                vp.SetProgress(++cnt, ShowFiles.Count);
            }

            if (errorList != "")
                MessageBox.Show("Error opening files:\r\n\r\n" + errorList);
        }

        private void AddFiles_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                TypeTab.SelectedTab = FilesTab;

                AddToQueue(null, folderBrowserDialog1.SelectedPath);

                if (FilesView.SelectedNodes.Count == 0)
                    FilesView.SelectedNodes.Add(FilesView.Nodes[0]);
            }
        }

        private void CDToMem_Click(object sender, EventArgs e)
        {
            AddFilesToMemory(GetCurFiles(true, false));
            UpdateMemTree();
        }

        private void FilesToMem_Click(object sender, EventArgs e)
        {
            AddFilesToMemory(GetCurFiles(true, false));
            UpdateMemTree();
        }

        private void AddFilesToMemory(Queue<SendableImage> FileList)
        {
            if (FileList == null || FileList.Count == 0)
            {
                MessageBox.Show("You must select something before attempting to view it.  Make sure you have a CD or local files loaded first.");
                return;
            }

            LoadingCD = true;
            LoadStrip = new StatusStrip();
            LoadStrip.SizingGrip = false;
            LoadStrip.Items.Add("Loading " + FileList.Count.ToString() + " Files...");
            IntPanel.Height -= LoadStrip.Height;
            this.Height += LoadStrip.Height;
            LoadStrip.Parent = this;

            ToolStripProgressBar npb = new ToolStripProgressBar();
            npb.Style = ProgressBarStyle.Continuous;
            npb.AutoSize = false;
            npb.Width = 300;
            LoadStrip.Items.Add(npb);

            npb.Maximum = FileList.Count;
            npb.Value = 0;

            foreach(var sendable in FileList)
            {
                DICOMData ndata = new DICOMData();
                ndata.ParseFile(sendable.FilePath, true, logger);
                AddObjToMemory(ndata, false);
                npb.Value++;
                Application.DoEvents();
            }

            LoadStrip.Parent = null;
            IntPanel.Height += LoadStrip.Height;
            this.Height -= LoadStrip.Height;
            LoadStrip.Dispose();
            LoadStrip = null;
            LoadingCD = false;
        }

        Hashtable PatLookup = new Hashtable();
        Hashtable StuLookup = new Hashtable();
        Hashtable SerLookup = new Hashtable();

        public void AddObjToMemory(DICOMData DFile, bool AutoUpdate)
        {
            string PatID = String.IsNullOrEmpty(DFile[DICOMTags.PatientID].Display) ? "" : DFile[DICOMTags.PatientID].Display;
            string StuID = DFile[DICOMTags.StudyInstanceUID].Display;
            string SerID = DFile[DICOMTags.SeriesInstanceUID].Display;

            if (!MemPatItems.Contains(PatID))
                MemPatItems[PatID] = new Hashtable();

            Hashtable PatItem = (Hashtable)MemPatItems[PatID];

            if (!PatItem.Contains(StuID))
                PatItem[StuID] = new Hashtable();

            Hashtable StuItem = (Hashtable)PatItem[StuID];

            if (!StuItem.Contains(SerID))
                StuItem[SerID] = new List<DICOMData>();

            List<DICOMData> SerItem = (List<DICOMData>)StuItem[SerID];

            SerItem.Add(DFile);

            string PatName = String.IsNullOrEmpty(DFile[DICOMTags.PatientName].Display) ? "" : DFile[DICOMTags.PatientName].Display;
            string StuAcc = String.IsNullOrEmpty(DFile[DICOMTags.AccessionNumber].Display) ? "" : DFile[DICOMTags.AccessionNumber].Display;
            string StuName = String.IsNullOrEmpty(DFile[DICOMTags.StudyDescription].Display) ? "" : DFile[DICOMTags.StudyDescription].Display;
            string SerName = String.IsNullOrEmpty(DFile[DICOMTags.SeriesDescription].Display) ? "" : DFile[DICOMTags.SeriesDescription].Display;

            PatLookup[PatID] = PatID + " - " + PatName;
            StuLookup[StuID] = StuAcc + " - " + StuName;
            SerLookup[SerID] = SerName;

            if (AutoUpdate)
                UpdateMemTree();
        }

        public void RemObjFromMemory(DICOMData DFile)
        {
            string PatID = DFile[DICOMTags.PatientID].Display;
            string StuID = DFile[DICOMTags.StudyInstanceUID].Display;
            string SerID = DFile[DICOMTags.SeriesInstanceUID].Display;

            //Walk up the tree...
            if (MemPatItems.Contains(PatID))
            {
                Hashtable PatItem = (Hashtable)MemPatItems[PatID];

                if (PatItem.Contains(StuID))
                {
                    Hashtable StuItem = (Hashtable)PatItem[StuID];

                    if (StuItem.Contains(SerID))
                    {
                        List<DICOMData> SerItem = (List<DICOMData>)StuItem[SerID];

                        if (SerItem.Contains(DFile))
                        {
                            //Found it.  Now delete.
                            SerItem.Remove(DFile);
                            if (SerItem.Count == 0)
                            {
                                StuItem.Remove(SerID);
                                if (StuItem.Count == 0)
                                {
                                    PatItem.Remove(StuID);
                                    if (PatItem.Count == 0)
                                    {
                                        MemPatItems.Remove(PatID);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        Hashtable MemPatItems = new Hashtable();

        private void UpdateMemTree()
        {
            foreach (string PatID in MemPatItems.Keys)
            {
                TreeNode PatNode;
                if (!MemoryFiles.Nodes.ContainsKey(PatID))
                {
                    PatNode = MemoryFiles.Nodes.Add(PatID, (string)PatLookup[PatID]);
                    PatNode.Tag = "Pat" + PatID;
                }
                else
                {
                    PatNode = MemoryFiles.Nodes.Find(PatID, false)[0];
                }

                Hashtable PatItem = (Hashtable)MemPatItems[PatID];
                foreach (string StuID in PatItem.Keys)
                {
                    TreeNode StuNode;
                    if (!PatNode.Nodes.ContainsKey(StuID))
                    {
                        StuNode = PatNode.Nodes.Add(StuID, (string)StuLookup[StuID]);
                        StuNode.Tag = "Stu" + StuID;
                    }
                    else
                    {
                        StuNode = PatNode.Nodes.Find(StuID, false)[0];
                    }

                    Hashtable StuItem = (Hashtable)PatItem[StuID];
                    foreach (string SerID in StuItem.Keys)
                    {
                        TreeNode SerNode;
                        if (!StuNode.Nodes.ContainsKey(SerID))
                        {
                            SerNode = StuNode.Nodes.Add(SerID, (string)SerLookup[SerID]);
                            SerNode.Tag = "Ser" + SerID;
                        }
                        else
                        {
                            SerNode = StuNode.Nodes.Find(SerID, false)[0];
                        }

                        List<DICOMData> SerItem = (List<DICOMData>)StuItem[SerID];
                        SerNode.Text = (string)SerLookup[SerID] + " (" + SerItem.Count + " Ims)";
                    }

                    LinkedList<TreeNode> ToNuke3 = new LinkedList<TreeNode>();
                    foreach (TreeNode SerNode in StuNode.Nodes)
                    {
                        string SerID = ((string)SerNode.Tag).Substring(3);
                        if (!StuItem.ContainsKey(SerID))
                            ToNuke3.AddLast(SerNode);
                    }
                    foreach (TreeNode tn in ToNuke3)
                        StuNode.Nodes.Remove(tn);
                }

                LinkedList<TreeNode> ToNuke2 = new LinkedList<TreeNode>();
                foreach (TreeNode StuNode in PatNode.Nodes)
                {
                    string StuID = ((string)StuNode.Tag).Substring(3);
                    if (!PatItem.ContainsKey(StuID))
                        ToNuke2.AddLast(StuNode);
                }
                foreach (TreeNode tn in ToNuke2)
                    PatNode.Nodes.Remove(tn);
            }

            LinkedList<TreeNode> ToNuke = new LinkedList<TreeNode>();
            foreach (TreeNode PatNode in MemoryFiles.Nodes)
            {
                string PatID = ((string)PatNode.Tag).Substring(3);
                if (!MemPatItems.ContainsKey(PatID))
                    ToNuke.AddLast(PatNode);
            }
            foreach (TreeNode tn in ToNuke)
                MemoryFiles.Nodes.Remove(tn);
        }

        private void Mem_QR_Click(object sender, EventArgs e)
        {
            if (SendList.SelectedItems.Count == 0)
                return;

            stDICOMEntity de = (stDICOMEntity)Entities[(string)SendList.SelectedItems[0].Tag];

            SearchWnd a = new SearchWnd(de, AETitle);
            a.Show();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //kill this before we stop listening so we don't get events after shutdown
            logger.MessageLogged -= new SubscribableListener.LoggedMessageHandler(logger_MessageLogged);

            //kill the listener thread
            listener.StopListening();

            //kill any lingering connection threads
            foreach (DICOMConnection conn in IncomingStores.Keys)
                conn.CloseConnection();
        }

        private string MakeSafeDirString(string instr)
        {
            instr = instr.Replace("*", "");
            instr = instr.Replace("\\", "");
            instr = instr.Replace("/", "");
            return instr;
        }

        private void Mem_Save_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (string PatID in MemPatItems.Keys)
                {
                    string PatPath = folderBrowserDialog1.SelectedPath + "\\" + MakeSafeDirString(PatID);
                    try
                    {
                        Directory.CreateDirectory(PatPath);
                    }
                    catch
                    {
                        MessageBox.Show("Error Creating Directory: " + PatPath);
                        return;
                    }
                    Hashtable PatItem = (Hashtable)MemPatItems[PatID];

                    foreach (string StuID in PatItem.Keys)
                    {
                        string StuPath = PatPath + "\\Stu" + MakeSafeDirString((string)StuLookup[StuID]);
                        try
                        {
                            Directory.CreateDirectory(StuPath);
                        }
                        catch
                        {
                            MessageBox.Show("Error Creating Directory: " + StuPath);
                            return;
                        }
                        Hashtable StuItem = (Hashtable)PatItem[StuID];

                        int serCount = 1;
                        foreach (string SerID in StuItem.Keys)
                        {
                            string SerPath = StuPath + "\\Ser_" + (serCount++) + "_" + MakeSafeDirString((string)SerLookup[SerID]);
                            try
                            {
                                Directory.CreateDirectory(SerPath);
                            }
                            catch
                            {
                                MessageBox.Show("Error Creating Directory: " + SerPath);
                                return;
                            }
                            List<DICOMData> SerItem = (List<DICOMData>)StuItem[SerID];

                            for (int i = 0; i < SerItem.Count; i++)
                            {
                                DICOMData td = (DICOMData)SerItem[i];
                                td.WriteFile(SerPath + "\\" + i.ToString() + ".dcm", logger);
                            }
                        }
                    }
                }
            }
        }

        public class DICOMDemos
        {
            private string sPatName;
            private string sPatID;
            private string sDOB;
            private string sAccession;
            private string sStudyID;
            private string sStudyDesc;
            private bool sRewriteUIDs;

            public string PatName
            {
                get { return sPatName; }
                set { sPatName = value; }
            }
            public string PatID
            {
                get { return sPatID; }
                set { sPatID = value; }
            }
            public string DOB
            {
                get { return sDOB; }
                set { sDOB = value; }
            }
            public string Accession
            {
                get { return sAccession; }
                set { sAccession = value; }
            }
            public string StudyID
            {
                get { return sStudyID; }
                set { sStudyID = value; }
            }
            public string StudyDesc
            {
                get { return sStudyDesc; }
                set { sStudyDesc = value; }
            }
            public bool RewriteUIDs
            {
                get { return sRewriteUIDs; }
                set { sRewriteUIDs = value; }
            }
        }

        private void Mem_Demographics_Click(object sender, EventArgs e)
        {
            EditDemos tf = new EditDemos();
            DICOMDemos dd = new DICOMDemos();
            tf.PropGrid.SelectedObject = dd;
            dd.RewriteUIDs = false;

            //fill in fields
            var selFiles = GetCurFiles(true, false);
            foreach (var sendable in selFiles)
            {
                var DFile = sendable.DicomData;

                string sPatName = DFile[DICOMTags.PatientName].Display;
                string sPatID = DFile[DICOMTags.PatientID].Display;
                string sDOB = DFile[DICOMTags.PatientBirthDate].Display;
                string sAccession = DFile[DICOMTags.AccessionNumber].Display;
                string sStudyID = DFile[DICOMTags.StudyID].Display;
                string sStudyDesc = DFile[DICOMTags.StudyDescription].Display;

                if (sPatName != dd.PatName)
                {
                    if (dd.PatName == null) dd.PatName = sPatName;
                    else dd.PatName = "[Multiple]";
                }
                if (sPatID != dd.PatID)
                {
                    if (dd.PatID == null) dd.PatID = sPatID;
                    else dd.PatID = "[Multiple]";
                }
                if (sDOB != dd.DOB)
                {
                    if (dd.DOB == null) dd.DOB = sDOB;
                    else dd.DOB = "[Multiple]";
                }
                if (sAccession != dd.Accession)
                {
                    if (dd.Accession == null) dd.Accession = sAccession;
                    else dd.Accession = "[Multiple]";
                }
                if (sStudyID != dd.StudyID)
                {
                    if (dd.StudyID == null) dd.StudyID = sStudyID;
                    else dd.StudyID = "[Multiple]";
                }
                if (sStudyDesc != dd.StudyDesc)
                {
                    if (dd.StudyDesc == null) dd.StudyDesc = sStudyDesc;
                    else dd.StudyDesc = "[Multiple]";
                }
            }

            DialogResult dr = tf.ShowDialog();

            if (dr != DialogResult.OK)
                return;

            foreach (var sendable in selFiles)
            {
                var DFile = sendable.DicomData;

                RemObjFromMemory(DFile);
                if ((dd.PatName != null) && (dd.PatName != "[Multiple]"))
                    DFile[DICOMTags.PatientName].Data = dd.PatName;
                if ((dd.PatID != null) && (dd.PatID != "[Multiple]"))
                    DFile[DICOMTags.PatientID].Data = dd.PatID;
                if ((dd.DOB != null) && (dd.DOB != "[Multiple]"))
                    DFile[DICOMTags.PatientBirthDate].Data = dd.DOB;
                if ((dd.Accession != null) && (dd.Accession != "[Multiple]"))
                    DFile[DICOMTags.AccessionNumber].Data = dd.Accession;
                if ((dd.StudyID != null) && (dd.StudyID != "[Multiple]"))
                    DFile[DICOMTags.StudyID].Data = dd.StudyID;
                if ((dd.StudyDesc != null) && (dd.StudyDesc != "[Multiple]"))
                    DFile[DICOMTags.StudyDescription].Data = dd.StudyDesc;
                if (dd.RewriteUIDs)
                {
                    //rewrite UIDs
                    long[] RewriteList = new long[] { 0x00080018, 0x0020000D, 0x0020000E }; //SOP inst ID, Study Inst, Series Inst
                    foreach (long Elem in RewriteList)
                    {
                        ushort Grp = (ushort)(Elem >> 16);
                        ushort El = (ushort)(Elem & 0xFFFF);

                        DICOMElement elem = DFile[DICOMTags.MakeTag(Grp, El)];
                        string OldUID = elem.Display;
                        long LastIndex = long.Parse(OldUID.Substring(OldUID.LastIndexOf('.') + 1));
                        elem.Data = OldUID.Substring(0, OldUID.LastIndexOf('.') + 1) + (LastIndex + 10000).ToString();
                    }
                }
                AddObjToMemory(DFile, false);
            }

            UpdateMemTree();
        }

        private void Mem_Clear_Click(object sender, EventArgs e)
        {
            foreach (Hashtable PatItem in MemPatItems.Values)
            {
                foreach (Hashtable StuItem in PatItem.Values)
                {
                    foreach (List<DICOMData> SerItem in StuItem.Values)
                    {
                        SerItem.Clear();
                    }
                    StuItem.Clear();
                }
                PatItem.Clear();
            }
            MemPatItems.Clear();

            UpdateMemTree();
        }
    }
}
