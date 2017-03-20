using System.Drawing;
using DICOMSharp.Data;
using DICOMSharp.Data.Tags;


namespace DICOMSharp.Imaging
{
    /// <summary>
    /// This class is intended to do quick renders of DICOM images for preview generation/thumbnail generation/etc.
    /// </summary>
    public class DICOMQuickRenderer
    {
        private DICOMQuickRenderer()
        {
        }

        /// <summary>
        /// Do a quick 1:1 rendering of a DICOM image.  Window and Level for rendering are pulled from the DICOM headers or calculated.
        /// Note: This will uncompress the DICOMData if it is compressed.
        /// </summary>
        /// <param name="data">The DICOM image to render</param>
        /// <param name="frameNum">In a multi-frame image, which frame to render -- 0 for the only frame in non-multiframe images</param>
        /// <returns>An <see cref="Image"/> containing the rendering of the DICOM image</returns>
        public static Bitmap QuickRender(DICOMData data, int frameNum)
        {
            return QuickRender(data, frameNum, true, 0, 0);
        }

        /// <summary>
        /// Do a quick 1:1 rendering of a DICOM image.  Window and Level for rendering are given by the caller.
        /// Note: This will uncompress the DICOMData if it is compressed.
        /// </summary>
        /// <param name="data">The DICOM image to render</param>
        /// <param name="frameNum">In a multi-frame image, which frame to render -- 0 for the only frame in non-multiframe images</param>
        /// <param name="window">The window (range) to render with</param>
        /// <param name="level">The level (center) to render with</param>
        /// <returns>An <see cref="Image"/> containing the rendering of the DICOM image</returns>
        public static Bitmap QuickRender(DICOMData data, int frameNum, short window, short level)
        {
            return QuickRender(data, frameNum, false, window, level);
        }

        internal static Bitmap QuickRender(DICOMData data, int frameNum, bool calcWL, short window, short level)
        {
            RenderAssistant assistant = new RenderAssistant();

            assistant.SetSource(data);

            if (calcWL)
                assistant.CalculateWindowLevel(frameNum);
            else
            {
                assistant.Window = window;
                assistant.Level = level;
            }

            ushort imWidth = (ushort)data.Elements[DICOMTags.ImageWidth].Data;
            ushort imHeight = (ushort)data.Elements[DICOMTags.ImageHeight].Data;
            assistant.Resize(new Size(imWidth, imHeight));

            assistant.RenderFrame(frameNum, true);

            return assistant.RenderedImage;
        }
    }
}
