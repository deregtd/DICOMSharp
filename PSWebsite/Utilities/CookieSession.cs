using PSCommon.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PSWebsite.Utilities
{
    public class CookieSession
    {
        public static CookieSession Current
        {
            get
            {
                if (HttpContext.Current == null)
                {
                    throw new Exception("Not in an HttpContext");
                }

                if (HttpContext.Current.Items.Contains("session"))
                {
                    return (CookieSession)HttpContext.Current.Items["session"];
                }

                var session = new CookieSession();
                HttpContext.Current.Items["session"] = session;
                return session;
            }
        }

        const string CookieSessionKey = "pacssoft_session";

        private string _sessionId = null;

        private bool _fetchedUsername = false;
        private string _username = null;

        private bool _fetchedUser = false;
        private PSUser _user = null;

        public CookieSession()
        {
            var sessionKey = HttpContext.Current.Request.Cookies.Get(CookieSessionKey);
            if (sessionKey == null)
            {
                _sessionId = Guid.NewGuid().ToString("N");
                HttpContext.Current.Response.Cookies.Set(new HttpCookie(CookieSessionKey, _sessionId)
                {
                    Expires = DateTime.UtcNow.AddMonths(1)
                });
            }
            else
            {
                _sessionId = sessionKey.Value;
            }
        }

        public void AssociateUser(PSUser user)
        {
            var db = PSUtils.GetDb();
            // Delete it first just in case
            db.SessionDelete(_sessionId);
            db.AttachUsernameToSession(_sessionId, user.username);

            _username = user.username;
            _fetchedUsername = true;

            _user = user;
            _fetchedUser = true;
        }

        public void DissociateUser()
        {
            var db = PSUtils.GetDb();
            db.SessionDelete(_sessionId);

            _username = null;
            _fetchedUsername = true;

            _user = null;
            _fetchedUser = true;
        }

        public string GetUsername()
        {
            if (!_fetchedUsername)
            {
                var db = PSUtils.GetDb();
                _username = db.GetUsernameForSession(_sessionId);

                _fetchedUsername = true;
            }

            return _username;
        }

        public PSUser GetUser()
        {
            if (!_fetchedUser)
            {
                var username = GetUsername();
                if (!string.IsNullOrWhiteSpace(username))
                {
                    var db = PSUtils.GetDb();
                    _user = db.GetUser(GetUsername(), true);
                }

                _fetchedUser = true;
            }

            return _user;
        }
    }
}
