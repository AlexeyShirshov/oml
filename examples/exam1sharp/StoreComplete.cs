using System;
using System.Collections.Generic;
using System.Text;
using Worm.Entities.Meta;
using Worm.Query;
using System.Xml;

namespace exam1sharp.Sales
{
    public class SalesBase
    {
        public DateTime ModifiedDate { get; set; }

        [EntityProperty("rowguid", Field2DbRelations.RowVersion)]
        public Guid Timestamp { get; protected set; }
    }

    [Entity("Sales", "Store", "1")]
    public class Store : SalesBase
    {
        [EntityProperty("CustomerID", Field2DbRelations.PK)]
        public int ID { get; set; }

        public string Name { get; set; }

        public XmlDocument Demographics { get; set; }

        [EntityProperty("SalesPersonID")]
        public SalesPerson SalesPerson { get; set; }

        public static QueryCmd Query
        {
            get
            {
                return new QueryCmd(exam1sharp.Properties.Settings.Default.connString)
                    .From(typeof(Store))
                    .SelectEntity(typeof(Store));
            }
        }
    }

    [Entity("Sales", "SalesPerson", "1")]
    public class SalesPerson : SalesBase
    {
        [EntityProperty("SalesPersonID", Field2DbRelations.PK)]
        public int ID { get; set; }

        public decimal? SalesQuota { get; set; }

        [EntityProperty("TerritoryID")]
        public SalesTerritory SalesTerritory { get; set; }

        public decimal Bonus { get; set; }

        public decimal CommissionPct { get; set; }

        public decimal SalesYTD { get; set; }

        public decimal SalesLastYear { get; set; }

        public static QueryCmd Query
        {
            get
            {
                return new QueryCmd(exam1sharp.Properties.Settings.Default.connString)
                    .From(typeof(SalesPerson))
                    .SelectEntity(typeof(SalesPerson));
            }
        }

        public QueryCmd Stores
        {
            get
            {
                return Store.Query
                    .Where(Ctor.prop(typeof(Store), "SalesPerson").eq(this));
            }
        }
    }

    [Entity("Sales", "SalesTerritory", "1", RawProperties=true)]
    public class SalesTerritory : SalesBase
    {
        [EntityProperty("TerritoryID", Field2DbRelations.PrimaryKey)]
        public int ID { get; set; }

        public string Name { get; set; }

        public string CountryRegionCode { get; set; }

        [EntityProperty("[Group]")]
        public string Group { get; set; }

        public decimal SalesYTD { get; set; }

        public decimal SalesLastYear { get; set; }

        public decimal CostYTD { get; set; }

        public decimal CostLastYear { get; set; }
    }
}
