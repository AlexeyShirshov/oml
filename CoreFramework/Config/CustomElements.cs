using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace CoreFramework.Configuration
{
    public class CustomElementsCollection : ConfigurationElementCollection
    {
        public CustomElementsElement this[int index]
        {
            get { return BaseGet(index) as CustomElementsElement; }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new CustomElementsElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((CustomElementsElement)element).Name;
        }

        public void SetValue(string key, string value)
        {
            this.BaseAdd(new CustomElementsElement { Name = key, Type = value });
        }
        public ConfigurationElement CreateConfigurationElement(string elementName, System.Xml.XmlReader reader, out bool found)
        {
            found = false;
            foreach (CustomElementsElement ce in this)
            {
                if (ce.Name == elementName)
                {
                    found = true;
                    var t = ce.GetCustomElementType();
                    if (t != null)
                    {
                        return Activator.CreateInstance(t, reader) as ConfigurationElement;
                    }                    
                }
            }

            return null;
        }
    }

    public class CustomElementsElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }

        public Type GetCustomElementType()
        {
            try
            {
                return System.Type.GetType(Type);
            }
            catch
            {
                return null;
            }
            
        }
    }

}
