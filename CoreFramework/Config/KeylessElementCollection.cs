using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Xml;

namespace CoreFramework.Configuration
{
    public interface ICustomConfigurationElement
    {
        void DeserializeElement(System.Xml.XmlReader reader, bool serializeCollectionKey);
    }
    public class KeylessElementCollection<T> : ConfigurationElement, ICollection<T>
        where T : ConfigurationElement, ICustomConfigurationElement, new()
    {
        private List<T> _list = new List<T>();
        public KeylessElementCollection()
        {

        }
        public KeylessElementCollection(XmlReader reader)
        {
            DeserializeElement(reader, false);
        }
        protected override bool OnDeserializeUnrecognizedElement(string elementName, System.Xml.XmlReader reader)
        {
            if (elementName == "add")
            {
                var el = new T();
                el.DeserializeElement(reader, false);
                _list.Add(el);
                return true;
            }
            return base.OnDeserializeUnrecognizedElement(elementName, reader);
        }

        public void Add(T item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _list.Count; }
        }

        public new bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            return _list.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }
}
