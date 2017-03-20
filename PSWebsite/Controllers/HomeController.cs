using PSCommon.Models;
using PSWebsite.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace PSWebsite.Controllers
{
    public class HomeController : Controller
    {
        public class IndexModel
        {
            public PSUser.UserInfo UserInfo;
            public bool Release;
        }

        public ActionResult Index()
        {
            var user = CookieSession.Current.GetUser();
            var model = new IndexModel()
            {
                UserInfo = user != null ? user.GetUserInfo() : null,
                Release = 
                #if DEBUG
                    false
                #else
                    true
                #endif
        };
            return View(model);
        }
    }
}
