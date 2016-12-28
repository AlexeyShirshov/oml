using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace CoreFramework.Configuration
{
    public class ParamsElementCollection : ConfigurationElementCollection
    {
        public new ParamsElement this[string key]
        {
            get { return BaseGet(key) as ParamsElement; }
            set
            {
                if (BaseGet(key) != null)
                {
                    BaseRemove(key);
                }
                SetValue(key, value == null ? string.Empty : value.ToString());
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ParamsElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ParamsElement)element).Key;
        }

        public void SetValue(string key, string value)
        {
            this.BaseAdd(new ParamsElement { Key = key, Value = value });
        }
    }

    public class ParamsElement : ConfigurationElement
    {
        [ConfigurationProperty("key", IsRequired = true)]
        public string Key
        {
            get { return (string)this["key"]; }
            set { this["key"] = value; }
        }

        [ConfigurationProperty("value", IsRequired = true)]
        public string Value
        {
            get { return (string)this["value"]; }
            set { this["value"] = value; }
        }
    }

}
