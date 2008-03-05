using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Tests
{
    public class ReportCreator
    {
        static string fileName = string.Format("checkTime{0}.xml", DateTime.Now.ToString("dd:MM:hh:mm:ss"));
        static XmlDocument xmlDoc = new XmlDocument();
        static ReportCreator()
        {

        }
    }
}
