using DICOMSharp.Data;
using DICOMSharp.Util;
using PSCommon.Models;
using PSCommon.Utilities;
using PSWebsite.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web.Http;

namespace PSWebsite.Controllers
{
    [CookieApiAuthorizer(RequiredAuthFlags = PSUser.UserAccess.None)]
    public class AuthController : ApiController
    {
        public class LoginResult
        {
            public bool success;
            public PSUser.UserInfo userInfo;
            public string errorMessage;
        }

        public class LoginParams
        {
            public string username;
            public string password;
        }
        
        [HttpPost]
        [ActionName("Login")]
        public LoginResult Login([FromBody] LoginParams parms)
        {
            var db = PSUtils.GetDb();

            if (db.IsSetup)
            {
                var user = db.GetUser(parms.username, true);

                if (user == null || user.password != parms.password)
                {
                    return new LoginResult()
                    {
                        success = false,
                        errorMessage = "Username or password incorrect"
                    };
                }

                // Save it to the session
                CookieSession.Current.AssociateUser(user);
                
                return new LoginResult()
                {
                    success = true,
                    userInfo = user.GetUserInfo()
                };
            }
            else
            {
                // TODO: Once we support configuration from the website, enable promiscuous startup login

                return new LoginResult()
                {
                    success = false,
                    errorMessage = "Server not configured, please contact your Administrator"
                };
            }
        }

        public class ChangePasswordModel
        {
            public string NewPassword;
        }

        [HttpPut]
        [ActionName("Password")]
        public HttpResponseMessage ChangePassword([FromBody] ChangePasswordModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.NewPassword))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            var db = PSUtils.GetDb();

            var oldUser = db.GetUser(CookieSession.Current.GetUsername(), true);
            if (oldUser == null)
            {
                throw new HttpResponseException(HttpStatusCode.Unauthorized);
            }

            oldUser.password = model.NewPassword;
            db.UserUpdate(oldUser.username, oldUser);

            return Request.CreateResponse();
        }

        [HttpGet]
        [ActionName("Logoff")]
        public HttpResponseMessage Logoff()
        {
            CookieSession.Current.DissociateUser();

            return Request.CreateResponse();
        }
    }
}
