using System;
using System.Collections.Generic;
using System.Text;
using Worm.Entities.Meta;

namespace exam1sharp
{
    public class Store
    {
        //public Store() { }
        public int CustomerID { get; set; }
        public string Name { get; set; }
    }

    [Entity("Sales", "Store", "1")]
    public class Store2
    {
        [EntityProperty("CustomerID")]
        public int ID { get; set; }
        public string Name { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
