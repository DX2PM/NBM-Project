using System;
using System.IO;
using System.Linq;
using System.Text;

namespace NbmPatcher
{
    public static class NbmLib
    {
        public const int BootCodeOffset = 88; // 4(Magic)+4(Flags)+8(Cert)+8(Res)+56(Cnf)+8(Pad)

        public static byte[] HexToBytes(string hex)
        {
            // Убираем пробелы и мусор
            hex = hex.Replace(" ", "").Replace("\n", "").Replace("\r", "");
            return Enumerable.Range(0, hex.Length / 2)
                             .Select(x => Convert.ToByte(hex.Substring(x * 2, 2), 16))
                             .ToArray();
        }

        public static void CreateEmptyXvd(string path, string config = "Default Config")
        {
            byte[] data = new byte[1112];
            byte[] magic = Encoding.ASCII.GetBytes("NBM0");
            byte[] cnf = Encoding.ASCII.GetBytes(config.PadRight(56));

            Array.Copy(magic, 0, data, 0, 4);
            Array.Copy(cnf, 0, data, 24, Math.Min(cnf.Length, 56));

            File.WriteAllBytes(path, data);
        }

        public static void PatchBootCode(string path, byte[] code)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
            {
                stream.Position = BootCodeOffset;
                stream.Write(code, 0, Math.Min(code.Length, 1024));
            }
        }
    }
}