﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using TShockAPI.DB;
using TShockAPI;

namespace ExtendedAdmin.DB
{
    public class RaffleManager
    {
        private IDbConnection _Connection;

        public RaffleManager(IDbConnection db)
        {
            _Connection = db;

            var raffleTable = new SqlTable("Raffle",
                new SqlColumn("RaffleID", MySql.Data.MySqlClient.MySqlDbType.Int32)
                {
                    AutoIncrement = true,
                    Primary = true,
                    Unique = true,
                    NotNull = true
                },
                new SqlColumn("LastRaffle", MySql.Data.MySqlClient.MySqlDbType.Text),
                new SqlColumn("Pot", MySql.Data.MySqlClient.MySqlDbType.Int32),
                new SqlColumn("Winner", MySql.Data.MySqlClient.MySqlDbType.Text));

            var creator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            creator.EnsureExists(raffleTable);

            var raffleTicket = new SqlTable("RaffleTicket",
                new SqlColumn("RaffleID", MySql.Data.MySqlClient.MySqlDbType.Int32),
                new SqlColumn("User", MySql.Data.MySqlClient.MySqlDbType.Text),
                new SqlColumn("TicketCount", MySql.Data.MySqlClient.MySqlDbType.Int32));

            creator.EnsureExists(raffleTicket);
        }

