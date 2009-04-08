using System;
using System.Collections.Generic;
using System.Text;
using Worm.Entities.Meta;
using Worm.Query;
using System.Xml;

namespace exam1sharp
{
    public class Store
    {
        //public Store() { }
        public int CustomerID { get; set; }
        public string Name { get; set; }
    }

    public class Store2
    {
        [EntityProperty("CustomerID")]
        public int ID { get; set; }
        public string Name { get; set; }
        public DateTime ModifiedDate { get; set; }
    }

    [Entity("Sales", "Store", "1")]
    public class Store3
    {
        [EntityProperty("CustomerID", Field2DbRelations.PK)]
        public int ID { get; set; }

        public string Name { get; set; }
        
        public DateTime ModifiedDate { get; set; }
        
        public XmlDocument Demographics { get; set; }        
        
        [EntityProperty("rowguid", Field2DbRelations.RowVersion)]
        public Guid Timestamp { get; protected set; }

        public int SalesPersonID { get; set; }

        public static QueryCmd Query
        {
            get
            {
                return new QueryCmd(exam1sharp.Properties.Settings.Default.connString)
                    .From(typeof(Store3))
                    .Select(typeof(Store3));
            }
        }
    }
}
