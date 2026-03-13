using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NewBootMode
{
    public class NBM
    {
        public byte[] MAGIC = new byte[4];
        public byte[] Flags = new byte[4];
        public byte[] Certificate = new byte[8];
        public byte[] Reserved = new byte[8];
        public byte[] Padding = new byte[8];
        public char[] Cnf = new char[56];
        public byte[] BootCode = new byte[1024];

        // Constructor for loading
        public NBM(string path) => LoadNBM(path);

        // Constructor for creating (matches your initial usage)
        public NBM(string path, byte[] flags, char[] cnf, byte[] bootCode)
        {
            CreateNBM(path, flags, cnf, bootCode);
            LoadNBM(path);
        }

        public void LoadNBM(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Image file not found.");
            byte[] data = File.ReadAllBytes(path);

            if (data.Length < 1112) throw new Exception("Invalid NBM image size.");
            if (data[0] != 0x4E || data[1] != 0x42 || data[2] != 0x4D || data[3] != 0x30)
                throw new Exception("Invalid Magic Number.");

            Array.Copy(data, 0, MAGIC, 0, 4);
            Array.Copy(data, 4, Flags, 0, 4);
            Array.Copy(data, 8, Certificate, 0, 8);
            Array.Copy(data, 16, Reserved, 0, 8);

            string configStr = Encoding.ASCII.GetString(data, 24, 56);
            Cnf = configStr.ToCharArray();

            Array.Copy(data, 80, Padding, 0, 8);
            Array.Copy(data, 88, BootCode, 0, 1024);

            Console.WriteLine($"[SYSTEM]: NBM Loaded. Magic: {Encoding.ASCII.GetString(MAGIC)}");
        }

        // STATIC METHOD: CreateNBM (Fixed the missing definition)
        public static void CreateNBM(string path, byte[] flags, char[] cnf, byte[] bootCode)
        {
            byte[] magic = { 0x4E, 0x42, 0x4D, 0x30 };
            byte[] cert = { 0x24, 0x14, 0x12, 0x41, 0x42, 0x14, 0x21, 0x43 };

            List<byte> data = new List<byte>();
            data.AddRange(magic);
            data.AddRange(flags);
            data.AddRange(cert);
            data.AddRange(new byte[8]); // Reserved

            byte[] cnfBytes = Encoding.ASCII.GetBytes(new string(cnf).PadRight(56, '\0'));
            data.AddRange(cnfBytes);

            data.AddRange(new byte[8]); // Padding
            data.AddRange(bootCode);

            File.WriteAllBytes(path, data.ToArray());
            Console.WriteLine($"[OK]: Disk image created: {path}");
        }
    }

    public static class BootParse
    {
        private static byte[] Regs = new byte[16];
        private static byte[] RAM = new byte[65536];
        private static ushort PC = 0;
        private static ushort SP = 0xFFFF;
        private static bool Halted = false;
        private static bool ZF = false, CF = false, SF = false;

        // Alias for ParseNBMB to match your call "ParseNBM"
        public static void ParseNBM(string path) => ParseNBMB(path);

        public static void ParseNBMB(string path)
        {
            try
            {
                NBM mNBM = new NBM(path);
                ParseBoot(mNBM.BootCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[CPU ERROR]: {ex.Message}");
            }
        }

        public static void ParseOpCode(byte[] bootCode)
        {
            byte opcode = bootCode[PC];
            if (opcode == 0x00) return;
            if (opcode == 0xFF) { Halted = true; return; }

            // MOV imm8
            if (opcode >= 0x10 && opcode <= 0x1F)
            {
                Regs[opcode - 0x10] = bootCode[++PC];
                return;
            }

            // MOV Reg-Reg
            if (opcode >= 0x20 && opcode <= 0x2F)
            {
                byte srcReg = bootCode[++PC];
                Regs[opcode - 0x20] = Regs[srcReg & 0x0F];
                return;
            }

            switch (opcode)
            {
                case 0x30: UpdateFlags(Regs[0] += Regs[1]); break;
                case 0x31: UpdateFlags(Regs[0] -= Regs[1]); break;
                case 0x32: UpdateFlags(Regs[0] *= Regs[1]); break;
                case 0x33: UpdateFlags(Regs[0] /= (byte)(Regs[1] == 0 ? 1 : Regs[1])); break;
                case 0x34: Regs[0]++; UpdateFlags(Regs[0]); break;
                case 0x35: Regs[0]--; UpdateFlags(Regs[0]); break;
                case 0x60: PC = (ushort)(bootCode[++PC] - 1); break;
                case 0x61: ZF = (Regs[0] == Regs[1]); SF = (Regs[0] < Regs[1]); break;
                case 0x62: if (ZF) PC = (ushort)(bootCode[++PC] - 1); else PC++; break;
                case 0x63: if (!ZF) PC = (ushort)(bootCode[++PC] - 1); else PC++; break;
                case 0x80: RAM[bootCode[++PC]] = Regs[0]; break;
                case 0x81: Regs[0] = RAM[bootCode[++PC]]; break;
                case 0xE0: Console.Write((char)Regs[0]); break;
                case 0xE1: Console.Write(Regs[0]); break;
                case 0xE4: Console.ForegroundColor = (ConsoleColor)(Regs[0] % 16); break;
                case 0xE5: Console.Clear(); break;
                case 0xE8: Regs[0] = (byte)new Random().Next(0, 256); break;
                default: break;
            }
        }

        public static void ParseBoot(byte[] bootCode)
        {
            Halted = false; PC = 0; SP = 0xFFFF;
            Array.Clear(Regs, 0, Regs.Length);
            Array.Clear(RAM, 0, RAM.Length);

            Console.WriteLine("--- System Boot Initiated ---");
            while (PC < bootCode.Length && !Halted)
            {
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.F12) DumpState();
                ParseOpCode(bootCode);
                PC++;
            }
            Console.WriteLine("\n--- System Halted ---");
            DumpState();
        }

        private static void UpdateFlags(int res) { ZF = (byte)res == 0; SF = (byte)res > 127; }
        private static void Push(byte val) => RAM[SP--] = val;
        private static byte Pop() => RAM[++SP];

        public static void DumpState()
        {
            Console.WriteLine("\n" + new string('=', 40));
            Console.WriteLine($" PC: 0x{PC:X4} | Flags: ZF:{(ZF ? 1 : 0)} SF:{(SF ? 1 : 0)}");
            for (int i = 0; i < 16; i++)
            {
                Console.Write($" R{i:X}: 0x{Regs[i]:X2} ");
                if ((i + 1) % 4 == 0) Console.WriteLine();
            }
            Console.WriteLine(new string('=', 40));
        }
    }
}