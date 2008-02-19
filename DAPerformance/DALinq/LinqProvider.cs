using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DALinq
{
    public class LinqProvider
    {

        public void Select()
        {
            DatabaseDataContext db = new DatabaseDataContext();

            var results = from p in db.tbl_users
                          //where p.phone_number == "12345678" 
                          //   orderby p.phone_number
                          select p;
            foreach (var result in results)
            {
                
            }
        }
    }
}
