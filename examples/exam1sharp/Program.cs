using Worm.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using Worm.Database;
using Worm.Cache;
using Worm;
using Worm.Query;
using Worm.Entities.Meta;

namespace exam1sharp
{
    class Program
    {
        /// <summary>
        /// Create database manager - the gateway to database
        /// </summary>
        /// <returns></returns>
        /// <remarks>The function creates instance of OrmDBManager class and pass to ctor new Cache, new database schema with version 1 and connection string</remarks>
        static OrmDBManager GetDBManager()
        {
            return new OrmDBManager(new OrmCache(), new ObjectMappingEngine("1"), new SQLGenerator(), new Properties.Settings().connectionString);
        }

        static void Main2(string[] args)
        {
            using (OrmDBManager mgr = GetDBManager())
            {
                //create in-memory object
                //it is a simple object that have no relation to database at all
                test.Album firstAlbum = new test.Album();

                //set properties
                firstAlbum.Name = "firstAlbum";
                firstAlbum.Release = new DateTime(2005, 1, 1);

                //create transaction
                mgr.BeginTransaction();
                try
                {
                    //ok. save it
                    //we pass true to Save parameter to accept changes immediately after saving into database
                    firstAlbum.SaveChanges(true);
                }
                finally
                {
                    //rollback transaction to undo database changes
                    mgr.Rollback();
                }
            }
        }

        static void Main3(string[] args)
        {
            var query = new QueryCmd(exam1sharp.Properties.Settings.Default.connString);

            foreach (Store s in query
                .From(new SourceFragment("Sales", "Store"))
                .ToPOCOList<Store>())
            {
                Console.WriteLine("Store id: {0}, name: {1}", s.CustomerID, s.Name);
            }
        }

        static void Main4(string[] args)
        {
            var query = new QueryCmd(exam1sharp.Properties.Settings.Default.connString);

            foreach (Store2 s in query
                .From(new SourceFragment("Sales", "Store"))
                .ToPOCOList<Store2>())
            {
                Console.WriteLine("Store id: {0}, name: {1}", s.ID, s.Name);
            }
        }

        static void Main5(string[] args)
        {
            foreach (Store3 s in Store3.Query
                .Where(Ctor.prop(typeof(Store3), "Name").like("A%"))
                .ToList())
            {
                Console.WriteLine("Store id: {0}, name: {1}, timestamp: {2}", s.ID, s.Name, s.Timestamp);
            }
        }

        static void Main6(string[] args)
        {
            foreach (Store4 s in Store4.Query
                .Where(Ctor.prop(typeof(Store4), "Name").like("A%"))
                .ToList())
            {
                Console.WriteLine("Store id: {0}, name: {1}, sales person quota: {2}",
                    s.ID, s.Name, s.SalesPerson.SalesQuota);
            }
        }

        static void Main7(string[] args)
        {
            foreach (exam1sharp.Sales.Store s in exam1sharp.Sales.Store.Query
                .Where(Ctor.prop(typeof(exam1sharp.Sales.Store), "Name").like("A%"))
                .ToList())
            {
                Console.WriteLine("Store id: {0}, name: {1}, sales territory: {2}",
                    s.ID,
                    s.Name,
                    s.SalesPerson.SalesTerritory.Name);
            }

            exam1sharp.Sales.SalesPerson p = exam1sharp.Sales.SalesPerson.Query
                .Where(Ctor.prop(typeof(exam1sharp.Sales.SalesPerson), "ID").eq(280))
                .Single() as exam1sharp.Sales.SalesPerson;

            foreach (exam1sharp.Sales.Store s in p.Stores.ToList())
            {
                Console.WriteLine("Store id: {0}, name: {1}, sales territory: {2}",
                    s.ID,
                    s.Name,
                    s.SalesPerson.SalesTerritory.Name);
            }
        }

        static void Main8(string[] args)
        {
            Worm.Database.OrmReadOnlyDBManager.StmtSource.Listeners.Add(
                new System.Diagnostics.TextWriterTraceListener(Console.Out)
            );

            Worm.OrmManager.ExecSource.Listeners.Add(
                new System.Diagnostics.TextWriterTraceListener(Console.Out)
            );

            foreach (SalesOrder s in SalesOrder.Query
                .Where(Ctor
                    .prop(typeof(SalesOrder), "OrderDate").eq("2003-08-01")
                    .and(typeof(SalesOrder), "LineTotal").less_than(10))
                .ToList())
            {
                Console.WriteLine("Date: {0}, LineTotal: {1}, sales territory: {2}",
                    s.OrderDate,
                    s.LineTotal,
                    s.Territory.Name);
            }
        }

