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
                .ToPODList<Store>())
            {
                Console.WriteLine("Store id: {0}, name: {1}", s.CustomerID, s.Name);
            }
        }

        static void Main(string[] args)
        {
            var query = new QueryCmd(exam1sharp.Properties.Settings.Default.connString);

            foreach (Store2 s in query.ToPODList<Store2>())
            {
                Console.WriteLine("Store id: {0}, name: {1}", s.ID, s.Name);
            }
        }
	}
}
