using System;
using System.Collections.Generic;
using System.Text;

namespace Worm.CodeGen.Core.Descriptors
{
    public class TableDescription
    {
        private string _id;
        private string _name;

        public string Identifier
        {
            get { return _id; }
            set { _id = value; }
        }        

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public TableDescription(string id, string name)
        {
            if (String.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");
            if (String.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            _id = id;
            _name = name;
        }
    }
}
