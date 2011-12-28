using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using CommonLibrary.Native;

namespace ExtendedAdmin
{
    public class ExtendedAdminConfig
    {
        [Description("Raffle starting pot")]
        public int RaffleStartPot = 100;

        [Description("Minutes between raffles")]
        public int RaffleDuration = 60;

        [Description("The odds of winning the raffle")]
        public int RaffleOdds = 65535;

        [Description("The cost of a raffle ticket")]
        public int RaffleTicketCost = 100;

        [Description("Time between raffle updates (minutes)")]
        public int RaffleUpdateDuration = 10;

        [Description("Maximum number of raffle tickets")]
        public int MaxRaffleTickets = 500;

        [Description("Ticket percentage kept")]
        public float RaffleTicketsKept = 75f;

        [Description("The group players that are in prison are part of.")]
        public string PrisonGroup = "Prison";

        [Description("The name of the prison warp")]
        public string PrisonWarp = "prison";

        public static ExtendedAdminConfig Read(string path)
        {
            if (!File.Exists(path))
                return new ExtendedAdminConfig();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Read(fs);
            }
        }

        public static ExtendedAdminConfig Read(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                var cf = JsonConvert.DeserializeObject<ExtendedAdminConfig>(sr.ReadToEnd());
                if (ConfigRead != null)
                    ConfigRead(cf);
                return cf;
            }
        }

        public void Write(string path)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                Write(fs);
            }
        }

        public void Write(Stream stream)
        {
            var str = JsonConvert.SerializeObject(this, Formatting.Indented);
            using (var sw = new StreamWriter(stream))
            {
                sw.Write(str);
            }
        }

        public static Action<ExtendedAdminConfig> ConfigRead;


        static void DumpDescriptions()
        {
            var sb = new StringBuilder();
            var defaults = new ExtendedAdminConfig();

            foreach (var field in defaults.GetType().GetFields())
            {
                if (field.IsStatic)
                    continue;

                var name = field.Name;
                var type = field.FieldType.Name;

                var descattr = field.GetCustomAttributes(false).FirstOrDefault(o => o is DescriptionAttribute) as DescriptionAttribute;
                var desc = descattr != null && !string.IsNullOrWhiteSpace(descattr.Description) ? descattr.Description : "None";

                var def = field.GetValue(defaults);

                sb.AppendLine("## {0}  ".SFormat(name));
                sb.AppendLine("**Type:** {0}  ".SFormat(type));
                sb.AppendLine("**Description:** {0}  ".SFormat(desc));
                sb.AppendLine("**Default:** \"{0}\"  ".SFormat(def));
                sb.AppendLine();
            }

            File.WriteAllText("ConfigDescriptions.txt", sb.ToString());
        }
    }
}