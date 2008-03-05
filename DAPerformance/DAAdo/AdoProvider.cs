using System;
using System.Data;
using System.Data.SqlClient;

namespace DAAdo
{
    public class AdoProvider
    {
        private string _connectionString;

        public AdoProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

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
    }
}
