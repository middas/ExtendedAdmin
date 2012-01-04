using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using TShockAPI.DB;
using ExtendedAdmin;

namespace ExtendedAdmin.DB
{
    public class RegionHelperManager : IBaseTable
    {
        private IDbConnection _Connection;

        public RegionHelperManager(IDbConnection db)
        {
            _Connection = db;
        }

        public void InitializeTable()
        {
            var table = new SqlTable("RegionHelper",
                new SqlColumn("RegionName", MySql.Data.MySqlClient.MySqlDbType.Text),
                new SqlColumn("IsLocked", MySql.Data.MySqlClient.MySqlDbType.Text));

            var creator = new SqlTableCreator(_Connection, _Connection.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            creator.EnsureExists(table);
        }

        public bool LockRegion(string region)
        {
            bool success = false;

            try
            {
                success = _Connection.Query("UPDATE RegionHelper SET IsLocked = @0 WHERE RegionName = @1", "true", region) != 0;

                if (!success)
                {
                    success = _Connection.Query("INSERT INTO RegionHelper (RegionName, IsLocked) VALUES (@0, @1);", region, "true") != 0;
                }
            }
            catch (Exception ex)
            {
                ExtendedLog.Current.Log(ex.ToString());
            }

            return success;
        }

        public bool UnlockRegion(string region)
        {
            bool success = false;

            try
            {
                success = _Connection.Query("UPDATE RegionHelper SET IsLocked = @0 WHERE RegionName = @1", "false", region) != 0;

                if (!success)
                {
                    success = _Connection.Query("INSERT INTO RegionHelper (RegionName, IsLocked) VALUES (@0, @1);", region, "false") != 0;
                }
            }
            catch (Exception ex)
            {
                ExtendedLog.Current.Log(ex.ToString());
            }

            return success;
        }

        public RegionHelper GetRegionHelperByRegion(string region)
        {
            RegionHelper rh = null;

            try
            {
                using (var reader = _Connection.QueryReader("SELECT * FROM RegionHelper WHERE RegionName = @0", region))
                {
                    if (reader.Read())
                    {
                        rh = new RegionHelper()
                        {
                            RegionName = reader.Get<string>("RegionName"),
                            IsLocked = bool.Parse(reader.Get<string>("IsLocked"))
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                ExtendedLog.Current.Log(ex.ToString());
            }

            return rh;
        }
    }

    public class RegionHelper
    {
        public string RegionName
        {
            get;
            set;
        }

        public bool IsLocked
        {
            get;
            set;
        }
    }
}
