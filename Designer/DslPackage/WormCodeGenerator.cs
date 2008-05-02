using System;
using System.Runtime.InteropServices;
using System.CodeDom.Compiler;
using System.CodeDom;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using Microsoft.Win32;
using Microsoft.VisualStudio.Shell;
using Worm.CodeGen.Core;
using VSLangProj80;
using System.Resources;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using EnvDTE80;

namespace Worm.Designer
{
    [Guid("5B7BDFA0-2C1C-4a71-A11D-D6CBB811F25C")]
    internal class WormCodeGenerator : TemplatedCodeGenerator
    {
        private CodeDomProvider codeDomProvider = null;
        protected override byte[] GenerateCode(string inputFileName, string inputFileContent)
        {
            System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            string fileExtension = "cs";

            DTE dte = Helper.GetDTE(currentProcess.Id.ToString());
            ProjectItem projectItem = dte.Solution.FindProjectItem(inputFileName);
            string ext = Helper.GetProjectLanguage(projectItem.ContainingProject);
            switch (ext)
            {
                case ".cs":
                    fileExtension = "cs";
                    break;
                case ".vb":
                    fileExtension = "vb";
                    break;
                default:
                    throw new ArgumentException(
                        "Unsupported project type. ActiveWriter currently supports C# and Visual Basic.NET projects.");
            }

            if (!inputFileName.EndsWith(".wxml"))
            {
                projectItem.Properties.Item("CustomTool").Value = string.Empty;
                return null;
            }
          

            ResourceManager manager =
                new ResourceManager("Worm.Designer.VSPackage",
                                    typeof(WormCodeGenerator).Assembly);
            FileInfo fi = new FileInfo(inputFileName);
            inputFileContent =
                manager.GetObject("XmlGeneratorTemplate").ToString()
                    .Replace("%MODELFILE%", fi.Name)
                    .Replace("%MODELFILEFULLNAME%", fi.FullName)
                    .Replace("%NAMESPACE%", FileNamespace)
                    .Replace("%EXT%", fileExtension)
                    .Replace("%ISCODE%", "False")
                    .Replace("%PID%", currentProcess.Id.ToString());

            byte[] data = base.GenerateCode(inputFileName, inputFileContent);

            string localFile = Path.Combine(Path.GetDirectoryName(projectItem.ContainingProject.FullName),
            fi.Name + ".xml");

            using (FileStream fs = File.Create(localFile))
            {
                // Add some information to the file.
                fs.Write(data, 0, data.Length);
            }

            projectItem.ProjectItems.AddFromFile(localFile);
            EnvDTE.ProjectItem xmlProjectItem = projectItem.ProjectItems.Item(fi.Name + ".xml");

           

            inputFileContent =
               manager.GetObject("XmlGeneratorTemplate").ToString()
                   .Replace("%MODELFILE%", fi.Name)
                   .Replace("%MODELFILEFULLNAME%", fi.FullName)
                   .Replace("%NAMESPACE%", FileNamespace)
                   .Replace("%EXT%", fileExtension)
                   .Replace("%ISCODE%", "True")
                   .Replace("%PID%", currentProcess.Id.ToString());


            data = base.GenerateCode(inputFileName, inputFileContent);
            return data;
        }

        /// <summary>
        /// Function that builds the contents of the generated file based on the contents of the input file
        /// </summary>
        /// <param name="inputFileContent">Content of the input file</param>
        /// <returns>Generated file as a byte array</returns>
        protected byte[] GenerateCode(byte[] data, string ext)
        {

            MemoryStream stream = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(stream, Encoding.UTF8);
            // get serialise object
            stream.Write(data, 0, data.Length);

            byte[] arr = new byte[data.Length];
            stream.Seek(0, SeekOrigin.Begin);
            // copy stream contents in byte array
            stream.Read(arr, 0, data.Length);
            UTF8Encoding utf = new UTF8Encoding(); // convert byte array to string
            string inputFileContent = utf.GetString(arr).Trim();

            //Validate the XML file against the schema
            OrmObjectsDef ormObjectsDef;
            using (StringReader contentReader = new StringReader(inputFileContent))
            {
                //try
                //{
                    using (XmlReader rdr = XmlReader.Create(contentReader))
                    {
                        ormObjectsDef = OrmObjectsDef.LoadFromXml(rdr, new XmlUrlResolver());
                    }
                //}
                //catch (Exception ex)
                //{
                //    return null;
                //}
               
            }

            CodeDomProvider provider = GetCodeProvider();

            try
            {
              
                OrmCodeDomGenerator generator = new OrmCodeDomGenerator(ormObjectsDef);

                //Create the CodeCompileUnit from the passed-in XML file
                OrmCodeDomGeneratorSettings settings = new OrmCodeDomGeneratorSettings();
                settings.EntitySchemaDefClassNameSuffix = "SchemaDef";
                switch (ext)
                {
                    case ".cs":
                        settings.LanguageSpecificHacks = LanguageSpecificHacks.CSharp;
                        break;
                    case ".vb":
                        settings.LanguageSpecificHacks = LanguageSpecificHacks.VisualBasic;
                        break;
                    //case ".js":
                    //    settings.LanguageSpecificHacks = LanguageSpecificHacks.VisualBasic;
                    //    break;
                }
                settings.PrivateMembersPrefix = "m_";
                settings.Split = false;

                CodeCompileUnit compileUnit = generator.GetFullSingleUnit(settings);

                using (StringWriter writer = new StringWriter(new StringBuilder()))
                {
                    CodeGeneratorOptions options = new CodeGeneratorOptions();
                    options.BlankLinesBetweenMembers = false;
                    options.BracingStyle = "C";

                    //Generate the code
                    provider.GenerateCodeFromCompileUnit(compileUnit, writer, options);

                    
                    writer.Flush();

                    //Get the Encoding used by the writer. We're getting the WindowsCodePage encoding, 
                    //which may not work with all languages
                    Encoding enc = Encoding.GetEncoding(writer.Encoding.WindowsCodePage);

                    //Get the preamble (byte-order mark) for our encoding
                    byte[] preamble = enc.GetPreamble();
                    int preambleLength = preamble.Length;

                    //Convert the writer contents to a byte array
                    byte[] body = enc.GetBytes(writer.ToString());

                    //Prepend the preamble to body (store result in resized preamble array)
                    Array.Resize<byte>(ref preamble, preambleLength + body.Length);
                    Array.Copy(body, 0, preamble, preambleLength, body.Length);

                    //Return the combined byte array
                    return preamble;
                }
            }
            catch (Exception e)
            {
                // this.GeneratorError(4, e.ToString(), 1, 1);
                //Returning null signifies that generation has failed
                return null;
            }
        }

        protected virtual CodeDomProvider GetCodeProvider()
        {
            if (codeDomProvider == null)
            {
                //Query for IVSMDCodeDomProvider/SVSMDCodeDomProvider for this project type
                IVSMDCodeDomProvider provider = GetService(typeof(SVSMDCodeDomProvider)) as IVSMDCodeDomProvider;
                if (provider != null)
                {
                    codeDomProvider = provider.CodeDomProvider as CodeDomProvider;
                }
                else
                {
                    //In the case where no language specific CodeDom is available, fall back to C#
                    codeDomProvider = CodeDomProvider.CreateProvider("C#");
                }
            }
            return codeDomProvider;
        }
    }
}