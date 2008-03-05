using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Tests
{
    class Utils
    {

        public static void SetDataDirectory()
        {
            string _executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            // Remove file:\\
            AppDomain.CurrentDomain.SetData("DataDirectory", Path.GetFullPath(_executingPath.Substring(6) + @"..\..\..\..\DB"));
        }
    }
}
