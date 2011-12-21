using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TShockAPI;

namespace ExtendedAdmin
{
    public class ExtendedFileTools
    {
        private static string ConfigPath = Path.Combine(TShock.SavePath, "ExtendedConfig.json");

        public static void InitConfig()
        {
            if (!Directory.Exists(TShock.SavePath))
            {
                Directory.CreateDirectory(TShock.SavePath);
            }

            if (File.Exists(ConfigPath))
            {
                ExtendedAdmin.Config = ExtendedAdminConfig.Read(ConfigPath);
            }

            ExtendedAdmin.Config.Write(ConfigPath);
        }
    }
}
