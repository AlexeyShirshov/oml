using System;
using EnvDTE;

using System.IO;
using System.Resources;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TextTemplating.VSHost;

namespace Worm.Designer
{
    [Guid("5B7BDFA0-2C1C-4a71-A11D-D6CBB811F25C")]
    internal class XmlTemplatedCodeGenerator : TemplatedCodeGenerator
    {
        protected override byte[] GenerateCode(string inputFileName, string inputFileContent)
        {
            System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            string fileExtension = "xml";

            DTE dte = Helper.GetDTE(currentProcess.Id.ToString());

            ProjectItem projectItem = dte.Solution.FindProjectItem(inputFileName);

            
            ResourceManager manager =
                new ResourceManager("Worm.Designer.VSPackage",
                                    typeof(XmlTemplatedCodeGenerator).Assembly);
            FileInfo fi = new FileInfo(inputFileName);
            inputFileContent =
                manager.GetObject("XmlGeneratorTemplate").ToString()
                    .Replace("%MODELFILE%", fi.Name)
                    .Replace("%MODELFILEFULLNAME%", fi.FullName)
                    .Replace("%NAMESPACE%", FileNamespace)
                    .Replace("%EXT%", fileExtension)
                    .Replace("%PID%", currentProcess.Id.ToString());

                      
            byte[] data = base.GenerateCode(inputFileName, inputFileContent);
            return data;
        }
    }
}