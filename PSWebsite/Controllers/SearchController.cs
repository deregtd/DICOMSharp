using PSCommon.Models;
using PSCommon.Models.StudyBrowser;
using PSWebsite.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace PSWebsite.Controllers
{
    [CookieApiAuthorizer(RequiredAuthFlags = PSUser.UserAccess.Reader)]
    public class SearchController : ApiController
    {
        [ActionName("HttpVerb")]
        [HttpGet]
        public List<PSStudyBrowserSearchResult> StudySearch(DateTime? startDate = null, DateTime? endDate = null, string accession = null,
            string description = null, string patName = null, string patId = null, int maxResults = 50)
        {
            var db = PSUtils.GetDb();

            var search = new PSStudyBrowserSearch()
            {
                StartDate = startDate,
                EndDate = endDate,
                AccessionNum = accession,
                Description = description,
                PatName = patName,
                PatId = patId
            };

            return db.StudySearch(search, maxResults);
        }

        [ActionName("PatientSnapshot")]
        [HttpGet]
        public IEnumerable<PSStudySnapshotExtended> PatientSnapshot(string patId, int maxResults = 50)
        {
            var db = PSUtils.GetDb();

            return db.GetPatientSnapshot(patId, maxResults);
        }
    }
}
