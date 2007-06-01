using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using System.CodeDom;
using OrmCodeGenLib;
using OrmCodeGenLib.Descriptors;
using System.Xml;
using System.CodeDom.Compiler;
using System.IO;


namespace OrmCodeGen
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("OrmObjects CodeGen utility.");
            Console.WriteLine();

			CommandLine.Utility.Arguments cmdLine = new CommandLine.Utility.Arguments(args);

            string outputLanguage;
            string outputFolder;
            OrmObjectsDef ormObjectsDef;
            string inputFilename;
            CodeDomProvider codeDomProvider;
            bool split, separateFolder;
            string[] skipEntities;
            string[] processEntities;
            OrmObjectGeneratorBehaviour behaviour;
            OrmCodeDomGeneratorSettings settings = new OrmCodeDomGeneratorSettings();

            if (cmdLine["?"] != null || cmdLine["h"] != null || cmdLine["help"] != null || args == null || args.Length == 0)
            {
                Console.WriteLine("Command line parameters:");
                Console.WriteLine("  -f\t- source xml file");
                Console.WriteLine("  -l\t- code language [cs, vb] (\"cs\" by default)");
                Console.WriteLine("  -p\t- generate partial classes (\"false\" by default)");
                Console.WriteLine("  -sp\t- split entity class and entity's schema definition\n\t\t  class code by diffrent files (\"false\" by default)");
                Console.WriteLine("  -sk\t- skip entities");
                Console.WriteLine("  -e\t- entities to process");
                Console.WriteLine("  -cB\t- behaviour of class codegenerator\n\t\t  [Objects, PartialObjects] (\"Objects\" by default)");
                Console.WriteLine("  -sF\t- create folder for each entity.");
				Console.WriteLine("  -o\t- output files folder.");
                Console.WriteLine("  -pmp\t- private members prefix (\"m_\" by default)");
                Console.WriteLine("  -cnP\t- class name prefix (null by default)");
                Console.WriteLine("  -cnS\t- class name suffix (null by default)");
                Console.WriteLine("  -fnP\t- file name prefix (null by default)");
                Console.WriteLine("  -fnS\t- file name suffix (null by default)");
                return;
            }

            if (cmdLine["l"] != null)
                outputLanguage = cmdLine["l"];
            else
                outputLanguage = "CS";

            if (cmdLine["sF"] != null)
                separateFolder = true;
            else
                separateFolder = false;
            LanguageSpecificHacks languageHacks = LanguageSpecificHacks.None;
            if(outputLanguage.ToUpper() == "VB")
            {
                codeDomProvider = new VBCodeProvider();
                languageHacks = LanguageSpecificHacks.VisualBasic;
            }
            else if(outputLanguage.ToUpper() == "CS")
            {
                codeDomProvider = new CSharpCodeProvider();
                languageHacks = LanguageSpecificHacks.CSharp;
            }
            else
            {
                Console.WriteLine("Error: incorrect value in \"l\" parameter.");
                return;
            }

            if (cmdLine["f"] != null)
                inputFilename = cmdLine["f"];
            else
            {
                Console.WriteLine("Please give 'f' parameter");
                return;
            }

            if(cmdLine["sp"] != null)
                split = true;
            else
                split = false;

            if (cmdLine["p"] != null)
                settings.Partial = true;
            else
                settings.Partial = false;

            if (cmdLine["cB"] != null)
                try
                {
                    behaviour = (OrmObjectGeneratorBehaviour)Enum.Parse(typeof(OrmObjectGeneratorBehaviour), cmdLine["cB"]);
                }
                catch
                {
                    Console.WriteLine("Error: incorrect value in \"cB\" parameter.");
                    return;
                }
            else
                behaviour = OrmObjectGeneratorBehaviour.Objects;

            if (cmdLine["o"] != null)
                outputFolder = cmdLine["o"];
            else
            {
                outputFolder = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(outputFolder))
                {
                    outputFolder = System.IO.Path.GetPathRoot(System.Reflection.Assembly.GetExecutingAssembly().Location);
                }
            }

            if (cmdLine["sk"] != null)
                skipEntities = cmdLine["sk"].Split(',');
            else
                skipEntities = new string[] { };

            if (cmdLine["e"] != null)
                processEntities = cmdLine["e"].Split(',');
            else
                processEntities = new string[] { };

            if(cmdLine["pmp"] != null)
            {
                settings.PrivateMembersPrefix = cmdLine["pmp"];
            }

            if(cmdLine["fnP"] != null)
            {
                settings.FileNamePrefix = cmdLine["fnP"];
            }
            if (cmdLine["fnS"] != null)
            {
                settings.FileNameSuffix = cmdLine["fnS"];
            }

            if (cmdLine["cnP"] != null)
            {
                settings.ClassNamePrefix = cmdLine["cnP"];
            }
            if (cmdLine["cnS"] != null)
            {
                settings.ClassNameSuffix = cmdLine["cnS"];
            }

            if(!System.IO.File.Exists(inputFilename))
            {
                Console.WriteLine("Error: source file not found.");
                return;
            }

			//if (!System.IO.Directory.Exists(outputFolder))
			//{
			//    Console.WriteLine("Error: output folder not found.");
			//    return;
			//}

			//if(string.IsNullOrEmpty(System.IO.Path.GetDirectoryName(outputFolder)))
			//    outputFolder = System.IO.Path.GetPathRoot(outputFolder + System.IO.Path.DirectorySeparatorChar.ToString());
			//else
			//    outputFolder = System.IO.Path.GetDirectoryName(outputFolder + System.IO.Path.DirectorySeparatorChar.ToString());

			if (!System.IO.Path.IsPathRooted(outputFolder))
			{
				outputFolder = System.IO.Path.Combine(Directory.GetCurrentDirectory(), outputFolder);
			}

			if (!System.IO.Directory.Exists(outputFolder))
			{
				System.IO.Directory.CreateDirectory(outputFolder);
			}
			
			try
            {
                Console.Write("Parsing file '{0}'...   ", inputFilename);
                using (XmlReader rdr = XmlReader.Create(inputFilename))
                {
                    ormObjectsDef = OrmObjectsDef.LoadFromXml(rdr);
                }
                Console.WriteLine("done!");
            }
            catch (Exception exc)
            {
                Console.WriteLine("error: {0}", exc.Message);
                return;
            }

			//if(!Directory.Exists(outputFolder))
			//{
			//    try
			//    {
			//        Directory.CreateDirectory(outputFolder);
			//    }
			//    catch (Exception)
			//    {
			//    }
			//}

            OrmCodeDomGenerator gen = new OrmCodeDomGenerator(ormObjectsDef);

            
            settings.Behaviour = behaviour;
            settings.Split = split;
            settings.LanguageSpecificHacks = languageHacks;

            Console.WriteLine("Generation entities from file '{0}' using these settings:", inputFilename);
            Console.WriteLine("  Output folder: {0}", outputFolder);
            Console.WriteLine("  Language: {0}", outputLanguage.ToLower());
            Console.WriteLine("  Behaviour: {0}", settings.Behaviour);
            Console.WriteLine("  Split files: {0}", split);
            Console.WriteLine("  Skip entities: {0}", string.Join(" ", skipEntities));
            Console.WriteLine("  Process entities: {0}", string.Join(" ", processEntities));
            Console.WriteLine("  IsPartial: {0}", settings.IsPartial);            

            foreach (EntityDescription entity in ormObjectsDef.Entities)
            {
                bool skip = false;
                if (processEntities.Length != 0)
                {
                    skip = true;
                    foreach (string processEntityId in processEntities)
                    {
                        if (processEntityId == entity.Identifier)
                        {
                            skip = false;
                            break;
                        }
                    }
                }
                foreach (string skipEntityId in skipEntities)
                {
                    if (skipEntityId == entity.Identifier)
                    {
                        skip = true;
                        break;
                    }
                }

                if (skip)
                    continue;

                string privateFolder;
                if (separateFolder)
                    privateFolder = outputFolder + System.IO.Path.DirectorySeparatorChar.ToString() + entity.Name + System.IO.Path.DirectorySeparatorChar;
                else
                    privateFolder = outputFolder + System.IO.Path.DirectorySeparatorChar.ToString();
                
                Dictionary<string, CodeCompileUnit> unitsDic;

                unitsDic = gen.GetEntityDom(entity.Identifier, settings);

                Console.WriteLine("Generating entity '{0}':", entity.Name);

                if (!System.IO.Directory.Exists(privateFolder))
                    System.IO.Directory.CreateDirectory(privateFolder);
                foreach (string name in unitsDic.Keys)
                {
                    Console.Write("  file '{0}'...   ", name);
                    try
                    {
                        GenerateCode(codeDomProvider, unitsDic[name], System.IO.Path.GetFullPath(privateFolder + System.IO.Path.DirectorySeparatorChar.ToString() + name));
                        Console.WriteLine("Done!");
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine("Error : {0}", exc.Message);
						if (cmdLine["debug"] == "true")
						{
							Console.WriteLine(exc.ToString());
						}
                        Console.WriteLine();
                    }
                }
                Console.WriteLine();
            }

            Console.WriteLine();
            
        }

        public static void GenerateCode(CodeDomProvider provider, CodeCompileUnit compileUnit, string filename)
        {
            String sourceFile;
            if (provider.FileExtension[0] == '.')
            {
                sourceFile = filename + provider.FileExtension;
            }
            else
            {
                sourceFile = filename + "." + provider.FileExtension;
            }

            using (IndentedTextWriter tw = new IndentedTextWriter(new StreamWriter(sourceFile, false), "\t"))
            {
                CodeGeneratorOptions opts = new CodeGeneratorOptions();
                opts.ElseOnClosing = false;
                opts.BracingStyle = "C";
                opts.ElseOnClosing = false;
                opts.IndentString = "\t";
                opts.VerbatimOrder = false;

                provider.GenerateCodeFromCompileUnit(compileUnit, tw, opts);
                tw.Close();
            }

        }

    }
}
