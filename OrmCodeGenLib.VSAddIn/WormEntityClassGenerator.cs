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

namespace Worm.CodeGen.VSTool
{
    /// <summary>
    /// This is the generator class. 
    /// When setting the 'Custom Tool' property of a C#, VB, or J# project item to "XmlClassGenerator", 
    /// the GenerateCode function will get called and will return the contents of the generated file 
    /// to the project system
    /// </summary>
    [ComVisible(true)]
    [Guid("30002F61-C5E0-4446-B306-8DD16358C006")]
    [CodeGeneratorRegistration(typeof(WormEntityClassGenerator), "C# WORM Entity Class Generator", vsContextGuids.vsContextGuidVCSProject, GeneratesDesignTimeSource = true)]
    [CodeGeneratorRegistration(typeof(WormEntityClassGenerator), "VB WORM Entity Class Generator", vsContextGuids.vsContextGuidVBProject, GeneratesDesignTimeSource = true)]
    [CodeGeneratorRegistration(typeof(WormEntityClassGenerator), "J# WORM Entity Class Generator", vsContextGuids.vsContextGuidVJSProject, GeneratesDesignTimeSource = true)]
    [ProvideObject(typeof(WormEntityClassGenerator))]
    public class WormEntityClassGenerator : Microsoft.Samples.VisualStudio.GeneratorSample.BaseCodeGeneratorWithSite
    {
#pragma warning disable 0414
        //The name of this generator (use for 'Custom Tool' property of project item)
        internal static string name = "WormEntityClassGenerator";
#pragma warning restore 0414

        //internal static bool validXML;

        /// <summary>
        /// Function that builds the contents of the generated file based on the contents of the input file
        /// </summary>
        /// <param name="inputFileContent">Content of the input file</param>
        /// <returns>Generated file as a byte array</returns>
        protected override byte[] GenerateCode(string inputFileContent)
        {
            //Validate the XML file against the schema
            OrmObjectsDef ormObjectsDef;
            using (StringReader contentReader = new StringReader(inputFileContent))
            {
                try
                {
                    using (XmlReader rdr = XmlReader.Create(contentReader))
                    {
                        ormObjectsDef = OrmObjectsDef.LoadFromXml(rdr, new XmlUrlResolver());
                    }
                }
                catch
                {
                    return null;
                }
            }

            CodeDomProvider provider = GetCodeProvider();

            try
            {
                if (this.CodeGeneratorProgress != null)
                {
                    //Report that we are 1/2 done
                    this.CodeGeneratorProgress.Progress(33, 100);
                }

                //Create the CodeCompileUnit from the passed-in XML file
                OrmCodeDomGeneratorSettings settings = new OrmCodeDomGeneratorSettings();
                settings.EntitySchemaDefClassNameSuffix = "SchemaDef";
                switch (GetDefaultExtension())
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
                //settings.Split = false;

                //ormObjectsDef.GenerateSchemaOnly
                OrmCodeDomGenerator generator = new OrmCodeDomGenerator(ormObjectsDef, settings);
                CodeCompileUnit compileUnit = generator.GetFullSingleUnit();

                if (this.CodeGeneratorProgress != null)
                {
                    //Report that we are 1/2 done
                    this.CodeGeneratorProgress.Progress(66, 100);
                }

                using (StringWriter writer = new StringWriter(new StringBuilder()))
                {
                    CodeGeneratorOptions options = new CodeGeneratorOptions();
                    options.BlankLinesBetweenMembers = false;
                    options.BracingStyle = "C";

                    //Generate the code
                    provider.GenerateCodeFromCompileUnit(compileUnit, writer, options);

                    if (this.CodeGeneratorProgress != null)
                    {
                        //Report that we are done
                        this.CodeGeneratorProgress.Progress(100, 100);
                    }
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
                this.GeneratorError(4, e.ToString(), 1, 1);
                //Returning null signifies that generation has failed
                return null;
            }
        }
    }
}