        static void Main9(string[] args)
        {
            ReadonlyCache cache = new ReadonlyCache();

            var SalesOrderQuery = new QueryCmd(cache, exam1sharp.Properties.Settings.Default.connString)
                    .From(typeof(SalesOrder))
                    .SelectEntity(typeof(SalesOrder));

            //Worm.Database.OrmReadOnlyDBManager.StmtSource.Listeners.Add(
            //    new System.Diagnostics.TextWriterTraceListener(Console.Out)
            //);

            DateTime start = DateTime.Now;
            for (int i = 0; i < 100; i++)
            {
                foreach (SalesOrder s in new QueryCmd(cache, exam1sharp.Properties.Settings.Default.connString)
                    .From(typeof(SalesOrder))
                    .SelectEntity(typeof(SalesOrder))
                    .Where(Ctor
                        .prop(typeof(SalesOrder), "OrderDate").eq("2003-08-01")
                        .and(typeof(SalesOrder), "LineTotal").less_than(10))
                    .ToList())
                {
                    string str = String.Format("Date: {0}, LineTotal: {1}, sales territory: {2}",
                        s.OrderDate,
                        s.LineTotal,
                        s.Territory.Name);
                    Console.WriteLine(str);
                }
            }
            Console.WriteLine("Elapsed {0}", DateTime.Now - start);
        }

        static void Main10(string[] args)
        {
            var o = SalesOrder.Query
                .Where(Ctor.prop(typeof(SalesOrder), "SalesOrderDetailID").eq(1))
                .Single() as SalesOrder;

            o.OrderQty += 10;

            Console.WriteLine("LineTotal={0}", o.LineTotal);

            using (ModificationsTracker mt = new ModificationsTracker(exam1sharp.Properties.Settings.Default.connString))
            {
                mt.Add(o);
                mt.AcceptModifications();
            }

            Console.WriteLine("LineTotal={0}", o.LineTotal);
        }

        static void Main11(string[] args)
        {
            var o = SalesOrder.Query
                .Where(Ctor.prop(typeof(SalesOrder), "SalesOrderDetailID").eq(1))
                .Single() as SalesOrder;

            o.OrderQty += 10;
            o.OrderDate = new DateTime(2001, 07, 5);

            using (ModificationsTracker mt = new ModificationsTracker(exam1sharp.Properties.Settings.Default.connString))
            {
                mt.Add(o);
                mt.AcceptModifications();
            }

            o.OrderDate = new DateTime(2001, 07, 1);

            using (ModificationsTracker mt = new ModificationsTracker(exam1sharp.Properties.Settings.Default.connString))
            {
                mt.Add(o);
                mt.AcceptModifications();
            }
        }

        static void Main12(string[] args)
        {
            //create new instance
            Sales.SalesTerritory t = new exam1sharp.Sales.SalesTerritory()
            {
                Name = "China",
                CountryRegionCode = "CH",
                Group = "Asia",
                SalesYTD = 100,
                SalesLastYear = 100,
                ModifiedDate = DateTime.Now
            };

            //save new instance
            using (ModificationsTracker mt = new ModificationsTracker(exam1sharp.Properties.Settings.Default.connString))
            {
                mt.Add(t);
                mt.AcceptModifications();
            }

            Console.WriteLine("New SalesTerritory id = {0}", t.ID);

            //delete created instance
            using (ModificationsTracker mt = new ModificationsTracker(exam1sharp.Properties.Settings.Default.connString))
            {
                mt.Delete(t);
                mt.AcceptModifications();
            }
        }

        static void Main(string[] args)
        {
            int iterCount = 1000;
            DateTime start = DateTime.Now;
            for (int i = 0; i < iterCount; i++)
            {
                foreach (Sales.SalesPerson s in Sales.SalesPerson.Query
                    .ToPOCOList<Sales.SalesPerson>())
                {
                    string str = string.Format("Store id: {0}, bonus: {1}, territory name: {2}",
                        s.ID, s.Bonus, s.SalesTerritory != null ? s.SalesTerritory.Name : "no territory");
                    //Console.WriteLine(str);
                }
            }
            Console.WriteLine("POCO time {0}", DateTime.Now - start);

            start = DateTime.Now;
            for (int i = 0; i < iterCount; i++)
            {
                foreach (Entity.SalesPerson s in Entity.SalesPerson.Query
                    .ToList<Entity.SalesPerson>())
                {
                    string str = string.Format("Store id: {0}, bonus: {1}, territory name: {2}",
                        s.ID, s.Bonus, s.SalesTerritory != null ? s.SalesTerritory.Name : "no territory");
                    //Console.WriteLine(str);
                }
            }
            Console.WriteLine("Entity time {0}", DateTime.Now - start);
        }
    }
}
