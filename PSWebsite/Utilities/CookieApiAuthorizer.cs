using PSCommon.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace PSWebsite.Utilities
{
    [AttributeUsage(AttributeTargets.All)]
    public class CookieApiAuthorizer : AuthorizeAttribute
    {
        public CookieApiAuthorizer() : base()
        {
            RequiredAuthFlags = PSUser.UserAccess.None;
        }

        public PSUser.UserAccess RequiredAuthFlags { get; set; }

        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            if (RequiredAuthFlags != PSUser.UserAccess.None)
            {
                var user = CookieSession.Current.GetUser();
                if (user == null || ((user.access & RequiredAuthFlags) < RequiredAuthFlags))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
