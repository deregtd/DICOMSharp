using DICOMSharp.Data;
using DICOMSharp.Data.Transfers;
using PSCommon.Models;
using PSWebsite.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace PSWebsite.Controllers
{
    [CookieApiAuthorizer(RequiredAuthFlags = PSUser.UserAccess.Reader)]
    public class DICOMController : ApiController
    {
        [HttpGet]
        [ActionName("SeriesImages")]
        public HttpResponseMessage SeriesImages(string seriesInstanceUID)
        {
            var db = PSUtils.GetDb();
            var images = db.FetchSeriesImages(seriesInstanceUID);

            var response = Request.CreateResponse();
            response.Content = new PushStreamContent((Stream outputStream, HttpContent content, TransportContext context) =>
            {
                var writer = new BinaryWriter(outputStream);
                foreach (var image in images)
                {
                    this._sendImage(image, outputStream, writer);
                }

                // All out of images!
                outputStream.Close();
            }, "application/pacssoft-streaming-raw-dicom");
            return response;
        }

        private void _sendImage(PSImage image, Stream outputStream, BinaryWriter writer)
        {
            var transferSyntax = TransferSyntaxes.Lookup(image.TransferSyntaxID);
            if (transferSyntax.Compression != DICOMSharp.Data.Compression.CompressionInfo.None)
            {
                // Need to decompress
                var data = new DICOMData();
                data.ParseFile(PSUtils.GetParsedFilePath(image.Path), true, PSUtils.GetLogger());
                data.Uncompress();

                var memStream = new MemoryStream();
                data.WriteToStreamAsPart10File(memStream, PSUtils.GetLogger());

                writer.Write((UInt32)(memStream.Length + 4));
                memStream.Position = 0;
                memStream.CopyTo(outputStream);
            }
            else
            {
                // Write the file out directly.
                var fileInfo = new FileInfo(PSUtils.GetParsedFilePath(image.Path));
                var size = fileInfo.Length;
                writer.Write((UInt32)(size + 4));
                fileInfo.OpenRead().CopyTo(outputStream);
            }
        }

        public class ImageInfoResp
        {
            public string imageInstanceUID;
            public uint fileSizeKB;
        }

        [HttpGet]
        [ActionName("SeriesImageList")]
        public IEnumerable<ImageInfoResp> SeriesImageList(string seriesInstanceUID)
        {
            var db = PSUtils.GetDb();
            var images = db.FetchSeriesImages(seriesInstanceUID);
            return images.Select(image => new ImageInfoResp { imageInstanceUID = image.ImaInstID, fileSizeKB = image.FileSizeKB });
        }

        public class ImagesParms
        {
            public string[] imageInstanceUIDs;
        }

        [HttpPost]
        [ActionName("Images")]
        public HttpResponseMessage FetchImages([FromBody] ImagesParms parms)
        {
            var db = PSUtils.GetDb();
            var images = db.FetchImages(parms.imageInstanceUIDs);

            var response = Request.CreateResponse();
            response.Content = new PushStreamContent((Stream outputStream, HttpContent content, TransportContext context) =>
            {
                var writer = new BinaryWriter(outputStream);
                foreach (var image in images)
                {
                    try
                    {
                        this._sendImage(image, outputStream, writer);
                    }
                    catch (Exception e)
                    {
                        var logger = PSUtils.GetLogger();
                        logger.Log(DICOMSharp.Logging.LogLevel.Debug, "FetchImages Error: " + e.ToString());
                    }
                }

                // All out of images!
                outputStream.Close();
            }, "application/pacssoft-streaming-dicom");
            return response;
        }
    }
}
