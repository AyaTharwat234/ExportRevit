using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFRvt
{
    internal static class Globals
    {
        public static string GetWorkingFolder()
        {
            string s = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            s += "\\Elibre\\EFraming\\WorkingFolder";

            try { Directory.CreateDirectory(s); }
            catch { }

            return s;
        }

    }
}

