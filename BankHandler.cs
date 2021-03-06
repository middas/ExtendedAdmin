using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtendedAdmin.DB;
using TShockAPI;
using CommonLibrary.Native;

namespace ExtendedAdmin
{
    public class BankHandler
    {
        public void Deposit(TSPlayer player, int amount)
        {
            if (!CheckInRegion(player))
            {
                return;
            }

            BankManager manager = new BankManager(TShock.DB);

            var account = manager.GetBalance(player.UserAccountName);

            //// piggy back on the raffle manager to get shards
            //RaffleManager raffleManager = new RaffleManager(TShock.DB);

            //var shards = raffleManager.GetServerPointAccounts(player.UserAccountName);
            try
            {
                var ePlayer = ServerPointSystem.ServerPointSystem.EPRPlayers.Single(p => p.TSPlayer == player);

                if (ePlayer.DisplayAccount < amount)
                {
                    player.SendMessage("You do not have the required shards.", Color.Red);
                }
                else
                {
                    manager.Deposit(player.UserAccountName, amount);

                    ServerPointSystem.EPREvents.PointOperate(ePlayer, -amount, ServerPointSystem.PointOperateReason.Deduct);

                    //ServerPointSystem.ServerPointSystem.Deduct(new CommandArgs("deduct", player, new List<string>() 
                    //{
                    //    player.UserAccountName,
                    //    amount.ToString()
                    //}));

                    player.SendMessage("You have successfully deposited into your account.", Color.Green);
                }
            }
            catch (Exception ex)
            {
                player.SendMessage("Could not deposit at this time.", Color.Red);
            }
        }

        private bool CheckInRegion(TSPlayer player)
        {
            bool inRegion = true;

            var regions = TShock.Regions.InAreaRegionName(player.TileX, player.TileY);

            if (!regions.ContainsProperty(r => r == ExtendedAdmin.Config.BankRegion))
            {
                player.SendMessage("You are not in the bank region.", Color.Red);
                inRegion = false;
            }

            return inRegion;
        }

        public void Withdraw(TSPlayer player, int amount)
        {
            if (!CheckInRegion(player))
            {
                return;
            }

            BankManager manager = new BankManager(TShock.DB);

            var account = manager.GetBalance(player.UserAccountName);

            if (account.Amount - amount < 0)
            {
                player.SendMessage("You do not have enough to withdraw that amount.", Color.Red);
            }
            else
            {
                try
                {
                    var ePlayer = ServerPointSystem.ServerPointSystem.EPRPlayers.Single(p => p.TSPlayer == player);

                    manager.Withdraw(player.UserAccountName, amount);

                    ServerPointSystem.EPREvents.PointOperate(ePlayer, amount, ServerPointSystem.PointOperateReason.Award);

                    //ServerPointSystem.ServerPointSystem.Award(new CommandArgs("award", player, new List<string>()
                    //{
                    //    player.Name,
                    //    amount.ToString()
                    //}));

                    player.SendMessage("You have successfully withdrawn from your account.", Color.Green);
                }
                catch (Exception ex)
                {
                    player.SendMessage("Could not withdraw shards at this time.", Color.Red);
                }
            }
        }

        public void GetBalance(TSPlayer player)
        {
            BankManager manager = new BankManager(TShock.DB);

            var account = manager.GetBalance(player.UserAccountName);

            player.SendMessage(string.Format("Your account has a balance of: {0}", account.Amount), Color.Green);
        }
    }
}
