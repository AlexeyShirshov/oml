using Worm.Orm;
using System;
using System.Collections.Generic;
using System.Text;

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
			return new OrmDBManager(new OrmCache(), new DbSchema("1"), new Properties.Settings().connectionString);
		}

		static void Main(string[] args)
		{
			using (OrmDBManager mgr = GetDBManager())
			{
				//create in-memory object
				//it is a simple object that have no relation to database at all
				int someTempIdentifier = -100;
				test.Albums firstAlbum = new test.Albums(someTempIdentifier, mgr.Cache, mgr.DbSchema);

				//set properties
				firstAlbum.Name = "firstAlbum";
				firstAlbum.Release = new DateTime(2005, 1, 1);

				//create transaction
				mgr.BeginTransaction();
				try
				{
					//ok. save it
					//we pass true to Save parameter to accept changes immediately after saving into database
					firstAlbum.Save(true);
				}
				finally
				{
					//rollback transaction to undo database changes
					mgr.Rollback();
				}
			}
		}
	}
}
