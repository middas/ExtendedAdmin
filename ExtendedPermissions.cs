using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace ExtendedAdmin
{
    public static class ExtendedPermissions
    {
        [Description("Required for invincible command")]
        public static readonly string caninvincible;

        [Description("Required for the ghost command")]
        public static readonly string canghost;

        [Description("Raffle manager")]
        public static readonly string rafflemanager;

        static ExtendedPermissions()
        {
            foreach (var field in typeof(ExtendedPermissions).GetFields())
            {
                field.SetValue(null, field.Name);
            }
        }
    }
}
