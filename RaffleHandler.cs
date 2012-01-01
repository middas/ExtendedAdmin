using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ExtendedAdmin.DB;
using TShockAPI;
using CommonLibrary.Native;

namespace ExtendedAdmin
{
    public class RaffleHandler
    {
        public static DateTime NextRaffleTime;
        public static DateTime NextRaffleUpdate;

        public RaffleHandler()
        {
            StartRaffle();

            NextRaffleUpdate = DateTime.Now;
        }

        private void StartRaffle()
        {
            RaffleManager manager = new RaffleManager(TShock.DB);

            var raffle = manager.GetCurrentRaffle();

            if (raffle == null)
            {
                manager.CreateRaffle(ExtendedAdmin.Config.RaffleStartPot);

                NextRaffleTime = DateTime.Now.AddMinutes(ExtendedAdmin.Config.RaffleDuration);
            }
            else
            {
                NextRaffleTime = raffle.LastRaffle == DateTime.MinValue ? DateTime.Now.AddMinutes(ExtendedAdmin.Config.RaffleDuration) : raffle.LastRaffle.AddMinutes(ExtendedAdmin.Config.RaffleDuration);
            }
        }

        public void BeginRaffle()
        {
            NextRaffleTime = DateTime.Now.AddMinutes(ExtendedAdmin.Config.RaffleDuration);

            ThreadPool.QueueUserWorkItem(ExecuteRaffle);
        }

        private void ExecuteRaffle(object o)
        {
            TShock.Utils.Broadcast("Raffle beginning!", Color.GreenYellow);

            List<string> raffleValues = new List<string>(ExtendedAdmin.Config.RaffleOdds);

            RaffleManager manager = new RaffleManager(TShock.DB);

            var raffleTickets = manager.GetAllCurrentRaffleTickets();

            foreach (var ticket in raffleTickets)
            {
                for(int i = 0; i < ticket.TicketCount; i++)
                {
                    raffleValues.Add(ticket.User);
                }
            }

            while (raffleValues.Count < ExtendedAdmin.Config.RaffleOdds)
            {
                raffleValues.Add(null);
            }

            raffleValues = raffleValues.Shuffle().ToList();

            Random random = new Random();

            var winner = raffleValues[random.Next(0, raffleValues.Count - 1)];

            var raffle = manager.GetCurrentRaffle();

            if (!winner.IsNullOrEmptyTrim())
            {
                var winnerTicket = raffleTickets.FirstOrDefault(t => t.User == winner);

                TShock.Utils.Broadcast(string.Format("Congratulations {0}, you are the winner of {1} shards!", winnerTicket.Name, raffle.Pot), Color.GreenYellow);
                TShock.Utils.Broadcast("A new raffle begins now!", Color.GreenYellow);

                var player = TShock.Players.Where(p => p!= null && p.UserAccountName == winner).SingleOrDefault();

                manager.Reward(winnerTicket, player, raffle.Pot);

                StartRaffle();
            }
            else
            {
                manager.UpdateRaffleTime();

                manager.DecreaseTickets(ExtendedAdmin.Config.RaffleTicketsKept);

                TShock.Utils.Broadcast(string.Format("There was no winner, {0} shards are now up for grabs!", raffle.Pot), Color.GreenYellow);
                TShock.Utils.Broadcast(string.Format("{0}% of tickets were claimed by the raffle.  Remember to buy more!", (100f - ExtendedAdmin.Config.RaffleTicketsKept).ToString("F2")), Color.GreenYellow);

                Update();
            }
        }

        public void Update()
        {
            RaffleHandler.NextRaffleUpdate = DateTime.Now.AddMinutes(ExtendedAdmin.Config.RaffleUpdateDuration);

            RaffleManager manager = new RaffleManager(TShock.DB);

            var raffle = manager.GetCurrentRaffle();

            TimeSpan nextRaffle = RaffleHandler.NextRaffleTime - DateTime.Now;

            TShock.Utils.Broadcast(string.Format("Current raffle pot: {0}! Next raffle in {1} minute(s) {2} second(s)!", raffle.Pot, (int)nextRaffle.TotalMinutes, (int)nextRaffle.Seconds));
            
            var lastRaffle = manager.GetLastRaffle();

            if (lastRaffle != null)
            {
                TShock.Utils.Broadcast(string.Format("{0} won {1} in the last raffle!", lastRaffle.Winner, lastRaffle.Pot), Color.GreenYellow);
            }
        }
    }
}
