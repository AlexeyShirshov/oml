using System;
using System.Collections.Generic;
using System.Text;
using exam1sharp.Sales;
using Worm.Entities.Meta;
using Worm.Query;

namespace exam1sharp
{
    [Entity(typeof(EntitySchema), "1")]
    public class SalesOrder
    {
        public int SalesOrderDetailID { get; set; }
        public short OrderQty { get; set; }
        public decimal LineTotal { get; set; }

        //SalesOrderHeader fields
        public DateTime OrderDate { get; set; }
        public exam1sharp.Sales.SalesTerritory Territory { get; set; }
        public exam1sharp.Sales.SalesPerson Person { get; set; }

        public static QueryCmd Query
        {
            get
            {
                return new QueryCmd(exam1sharp.Properties.Settings.Default.connString)
                    .From(typeof(SalesOrder))
                    .Select(typeof(SalesOrder));
            }
        }
    }

    public class EntitySchema : IMultiTableObjectSchema
    {
        private SourceFragment[] _tables = new SourceFragment[]{
                new SourceFragment("Sales","SalesOrderHeader"),
                new SourceFragment("Sales","SalesOrderDetail")
            };

        public Worm.Criteria.Joins.QueryJoin GetJoins(SourceFragment left, SourceFragment right)
        {
            return JCtor.join(right).on(left, "SalesOrderID").eq(right, "SalesOrderID");
        }

        public Worm.Collections.IndexedCollection<string, MapField2Column> GetFieldColumnMap()
        {
            OrmObjectIndex columns = new OrmObjectIndex();
            columns.Add(new MapField2Column("SalesOrderDetailID", "SalesOrderDetailID", _tables[1], Field2DbRelations.PK));
            columns.Add(new MapField2Column("OrderQty", "OrderQty", _tables[1]));
            columns.Add(new MapField2Column("LineTotal", "LineTotal", _tables[1]));
            columns.Add(new MapField2Column("OrderDate", "OrderDate", _tables[0]));
            columns.Add(new MapField2Column("Territory", "TerritoryID", _tables[0]));
            columns.Add(new MapField2Column("Person", "SalesPersonID", _tables[0]));
            return columns;
        }

        public SourceFragment[] GetTables()
        {
            return _tables;
        }

        public SourceFragment Table
        {
            get { return _tables[0]; }
        }
    }

}
