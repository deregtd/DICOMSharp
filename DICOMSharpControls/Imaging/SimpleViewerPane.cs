using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DICOMSharp.Data;
using System.Diagnostics;
using DICOMSharp.Imaging;

namespace DICOMSharpControls.Imaging
{
    /// <summary>
    /// This simple control is a very simple DICOM Viewer.  You can attach <see cref="DICOMData"/>s to it and
    /// it will allow the user to scroll through them, window/level, and zoom/pan.
    /// </summary>
    public partial class SimpleViewerPane : UserControl
    {
        /// <summary>
        /// Create a new SimpleViewerPane with no images and default render settings (Automatic filtering).
        /// </summary>
        public SimpleViewerPane()
        {
            this.InitializeComponent();

            MouseWheel += new MouseEventHandler(SimpleViewerPane_MouseWheel);

            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);

            FilteringMode = FilteringModeType.AutoFilter;
            dataList = new List<DICOMData>();
            assistant = new RenderAssistant();

            ClearImages();

            this.OnResize(null);
        }

        /// <summary>
        /// Add a new DICOMData to the set of contained images.  If this is the first image added, it will automatically
        /// select it and render it.
        /// </summary>
        /// <param name="data">The DICOMData to add.</param>
        public void AddImage(DICOMData data)
        {
            dataList.Add(data);
            if (dataList.Count == 1)
            {
                assistant.SetSource(data);
                //ScrollImages(1);

                //Calculate first W/L
                if (!manualWindowLevel)
                    assistant.CalculateWindowLevel(frameNum);
            }
            Invalidate();
        }

        /// <summary>
        /// Clear all images from the control.  Also calls <see cref="ResetWindowLevel"/> and <see cref="ResetView"/>.
        /// </summary>
        public void ClearImages()
        {
            dataList.Clear();
            dataNum = 0;
            frameNum = 0;

            assistant.SetSource(null);

            ResetWindowLevel();
            ResetView();
        }

        /// <summary>
        /// Resets the window/level to defaults.  Once the user manually window/levels any image in the panel, then that
        /// window/level is used across all images instead of using whatever the image specifies.  Resetting this restores
        /// automatic window/level usage.
        /// </summary>
        public void ResetWindowLevel()
        {
            manualWindowLevel = false;
            if (dataList.Count > 0)
                assistant.CalculateWindowLevel(frameNum);
            Invalidate();
        }

        /// <summary>
        /// Resets the viewport to the defaults (centered, 1x zoom).
        /// </summary>
        public void ResetView()
        {
            //manualViewpane = false;
            assistant.ResetViewport();
            Invalidate();
        }

        private void ScrollImages(int delta)
        {
            if (delta == 0)
                return;

            if (dataList.Count == 0)
                return;

            if (delta > 0)
                frameNum++;
            else
                frameNum--;

            if (frameNum < 0)
            {
                //previous image
                dataNum--;
                if (dataNum < 0)
                    dataNum = dataList.Count - 1;

                assistant.SetSource(dataList[dataNum]);

                //last in set
                frameNum = assistant.FrameCount - 1;
            }
            if (frameNum >= assistant.FrameCount)
            {
                dataNum++;
                if (dataNum >= dataList.Count)
                    dataNum = 0;

                assistant.SetSource(dataList[dataNum]);

                frameNum = 0;
            }

            if (!manualWindowLevel && dataList.Count > 0)
                assistant.CalculateWindowLevel(frameNum);

            Invalidate();
        }

