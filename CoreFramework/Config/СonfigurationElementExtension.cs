using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace CoreFramework.Configuration
{
    public static class СonfigurationElementExtension
    {
        public static void DeserializeElement(this ConfigurationElement ce, System.Xml.XmlReader reader, bool serializeCollectionKey)
        {
            var dynMethod = ce.GetType().GetMethod("DeserializeElement", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, null, 
                new[] { typeof(XmlReader), typeof(bool) }, null);
            dynMethod.Invoke(ce, new object[] { reader, serializeCollectionKey });
        }
    }
}
