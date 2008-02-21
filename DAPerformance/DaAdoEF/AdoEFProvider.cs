using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TestDAModel;

namespace DaAdoEF
{
    public class AdoEFProvider
    {
        TestDAEntities entities = new TestDAEntities();

        public void Select()
        {
            List<tbl_user> users = entities.tbl_user.ToList();
        }
    }
}
