using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using ExtendedAdmin.DB;
using CommonLibrary.Native;

namespace ExtendedAdmin
{
    public static class CommandHandlers
    {
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
            if (args.Parameters.Count == 0 || args.Parameters.Count > 1)
            {
                args.Player.SendMessage("Incorrect syntax! Correct syntax: /lockdoors <region>", Color.Red);
                return;
            }

            var region = TShock.Regions.GetRegionByName(args.Parameters[0]);

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

                regionHelper.LockRegion(args.Parameters[0]);

                args.Player.SendMessage(string.Format("Region {0} doors are now locked.", args.Parameters[0]), Color.Green);
            }

        }

        public static void HandleUnlockDoor(CommandArgs args)
        {
            if (args.Parameters.Count == 0 || args.Parameters.Count > 1)
            {
                args.Player.SendMessage("Incorrect syntax! Correct syntax: /unlockdoors <region>", Color.Red);
                return;
            }

            var region = TShock.Regions.GetRegionByName(args.Parameters[0]);

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

                regionHelper.UnlockRegion(args.Parameters[0]);

                args.Player.SendMessage(string.Format("Region {0} doors are now unlocked.", args.Parameters[0]), Color.Green);
            }
        }

        public static void HandleCurrentRegion(CommandArgs args)
        {
            var region = TShock.Regions.InAreaRegionName(args.Player.TileX, args.Player.TileY);

            if (region.IsNullOrEmptyTrim())
            {
                args.Player.SendMessage("Not currently in a region.", Color.Yellow);
            }
            else
            {
                args.Player.SendMessage(string.Format("Current region is {0}", region), Color.Yellow);
            }
        }
        #endregion
    }
}
