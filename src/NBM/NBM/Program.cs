using System;
using System.IO;
using System.Text;
using NewBootMode;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0) { PrintHelp(); return; }

        try
        {
            switch (args[0])
            {
                case "--version":
                    Console.WriteLine("New Boot Mode v0.1 <Beta>");
                    break;

                case "-h":
                case "--help":
                    PrintHelp();
                    break;

                case "-n": // Create new NBM
                    if (args.Length < 2) return;
                    byte[] defaultFlags = { 0x00, 0x00, 0x00, 0x03 };
                    NBM.CreateNBM(args[1], defaultFlags, new char[56], new byte[1024]);
                    Console.WriteLine($"[OK]: File {args[1]} created successfully.");
                    break;

                case "-l":
                    if (args.Length < 2) return;
                    // nbm -l !boot_code <file.bin> <nbm_file>
                    if (args[1] == "!boot_code" && args.Length == 4)
                    {
                        UpdateBootCode(args[2], args[3]);
                    }
                    else // nbm -l <file>
                    {
                        InspectNBM(args[1]);
                    }
                    break;

                default: // Execute: nbm <file> [-m size]
                    RunNBM(args);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FATAL ERROR]: {ex.Message}");
            Console.ResetColor();
        }
    }

    static void InspectNBM(string path)
    {
        var nbm = new NBM(path);
        Console.WriteLine($"--- NBM Header Info: {path} ---");
        Console.WriteLine($"Magic (Hex):   {BitConverter.ToString(nbm.MAGIC)}");
        Console.WriteLine($"Magic (Str):   {Encoding.ASCII.GetString(nbm.MAGIC)}");
        Console.WriteLine($"Flags:         {BitConverter.ToString(nbm.Flags)}");
        Console.WriteLine($"Config:        {new string(nbm.Cnf).Trim('\0')}");
        Console.WriteLine($"BootCode Len:  {nbm.BootCode.Length} bytes");
    }

    // ИСПРАВЛЕНО: Теперь читаем СЫРЫЕ БАЙТЫ, а не текст!
    static void UpdateBootCode(string binPath, string nbmPath)
    {
        if (!File.Exists(binPath)) throw new Exception($"Source file {binPath} not found.");

        // Читаем напрямую байты из bc.bc
        byte[] newCodeRaw = File.ReadAllBytes(binPath);
        byte[] finalCode = new byte[1024];

        // Копируем столько, сколько влезет (до 1024 байт)
        Array.Copy(newCodeRaw, 0, finalCode, 0, Math.Min(newCodeRaw.Length, 1024));

        var nbm = new NBM(nbmPath);

        // Используем твои текущие флаги и конфиг, меняем только код
        NBM.CreateNBM(nbmPath, nbm.Flags, nbm.Cnf, finalCode);

        Console.WriteLine($"[OK]: BootCode successfully injected from binary: {binPath}");
    }

    static void RunNBM(string[] args)
    {
        string path = args[0];
        if (!File.Exists(path)) { Console.WriteLine($"Error: File {path} not found."); return; }

        int ramSize = 65536; // По умолчанию 64KB, как мы делали в BootParse

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "-m" && i + 1 < args.Length)
                int.TryParse(args[++i], out ramSize);
        }

        Console.WriteLine($"[BOOT]: Loading {path}...");
        BootParse.ParseNBMB(path);
    }

    static void PrintHelp()
    {
        Console.WriteLine("NBM Emulator Interface");
        Console.WriteLine("Usage:");
        Console.WriteLine("  nbm -n <file>                     Create new NBM container");
        Console.WriteLine("  nbm -l <file>                     Inspect NBM header");
        Console.WriteLine("  nbm -l !boot_code <bin> <dest>    Inject RAW binary into NBM disk");
        Console.WriteLine("  nbm <file> [-m <size>]            Execute NBM file");
        Console.WriteLine("  nbm --version                     Show version");
    }
}