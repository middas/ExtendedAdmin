using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;

namespace ExtendedAdmin
{
    public class ExtendedTSPlayer : TSPlayer
    {
        public bool IsInvincible { get; set; }

        public ExtendedTSPlayer(int ply) :
            base(ply)
        {
            
        }
    }
}
