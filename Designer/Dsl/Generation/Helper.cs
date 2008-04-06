using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.Modeling;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes; 


namespace Worm.Designer
{
    public static class Helper
    {
        public static string vsProgIdBase = "!VisualStudio.DTE.9.0:";

        [DllImport("ole32.dll")]
        public static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

        [DllImport("ole32.dll")]
        public static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

        [CLSCompliant(false)]
        public static DTE GetDTE(string processID)
        {
            IRunningObjectTable prot;
            IEnumMoniker pMonkEnum;

            string progID = vsProgIdBase + processID;

            GetRunningObjectTable(0, out prot);
            prot.EnumRunning(out pMonkEnum);
            pMonkEnum.Reset();

            IntPtr fetched = IntPtr.Zero;
            IMoniker[] pmon = new IMoniker[1];
            while (pMonkEnum.Next(1, pmon, fetched) == 0)
            {
                IBindCtx pCtx;
                CreateBindCtx(0, out pCtx);
                string str;
                pmon[0].GetDisplayName(pCtx, null, out str);
                if (str == progID)
                {
                    object objReturnObject;
                    prot.GetObject(pmon[0], out objReturnObject);
                    DTE ide = (DTE)objReturnObject;
                    return ide;
                }
            }

            return null;
        }
    }
}
