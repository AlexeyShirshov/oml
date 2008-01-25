using System.Reflection;
using System.Xml;
using System.IO;

namespace TestsCodeGenLib
{
    class Resources
    {
        public static Stream GetXmlDocumentStream(string documentName)
        {
            string extension = "xml";
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName;
            resourceName = string.Format("{0}.{1}.{2}", assembly.GetName().Name, documentName, extension);

            return assembly.GetManifestResourceStream(resourceName);
        }

        public static XmlDocument GetXmlDocument(string documentName)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(GetXmlDocumentStream(documentName));
            return doc;
        }
    }
}