        public RaffleHelper GetCurrentRaffle()
        {
            RaffleHelper raffle = null;

            try
            {
                using (var reader = _Connection.QueryReader("SELECT * FROM Raffle WHERE Winner IS NULL"))
                {
                    if (reader.Read())
                    {
                        raffle = new RaffleHelper()
                        {
                            RaffleID = reader.Get<int>("RaffleID"),
                            LastRaffle = reader.Get<string>("LastRaffle") != null ? DateTime.Parse(reader.Get<string>("LastRaffle")) : DateTime.MinValue,
                            Pot = reader.Get<int>("Pot"),
                            Winner = reader.Get<string>("Winner")
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                ExtendedLog.Current.Log(ex.ToString());
            }

            return raffle;
        }

        public bool BuyTicket(TSPlayer player, int amount, int cost)
        {
            bool success = false;

            string user = player.UserAccountName;

            RaffleHelper raffle = GetCurrentRaffle();

            if (raffle != null)
            {
                RaffleTicketHelper raffleTicket = GetRaffleTickets(user, raffle.RaffleID);

                try
                {
                    var account = GetServerPointAccounts(user);

                    ServerPointSystem.ServerPointSystem.Deduct(new CommandArgs("deduct", player, new List<string>() {
                        player.Name,
                        cost.ToString()
                    }));

                    //_Connection.Query("UPDATE serverpointaccounts SET amount = @0 WHERE name = @1", (account.Amount - cost), user);

                    if (raffleTicket.Exists)
                    {
                        _Connection.Query("UPDATE RaffleTicket SET TicketCount = @0 WHERE User = @1 AND RaffleID = @2", raffleTicket.TicketCount + amount, user, raffle.RaffleID);
                    }
                    else
                    {
                        _Connection.Query("INSERT INTO RaffleTicket (RaffleID, User, TicketCount) VALUES (@0, @1, @2)", raffle.RaffleID, raffleTicket.User, amount);
                    }

                    _Connection.Query("UPDATE Raffle SET Pot = @0 WHERE RaffleID = @1", raffle.Pot + cost, raffle.RaffleID);

                    success = true;
                }
                catch (Exception ex)
                {
                    ExtendedLog.Current.Log(ex.ToString());
                }
            }

            return success;
        }

        public ServerPointAccountsHelper GetServerPointAccounts(string user)
        {
            ServerPointAccountsHelper account = null;

            try
            {
                using (var reader = _Connection.QueryReader("SELECT * FROM serverpointaccounts WHERE name = @0", user))
                {
                    if (reader.Read())
                    {
                        account = new ServerPointAccountsHelper()
                        {
                            ID = reader.Get<int>("ID"),
                            Name = reader.Get<string>("name"),
                            Amount = reader.Get<int>("amount")
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                ExtendedLog.Current.Log(ex.ToString());
            }

            return account;
        }

        public RaffleTicketHelper GetRaffleTickets(string user, int raffleID)
        {
            RaffleTicketHelper raffleTicket = null;

            try
            {
                using (var reader = _Connection.QueryReader("SELECT * FROM RaffleTicket WHERE User = @0 AND RaffleID = @1", user, raffleID))
                {
                    if (reader.Read())
                    {
                        raffleTicket = new RaffleTicketHelper()
                        {
                            RaffleID = reader.Get<int>("RaffleID"),
                            TicketCount = reader.Get<int>("TicketCount"),
                            User = reader.Get<string>("User"),
                            Exists = true
                        };
                    }
                    else
                    {
                        raffleTicket = new RaffleTicketHelper()
                        {
                            RaffleID = raffleID,
                            User = user,
                            TicketCount = 0,
                            Exists = false
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                ExtendedLog.Current.Log(ex.ToString());
            }

            return raffleTicket;
        }

        public bool CreateRaffle(int startPot)
        {
            bool success = false;

            try
            {
                _Connection.Query("INSERT INTO Raffle (Pot) VALUES (@0)", startPot);

                success = true;
            }
            catch (Exception ex)
            {
                ExtendedLog.Current.Log(ex.ToString());
            }

            return success;
        }

        public List<RaffleTicketHelper> GetAllCurrentRaffleTickets()
        {
            List<RaffleTicketHelper> raffleTickets = new List<RaffleTicketHelper>();

            try
            {
                var raffle = GetCurrentRaffle();

                using (var reader = _Connection.QueryReader("SELECT * FROM RaffleTicket WHERE RaffleID = @0", raffle.RaffleID))
                {
                    while (reader.Read())
                    {
                        raffleTickets.Add(new RaffleTicketHelper()
                        {
                            RaffleID = reader.Get<int>("RaffleID"),
                            TicketCount = reader.Get<int>("TicketCount"),
                            User = reader.Get<string>("User"),
                            Exists = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ExtendedLog.Current.Log(ex.ToString());
                raffleTickets = null;
            }

            return raffleTickets;
        }

        public void Reward(string user, TSPlayer player, int amount)
        {
            var raffle = GetCurrentRaffle();

            try
            {
                _Connection.Query("UPDATE Raffle SET Winner = @0, LastRaffle = @1 WHERE RaffleID = @2", user, DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"), raffle.RaffleID);

                var account = GetServerPointAccounts(user);

                if (player != null)
                {
                    ServerPointSystem.ServerPointSystem.Award(new CommandArgs("award", player, new List<string>()
                    {
                        player.Name,
                        amount.ToString()
                    }));
                }
                else
                {
                    _Connection.Query("UPDATE serverpointaccounts SET amount = @0 WHERE name = @1", account.Amount + amount, user);
                }
            }
            catch (Exception ex)
            {
                ExtendedLog.Current.Log(ex.ToString());
            }
        }

        public void UpdateRaffleTime()
        {
            try
            {
                var raffle = GetCurrentRaffle();

                _Connection.Query("UPDATE Raffle SET LastRaffle = @0 WHERE RaffleID = @1", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"), raffle.RaffleID);
            }
            catch (Exception ex)
            {
                ExtendedLog.Current.Log(ex.ToString());
            }
        }

        public void DecreaseTickets(float percentKept)
        {
            try
            {
                var raffle = GetCurrentRaffle();

                using (var reader = _Connection.QueryReader("SELECT * FROM RaffleTicket WHERE RaffleID = @0", raffle.RaffleID))
                {
                    while (reader.Read())
                    {
                        string user = reader.Get<string>("User");
                        int ticketCount = reader.Get<int>("TicketCount");

                        _Connection.Query("UPDATE RaffleTicket SET TicketCount = @0 WHERE User = @1", (int)(ticketCount * (percentKept / 100f)), user);
                    }
                }
            }
            catch (Exception ex)
            {
                ExtendedLog.Current.Log(ex.ToString());
            }
        }
    }

    public class RaffleHelper
    {
        public int RaffleID;
        public DateTime LastRaffle;
        public int Pot;
        public string Winner;
    }

    public class RaffleTicketHelper
    {
        public int RaffleID;
        public string User;
        public int TicketCount;
        public bool Exists;
    }

    public class ServerPointAccountsHelper
    {
        public int ID;
        public string Name;
        public int Amount;
    }
}