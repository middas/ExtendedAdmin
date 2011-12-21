using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using Hooks;
using System.ComponentModel;
using System.Reflection;
using System.IO;
using System.IO.Streams;

namespace ExtendedAdmin
{
    [APIVersion(1, 10)]
    public class ExtendedAdmin : TerrariaPlugin
    {
        public static ExtendedTSPlayer[] Players = new ExtendedTSPlayer[Main.maxPlayers];
        public static ExtendedAdminConfig Config;

        public ExtendedAdmin(Terraria.Main game) :
            base(game)
        {
            Config = new ExtendedAdminConfig();
        }

        public override string Author
        {
            get
            {
                return "Middas";
            }
        }

        public override string Description
        {
            get
            {
                return "Extended admin controls";
            }
        }

        public override string Name
        {
            get
            {
                return "Extended Admin";
            }
        }

        public override Version Version
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }

        public override void Initialize()
        {
            ExtendedFileTools.InitConfig();

            ServerHooks.Join += new Action<int, System.ComponentModel.HandledEventArgs>(ServerHooks_Join);
            NetHooks.GetData += new NetHooks.GetDataD(NetHooks_GetData);

            Commands.ChatCommands.Add(new Command(Permissions.manageregion, CommandHandlers.GetUserName, "username", "un"));
            Commands.ChatCommands.Add(new Command(ExtendedPermissions.caninvincible, CommandHandlers.HandleInvincible, "invincible"));
            Commands.ChatCommands.Add(new Command(Permissions.spawnmob, CommandHandlers.SpawnMobAtPlayerHandler, "spawnmobat", "sma"));
        }

        private void NetHooks_GetData(GetDataEventArgs e)
        {
            var player = Players[e.Msg.whoAmI];

            PacketTypes type = e.MsgID;

            if (type == PacketTypes.PlayerDamage)
            {
                if (player == null)
                {
                    e.Handled = true;
                    return;
                }

                if (player.IsInvincible)
                {
                    if (player.Player.TPlayer.statLife < player.Player.TPlayer.statLifeMax)
                    {
                        int deficit = player.Player.TPlayer.statLifeMax - player.Player.TPlayer.statLife;

                        int heartNum = (deficit / 20) + 4;

                        var heart = TShock.Utils.GetItemById(58);

                        for (int i = 0; i < heartNum; i++)
                        {
                            player.Player.GiveItem(heart.type, heart.name, heart.width, heart.height, 1);
                        }
                    }
                }
            }
            else if (type == PacketTypes.ChestGetContents)
            {
                if (player == null)
                {
                    e.Handled = false;
                    return;
                }

                using (MemoryStream ms = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                {
                    int x = ms.ReadInt32();
                    int y = ms.ReadInt32();
                    if (!player.Player.Group.HasPermission(Permissions.editspawn) && !TShock.Regions.CanBuild(x, y, player.Player) && TShock.Regions.InArea(x, y))
                    {
                        player.Player.SendMessage(string.Format("Chests in region name: {0} are protected.", TShock.Regions.InAreaRegionName(x, y)), Color.Red);
                        e.Handled = true;
                    }
                }
            }
        }

        private void ServerHooks_Join(int ply, HandledEventArgs args)
        {
            var tPlayer = TShock.Players[ply];
            var player = new ExtendedTSPlayer(tPlayer);

            Players[ply] = player;
        }
    }
}
