using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSCommon.Models.StudyBrowser
{
    public class PSStudyBrowserSearchResult
    {
        public string PatID;
        public string PatName;
        public DateTime PatBirthDate;
        public string PatSex;

        public string StuInstID;
        public string AccessionNum;

        public string StuID;
        public DateTime StuDateTime;
        public string Modality;
        public string RefPhysician;
        public string StuDesc;
        public string DeptName;

        public int NumSeries;
        public int NumImages;
        public int StuSizeKB;

        public PSStudyBrowserSearchResult(PSPatient pat, PSStudy stu)
        {
            PatID = stu.PatID;
            PatName = pat.PatName;
            PatBirthDate = pat.PatBirthDate;
            PatSex = pat.PatSex;

            StuInstID = stu.StuInstID;
            AccessionNum = stu.AccessionNum;

            StuID = stu.StuID;
            StuDateTime = stu.StuDateTime;
            Modality = stu.Modality;
            RefPhysician = stu.RefPhysician;
            StuDesc = stu.StuDesc;
            DeptName = stu.DeptName;

            NumSeries = stu.NumSeries;
            NumImages = stu.NumImages;
            StuSizeKB = stu.StuSizeKB;
        }
    }
}
