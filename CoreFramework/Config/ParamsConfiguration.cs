using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace CoreFramework.Configuration
{
    public class ParamsElementCollection : ConfigurationElementCollection, IEnumerable<ParamsElement>
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

        IEnumerator<ParamsElement> IEnumerable<ParamsElement>.GetEnumerator()
        {
            foreach (ParamsElement item in this)
            {
                yield return item;
            }
        }
        public IDictionary<string, string> ToDictionary()
        {
            return this.AsEnumerable().ToDictionary(it=>it.Key, it=>it.Value);
        }
        public int GetInt(string key, int def = 0)
        {
            var p = this[key];
            if (p!= null)
            {
                int i;
                if (int.TryParse(p.Value, out i))
                    return i;
            }

            return def;
        }
        public bool GetBool(string key, bool def = false)
        {
            var p = this[key];
            if (p != null)
            {
                bool i;
                if (bool.TryParse(p.Value, out i))
                    return i;
            }

            return def;
        }
    }

    public class ParamsElement : ConfigurationElement
    {
        [ConfigurationProperty("key", IsRequired = true, IsKey = true)]
        public string Key
        {
            get { return (string)this["key"]; }
            set { this["key"] = value; }
        }

        [ConfigurationProperty("value", IsRequired = false)]
        public string Value
        {
            get
            {
                if (this["value"] == null)
                    return null;
                return this["value"].ToString();
            }
            set { this["value"] = value; }
        }
    }

}
