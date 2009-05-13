using System;
using System.Collections.Generic;
using System.Text;
using Worm.Entities;
using Worm.Entities.Meta;
using exam1sharp.Sales;
using Worm.Query;

namespace exam1sharp.Entity
{
    [Entity("Sales", "SalesPerson", "1")]
    public class SalesPerson : CachedEntity
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

        public DateTime ModifiedDate { get; set; }

        [EntityProperty("rowguid", Field2DbRelations.RowVersion)]
        public Guid Timestamp { get; protected set; }

        public static QueryCmd Query
        {
            get
            {
                return new QueryCmd(exam1sharp.Properties.Settings.Default.connString)
                    .From(typeof(SalesPerson))
                    .SelectEntity(typeof(SalesPerson));
            }
        }
    }
}
