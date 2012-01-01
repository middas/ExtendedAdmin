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
using ExtendedAdmin.DB;
using CommonLibrary.Native;

namespace ExtendedAdmin
{
    [APIVersion(1, 10)]
    public class ExtendedAdmin : TerrariaPlugin
    {
        public static ExtendedTSPlayer[] Players = new ExtendedTSPlayer[Main.maxPlayers];
        public static ExtendedAdminConfig Config;
        public static RaffleHandler Raffle;

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

            Raffle = new RaffleHandler();

            ServerHooks.Join += new Action<int, System.ComponentModel.HandledEventArgs>(ServerHooks_Join);
            NetHooks.GetData += new NetHooks.GetDataD(NetHooks_GetData);
            NetHooks.SendData += new NetHooks.SendDataD(NetHooks_SendData);
            GameHooks.Update += new Action(GameHooks_Update);
            ServerHooks.Chat += new Action<messageBuffer, int, string, HandledEventArgs>(ServerHooks_Chat);
            ServerHooks.Command += new ServerHooks.CommandD(ServerHooks_Command);

            Commands.ChatCommands.Add(new Command(Permissions.manageregion, CommandHandlers.GetUserName, "username", "un"));
            Commands.ChatCommands.Add(new Command(ExtendedPermissions.caninvincible, CommandHandlers.HandleInvincible, "invincible"));
            Commands.ChatCommands.Add(new Command(Permissions.spawnmob, CommandHandlers.SpawnMobAtPlayerHandler, "spawnmobat", "sma"));
            Commands.ChatCommands.Add(new Command(CommandHandlers.HandleLockDoor, "lockdoors", "ld"));
            Commands.ChatCommands.Add(new Command(CommandHandlers.HandleUnlockDoor, "unlockdoors", "ud"));
            Commands.ChatCommands.Add(new Command(CommandHandlers.HandleCurrentRegion, "currentregion"));
            Commands.ChatCommands.Add(new Command(CommandHandlers.BuyRaffleTicket, "buyraffleticket"));
            Commands.ChatCommands.Add(new Command(CommandHandlers.RaffleInfo, "raffleinfo"));
            Commands.ChatCommands.Add(new Command(ExtendedPermissions.rafflemanager, CommandHandlers.StartRaffle, "startraffle"));
            Commands.ChatCommands.Add(new Command(ExtendedPermissions.prisonmanager, CommandHandlers.SendToPrison, "sendtoprison"));
            Commands.ChatCommands.Add(new Command(ExtendedPermissions.prisonmanager, CommandHandlers.ReleaseFromPrison, "releaseprisoner"));
            Commands.ChatCommands.Add(new Command(ExtendedPermissions.prisonmanager, CommandHandlers.ClearPrison, "clearprison"));
            Commands.ChatCommands.Add(new Command(ExtendedPermissions.prisonmanager, CommandHandlers.ExtendSentence, "extendsentence"));
            Commands.ChatCommands.Add(new Command(ExtendedPermissions.canghost, CommandHandlers.Ghost, "ghost"));
        }

