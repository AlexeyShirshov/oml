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
                SqlCommand command = new SqlCommand("select * from tbl_user", conn);
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                conn.Open();                
                adapter.Fill(ds);
                conn.Close();
            }
            return ds;
        }
    }
}
