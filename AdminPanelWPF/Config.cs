using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminPanelWPF
{
    internal class Config
    {
        public static void CreateConfig()
        {
            if (!File.Exists("Config.ini"))
            {
                using (StreamWriter sw = new StreamWriter("Config.ini", false, Encoding.UTF8))
                {
                    sw.WriteLine("FTPserver - \n" +
                        "Login - \n" +
                        "Password - ");
                }
            }
        }
    }
}