        private void SimpleViewerPane_Paint(object sender, PaintEventArgs e)
        {
            if (dataList.Count > 0)
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();
                bool filter = (FilteringMode == FilteringModeType.AlwaysFilter) || (FilteringMode == FilteringModeType.AutoFilter && !careAboutMouse);
                assistant.RenderFrame(frameNum, filter);
                long checkpoint = watch.ElapsedTicks;
                e.Graphics.DrawImage(assistant.RenderedImage, new Point(0, 0));

                e.Graphics.DrawString("Frame " + (frameNum + 1) + "/" + assistant.FrameCount + ", W/L: " + assistant.Window + "/" + assistant.Level + ", Zoom: " + assistant.Zoom + ", Center: " + assistant.CenterX + "," + assistant.CenterY, SystemFonts.DefaultFont, new SolidBrush(Color.White), new PointF(0, 1));
                e.Graphics.DrawString("Render: " + (1000f * checkpoint / (float)Stopwatch.Frequency) + "ms, Draw: " + (1000f * (watch.ElapsedTicks - checkpoint) / (float)Stopwatch.Frequency) + "ms", SystemFonts.DefaultFont, new SolidBrush(Color.White), new PointF(0, Height - 12));
            }
            else
                e.Graphics.FillRectangle(new SolidBrush(Color.Black), ClientRectangle);
        }

        private void SimpleViewerPane_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta == 0)
                return;

            if ((ModifierKeys & Keys.Control) > 0)
            {
                //ctrl pressed
                if (e.Delta > 0) assistant.Zoom *= 1.1f;
                else if (e.Delta < 0) assistant.Zoom /= 1.1f;

                Invalidate();
            }
            else
            {
                //no keys we care about pressed, scroll
                ScrollImages(e.Delta);
            }
        }

        private void SimpleViewerPane_MouseDown(object sender, MouseEventArgs e)
        {
            lastMouse = e.Location;
            careAboutMouse = true;
        }

        private void SimpleViewerPane_MouseMove(object sender, MouseEventArgs e)
        {
            if (!careAboutMouse)
                return;

            int diffX = e.Location.X - lastMouse.X;
            int diffY = e.Location.Y - lastMouse.Y;
            lastMouse = e.Location;

            if (diffX == 0 && diffY == 0)
                return;

            if ((ModifierKeys & Keys.Control) > 0)
            {
                //ctrl pressed

                if ((e.Button & MouseButtons.Left) > 0)
                {
                    assistant.CenterX += (float)diffX * assistant.GetPixelPitch();
                    assistant.CenterY += (float)diffY * assistant.GetPixelPitch();
                }

                if ((e.Button & MouseButtons.Right) > 0)
                {
                    if (diffY < 0) assistant.Zoom *= 1.05f;
                    else if (diffY > 0) assistant.Zoom /= 1.05f;
                }

                Invalidate();
            }
            else
            {
                //no keys pressed

                if ((e.Button & MouseButtons.Left) > 0)
                {
                    ScrollImages(diffY);
                }

                if ((e.Button & MouseButtons.Right) > 0)
                {
                    assistant.Window += (short)diffX;
                    if (assistant.Window < 1) assistant.Window = 1;
                    assistant.Level += (short)diffY;

                    manualWindowLevel = true;

                    Invalidate();
                }
            }
        }

        private void SimpleViewerPane_MouseUp(object sender, MouseEventArgs e)
        {
            careAboutMouse = false;

            //Make it filter again
            if (FilteringMode == FilteringModeType.AutoFilter)
                Invalidate();
        }

        private void SimpleViewerPane_Resize(object sender, EventArgs e)
        {
            Size calcSize = ClientSize;

            if (assistant != null)
                assistant.Resize(calcSize);

            Invalidate();
        }

        /// <summary>
        /// The various options for filtering modes for the panel.
        /// </summary>
        public enum FilteringModeType
        {
            /// <summary>
            /// No matter what, always bilinear filter every frame.
            /// </summary>
            AlwaysFilter,
            /// <summary>
            /// If a mouse button is down, implying quicker reactions are desired, then do not filter.
            /// When the mouse button is released, it will redraw with filtering.
            /// </summary>
            AutoFilter,
            /// <summary>
            /// Never filter -- always use nearest neighbor sampling.
            /// </summary>
            NeverFilter
        }

        /// <summary>
        /// How to handle bilinear filtering in the panel.  This does not automatically cause a redraw.
        /// To force a redraw, call <see cref="Control.Invalidate()"/> after you change this.
        /// </summary>
        public FilteringModeType FilteringMode;


        private Point lastMouse;
        private bool careAboutMouse = false;

        private RenderAssistant assistant;

        private List<DICOMData> dataList;
        private int dataNum, frameNum;

        private bool manualWindowLevel;//, manualViewpane;
    }
}
