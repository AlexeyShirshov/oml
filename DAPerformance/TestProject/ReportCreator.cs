using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using TestProject;

namespace Tests
{
    public class ReportCreator
    {
        static string fileName = string.Format("CheckTime {0}.xml", DateTime.Now.ToString("yyyy-MM-dd hh_mm_ss"));
        static XmlDocument xmlDoc = new XmlDocument();
        static ReportCreator()
        {

        }

        public static void Write(DSTime dsTime)
        {
            dsTime.WriteXml(fileName);
        }


    }
}
