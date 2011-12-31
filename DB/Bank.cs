using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using TShockAPI.DB;

namespace ExtendedAdmin.DB
{
    public class BankManager
    {
        private IDbConnection _Connection;

        public BankManager(IDbConnection db)
        {
            _Connection = db;

            var table = new SqlTable("Bank",
                new SqlColumn("User", MySql.Data.MySqlClient.MySqlDbType.Text) { Unique = true, Primary = true },
                new SqlColumn("Amount", MySql.Data.MySqlClient.MySqlDbType.Int32));

            var creator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

            creator.EnsureExists(table);
        }

        public BankHelper GetBalance(string user)
        {
            BankHelper balance = null;

            try
            {
                using (var reader = _Connection.QueryReader("SELECT * FROM Bank WHERE User = @0", user))
                {
                    if (reader.Read())
                    {
                        balance = new BankHelper()
                        {
                            User = reader.Get<string>("User"),
                            Amount = reader.Get<int>("Amount")
                        };
                    }
                    else
                    {
                        _Connection.Query("INSERT INTO Bank (User, Amount) VALUES (@0, 0)", user);

                        balance = new BankHelper()
                        {
                            User = user,
                            Amount = 0
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                ExtendedLog.Current.Log(ex.ToString());
            }

            return balance;
        }

        public bool Deposit(string user, int amount)
        {
            bool success = false;

            try
            {
                var account = GetBalance(user);

                _Connection.Query("UPDATE Bank SET Amount = @0 WHERE User = @1", account.Amount + amount, user);

                success = true;
            }
            catch (Exception ex)
            {
                ExtendedLog.Current.Log(ex.ToString());
            }

            return success;
        }

        public bool Withdraw(string user, int amount)
        {
            bool success = false;

            try
            {
                var account = GetBalance(user);

                _Connection.Query("UPDATE Bank SET Amount = @0 WHERE User = @1", account.Amount - amount, user);

                success = true;
            }
            catch (Exception ex)
            {
                ExtendedLog.Current.Log(ex.ToString());
            }

            return success;
        }
    }

    public class BankHelper
    {
        public string User;
        public int Amount;
    }
}
