using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using Hooks;
using System.ComponentModel;

namespace ExtendedAdmin
{
    [APIVersion(1, 10)]
    public class ExtendedAdmin : TerrariaPlugin
    {
        public static ExtendedTSPlayer[] Players = new ExtendedTSPlayer[Main.maxPlayers];

        public ExtendedAdmin(Terraria.Main game) :
            base(game)
        {
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
                return new Version(1, 0);
            }
        }

        public override void Initialize()
        {
            ServerHooks.Join += new Action<int, System.ComponentModel.HandledEventArgs>(ServerHooks_Join);
            NetHooks.GetData += new NetHooks.GetDataD(NetHooks_GetData);

            Commands.ChatCommands.Add(new Command(Permissions.manageregion, CommandHandlers.GetUserName, "username", "un"));
            Commands.ChatCommands.Add(new Command(ExtendedPermissions.caninvincible, CommandHandlers.HandleInvincible, "invincible"));
            Commands.ChatCommands.Add(new Command(Permissions.spawnmob, CommandHandlers.SpawnMobAtPlayerHandler, "spawnmobat", "sma"));
        }

        private void NetHooks_GetData(GetDataEventArgs e)
        {
            PacketTypes type = e.MsgID;

            if (type == PacketTypes.PlayerDamage)
            {
                var player = Players[e.Msg.whoAmI];

                if (player == null)
                {
                    e.Handled = true;
                    return;
                }

                if (player.IsInvincible)
                {
                    if (player.TPlayer.statLife < player.TPlayer.statLifeMax)
                    {
                        int deficit = player.TPlayer.statLifeMax - player.TPlayer.statLife;

                        int heartNum = (deficit / 20) + 1;

                        var heart = TShock.Utils.GetItemById(58);

                        for (int i = 0; i < heartNum; i++)
                        {
                            player.GiveItem(heart.type, heart.name, heart.width, heart.height, 1);
                        }
                    }
                }
            }
        }

        private void ServerHooks_Join(int ply, HandledEventArgs args)
        {
            var player = new ExtendedTSPlayer(ply);

            Players[ply] = player;
        }
    }
}
