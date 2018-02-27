using System;
using System.Collections.Generic;
using System.Text;

namespace KoikatsuUnlimited.Shared
{
    internal static class Debug
    {
        public static void DumpStackTrace()
        {
            var fname = System.IO.Path.GetTempFileName();
            using (var f = System.IO.File.OpenWrite(fname))
            using (var sw = new System.IO.StreamWriter(f, Encoding.UTF8))
            {
                sw.WriteLine(System.Environment.StackTrace);
            }
            System.Diagnostics.Process.Start("notepad.exe", fname);
        }
    }
}
