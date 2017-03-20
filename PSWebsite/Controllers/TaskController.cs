using PSCommon.Models;
using PSCommon.Models.StudyBrowser;
using PSWebsite.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace PSWebsite.Controllers
{
    public class TaskController : ApiController
    {
        public class SendStudiesModel
        {
            public string[] StudyInstanceUIDs;
            public string TargetAE;
        }

        [CookieApiAuthorizer(RequiredAuthFlags = PSUser.UserAccess.StudySend)]
        [ActionName("SendStudies")]
        [HttpPost]
        public void SendStudies([FromBody] SendStudiesModel model)
        {
            var server = PSUtils.GetDicomServer();

            foreach (var uid in model.StudyInstanceUIDs)
            {
                server.TrackSendStudyTask(uid, model.TargetAE);
            }
        }

        public class DeleteStudiesModel
        {
            public string[] StudyInstanceUIDs;
        }

        [CookieApiAuthorizer(RequiredAuthFlags = PSUser.UserAccess.StudySend)]
        [ActionName("DeleteStudies")]
        [HttpPost]
        public void DeleteStudies([FromBody] DeleteStudiesModel model)
        {
            var server = PSUtils.GetDicomServer();

            foreach (var uid in model.StudyInstanceUIDs)
            {
                server.TrackDeleteStudyTask(uid);
            }
        }

        [CookieApiAuthorizer(RequiredAuthFlags = PSUser.UserAccess.ServerAdmin)]
        [ActionName("ImportPath")]
        [HttpGet]
        public void ImportPath(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            var server = PSUtils.GetDicomServer();

            server.TrackImportTask(path);
        }
    }
}
