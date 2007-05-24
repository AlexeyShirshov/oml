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
            OrmObjectGeneratorBehaviour behaviour;

            if (cmdLine["?"] != null || cmdLine["h"] != null || cmdLine["help"] != null || args == null || args.Length == 0)
            {
                Console.WriteLine("Command line parameters:");
                Console.WriteLine("  -file\t- source xml file");
                Console.WriteLine("  -language\t- code language [cs, vb] (\"cs\" by default)");
                Console.WriteLine("  -split\t- split entity class and entity's schema definition\n\t\t  class code by diffrent files (\"false\" by default)");
                Console.WriteLine("  -behaviour\t- behaviour of codegenerator\n\t\t  [Objects, PartialObjects, BaseObjects] (\"Objects\" by default)");
                Console.WriteLine("  -separateFolder\t- create folder for each entity.");
				Console.WriteLine("  -output\t- output files folder.");
                return;
            }

            if (cmdLine["language"] != null)
                outputLanguage = cmdLine["language"];
            else
                outputLanguage = "CS";

            if (cmdLine["separateFolder"] != null || cmdLine["sf"] != null)
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
                Console.WriteLine("Error: incorrect value in \"language\" parameter.");
                return;
            }

            if (cmdLine["file"] != null)
                inputFilename = cmdLine["file"];
            else
            {
                Console.WriteLine("Please give 'file' parameter");
                return;
            }

            if(cmdLine["split"] != null)
                split = true;
            else
                split = false;

            if (cmdLine["behaviour"] != null)
                try
                {
                    behaviour = (OrmObjectGeneratorBehaviour)Enum.Parse(typeof(OrmObjectGeneratorBehaviour), cmdLine["behaviour"]);
                }
                catch
                {
                    Console.WriteLine("Error: incorrect value in \"behaviour\" parameter.");
                    return;
                }
            else
                behaviour = OrmObjectGeneratorBehaviour.Objects;

            if (cmdLine["output"] != null)
                outputFolder = cmdLine["output"];
            else
            {
                outputFolder = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(outputFolder))
                {
                    outputFolder = System.IO.Path.GetPathRoot(System.Reflection.Assembly.GetExecutingAssembly().Location);
                }
            }

            if (cmdLine["skip"] != null)
                skipEntities = cmdLine["skip"].Split(',');
            else
                skipEntities = new string[] { };

            if(!System.IO.File.Exists(inputFilename))
            {
                Console.WriteLine("Error: source file not found.");
                return;
            }

			if (!System.IO.Directory.Exists(outputFolder))
			{
				System.IO.Directory.CreateDirectory(outputFolder);
			}

			if (!System.IO.Path.IsPathRooted(outputFolder))
			{
				outputFolder = System.IO.Path.Combine(Directory.GetCurrentDirectory(), outputFolder);
			}

			//if (string.IsNullOrEmpty(System.IO.Path.GetDirectoryName(outputFolder)))
			//    outputFolder = System.IO.Path.GetPathRoot(outputFolder + System.IO.Path.DirectorySeparatorChar.ToString());
			//else
			//    outputFolder = System.IO.Path.GetDirectoryName(outputFolder + System.IO.Path.DirectorySeparatorChar.ToString());

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

            if(!Directory.Exists(outputFolder))
            {
                try
                {
                    Directory.CreateDirectory(outputFolder);
                }
                catch (Exception)
                {
                }
            }

            OrmCodeDomGenerator gen = new OrmCodeDomGenerator(ormObjectsDef);

            OrmCodeDomGeneratorSettings settings = new OrmCodeDomGeneratorSettings();
            settings.Behaviour = behaviour;
            settings.Split = split;
            settings.LanguageSpecificHacks = languageHacks;

            Console.WriteLine("Generation entities from file '{0}' using these settings:", inputFilename);
            Console.WriteLine("  Output folder: {0}", outputFolder);
            Console.WriteLine("  Language: {0}", outputLanguage.ToLower());
            Console.WriteLine("  Behaviour: {0}", settings.Behaviour);
            Console.WriteLine("  Split files: {0}", split);
            Console.WriteLine("  Skip entities: {0}", string.Join(" ", skipEntities));
            Console.WriteLine("  IsPartial: {0}", settings.IsPartial);            

            foreach (EntityDescription entity in ormObjectsDef.Entities)
            {
                bool skip = false;
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
