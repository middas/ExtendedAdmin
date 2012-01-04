using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace ExtendedAdmin.DB
{
    public class SqlTableFactory
    {
        public static IBaseTable GetInstance<U>(IDbConnection db) where U : IBaseTable
        {
            IBaseTable baseClass = (IBaseTable)Activator.CreateInstance(typeof(U), db);

            return baseClass;
        }
    }
}
