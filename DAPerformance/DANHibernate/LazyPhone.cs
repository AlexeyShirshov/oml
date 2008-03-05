using System;
using System.Collections.Generic;
using System.Text;
namespace DANHibernate
{
    public class LazyPhone
    {
        private int _userId;
        private int _phoneId;
        private string _phoneNumber;

        public LazyPhone()
        {
        }

        public LazyPhone(int phoneId, int userId, string phoneNumber)
        {
            _phoneId = phoneId;
            _userId = userId;
            _phoneNumber = phoneNumber;
        }

        public virtual int UserId 
        {
            get{ return _userId; }
            set{ _userId = value; }
        }

        public virtual int PhoneId 
        {
            get { return _phoneId; }
            set { _phoneId = value; }
        }

         public virtual string PhoneNumber 
        {
            get { return _phoneNumber; }
            set { _phoneNumber = value; }
        }
    }

}
