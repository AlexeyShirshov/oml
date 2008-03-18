using System;
using System.Data;
using System.Data.SqlClient;
using Helper;

namespace DAAdo
{
    public class AdoProvider
    {
        private SqlConnection conn;
        private string _connectionString = null;

        public AdoProvider(SqlConnection connection)
        {
            this.conn = connection;
        }

     
        public void TypeCycleWithLoad(int[] userIds)
        {
            foreach (int id in userIds)
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

   
        public void SmallCollection()
        {
            SqlCommand command = new SqlCommand(
                "select top " + Constants.Small  + "* from tbl_user", conn);
            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                int userId = reader.GetInt32(0);
                string firstName = reader.GetString(1);
                string lastName = reader.GetString(2);
            }
            reader.Close();
        }

        private string Convert(int[] ids)
        {
            System.Text.StringBuilder s = new System.Text.StringBuilder();
            foreach (int i in ids)
            {
                s.Append(i.ToString() + ",");
            }
            if (s.Length > 0) s.Remove(s.Length - 1, 1); //remove last comma
            return s.ToString();
        }

        public void CollectionWithChildrenByIdArray(int[] userIds)
        {
            
            SqlCommand command = new SqlCommand(
              "select tbl_user.user_id, first_name, last_name, phone_id, phone_number" +
            " from tbl_user inner join tbl_phone on tbl_user.user_id=tbl_phone.user_id" +
            " where tbl_user.user_id in (" + Convert(userIds) + ")", conn);
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

        public void CollectionByIdArray(int[] userIds)
        {
            SqlCommand command = new SqlCommand(
              "select * from tbl_user where [user_id] in (" + Convert(userIds) + ")", conn);
            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                int userId = reader.GetInt32(0);
                string firstName = reader.GetString(1);
                string lastName = reader.GetString(2);
                
            }
            reader.Close();
        }

        public void LargeCollection()
        {
            SqlCommand command = new SqlCommand(
               "select top " + Constants.Large + " * from tbl_user", conn);
            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                int userId = reader.GetInt32(0);
                string firstName = reader.GetString(1);
                string lastName = reader.GetString(2);
            }
            reader.Close();
        }

        public void CollectionByPredicateWithLoad()
        {
            for (int i = 0; i < Constants.LargeIteration; i++)
            {
                SqlCommand command = new SqlCommand(
              "select * from tbl_user inner join tbl_phone on tbl_user.user_id=tbl_phone.user_id" + 
              " where phone_number like '" + (i + 1).ToString() + "%'", conn);
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

        public void SelectBySamePredicate()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                SqlCommand command = new SqlCommand(
            "select * from tbl_user inner join tbl_phone on tbl_user.user_id=tbl_phone.user_id" +
            " where phone_number like '1%'", conn);
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


        #region Old
        public DataSet Select()
        {
            DataSet ds = new DataSet();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand("select * from tbl_user where user_id=1", conn);
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                conn.Open();                
                adapter.Fill(ds);
                conn.Close();
            }
            return ds;
        }

        public void SelectWithReader()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand("select * from tbl_user where user_id=1", conn);                
                conn.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string firstName = reader.GetString(1);
                    string lastName = reader.GetString(2);
                }
                reader.Close();
                conn.Close();
            }
        }

        public DataSet SelectCollection()
        {
            DataSet ds = new DataSet();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand("select * from tbl_user", conn);
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                conn.Open();
                adapter.Fill(ds);
                conn.Close();
            }
            return ds;
        }

        public void SelectCollectionWithReader()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand("select * from tbl_user", conn);
                conn.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string firstName = reader.GetString(1);
                    string lastName = reader.GetString(2);
                }
                reader.Close();
                conn.Close();
            }
        }

        public DataSet SelectSmall()
        {
            DataSet ds = new DataSet();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand("select * from tbl_phone where phone_id=1", conn);
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                conn.Open();
                adapter.Fill(ds);
                conn.Close();
            }
            return ds;
        }

        public void SelectSmallWithReader()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand("select * from tbl_phone where phone_id=1", conn);
                conn.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    int userId = reader.GetInt32(1);
                    string phoneNumber = reader.GetString(2);
                }
                reader.Close();
                conn.Close();
            }
        }

        public DataSet SelectCollectionSmall()
        {
            DataSet ds = new DataSet();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand("select * from tbl_phone", conn);
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                conn.Open();
                adapter.Fill(ds);
                conn.Close();
            }
            return ds;
        }

        public void SelectCollectionSmallWithReader()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand("select * from tbl_phone", conn);
                conn.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    int userId = reader.GetInt32(1);
                    string phoneNumber = reader.GetString(2);
                }
                reader.Close();
                conn.Close();
            }
        }
        #endregion
    }
}
