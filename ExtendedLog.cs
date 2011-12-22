using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using System.IO;

namespace ExtendedAdmin
{
    public class ExtendedLog
    {
        private static ExtendedLog _Log;
        public static ExtendedLog Current
        {
            get
            {
                if (_Log == null)
                {
                    _Log = new ExtendedLog();
                }

                return _Log;
            }
        }

        private readonly string FilePath = Path.Combine(TShock.SavePath, "ExtendedLog.log");

        public void Log(string value)
        {
            using (FileStream fs = new FileStream(FilePath, FileMode.Append))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.WriteLine(string.Format("{0:f} - {1}", DateTime.Now, value));
            }
        }
    }
}
