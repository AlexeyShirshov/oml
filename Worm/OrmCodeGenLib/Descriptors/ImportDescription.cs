using System;
using System.Collections.Generic;
using System.Text;

namespace OrmCodeGenLib.Descriptors
{
    public class ImportDescription
    {
        private readonly string _name;
        private readonly OrmObjectsDef _content;
        private readonly string _uri;

        public ImportDescription(string name, OrmObjectsDef content, string fileUri)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            if (content == null)
                throw new ArgumentNullException("content");
            if (string.IsNullOrEmpty(fileUri))
                throw new ArgumentNullException("fileUri");
            _name = name;
            _content = content;
            _uri = fileUri;
        }

        public OrmObjectsDef Content
        {
            get { return _content; }
        }

        public string Name
        {
            get { return _name; }
        }

        public string FileUri
        {
            get { return _uri; }
        }
    }
}
