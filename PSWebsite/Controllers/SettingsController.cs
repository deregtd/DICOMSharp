using DICOMSharp.Network.Connections;
using PSCommon.Dicom;
using PSCommon.Models;
using PSWebsite.Utilities;
using System.Collections.Generic;
using System.Web.Http;

namespace PSWebsite.Controllers
{
    [CookieApiAuthorizer(RequiredAuthFlags = PSUser.UserAccess.ServerAdmin)]
    public class SettingsController : ApiController
    {
        public class ServerSettingsResult
        {
            public DicomServerSettings DicomServerSettings;
            public List<PSEntity> DicomServerEntities;
            public List<PSUser> Users;
            public long StoredImagesKB;
        }

        [HttpGet]
        [ActionName("ServerSettings")]
        public ServerSettingsResult GetServerSettings()
        {
            var db = PSUtils.GetDb();
            var server = PSUtils.GetDicomServer();

            var ret = new ServerSettingsResult()
            {
                DicomServerSettings = server.GetSettings(),
                DicomServerEntities = db.GetEntities(),
                Users = db.GetUsers(false),
                StoredImagesKB = db.TotalSizeKB
            };

            return ret;
        }

        [HttpPost]
        [ActionName("DicomServerSettings")]
        public void SaveDicomServerSettings([FromBody] DicomServerSettings newSettings)
        {
            var server = PSUtils.GetDicomServer();

            server.StopServer();

            server.UseSettings(newSettings);

            server.StartServer();
        }

        [HttpGet]
        [ActionName("Entities")]
        [CookieApiAuthorizer(RequiredAuthFlags = PSUser.UserAccess.StudySend)]
        public List<PSEntity> GetEntities()
        {
            var db = PSUtils.GetDb();
            var server = PSUtils.GetDicomServer();

            return db.GetEntities();
        }

        public class SaveEntityListModel
        {
            public List<PSEntity> List;
        }

        [HttpPost]
        [ActionName("Entities")]
        public void SetEntities([FromBody] SaveEntityListModel model)
        {
            var db = PSUtils.GetDb();

            foreach (var ae in db.GetEntities())
            {
                db.EntityDelete(ae.Title);
            }

            HashSet<string> usedTitles = new HashSet<string>();
            foreach (var ae in model.List)
            {
                // Dedupe AE titles in case any got through to the API
                if (usedTitles.Contains(ae.Title))
                {
                    continue;
                }
                usedTitles.Add(ae.Title);

                db.EntityInsert(ae);
            }
        }

        [HttpDelete]
        [ActionName("Users")]
        public void DeleteUser(string username)
        {
            var db = PSUtils.GetDb();

            db.UserDelete(username);
        }

        [HttpPut]
        [ActionName("Users")]
        public void UpdateUser([FromBody] PSUser user, string username)
        {
            var db = PSUtils.GetDb();

            var oldUser = db.GetUser(username, true);

            // Copy over the updatable new fields from the user
            oldUser.username = user.username;
            oldUser.realname = user.realname;
            oldUser.access = user.access;

            if (!string.IsNullOrWhiteSpace(user.password))
            {
                // Looks like we updated the password, so copy that over too
                oldUser.password = user.password;
            }

            db.UserUpdate(username, oldUser);
        }

        [HttpPost]
        [ActionName("Users")]
        public void PostUser([FromBody] PSUser user)
        {
            var db = PSUtils.GetDb();

            db.UserInsert(user);
        }
    }
}
