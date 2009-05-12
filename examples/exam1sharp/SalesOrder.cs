using System;
using System.Collections.Generic;
using System.Text;
using exam1sharp.Sales;
using Worm.Entities.Meta;
using Worm.Query;
using Worm.Cache;

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
                    .SelectEntity(typeof(SalesOrder));
            }
        }

        public static QueryCmd GetQuery(CacheBase cache)
        {
            return new QueryCmd(cache, exam1sharp.Properties.Settings.Default.connString)
                .From(typeof(SalesOrder))
                .SelectEntity(typeof(SalesOrder));
        }
    }

    public class EntitySchema : IMultiTableObjectSchema
    {
        //define tables
        private SourceFragment[] _tables = new SourceFragment[]{
                new SourceFragment("Sales","SalesOrderHeader"),
                new SourceFragment("Sales","SalesOrderDetail")
            };

        //define join
        public Worm.Criteria.Joins.QueryJoin GetJoins(SourceFragment left, SourceFragment right)
        {
            if (right == _tables[0])        //join SalesOrderHeader
                return JCtor.join(right).on(left, "SalesOrderID").eq(right, "SalesOrderID");
            else if (right == _tables[1])   //join SalesOrderDetail
                return JCtor.join(right).on(left, "SalesOrderID").eq(right, "SalesOrderID");
            else
                throw new NotImplementedException();
        }

        //define mapping
        public Worm.Collections.IndexedCollection<string, MapField2Column> GetFieldColumnMap()
        {
            OrmObjectIndex columns = new OrmObjectIndex();
            columns.Add(new MapField2Column("SalesOrderDetailID", "SalesOrderDetailID", _tables[1], Field2DbRelations.PK));
            columns.Add(new MapField2Column("OrderQty", "OrderQty", _tables[1]));
            columns.Add(new MapField2Column("LineTotal", "LineTotal", _tables[1], Field2DbRelations.SyncUpdate | Field2DbRelations.ReadOnly));
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
