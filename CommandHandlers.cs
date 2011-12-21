using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;

namespace ExtendedAdmin
{
    public static class CommandHandlers
    {
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
    }
}
