using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;

namespace ExtendedAdmin
{
    public class ExtendedTSPlayer
    {
        public TSPlayer Player
        {
            get;
            private set;
        }

        public bool IsInvincible { get; set; }

        public ExtendedTSPlayer(TSPlayer ply)
        {
            Player = ply;
        }
    }
}
