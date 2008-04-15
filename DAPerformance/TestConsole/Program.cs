using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.EntityClient;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using DAAdo;
using DAWorm;
using DaAdoEF;
using DALinq;
using Common;
using Tests;

using Worm.Cache;
using Worm.Database;
using Worm.Orm;

using DANHibernate;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Hql;
using NHibernate.Expression;
using NHibernate.Cfg;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //for (int i = 0; i < 4; i++)
            //{
                TestRunner runner = new TestRunner();
                runner.Start();
                runner.RunAdo();
                runner.RunLinq();
                runner.RunNHibernate();
                runner.RunWorm();
                runner.RunAdoEF();
                runner.End();
            //}
        }
    }
}
