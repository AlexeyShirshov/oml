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
        static string fileName = string.Format("checkTime{0}.xml", DateTime.Now.ToString("ddMMhhmmss"));
        static XmlDocument xmlDoc = new XmlDocument();
        static ReportCreator()
        {

        }

        public static void Write(DSTime dsTime)
        {
                //fileStream = File.Create(fileName);
            dsTime.WriteXml(fileName);
        }


    }
}
