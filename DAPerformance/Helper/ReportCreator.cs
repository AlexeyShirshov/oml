using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Common
{
    public class ReportCreator
    {
        public static void Write(DSTestTime dsTestTime)
        {
            string fileName = string.Format("CheckTime {0}.xml", DateTime.Now.ToString("yyyy-MM-dd hh_mm_ss"));
            dsTestTime.WriteXml(fileName);
        }
    }
}
