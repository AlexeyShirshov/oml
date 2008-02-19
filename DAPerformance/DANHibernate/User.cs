using System;
using System.Collections.Generic;
using System.Text;
namespace DANHibernate
{
    public class User
    {
        private int _userId;
        private string _firstName;
        private string _lastName;

        public User()
        {
        }

        public User(int userId, string firstName, string lastName)
        {
            _userId = userId;
            _firstName = firstName;
            _lastName = lastName;
        }

        public virtual int UserId 
        {
            get{ return _userId; }
            set{ _userId = value; }
        }

         public virtual string FirstName 
        {
            get{ return _firstName; }
            set{ _firstName = value; }
        }

         public virtual string LastName 
        {
            get{ return _lastName; }
            set{ _lastName = value; }
        }
    }

}
