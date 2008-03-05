using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DALinq
{
    public class LinqProvider
    {
        DatabaseDataContext db = new DatabaseDataContext();
           
        public void SelectWithoutLoad()
        {
             var query = from e in db.tbl_users where e.user_id == 1
                        select new { e.user_id, e.first_name, e.last_name };
            
        }

        public void SelectShortWithoutLoad()
        {
            var query = db.tbl_users.Where(c => c.user_id == 1);
        }

        public void SelectShortWithListLoad()
        {
            var query = db.tbl_users.Where(c => c.user_id == 1).ToList();
        }

        public void SelectShortWithLoad()
        {
            var query = db.tbl_users.Where(c => c.user_id == 1);
            foreach (var user in query)
            {
                string result = string.Format("{0} {1} {2}", user.user_id, user.first_name, user.last_name);
            }
        }

        public void SelectWithLoad()
        {
            var users = (from e in db.tbl_users
                         where e.user_id == 1
                         select new { e.user_id, e.first_name, e.last_name });
            foreach (var user in users)
            {
                string result = string.Format("{0} {1} {2}", user.user_id, user.first_name, user.last_name);
            }
        }

        public void SelectWithListLoad()
        {
            var users = (from e in db.tbl_users
                         where e.user_id == 1
                         select new { e.user_id, e.first_name, e.last_name }).ToList();           
        }
      


 

         public void SelectCollectionWithoutLoad()
        {
            var query = from e in db.tbl_users select new { e.user_id, e.first_name, e.last_name };
        }

         public void SelectCollectionShortWithoutLoad()
         {
             var query = db.tbl_users;
         }

         public void SelectCollectionShortWithListLoad()
         {
             var query = db.tbl_users.ToList();
         }

         public void SelectCollectionShortWithLoad()
         {
             var query = db.tbl_users;
             foreach (var user in query)
             {
                 string result = string.Format("{0} {1} {2}", user.user_id, user.first_name, user.last_name);
             }
         }

        public void SelectCollectionWithLoad()
        {
            var query = from e in db.tbl_users
                        select new { e.user_id, e.first_name, e.last_name };

            foreach (var user in query)
            {
                string result = string.Format("{0} {1} {2}", user.user_id, user.first_name, user.last_name);
            }
        }

        public void SelectCollectionWithListLoad()
        {
            List<tbl_user> users = db.tbl_users.ToList();
        }
      
      
       




       

        public void SelectSmallCollectionWithoutLoad()
        {
            var phones = db.tbl_phones;
        }

        public void SelectSmallCollectionWithListLoad()
        {
           var phones = db.tbl_phones.ToList();
        }

        public void SelectSmallCollectionWithLoad()
        {
            var phones = db.tbl_phones;
            foreach (var phone in phones)
            {
                string result = string.Format("{0} {1} {2}", phone.phone_id, phone.user_id, phone.phone_number);
            }
        }

        public void SelectSmallWithoutLoad()
        {
            var phones = (from e in db.tbl_phones
                          where e.phone_id == 1
                          select new { e.phone_id, e.user_id, e.phone_number });
        }
       

        public void SelectSmallWithListLoad()
        {
            var phones = (from e in db.tbl_phones
                          where e.phone_id == 1
                          select new { e.phone_id, e.user_id, e.phone_number }).ToList();
           
        }

        public void SelectSmallWithLoad()
        {
            var phones = (from e in db.tbl_phones
                          where e.phone_id == 1
                          select new { e.phone_id, e.user_id, e.phone_number });
            foreach (var phone in phones)
            {
                string result = string.Format("{0} {1} {2}", phone.phone_id, phone.user_id, phone.phone_number);
            }
        }


    }
}
