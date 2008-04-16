using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Common;

namespace DAAdo
{
    public class AdoProvider// : IRunnable
    {
        private SqlConnection conn;
        private string _connectionString = null;
         int[] smallUserIds;
         int[] mediumUserIds;
         int[] largeUserIds;

        public AdoProvider(SqlConnection connection, int[] smallUserIds, int[] mediumUserIds, int[] largeUserIds)
        {
            this.conn = connection;
            this.smallUserIds = smallUserIds;
            this.mediumUserIds = mediumUserIds;
            this.largeUserIds = largeUserIds;

            CollectionDataset(Constants.Small);
        }

        
        #region New
        [QueryTypeAttribute(QueryType.TypeCycleWithLoad)]
        public void TypeCycleWithLoad()
        {
            foreach (int id in mediumUserIds)
            {
                SqlCommand command = new SqlCommand(
                    "select * from tbl_user where user_id=" + id, conn);
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int userId = reader.GetInt32(0);
                    string firstName = reader.GetString(1);
                    string lastName = reader.GetString(2);
                }
                reader.Close();
            }
        }

         [QueryTypeAttribute(QueryType.TypeCycleWithoutLoad)]
        public void TypeCycleWithoutLoad()
        {
            foreach (int id in mediumUserIds)
            {
                SqlCommand command = new SqlCommand(
                    "select user_id from tbl_user where user_id=" + id, conn);
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int userId = reader.GetInt32(0);
                }
                reader.Close();
            }
        }


        [QueryTypeAttribute(QueryType.SmallCollectionByIdArray)]
        public void SmallCollectionByIdArray()
        {
            CollectionByIdArray(smallUserIds);
        }

        [QueryTypeAttribute(QueryType.SmallCollection)]
        public void SmallCollection()
        {
            Collection(Constants.Small);
        }

        [QueryTypeAttribute(QueryType.SmallCollectionWithChildrenByIdArray)]
        public void SmallCollectionWithChildrenByIdArray()
        {
            CollectionWithChildrenByIdArray(smallUserIds);
        }

        [QueryTypeAttribute(QueryType.LargeCollectionByIdArray)]
        public void LargeCollectionByIdArray()
        {
            CollectionByIdArray(largeUserIds);
        }

        [QueryTypeAttribute(QueryType.LargeCollection)]
        public void LargeCollection()
        {
            Collection(Constants.Large);
        }

        [QueryTypeAttribute(QueryType.LargeCollectionWithChildrenByIdArray)]
        public void LargeCollectionWithChildrenByIdArray()
        {
            CollectionWithChildrenByIdArray(largeUserIds);
        }


        

        [QueryTypeAttribute(QueryType.SelectLargeCollection)]
        public void SelectLargeCollection()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                Collection(Constants.Large);
            }
        }

        [QueryTypeAttribute(QueryType.SameObjectInCycleLoad)]
        public void SameObjectInCycleLoad()
        {
            SameObjectInCycleLoad(smallUserIds[0]);
        }

        public void Collection(int count)
        {
            SqlCommand command = new SqlCommand(
                "select top " + count + "* from tbl_user", conn);
            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                int userId = reader.GetInt32(0);
                string firstName = reader.GetString(1);
                string lastName = reader.GetString(2);
            }
            reader.Close();
        }
          
        public void CollectionWithChildrenByIdArray(int[] userIds)
        {
            SqlCommand command = new SqlCommand(
              "select tbl_user.user_id, first_name, last_name, phone_id, phone_number" +
            " from tbl_user inner join tbl_phone on tbl_user.user_id=tbl_phone.user_id" +
            " where tbl_user.user_id in (" + Helper.Convert(userIds, ",") + ")", conn);
            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                int userId = reader.GetInt32(0);
                string firstName = reader.GetString(1);
                string lastName = reader.GetString(2);
                int phoneId = reader.GetInt32(3);
                string phoneNumber = reader.GetString(4);
            }
            reader.Close();
        }

         void CollectionByIdArray(int[] userIds)
        {
            SqlCommand command = new SqlCommand(
              "select * from tbl_user where [user_id] in (" + Helper.Convert(userIds, ",") + ")", conn);
            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                int userId = reader.GetInt32(0);
                string firstName = reader.GetString(1);
                string lastName = reader.GetString(2);
                
            }
            reader.Close();
        }


        [QueryTypeAttribute(QueryType.CollectionByPredicateWithLoad)]
        public void CollectionByPredicateWithLoad()
        {
            for (int i = 0; i < Constants.LargeIteration; i++)
            {
                SqlCommand command = new SqlCommand(
                    "select * from tbl_user where user_id=" + i.ToString(), conn);

                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int userId = reader.GetInt32(0);
                    string firstName = reader.GetString(1);
                    string lastName = reader.GetString(2);
                }
                reader.Close();
            }
        }        

        public void SameObjectInCycleLoad(int userId)
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                SqlCommand command = new SqlCommand(
               "select * from tbl_user where user_id=" + userId, conn);
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string firstName = reader.GetString(1);
                    string lastName = reader.GetString(2);
                }
                reader.Close();
            }
        }
        [QueryTypeAttribute(QueryType.SelectBySamePredicate)]
        public void SelectBySamePredicate()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                SqlCommand command = new SqlCommand(
                    "select * from tbl_user where user_id=1", conn);
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int userId = reader.GetInt32(0);
                    string firstName = reader.GetString(1);
                    string lastName = reader.GetString(2);
                }
                reader.Close();
            }
        }
        [QueryTypeAttribute(QueryType.ObjectsWithLoadWithPropertiesAccess)]
        public void ObjectsWithLoadWithPropertiesAccess()
        {
            SqlCommand command = new SqlCommand(
            "select * from tbl_user", conn);
            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                int userId = reader.GetInt32(0);
                string firstName = reader.GetString(1);
                string lastName = reader.GetString(2);
            }
            reader.Close();
        }
        #endregion New

        #region Dataset
        [QueryTypeAttribute(QueryType.TypeCycleWithLoad, Syntax.Dataset)]
        public void TypeCycleWithLoadDataset()
        {         
            foreach (int id in mediumUserIds)
            {
                SqlCommand command = new SqlCommand(
                    "select * from tbl_user where user_id=" + id, conn);

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataSet ds = new DataSet();
                adapter.Fill(ds);
            }
        }

        [QueryTypeAttribute(QueryType.TypeCycleWithoutLoad, Syntax.Dataset)]
        public void TypeCycleWithoutLoadLinq()
        {
            foreach (int id in mediumUserIds)
            {
                SqlCommand command = new SqlCommand(
                    "select user_id from tbl_user where user_id=" + id, conn);
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int userId = reader.GetInt32(0);                   
                }
                reader.Close();
            }
        }


        [QueryTypeAttribute(QueryType.SmallCollectionByIdArray, Syntax.Dataset)]
        public void SmallCollectionByIdArrayDataset()
        {
            CollectionByIdArrayDataset(smallUserIds);
        }

        [QueryTypeAttribute(QueryType.SmallCollection, Syntax.Dataset)]
        public void SmallCollectionDataset()
        {
            CollectionDataset(Constants.Small);
        }

        [QueryTypeAttribute(QueryType.SmallCollectionWithChildrenByIdArray, Syntax.Dataset)]
        public void SmallCollectionWithChildrenByIdArrayDataset()
        {
            CollectionWithChildrenByIdArrayDataset(smallUserIds);
        }

        [QueryTypeAttribute(QueryType.LargeCollectionByIdArray, Syntax.Dataset)]
        public void LargeCollectionByIdArrayDataset()
        {
            CollectionByIdArrayDataset(largeUserIds);
        }

        [QueryTypeAttribute(QueryType.LargeCollection, Syntax.Dataset)]
        public void LargeCollectionDataset()
        {
            CollectionDataset(Constants.Large);
        }

        [QueryTypeAttribute(QueryType.LargeCollectionWithChildrenByIdArray, Syntax.Dataset)]
        public void LargeCollectionWithChildrenByIdArrayDataset()
        {
            CollectionWithChildrenByIdArrayDataset(largeUserIds);
        }


        [QueryTypeAttribute(QueryType.CollectionByPredicateWithLoad, Syntax.Dataset)]
        public void CollectionByPredicateWithLoadDataset()
        {
            for (int i = 0; i < Constants.LargeIteration; i++)
            {
                SqlCommand command = new SqlCommand(
                    "select * from tbl_user where user_id=" + i.ToString(), conn);

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataSet ds = new DataSet();
                adapter.Fill(ds);
            }
        }

        [QueryTypeAttribute(QueryType.SelectLargeCollection, Syntax.Dataset)]
        public void SelectLargeCollectionDataset()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                CollectionDataset(Constants.Large);
            }
        }

        [QueryTypeAttribute(QueryType.SameObjectInCycleLoad, Syntax.Dataset)]
        public void SameObjectInCycleLoadDataset()
        {
            SameObjectInCycleLoadDataset(smallUserIds[0]);
        }

        [QueryTypeAttribute(QueryType.SelectBySamePredicate, Syntax.Dataset)]
        public void SelectBySamePredicateDataset()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                SqlCommand command = new SqlCommand(
                    "select * from tbl_user where user_id=1", conn);

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataSet ds = new DataSet();
                adapter.Fill(ds);
            }
        }

        [QueryTypeAttribute(QueryType.ObjectsWithLoadWithPropertiesAccess, Syntax.Dataset)]
        public void ObjectsWithLoadWithPropertiesAccessDataset()
        {
            SqlCommand command = new SqlCommand(
            "select * from tbl_user", conn);
            SqlDataAdapter adapter = new SqlDataAdapter(command);
            DataSet ds = new DataSet();
            adapter.Fill(ds);
        }

        #endregion Dataset

        #region Dataset
        public void TypeCycleWithLoadDataset(int[] userIds)
        {
            foreach (int id in userIds)
            {
                SqlCommand command = new SqlCommand(
                    "select * from tbl_user where user_id=" + id, conn);

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataSet ds = new DataSet();  
                adapter.Fill(ds);
            }
        }


        public void CollectionDataset(int count)
        {
            SqlCommand command = new SqlCommand(
                "select top " + count + "* from tbl_user", conn);
            SqlDataAdapter adapter = new SqlDataAdapter(command);
            DataSet ds = new DataSet();
            adapter.Fill(ds);
        }

        public void CollectionWithChildrenByIdArrayDataset(int[] userIds)
        {

            SqlCommand command = new SqlCommand(
              "select tbl_user.user_id, first_name, last_name, phone_id, phone_number" +
            " from tbl_user inner join tbl_phone on tbl_user.user_id=tbl_phone.user_id" +
            " where tbl_user.user_id in (" + Helper.Convert(userIds, ",") + ")", conn);
            SqlDataAdapter adapter = new SqlDataAdapter(command);
            DataSet ds = new DataSet();
            adapter.Fill(ds);
        }

        public void CollectionByIdArrayDataset(int[] userIds)
        {
            SqlCommand command = new SqlCommand(
              "select * from tbl_user where [user_id] in (" + Helper.Convert(userIds, ",") + ")", conn);
            SqlDataAdapter adapter = new SqlDataAdapter(command);
            DataSet ds = new DataSet();
            adapter.Fill(ds);
        }



       



        public void SameObjectInCycleLoadDataset(int userId)
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                SqlCommand command = new SqlCommand(
               "select * from tbl_user where user_id=" + userId, conn);
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataSet ds = new DataSet();
                adapter.Fill(ds);
            }
        }

     
        #endregion Dataset
 
    }
}
