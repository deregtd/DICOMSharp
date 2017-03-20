using System;
using System.Collections;
using System.Windows.Forms;
using System.Reflection;
using DICOMSharp.Data;
using DICOMSharp.Network.QueryRetrieve;
using DICOMSharp.Network.Connections;
using DICOMSharp.Data.Tags;
using DICOMSharp.Network.Workers;
using DICOMSharp.Logging;
using System.Collections.Generic;
using System.Threading;

namespace DICOMManager
{
    /// <summary>
    /// Summary description for SearchWnd.
    /// </summary>
    public class SearchWnd : System.Windows.Forms.Form
    {
        //comparing structure for the listview - allows clickable headings and sorting, with reversing.
        class ListViewItemComparer : IComparer
        {
            private int col;
            private bool desc;
            System.Globalization.NumberFormatInfo nfi = new System.Globalization.CultureInfo("en-US", false).NumberFormat;

            public ListViewItemComparer()
            {
                New();
            }

            public void New()
            {
                col = 1;
                desc = false;
            }

            public void New(int column)
            {
                if (col == column)
                {
                    desc ^= true;
                }
                else
                {
                    col = column;
                    desc = false;
                }
            }

            public int Compare(object x, object y)
            {
                string stra = ((ListViewItem)x).SubItems[col].Text;
                string strb = ((ListViewItem)y).SubItems[col].Text;
                //first try to convert to doubles and if so, do a numeric comparison, otherwise string comparison
                double douba, doubb;
                if (
                    Double.TryParse(stra, System.Globalization.NumberStyles.Float, nfi, out douba)
                    &&
                    Double.TryParse(strb, System.Globalization.NumberStyles.Float, nfi, out doubb)
                    )
                {
                    if (desc)
                        return Math.Sign(doubb - douba);
                    else
                        return Math.Sign(douba - doubb);
                }
                else
                {
                    //do string compare
                    if (desc)
                        return -String.Compare(stra, strb);
                    else
                        return String.Compare(stra, strb);
                }
            }
        }
        private ListViewItemComparer StuLIComp = new ListViewItemComparer(),
            SerLIComp = new ListViewItemComparer();

        private List<DICOMFinder> pendingFinders = new List<DICOMFinder>();

        private System.Collections.Hashtable ImTable = new System.Collections.Hashtable();
        private System.Collections.Hashtable SerImaNumTable = new System.Collections.Hashtable();

        private System.Windows.Forms.Button StartSearch;
        private System.Windows.Forms.ListView SearchList;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        private System.Windows.Forms.DateTimePicker S_Start;
        private System.Windows.Forms.TextBox S_Name;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox S_PatID;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.DateTimePicker S_End;
        private System.Windows.Forms.Button S_Clear;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.ColumnHeader columnHeader8;

        private string MyAE;
        private DICOMManager.Form1.stDICOMEntity TargetAE;
        private ColumnHeader columnHeader9;
        private ColumnHeader columnHeader19;
        private ColumnHeader columnHeader20;
        private ColumnHeader columnHeader17;
        private ColumnHeader columnHeader18;
        private ColumnHeader columnHeader21;
        private ListView SerList;
        private TextBox S_Acc;
        private Label label5;
        private Button RetrieveSel;
        private ColumnHeader columnHeader10;

        public SearchWnd(DICOMManager.Form1.stDICOMEntity targetAE, string myAE)
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            SearchList.ListViewItemSorter = StuLIComp;
            SerList.ListViewItemSorter = SerLIComp;

            MyAE = myAE;
            TargetAE = targetAE;

