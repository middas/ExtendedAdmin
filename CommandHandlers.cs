using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using ExtendedAdmin.DB;
using CommonLibrary.Native;
using TShockAPI.DB;

namespace ExtendedAdmin
{
    public static class CommandHandlers
    {
        #region Prison
        public static void SendToPrison(CommandArgs args)
        {
            if (args.Parameters.Count < 2 || args.Parameters.Count > 2)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /sendtoprison <player> <minutes>", Color.Red);
                return;
            }

            var player = TShock.Utils.FindPlayer(args.Parameters[0]);

            if (player == null || player.Count == 0)
            {
                args.Player.SendMessage("No player matched your query.", Color.Red);
                return;
            }

            if (player.Count > 1)
            {
                args.Player.SendMessage("More than one player matched your query.", Color.Red);
                return;
            }

            int minutes = args.Parameters[1].ToIntegerOrDefault(-1);

            if (minutes < 1)
            {
                args.Player.SendMessage("Invalid number of minutes.", Color.Red);
                return;
            }

            ExtendedTSPlayer ePlayer = ExtendedAdmin.Players[player[0].Index];

            PrisonManager manager = new PrisonManager(TShock.DB);

            manager.AddPrisonRecord(player[0], DateTime.Now.AddMinutes(minutes));

            ServerPointSystem.ServerPointSystem.Deduct(new CommandArgs("deduct", player[0], new List<string>()
            {
                player[0].Name,
                ExtendedAdmin.Config.PrisonShards.ToString()
            }));

            UpdateGroup(player[0], player[0].UserAccountName, ExtendedAdmin.Config.PrisonGroup);

            ePlayer.PrisonRecord = manager.GetPrisonerUser(player[0].UserAccountName);

            var warp = TShock.Warps.FindWarp(ExtendedAdmin.Config.PrisonWarp);
            if (warp.WarpPos != Vector2.Zero)
            {
                player[0].Teleport((int)warp.WarpPos.X, (int)warp.WarpPos.Y + 3);
            }
            else
            {
                player[0].Spawn();
            }

            args.Player.SendMessage("Player sent to prison.", Color.Green);
            player[0].SendMessage(string.Format("You have been sent to prison for {0} minute(s)", minutes), Color.Red);
            TShock.Utils.Broadcast(string.Format("{0} has been sent to prison!", player[0].Name), Color.Goldenrod);
        }

        public static void ReleaseFromPrison(CommandArgs args)
        {
            if (args.Parameters.Count < 1 || args.Parameters.Count > 1)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /releaseprisoner <user>", Color.Red);
                return;
            }

            PrisonManager manager = new PrisonManager(TShock.DB);

            PrisonHelper prisoner = manager.GetPrisonerUser(args.Parameters[0]);

