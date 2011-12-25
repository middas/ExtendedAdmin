using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using TShockAPI.DB;
using TShockAPI;

namespace ExtendedAdmin.DB
{
    public class PrisonManager
    {
        private IDbConnection _Connection;

        public PrisonManager(IDbConnection db)
        {
            _Connection = db;

            var table = new SqlTable("Prison",
                new SqlColumn("PrisonID", MySql.Data.MySqlClient.MySqlDbType.Int32)
                {
                    AutoIncrement = true,
                    NotNull = true,
                    Primary = true,
                    Unique = true
                },
                new SqlColumn("User", MySql.Data.MySqlClient.MySqlDbType.Text) { NotNull = true },
                new SqlColumn("Until", MySql.Data.MySqlClient.MySqlDbType.Text) { NotNull = true },
                new SqlColumn("Group", MySql.Data.MySqlClient.MySqlDbType.Text) { NotNull = true },
                new SqlColumn("IP", MySql.Data.MySqlClient.MySqlDbType.Text) { NotNull = true });

            var creator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

            creator.EnsureExists(table);
        }

        public bool IPInPrison(string ip)
        {
            bool inPrison = false;

            try
            {
                using (var reader = _Connection.QueryReader("SELECT * FROM Prison WHERE IP = @0", ip))
                {
                    List<PrisonHelper> prisons = new List<PrisonHelper>();
                    while (reader.Read())
                    {
                        prisons.Add(new PrisonHelper()
                        {
                            PrisonID = reader.Get<int>("PrisonID"),
                            User = reader.Get<string>("User"),
                            Until = DateTime.Parse(reader.Get<string>("Until")),
                            Group = reader.Get<string>("Group"),
                            IP = reader.Get<string>("IP")
                        });
                    }

                    inPrison = prisons.Count(p => p.Until > DateTime.Now) > 0;
                }
            }
            catch (Exception ex)
            {
                ExtendedLog.Current.Log(ex.ToString());
            }

            return inPrison;
        }

        public void AddPrisonRecord(TSPlayer player, DateTime until)
        {
            try
            {
                _Connection.Query("INSERT INTO Prison (User, Until, Group, IP) VALUES (@0, @1, @2, @3)", player.UserAccountName, until.ToString("MM/dd/yyyy HH:mm:ss"), player.Group.Name, player.IP);
            }
            catch (Exception ex)
            {
                ExtendedLog.Current.Log(ex.ToString());
            }
        }

        public void ExtendSentence(TSPlayer player, int minutes)
        {
            try
            {
                var record = GetPrisonRecordByIP(player.IP);

                _Connection.Query("UPDATE Prison SET Until = @0 WHERE PrisonID = @1", record.Until.AddMinutes(minutes).ToString("MM/dd/yyyy HH:mm:ss"), record.PrisonID);
            }
            catch (Exception ex)
            {
                ExtendedLog.Current.Log(ex.ToString());
            }
        }

        private PrisonHelper GetPrisonRecordByIP(string IP)
        {
            PrisonHelper helper = null;

            try
            {
                using (var reader = _Connection.QueryReader("SELECT * FROM Prison WHERE IP = @0", IP))
                {
                    List<PrisonHelper> records = new List<PrisonHelper>();

                    while (reader.Read())
                    {
                        records.Add(new PrisonHelper()
                        {
                            PrisonID = reader.Get<int>("PrisonID"),
                            Until = DateTime.Parse(reader.Get<string>("Until")),
                            User = reader.Get<string>("User"),
                            Group = reader.Get<string>("Group"),
                            IP = reader.Get<string>("IP")
                        });
                    }

                    helper = records.SingleOrDefault(p => p.Until > DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                ExtendedLog.Current.Log(ex.ToString());
            }

            return helper;
        }
    }

    public class PrisonHelper
    {
        public int PrisonID;
        public string User;
        public DateTime Until;
        public string Group;
        public string IP;
    }
}
