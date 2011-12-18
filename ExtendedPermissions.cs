using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtendedAdmin
{
    public static class ExtendedPermissions
    {
        /// <summary>
        /// Whether or not invincibility can be used
        /// </summary>
        public static readonly string caninvincible;

        static ExtendedPermissions()
        {
            foreach (var field in typeof(ExtendedPermissions).GetFields())
            {
                field.SetValue(null, field.Name);
            }
        }
    }
}