            if (prisoner != null)
            {
                Release(manager, prisoner);

                if (args.Player != null)
                {
                    args.Player.SendMessage("Prisoner has been released.", Color.Green);
                }
            }
            else
            {
                if (args.Player != null)
                {
                    args.Player.SendMessage("No prisoner matched your query", Color.Red);
                }
            }
        }

        public static void ClearPrison(CommandArgs args)
        {
            PrisonManager manager = new PrisonManager(TShock.DB);

            foreach (PrisonHelper prioner in manager.GetAllCurrentPrisoners())
            {
                Release(manager, prioner);
            }
        }

        private static void UpdateGroup(TSPlayer player, string user, string group)
        {
            if (player != null)
            {
                player.Group = TShock.Utils.GetGroup(group);
            }

            var usr = TShock.Users.GetUserByName(user);

            TShock.Users.SetUserGroup(usr, group);
        }

        private static void Release(PrisonManager manager, PrisonHelper prioner)
        {
            manager.Release(prioner.PrisonID);

            var player = TShock.Players.FirstOrDefault(p => p != null && p.UserAccountName == prioner.User && p.RealPlayer);

            UpdateGroup(player, prioner.User, prioner.Group);

            if (player != null)
            {
                var ePlayer = ExtendedAdmin.Players[player.Index];

                ePlayer.PrisonRecord = null;

                player.Teleport(Main.spawnTileX, Main.spawnTileY);
                player.SendMessage("You have been freed from prison", Color.Green);
                TShock.Utils.Broadcast(string.Format("{0} has been released from prison!", player.Name), Color.Goldenrod);
            }
        }

        public static void ExtendSentence(CommandArgs args)
        {
            if (args.Parameters.Count < 2 || args.Parameters.Count > 2)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /extendsentence <player> <minutes>", Color.Red);
                return;
            }

            var player = TShock.Utils.FindPlayer(args.Parameters[0]);

            if (player.Count == 0)
            {
                args.Player.SendMessage("No player found with that query.", Color.Red);
                return;
            }

            if (player.Count > 1)
            {
                args.Player.SendMessage("More than one player matched query.", Color.Red);
                return;
            }

            if (args.Parameters[1].ToIntegerOrDefault(-1) < 1)
            {
                args.Player.SendMessage("Invalid number of minutes.", Color.Red);
                return;
            }

            PrisonManager manager = new PrisonManager(TShock.DB);

            if (manager.IPInPrison(player[0].IP))
            {
                manager.ExtendSentence(player[0], args.Parameters[1].ToIntegerOrDefault(-1));

                var ePlayer = ExtendedAdmin.Players[player[0].Index];

                ePlayer.PrisonRecord.Until = ePlayer.PrisonRecord.Until.AddMinutes(args.Parameters[1].ToIntegerOrDefault(-1));

                ePlayer.Player.SendMessage(string.Format("Your sentence has been extended by {0} minute(s)", args.Parameters[1].ToIntegerOrDefault(-1)), Color.Red);

                TShock.Utils.Broadcast(string.Format("{0}'s sentence has been extended!", ePlayer.Player.Name), Color.Goldenrod);
            }
            else
            {
                args.Player.SendMessage("Player is not currently in prison.", Color.Red);
                return;
            }
        }
        #endregion

        #region Users
        public static void GetUserName(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /username <player>", Color.Red);
                return;
            }

            var players = TShock.Utils.FindPlayer(args.Parameters[0]);

            if (players.Count > 1)
            {
                args.Player.SendMessage("More than one player matched your query.", Color.Red);
                return;
            }

            try
            {
                args.Player.SendMessage("Logged in as: " + players[0].UserAccountName, Color.Green);
            }
            catch
            {
                args.Player.SendMessage("Player not found.", Color.Red);
            }
        }

        public static void HandleInvincible(CommandArgs args)
        {
            var player = ExtendedAdmin.Players[args.Player.Index];
            string message = "You are now invincible.";

            if (player.IsInvincible)
            {
                message = "You are no longer invincible.";
            }

            player.IsInvincible = !player.IsInvincible;

            player.Player.SendMessage(message, Color.Green);
        }
        #endregion

        #region Spawning
        public static void SpawnMobAtPlayerHandler(CommandArgs args)
        {
            if (args.Parameters.Count < 2 || args.Parameters.Count > 3)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /spawnmobat <player> <mob name/id> [amount]", Color.Red);
                return;
            }

            var player = TShock.Utils.FindPlayer(args.Parameters[0]);
            if (player.Count > 1)
            {
                args.Player.SendMessage("More than one player matched query.");
                return;
            }

            int amount = 1;
            if (args.Parameters.Count == 3 && !int.TryParse(args.Parameters[2], out amount))
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /spawnmob <player> <mob name/id> [amount]", Color.Red);
                return;
            }

            amount = Math.Min(amount, Main.maxNPCs);

            var npcs = TShock.Utils.GetNPCByIdOrName(args.Parameters[1]);
            if (npcs.Count == 0)
            {
                args.Player.SendMessage("Invalid mob type!", Color.Red);
            }
            else if (npcs.Count > 1)
            {
                args.Player.SendMessage(string.Format("More than one ({0}) mob matched!", npcs.Count), Color.Red);
            }
            else
            {
                var npc = npcs[0];
                if (npc.type >= 1 && npc.type < Main.maxNPCTypes && npc.type != 113) //Do not allow WoF to spawn, in certain conditions may cause loops in client
                {
                    try
                    {
                        TSPlayer.Server.SpawnNPC(npc.type, npc.name, amount, player[0].TileX, player[0].TileY, 50, 20);
                        TShock.Utils.Broadcast(string.Format("{0} was spawned {1} time(s).", npc.name, amount));
                    }
                    catch
                    {
                        args.Player.SendMessage("Invalid player.", Color.Red);
                    }
                }
                else if (npc.type == 113)
                    args.Player.SendMessage("Sorry, you can't spawn Wall of Flesh! Try /wof instead."); // Maybe perhaps do something with WorldGen.SpawnWoF?
                else
                    args.Player.SendMessage("Invalid mob type!", Color.Red);
            }
        }
        #endregion

        #region Region Helper
        public static void HandleLockDoor(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                args.Player.SendMessage("Incorrect syntax! Correct syntax: /lockdoors <region>", Color.Red);
                return;
            }

            Region region = null;

            if (args.Parameters.Count > 0)
            {
                region = TShock.Regions.GetRegionByName(args.Parameters[0]);
            }
            else
            {
                if (!args.Player.RealPlayer)
                {
                    args.Player.SendMessage("You must be logged in to use this without a parameter.", Color.Red);
                    return;
                }

                var regions = TShock.Regions.InAreaRegionName(args.Player.TileX, args.Player.TileY);

                if (regions.Count > 1)
                {
                    args.Player.SendMessage("You cannot lock doors that have overlapping regions.", Color.Red);
                }
                else if (regions.Count == 0)
                {
                    region = null;
                }
                else
                {
                    region = TShock.Regions.GetRegionByName(regions[0]);
                }
            }

            if (region == null)
            {
                args.Player.SendMessage("No region found with that query.", Color.Red);
                return;
            }

            if (!args.Player.Group.HasPermission(Permissions.editspawn) && !TShock.Regions.CanBuild(args.Player.TileX, args.Player.TileY, args.Player) && TShock.Regions.InArea(args.Player.TileX, args.Player.TileY))
            {
                args.Player.SendMessage(string.Format("You do not have permission to build in region {0}", args.Parameters[0]), Color.Red);
            }
            else
            {
                RegionHelperManager regionHelper = new RegionHelperManager(TShock.DB);

                regionHelper.LockRegion(region.Name);

                args.Player.SendMessage(string.Format("Region {0} doors are now locked.", region.Name), Color.Green);
            }

        }

        public static void HandleUnlockDoor(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                args.Player.SendMessage("Incorrect syntax! Correct syntax: /unlockdoors <region>", Color.Red);
                return;
            }

            Region region = null;

            if (args.Parameters.Count > 0)
            {
                region = TShock.Regions.GetRegionByName(args.Parameters[0]);
            }
            else
            {
                if (!args.Player.RealPlayer)
                {
                    args.Player.SendMessage("You must be logged in to use this without a parameter.", Color.Red);
                    return;
                }

                var regions = TShock.Regions.InAreaRegionName(args.Player.TileX, args.Player.TileY);

                if (regions.Count > 1)
                {
                    args.Player.SendMessage("You cannot unlock doors in overlapping regions.", Color.Red);
                }
                else if (regions.Count == 0)
                {
                    region = null;
                }
                else
                {
                    region = TShock.Regions.GetRegionByName(regions[0]);
                }
            }

            if (region == null)
            {
                args.Player.SendMessage("No region found with that query.", Color.Red);
                return;
            }

            if (!args.Player.Group.HasPermission(Permissions.editspawn) && !TShock.Regions.CanBuild(args.Player.TileX, args.Player.TileY, args.Player) && TShock.Regions.InArea(args.Player.TileX, args.Player.TileY))
            {
                args.Player.SendMessage(string.Format("You do not have permission to build in region {0}", args.Parameters[0]), Color.Red);
            }
            else
            {
                RegionHelperManager regionHelper = new RegionHelperManager(TShock.DB);

                regionHelper.UnlockRegion(region.Name);

                args.Player.SendMessage(string.Format("Region {0} doors are now unlocked.", region.Name), Color.Green);
            }
        }

        public static void HandleCurrentRegion(CommandArgs args)
        {
            if (!args.Player.RealPlayer)
            {
                args.Player.SendMessage("You must be logged in to use this command.", Color.Red);
                return;
            }

            var region = TShock.Regions.InAreaRegionName(args.Player.TileX, args.Player.TileY);

            if (region == null || region.Count == 0)
            {
                args.Player.SendMessage("Not currently in a region.", Color.Yellow);
            }
            else
            {
                args.Player.SendMessage(string.Format("Current region is {0}", string.Join(",", region)), Color.Yellow);
            }
        }
        #endregion

        #region Raffle
        public static void StartRaffle(CommandArgs args)
        {
            RaffleHandler.BeginRaffle();
        }

        public static void RaffleInfo(CommandArgs args)
        {
            if (!args.Player.RealPlayer || args.Player.UserAccountName.IsNullOrEmptyTrim())
            {
                args.Player.SendMessage("You must be logged in to use this command.");
                return;
            }

            RaffleManager manager = new RaffleManager(TShock.DB);

            var raffle = manager.GetCurrentRaffle();
            var tickets = manager.GetRaffleTickets(args.Player.UserAccountName, raffle.RaffleID);

            TimeSpan nextRaffle = RaffleHandler.NextRaffleTime - DateTime.Now;

            args.Player.SendMessage(string.Format("Ticket cost: {0} Current tickets: {1} Next raffle: {2} minute(s) {4} second(s) Current pot: {3}", ExtendedAdmin.Config.RaffleTicketCost, tickets.TicketCount, (int)nextRaffle.TotalMinutes, raffle.Pot, nextRaffle.Seconds), Color.Green);
        }

        public static void BuyRaffleTicket(CommandArgs args)
        {
            if (!args.Player.RealPlayer || args.Player.UserAccountName.IsNullOrEmptyTrim())
            {
                args.Player.SendMessage("You must be logged in to use this command.", Color.Red);
                return;
            }

            if (args.Parameters.Count > 0 && args.Parameters[0].ToIntegerOrDefault(-1) < 1)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax /buyraffleticket <amount>", Color.Red);
                return;
            }

            RaffleManager manager = new RaffleManager(TShock.DB);

            //var account = manager.GetServerPointAccounts(args.Player.UserAccountName);
            var ePlayer = ServerPointSystem.ServerPointSystem.EPRPlayers.Single(p => p.TSPlayer == args.Player);

            if (ePlayer == null)
            {
                args.Player.SendMessage("You do not have any shards.");
                return;
            }

            int amount;

            if (args.Parameters.Count > 0)
            {
                amount = args.Parameters[0].ToIntegerOrDefault(0);
            }
            else
            {
                amount = 1;
            }

            int totalCost = amount * ExtendedAdmin.Config.RaffleTicketCost;

            if (ePlayer.DisplayAccount < totalCost)
            {
                args.Player.SendMessage(string.Format("You do not have enough shards to buy {0} tickets.", amount), Color.Red);
                return;
            }

            var raffle = manager.GetCurrentRaffle();
            var tickets = manager.GetRaffleTickets(args.Player.UserAccountName, raffle.RaffleID);

            if (tickets.TicketCount + amount > ExtendedAdmin.Config.MaxRaffleTickets)
            {
                args.Player.SendMessage(string.Format("You cannot have over {0} tickets.  You currently have {1}.", ExtendedAdmin.Config.MaxRaffleTickets, tickets.TicketCount), Color.Red);
                return;
            }

            if (manager.BuyTicket(args.Player, amount, totalCost))
            {
                tickets = manager.GetRaffleTickets(args.Player.UserAccountName, raffle.RaffleID);

                args.Player.SendMessage(string.Format("Successfully bought tickets. You now have {0} tickets.", tickets.TicketCount), Color.Green);
            }
            else
            {
                args.Player.SendMessage("Ticket purchase failed, please try again later.", Color.Red);
            }
        }
        #endregion

        #region Ghost
        public static void Ghost(CommandArgs args)
        {
            var player = ExtendedAdmin.Players[args.Player.Index];

            if (!player.IsGhost)
            {
                player.Player.TPlayer.team = 0;

                NetMessage.SendData((int)PacketTypes.PlayerTeam, -1, -1, "", player.Player.Index);

                // set ghost
                player.IsGhost = true;

                player.Player.SendMessage("You are now a ghost.", Color.Green);
            }
            else
            {
                player.IsGhost = false;

                player.Player.SendMessage("You are no longer a ghost.", Color.Green);
            }
        }
        #endregion

        #region TpTo
        public static void TpTo(CommandArgs args)
        {
            if (args.Parameters.Count < 2 || args.Parameters.Count > 2)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /tpto <target> <player>", Color.Red);
                return;
            }

            var target = TShock.Utils.FindPlayer(args.Parameters[0]);

            if (target == null || target.Count == 0)
            {
                args.Player.SendMessage("Invalid target, no player matched query.", Color.Red);
                return;
            }

            var player = TShock.Utils.FindPlayer(args.Parameters[1]);

            if (player == null || target.Count == 0)
            {
                args.Player.SendMessage("Invalid player, no player matched query.", Color.Red);
                return;
            }

            if (target.Count > 1)
            {
                args.Player.SendMessage("More than one target matched your query.", Color.Red);
                return;
            }

            if (player.Count > 1)
            {
                args.Player.SendMessage("More than one player matched your query.", Color.Red);
                return;
            }

            player[0].Teleport(target[0].TileX, target[0].TileY);

            target[0].SendMessage(string.Format("{0} has been teleported to you.", player[0].Name), Color.Green);
            player[0].SendMessage(string.Format("You have been teleported to {0}.", target[0].Name), Color.Green);

            args.Player.SendMessage(string.Format("{0} was successfully teleported to {1}.", player[0].Name, target[0].Name), Color.Green);
        }
        #endregion

        #region BuffAll
        public static void BuffAll(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /buffall <player>", Color.Red);
                return;
            }

            TSPlayer player;

            if (args.Parameters.Count == 1)
            {
                var players = TShock.Utils.FindPlayer(args.Parameters[0]);

                if (players.Count > 1)
                {
                    args.Player.SendMessage("More than one player matched your query.", Color.Red);
                    return;
                }

                if (players == null || players.Count == 0)
                {
                    args.Player.SendMessage("No players matched your query.", Color.Red);
                    return;
                }

                player = players[0];
            }
            else
            {
                player = args.Player;
            }

            if (!player.RealPlayer)
            {
                args.Player.SendMessage("You must be logged in to buff yourself.", Color.Red);
                return;
            }

            int[] buffs = new int[] { 1, 2, 3, 5, 6, 7, 12, 14, 16, 26, 29 };

            buffs.ForEach(b => player.SetBuff(b, 500 * 60));
        }
        #endregion

        #region PVP Safe Commands
        public static void Heal(CommandArgs args)
        {
            if (args.Player.TPlayer.hostile)
            {
                args.Player.SendMessage("You do not have access to this command while PVPing", Color.Red);
                return;
            }

            Item heart = TShock.Utils.GetItemById(58);
            Item star = TShock.Utils.GetItemById(184);
            for (int i = 0; i < 20; i++)
                args.Player.GiveItem(heart.type, heart.name, heart.width, heart.height, heart.maxStack);
            for (int i = 0; i < 10; i++)
                args.Player.GiveItem(star.type, star.name, star.width, star.height, star.maxStack);

            args.Player.SendMessage("You just got healed!", Color.Green);
        }

        public static void Buff(CommandArgs args)
        {
            if (args.Player.TPlayer.hostile)
            {
                args.Player.SendMessage("You do not have access to this command while PVPing", Color.Red);
                return;
            }

            int id = 0;
            int time = 60;
            if (!int.TryParse(args.Parameters[0], out id))
            {
                var found = TShock.Utils.GetBuffByName(args.Parameters[0]);
                if (found.Count == 0)
                {
                    args.Player.SendMessage("Invalid buff name!", Color.Red);
                    return;
                }
                else if (found.Count > 1)
                {
                    args.Player.SendMessage(string.Format("More than one ({0}) buff matched!", found.Count), Color.Red);
                    return;
                }
                id = found[0];
            }

            if (args.Parameters.Count == 2)
            {
                int.TryParse(args.Parameters[1], out time);
            }

            if (id > 0 && id < Main.maxBuffs)
            {
                if (time < 0 || time > short.MaxValue)
                {
                    time = 60;
                }

                args.Player.SetBuff(id, time * 60);
                args.Player.SendMessage(string.Format("You have buffed yourself with {0}({1}) for {2} seconds!",
                                                      TShock.Utils.GetBuffName(id), TShock.Utils.GetBuffDescription(id), (time)),
                                        Color.Green);
            }
            else
            {
                args.Player.SendMessage("Invalid buff ID!", Color.Red);
            }

        }
        #endregion

        #region Bank
        public static void Bank(CommandArgs args)
        {
            if (!args.Player.RealPlayer)
            {
                args.Player.SendMessage("You must be logged in to use this command.", Color.Red);
                return;
            }

            if (!args.Player.IsLoggedIn)
            {
                args.Player.SendMessage("You must be logged in to use this command.", Color.Red);
                return;
            }

            if (args.Parameters.Count < 1)
            {
                BankHelp(args.Player);
                return;
            }

            switch (args.Parameters[0])
            {
                case "deposit":
                    if (ValidateAmount(args.Player, args.Parameters))
                    {
                        Deposit(args.Player, args.Parameters[1].ToIntegerOrDefault(-1));
                    }
                    break;
                case "withdraw":
                    if (ValidateAmount(args.Player, args.Parameters))
                    {
                        Withdraw(args.Player, args.Parameters[1].ToIntegerOrDefault(-1));
                    }
                    break;
                case "balance":
                    Balance(args.Player);
                    break;
                default:
                    BankHelp(args.Player);
                    break;
            }
        }

        private static bool ValidateAmount(TSPlayer player, List<string> list)
        {
            bool valid = true;

            if (list.Count < 2)
            {
                BankHelp(player);
                valid = false;
            }

            if (list[1].ToIntegerOrDefault(-1) < 1)
            {
                BankHelp(player);
                valid = false;
            }

            return valid;
        }

        private static void BankHelp(TSPlayer player)
        {
            player.SendMessage("/bank deposit <amount>", Color.Yellow);
            player.SendMessage("/bank withdraw <amount>", Color.Yellow);
            player.SendMessage("/bank balance", Color.Yellow);
        }

        private static void Deposit(TSPlayer player, int amount)
        {
            BankHandler handler = new BankHandler();

            handler.Deposit(player, amount);
        }

        private static void Withdraw(TSPlayer player, int amount)
        {
            BankHandler handler = new BankHandler();

            handler.Withdraw(player, amount);
        }

        private static void Balance(TSPlayer player)
        {
            BankHandler handler = new BankHandler();

            handler.GetBalance(player);
        }
        #endregion
    }
}