        private void NetHooks_SendData(SendDataEventArgs e)
        {
            try
            {
                var player = Players.FirstOrDefault(p => p != null && p.Player.Index == e.number);

                switch (e.MsgID)
                {
                    case PacketTypes.DoorUse:
                    case PacketTypes.EffectHeal:
                    case PacketTypes.EffectMana:
                    case PacketTypes.PlayerDamage:
                    case PacketTypes.Zones:
                    case PacketTypes.PlayerAnimation:
                    case PacketTypes.PlayerTeam:
                    case PacketTypes.PlayerSpawn:
                        if (player != null && player.IsGhost)
                        {
                            e.Handled = true;
                        }
                        break;
                    case PacketTypes.ProjectileNew:
                    case PacketTypes.ProjectileDestroy:
                        var ignorePlayer = Players.FirstOrDefault(p => p != null && p.Player.Index == e.ignoreClient);
                        if (ignorePlayer != null && ignorePlayer.IsGhost)
                        {
                            e.Handled = true;
                        }
                        break;
                }

                if (e.number >= 0 && e.number <= 255 && player != null && player.IsGhost)
                {
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                ExtendedLog.Current.Log(ex.ToString());
                Console.WriteLine("An exception occurred, see ExtendedAdmin.log for details.");
            }
        }

        private void ServerHooks_Command(string cmd, HandledEventArgs e)
        {
            if (cmd.EqualsIgnoreCase("/reload"))
            {
                ExtendedFileTools.InitConfig();

                Console.WriteLine("ExtendedAdmin config reloaded.");
            }
        }

        private void ServerHooks_Chat(messageBuffer msg, int ply, string text, HandledEventArgs args)
        {
            if (text.EqualsIgnoreCase("/reload"))
            {
                var player = TShock.Players[ply];

                if (player.Group.HasPermission(Permissions.cfg))
                {
                    ExtendedFileTools.InitConfig();

                    player.SendMessage("ExtendedAdmin config reloaded.", Color.Green);
                }
            }
        }

        private void GameHooks_Update()
        {
            if (RaffleHandler.NextRaffleTime <= DateTime.Now)
            {
                Raffle.BeginRaffle();
            }

            if (RaffleHandler.NextRaffleUpdate <= DateTime.Now)
            {
                Raffle.Update();
            }
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
            else if (type == PacketTypes.DoorUse)
            {
                if (player == null)
                {
                    e.Handled = false;
                    return;
                }

                using (MemoryStream ms = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                {
                    bool closed = ms.ReadBoolean();
                    int x = ms.ReadInt32();
                    int y = ms.ReadInt32();

                    RegionHelperManager regionHelper = new RegionHelperManager(TShock.DB);

                    if (!player.Player.Group.HasPermission(Permissions.editspawn) && !TShock.Regions.CanBuild(x, y, player.Player) && TShock.Regions.InArea(x, y) && regionHelper.GetRegionHelperByRegion(TShock.Regions.InAreaRegionName(x, y)).IsLocked)
                    {
                        int size = 10;

                        NetMessage.SendData((int)PacketTypes.DoorUse, -1, -1, "", 1, x, y);

                        TSPlayer.All.SendTileSquare(x, y, size);
                        WorldGen.RangeFrame(x, y, x + size, y + size);

                        int warpX = player.Player.TileX > x ? player.Player.TileX + 3 : player.Player.TileX - 3;

                        player.Player.Teleport(warpX, player.Player.TileY + 3);

                        player.Player.SendMessage(string.Format("Doors in region name: {0} are locked.", TShock.Regions.InAreaRegionName(x, y)), Color.Red);

                        e.Handled = true;
                    }
                }
            }
            else if (type == PacketTypes.PlayerUpdate)
            {
                using (MemoryStream ms = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                {
                    if (player.PrisonRecord != null)
                    {
                        if (!player.PrisonRecord.Released && player.PrisonRecord.Until <= DateTime.Now)
                        {
                            CommandHandlers.ReleaseFromPrison(new CommandArgs("releaseprisoner", null, new List<string>()
                            {
                                player.Player.UserAccountName
                            }));

                            player.PrisonRecord = null;
                        }
                        else
                        {
                            var plr = ms.ReadInt8();
                            var control = ms.ReadInt8();
                            var item = ms.ReadInt8();
                            var pos = new Vector2(ms.ReadSingle(), ms.ReadSingle());
                            var vel = new Vector2(ms.ReadSingle(), ms.ReadSingle());

                            var warp = TShock.Warps.FindWarp(Config.PrisonWarp);

                            float tilex;
                            float tiley;

                            if (warp.WarpPos != Vector2.Zero)
                            {
                                tilex = (int)(warp.WarpPos.X);
                                tiley = (int)(warp.WarpPos.Y);
                            }
                            else
                            {
                                tilex = Main.spawnTileX;
                                tiley = Main.spawnTileY;
                            }

                            float distance = Vector2.Distance(new Vector2((pos.X / 16f), (pos.Y / 16f)), new Vector2(tilex, tiley));
                            if (distance > TShock.Config.MaxRangeForDisabled)
                            {
                                if (warp.WarpPos != Vector2.Zero)
                                {
                                    player.Player.Teleport((int)warp.WarpPos.X, (int)warp.WarpPos.Y + 3);
                                }
                                else
                                {
                                    player.Player.Spawn();
                                }

                                TimeSpan remaining = player.PrisonRecord.Until - DateTime.Now;

                                player.Player.SendMessage(string.Format("You are still serving your prison sentence. {0} hour(s) {1} minute(s) remain.", (int)remaining.TotalHours, remaining.Minutes), Color.Yellow);
                            }
                        }
                    }
                    else
                    {
                        if (player.Player.Group.Name == Config.PrisonGroup)
                        {
                            CommandHandlers.ReleaseFromPrison(new CommandArgs("releaseprisoner", null, new List<string>()
                            {
                                player.Player.UserAccountName
                            }));
                        }
                    }
                }
            }
        }

        private void ServerHooks_Join(int ply, HandledEventArgs args)
        {
            var tPlayer = TShock.Players[ply];
            var player = new ExtendedTSPlayer(tPlayer);

            Players[ply] = player;

            PrisonManager prisonManager = new PrisonManager(TShock.DB);

            if (prisonManager.IPInPrison(player.Player.IP))
            {
                player.PrisonRecord = prisonManager.GetPrisonRecordByIP(player.Player.IP);
            }
        }
    }
}