            this.Text = "Search Window - Query Target: " + TargetAE.AETitle;
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
            this.StartSearch = new System.Windows.Forms.Button();
            this.SearchList = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader10 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.S_Start = new System.Windows.Forms.DateTimePicker();
            this.S_Name = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.S_PatID = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.S_End = new System.Windows.Forms.DateTimePicker();
            this.S_Clear = new System.Windows.Forms.Button();
            this.columnHeader9 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader19 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader20 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader17 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader18 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader21 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SerList = new System.Windows.Forms.ListView();
            this.S_Acc = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.RetrieveSel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // StartSearch
            // 
            this.StartSearch.Location = new System.Drawing.Point(13, 7);
            this.StartSearch.Name = "StartSearch";
            this.StartSearch.Size = new System.Drawing.Size(54, 21);
            this.StartSearch.TabIndex = 0;
            this.StartSearch.Text = "Search";
            this.StartSearch.Click += new System.EventHandler(this.StartSearch_Click);
            // 
            // SearchList
            // 
            this.SearchList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SearchList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader10,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6,
            this.columnHeader7,
            this.columnHeader8});
            this.SearchList.FullRowSelect = true;
            this.SearchList.GridLines = true;
            this.SearchList.HideSelection = false;
            this.SearchList.Location = new System.Drawing.Point(0, 55);
            this.SearchList.Name = "SearchList";
            this.SearchList.Size = new System.Drawing.Size(712, 266);
            this.SearchList.TabIndex = 1;
            this.SearchList.UseCompatibleStateImageBehavior = false;
            this.SearchList.View = System.Windows.Forms.View.Details;
            this.SearchList.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.SearchList_ColumnClick);
            this.SearchList.SelectedIndexChanged += new System.EventHandler(this.SearchList_SelectedIndexChanged);
            this.SearchList.DoubleClick += new System.EventHandler(this.SearchList_DoubleClick);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "ID";
            this.columnHeader1.Width = 67;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Name";
            this.columnHeader2.Width = 174;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Mod";
            this.columnHeader3.Width = 39;
            // 
            // columnHeader10
            // 
            this.columnHeader10.DisplayIndex = 8;
            this.columnHeader10.Text = "Accession";
            // 
            // columnHeader4
            // 
            this.columnHeader4.DisplayIndex = 3;
            this.columnHeader4.Text = "Date";
            this.columnHeader4.Width = 74;
            // 
            // columnHeader5
            // 
            this.columnHeader5.DisplayIndex = 4;
            this.columnHeader5.Text = "Time";
            this.columnHeader5.Width = 54;
            // 
            // columnHeader6
            // 
            this.columnHeader6.DisplayIndex = 5;
            this.columnHeader6.Text = "Description";
            this.columnHeader6.Width = 134;
            // 
            // columnHeader7
            // 
            this.columnHeader7.DisplayIndex = 6;
            this.columnHeader7.Text = "Sex";
            this.columnHeader7.Width = 35;
            // 
            // columnHeader8
            // 
            this.columnHeader8.DisplayIndex = 7;
            this.columnHeader8.Text = "Birth";
            this.columnHeader8.Width = 71;
            // 
            // S_Start
            // 
            this.S_Start.Checked = false;
            this.S_Start.CustomFormat = "MM/dd/yyyy HH:mm";
            this.S_Start.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.S_Start.Location = new System.Drawing.Point(120, 7);
            this.S_Start.Name = "S_Start";
            this.S_Start.ShowCheckBox = true;
            this.S_Start.Size = new System.Drawing.Size(96, 20);
            this.S_Start.TabIndex = 6;
            // 
            // S_Name
            // 
            this.S_Name.Location = new System.Drawing.Point(300, 28);
            this.S_Name.Name = "S_Name";
            this.S_Name.Size = new System.Drawing.Size(93, 20);
            this.S_Name.TabIndex = 10;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(233, 28);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(54, 14);
            this.label2.TabIndex = 9;
            this.label2.Text = "Name:";
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(233, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 14);
            this.label1.TabIndex = 8;
            this.label1.Text = "Patient ID:";
            // 
            // S_PatID
            // 
            this.S_PatID.Location = new System.Drawing.Point(300, 7);
            this.S_PatID.Name = "S_PatID";
            this.S_PatID.Size = new System.Drawing.Size(93, 20);
            this.S_PatID.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(80, 7);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(33, 14);
            this.label3.TabIndex = 11;
            this.label3.Text = "Start:";
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(80, 28);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(33, 14);
            this.label4.TabIndex = 13;
            this.label4.Text = "End:";
            // 
            // S_End
            // 
            this.S_End.Checked = false;
            this.S_End.CustomFormat = "MM/dd/yyyy HH:mm";
            this.S_End.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.S_End.Location = new System.Drawing.Point(120, 28);
            this.S_End.Name = "S_End";
            this.S_End.ShowCheckBox = true;
            this.S_End.Size = new System.Drawing.Size(96, 20);
            this.S_End.TabIndex = 12;
            // 
            // S_Clear
            // 
            this.S_Clear.Location = new System.Drawing.Point(13, 28);
            this.S_Clear.Name = "S_Clear";
            this.S_Clear.Size = new System.Drawing.Size(54, 20);
            this.S_Clear.TabIndex = 18;
            this.S_Clear.Text = "Clear";
            this.S_Clear.Click += new System.EventHandler(this.S_Clear_Click);
            // 
            // columnHeader9
            // 
            this.columnHeader9.Text = "Mod";
            this.columnHeader9.Width = 39;
            // 
            // columnHeader19
            // 
            this.columnHeader19.Text = "Date";
            this.columnHeader19.Width = 82;
            // 
            // columnHeader20
            // 
            this.columnHeader20.Text = "Time";
            this.columnHeader20.Width = 56;
            // 
            // columnHeader17
            // 
            this.columnHeader17.Text = "Num";
            this.columnHeader17.Width = 40;
            // 
            // columnHeader18
            // 
            this.columnHeader18.Text = "Description";
            this.columnHeader18.Width = 323;
            // 
            // columnHeader21
            // 
            this.columnHeader21.Text = "Images";
            this.columnHeader21.Width = 57;
            // 
            // SerList
            // 
            this.SerList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SerList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader9,
            this.columnHeader19,
            this.columnHeader20,
            this.columnHeader17,
            this.columnHeader18,
            this.columnHeader21});
            this.SerList.FullRowSelect = true;
            this.SerList.GridLines = true;
            this.SerList.Location = new System.Drawing.Point(0, 318);
            this.SerList.Name = "SerList";
            this.SerList.Size = new System.Drawing.Size(712, 91);
            this.SerList.TabIndex = 19;
            this.SerList.UseCompatibleStateImageBehavior = false;
            this.SerList.View = System.Windows.Forms.View.Details;
            this.SerList.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.SerList_ColumnClick);
            // 
            // S_Acc
            // 
            this.S_Acc.Location = new System.Drawing.Point(486, 8);
            this.S_Acc.Name = "S_Acc";
            this.S_Acc.Size = new System.Drawing.Size(93, 20);
            this.S_Acc.TabIndex = 20;
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(419, 8);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(60, 14);
            this.label5.TabIndex = 21;
            this.label5.Text = "Accession:";
            // 
            // RetrieveSel
            // 
            this.RetrieveSel.Location = new System.Drawing.Point(609, 28);
            this.RetrieveSel.Name = "RetrieveSel";
            this.RetrieveSel.Size = new System.Drawing.Size(103, 21);
            this.RetrieveSel.TabIndex = 22;
            this.RetrieveSel.Text = "Retrieve Selected";
            this.RetrieveSel.Click += new System.EventHandler(this.RetrieveSel_Click);
            // 
            // SearchWnd
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(712, 408);
            this.Controls.Add(this.RetrieveSel);
            this.Controls.Add(this.S_Acc);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.SerList);
            this.Controls.Add(this.S_Clear);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.S_End);
            this.Controls.Add(this.S_Name);
            this.Controls.Add(this.S_PatID);
            this.Controls.Add(this.S_Start);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.SearchList);
            this.Controls.Add(this.StartSearch);
            this.MinimizeBox = false;
            this.Name = "SearchWnd";
            this.Text = "SearchWnd";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.SearchWnd_Closing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.SearchWnd_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        private void StartSearch_Click(object sender, System.EventArgs e)
        {
            SearchList.Items.Clear();
            SerList.Items.Clear();

            string DateStr = "";
            if (S_Start.Checked)
            {
                DateStr += S_Start.Value.ToString("yyyyMMdd");
                if (!S_End.Checked) DateStr += "-";
            }
            if (S_End.Checked)
            {
                if (DateStr != "") DateStr += "-";
                DateStr += S_End.Value.ToString("yyyyMMdd");
            }

            QRRequestData request = new QRRequestData(QueryRetrieveLevel.StudyRoot, QRLevelType.Study);
            request.SearchTerms[DICOMTags.StudyDate] = DateStr;
            request.SearchTerms[DICOMTags.StudyTime] = null;
            request.SearchTerms[DICOMTags.AccessionNumber] = S_Acc.Text;
            request.SearchTerms[DICOMTags.PatientName] = S_Name.Text;
            request.SearchTerms[DICOMTags.PatientID] = S_PatID.Text;
            request.SearchTerms[DICOMTags.Modality] = null;
            request.SearchTerms[DICOMTags.PatientSex] = null;
            request.SearchTerms[DICOMTags.PatientBirthDate] = null;
            request.SearchTerms[DICOMTags.StudyDescription] = null;
            request.SearchTerms[DICOMTags.BodyPartExamined] = null;

            DICOMFinder finder = new DICOMFinder(new NullLogger(), "", false);
            pendingFinders.Add(finder);
            finder.FindResponse += new DICOMFinder.FindResponseHandler(finder_FindResponse_Study);
            finder.Find(new ApplicationEntity(MyAE), TargetAE.ToAE(), request);
        }

        private string ValOrBlank(Dictionary<uint, object> row, uint tag)
        {
            if (!row.ContainsKey(tag))
                return "";

            object inval = row[tag];
            if (inval == null)
                return "";
            if (inval is string)
                return (string)inval;
            return inval.ToString();
        }

        void finder_FindResponse_Study(DICOMFinder finder, QRResponseData response)
        {
            pendingFinders.Remove(finder);
            if (response == null)
                return;

            foreach (Dictionary<uint, object> row in response.ResponseRows)
            {
                string StuDesc = "";
                if (row.ContainsKey(DICOMTags.StudyDescription))
                    StuDesc = (string)row[DICOMTags.StudyDescription];	//study desc
                else if (row.ContainsKey(DICOMTags.BodyPartExamined))
                    StuDesc = (string)row[DICOMTags.BodyPartExamined];	//bodypartexa

                this.Invoke((MethodInvoker)delegate
                {
                    SearchList.Items.Add(new ListViewItem(new string[]
                    {
                        ValOrBlank(row,DICOMTags.PatientID),
                        ValOrBlank(row,DICOMTags.PatientName),
                        ValOrBlank(row,DICOMTags.Modality),
                        ValOrBlank(row,DICOMTags.AccessionNumber),
                        ValOrBlank(row,DICOMTags.StudyDate),
                        ValOrBlank(row,DICOMTags.StudyTime),
                        StuDesc,
                        ValOrBlank(row,DICOMTags.PatientSex),
                        ValOrBlank(row,DICOMTags.PatientBirthDate),
                        ValOrBlank(row,DICOMTags.StudyInstanceUID)     //Study UID, Hidden!
                    }));
                });
            }
        }

        private void SearchList_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (SearchList.SelectedItems.Count == 0)
                return;

            //clear series list
            SerList.Items.Clear();

            //retrieve entire series history for patient
            string studyInsUID = SearchList.SelectedItems[0].SubItems[9].Text;

            QRRequestData request = new QRRequestData(QueryRetrieveLevel.StudyRoot, QRLevelType.Series);
            request.SearchTerms[DICOMTags.StudyInstanceUID] = studyInsUID;
            request.SearchTerms[DICOMTags.SeriesDescription] = "";
            request.SearchTerms[DICOMTags.BodyPartExamined] = "";
            request.SearchTerms[DICOMTags.SeriesDate] = "";
            request.SearchTerms[DICOMTags.SeriesTime] = "";
            request.SearchTerms[DICOMTags.SeriesNumber] = "";
            request.SearchTerms[DICOMTags.NumberOfSeriesRelatedInstances] = ""; //see if it works..
            DICOMFinder finder = new DICOMFinder(new NullLogger(), "", false);
            pendingFinders.Add(finder);
            finder.FindResponse += new DICOMFinder.FindResponseHandler(finder_FindResponse_Series);
            finder.Find(new ApplicationEntity(MyAE), TargetAE.ToAE(), request);
        }

        void finder_FindResponse_Series(DICOMFinder finder, QRResponseData response)
        {
            pendingFinders.Remove(finder);
            if (response == null)
                return;

            foreach (Dictionary<uint, object> row in response.ResponseRows)
            {
                string SerDesc = "";
                if (row.ContainsKey(DICOMTags.SeriesDescription))
                    SerDesc = (string)row[DICOMTags.SeriesDescription];	//series desc
                else if (row.ContainsKey(DICOMTags.BodyPartExamined))
                    SerDesc = (string)row[DICOMTags.BodyPartExamined];	//bodypartexa

                string SerTime = ValOrBlank(row,DICOMTags.SeriesTime);
                if (SerTime != "" && SerTime.Length >= 6)
                    SerTime = SerTime.Substring(0, 6);

                string seriesInsUID = ValOrBlank(row,DICOMTags.SeriesInstanceUID);

                string imCount = "";
                if (row.ContainsKey(DICOMTags.NumberOfSeriesRelatedInstances))
                    imCount = row[DICOMTags.NumberOfSeriesRelatedInstances].ToString();

                this.Invoke((MethodInvoker)delegate
                {
                    SerList.Items.Add(new ListViewItem(new string[]
                    {
                        ValOrBlank(row,DICOMTags.Modality),	//modality
                        ValOrBlank(row,DICOMTags.SeriesDate),	//ser date
                        SerTime,	//ser time
                        ValOrBlank(row,DICOMTags.SeriesNumber),	//series number
                        SerDesc,									//description
                        imCount,                                         //will be the number of images, after they're retrieved
                        seriesInsUID                               		//Series UID, Hidden!
                    }));
                });

                if (imCount == "")
                {
                    QRRequestData request = new QRRequestData(QueryRetrieveLevel.StudyRoot, QRLevelType.Image);
                    request.SearchTerms[DICOMTags.SeriesInstanceUID] = seriesInsUID;
                    DICOMFinder newfinder = new DICOMFinder(new NullLogger(), "", false);
                    pendingFinders.Add(finder);
                    newfinder.FindResponse += new DICOMFinder.FindResponseHandler(newfinder_FindResponse_Image);
                    newfinder.Find(new ApplicationEntity(MyAE), TargetAE.ToAE(), request);
                }
            }
        }

        void newfinder_FindResponse_Image(DICOMFinder finder, QRResponseData response)
        {
            pendingFinders.Remove(finder);
            if (response == null)
                return;

            string seriesInsUID = (string)finder.FindRequest.SearchTerms[DICOMTags.SeriesInstanceUID];

            //now modify the actual visible listview
            this.Invoke((MethodInvoker)delegate
            {
                for (int i = 0; i < SerList.Items.Count; i++)
                {
                    if (SerList.Items[i].SubItems[6].Text == seriesInsUID)
                        SerList.Items[i].SubItems[5].Text = response.ResponseRows.Count.ToString();
                }
            });
        }

        private void SearchList_ColumnClick(object sender, System.Windows.Forms.ColumnClickEventArgs e)
        {
            StuLIComp.New(e.Column);
            SearchList.Sort();
        }
        private void SerList_ColumnClick(object sender, System.Windows.Forms.ColumnClickEventArgs e)
        {
            SerLIComp.New(e.Column);
            SerList.Sort();
        }

        private void SearchWnd_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visible = false;
            while (pendingFinders.Count > 0)
            {
                pendingFinders[0].Abort();
                Thread.Sleep(1);
            }
        }

        private void SearchList_DoubleClick(object sender, System.EventArgs e)
        {
            if (SearchList.SelectedItems.Count == 0)
                return;

            foreach (ListViewItem lvi in SearchList.SelectedItems)
            {
                //string patid = lvi.SubItems[0].Text;
                string studyInsUID = lvi.SubItems[9].Text;

                //Start downloading studyid
                QRRequestData request = new QRRequestData(QueryRetrieveLevel.StudyRoot, QRLevelType.Study);
                request.SearchTerms[DICOMTags.StudyInstanceUID] = studyInsUID;
                DICOMMover mover = new DICOMMover(new NullLogger(), "", false);
                mover.StartMove(new ApplicationEntity(MyAE), TargetAE.ToAE(), MyAE, request);
            }
        }

        private void SearchWnd_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        private void S_Clear_Click(object sender, EventArgs e)
        {
            S_Start.Checked = false;
            S_End.Checked = false;
            S_PatID.Text = "";
            S_Acc.Text = "";
            S_Name.Text = "";
        }

        private void RetrieveSel_Click(object sender, EventArgs e)
        {
            SearchList_DoubleClick(sender, e);
        }
    }
}
