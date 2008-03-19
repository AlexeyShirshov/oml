using System;
using System.Collections.Generic;
using System.Text;

namespace Worm.CodeGen.XmlGenerator
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Worm xml schema generator. v0.1 2007");
			if (args.Length == 0)
			{
				ShowUsage();
				return;
			}

			CommandLine.Utility.Arguments param = new CommandLine.Utility.Arguments(args);

			string server = null;
			if (!param.TryGetParam("S",out server))
			{
				server = "(local)";
			}

			string m = null;
			if (!param.TryGetParam("M", out m))
			{
				m = "msft";
			}

			switch (m)
			{
				case "msft":
					//do nothing
					break;
				default:
					Console.WriteLine("Invalid manufacturer parameter.");
					ShowUsage();
					return;
			}

			string db = null;
			if (!param.TryGetParam("D", out db))
			{
				Console.WriteLine("Database is not specified");
				ShowUsage();
				return;
			}

			string e;
			string user = null;
			string psw = null;
			if (!param.TryGetParam("E", out e))
			{
				e = "false";
				if (!param.TryGetParam("U", out user))
				{
					Console.WriteLine("User is not specified");
					ShowUsage();
					return;
				}
				if (!param.TryGetParam("P", out psw))
				{
					Console.Write("Password: ");
					ConsoleColor c = Console.ForegroundColor;
					Console.ForegroundColor = Console.BackgroundColor;
					psw = Console.ReadLine();
					Console.ForegroundColor = c;
				}
			}
			bool i = bool.Parse(e);

			string schemas = null;
			param.TryGetParam("schemas", out schemas);

			string namelike = null;
			param.TryGetParam("name", out namelike);

			string file = null;
			if (!param.TryGetParam("O", out file))
			{
				file = server + ".xml";
			}

			string merge = null;
			if (!param.TryGetParam("F", out merge))
			{
				merge = "merge";
			}
			switch (merge)
			{
				case "merge":
				case "error":
					//do nothing
					break;
				default:
					Console.WriteLine("Invalid \"Existing file behavior\" parameter.");
					ShowUsage();
					return;
			}

			string drop = null;
			if (!param.TryGetParam("R", out drop))
				drop = "false";
			bool dr = bool.Parse(drop);

			string namesp = string.Empty;
			param.TryGetParam("N", out namesp);

			string u = null;
			if (!param.TryGetParam("Y", out u))
				u = "false";
			bool unify = bool.Parse(u);

            string tr = null;
            if (!param.TryGetParam("T", out tr))
                tr = "false";
            bool transform = bool.Parse(tr);

            Generator g = new Generator(server, m, db, i, user, psw, transform);

			g.MakeWork(schemas, namelike, file, merge, dr, namesp, unify);

			Console.WriteLine("Done!");
			//Console.ReadKey();
		}

		static void ShowUsage()
		{
			Console.WriteLine("Command line parameters");
			Console.WriteLine("  -O=value\t-  Output file name. Example: -O=test.xml. Default is <server>.xml");
			Console.WriteLine("  -S=value\t-  Database server. Example: -S=(local). Default is (local).");
			Console.WriteLine("  -E\t\t-  Integrated security.");
			Console.WriteLine("  -U=value\t-  Username");
			Console.WriteLine("  -P=value\t-  Password. Will requested if need.");
			Console.WriteLine("  -D=value\t-  Initial catalog(database). Example: -D=test");
			Console.WriteLine("  -M=[msft]\t-  Manufacturer. Example: -M=msft. Default is msft.");
			Console.WriteLine("  -schemas=list\t-  Database schema filter. Example: -schemas=\"dbo,one\"");
			Console.WriteLine("  -name=value\t-  Database table name filter. Example: -name=aspnet_%; -name=!aspnet_%");
			Console.WriteLine("  -F=[error|merge]\t-  Existing file behavior. Example: -F=error. Default is merge.");
			Console.WriteLine("  -R\t\t-  Drop deleted columns. Meaningfull only with merge behavior. Example: -R.");
			Console.WriteLine("  -N=value\t-  Objects namespace. Example: -N=test.");
			Console.WriteLine("  -Y\t\t-  Unify entyties with the same PK. Example: -Y.");
            Console.WriteLine("  -T\t\t-  Transform property names. Example: -T.");
		}
	}
}
