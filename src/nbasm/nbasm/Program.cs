using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;

namespace NBasM
{
    class Program
    {
        // Карта опкодов
        static Dictionary<string, byte> OpCodes = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase)
        {
            {"NOP", 0x00}, {"HALT", 0xFF},
            {"SET", 0x10}, // База для SET Rn, imm8
            {"MOV", 0x20}, // База для MOV Rn, Rm
            {"ADD", 0x30}, {"SUB", 0x31}, {"MUL", 0x32}, {"DIV", 0x33},
            {"INC", 0x34}, {"DEC", 0x35},
            {"PUSH", 0x50}, {"POP", 0x52},
            {"JMP", 0x60}, {"CMP", 0x61}, {"JZ", 0x62}, {"JNZ", 0x63},
            {"OUT_C", 0xE0}, {"READ", 0xE2}, {"COLOR", 0xE4}, {"RAND", 0xE8}
        };

        static void Main(string[] args)
        {
            if (args.Length == 0 || args.Contains("-h") || args.Contains("--help")) { ShowHelp(); return; }
            if (args.Contains("--version")) { Console.WriteLine("New Boot ASM v1.2"); return; }

            string inputFile = GetArg(args, "-i");
            string outputFile = GetArg(args, "-o") ?? "out.bin";
            string diskFile = GetArg(args, "-td");

            if (string.IsNullOrEmpty(inputFile)) { Console.WriteLine("Error: Input file (-i) is required."); return; }
            if (!File.Exists(inputFile)) { Console.WriteLine($"Error: File {inputFile} not found."); return; }

            try
            {
                string[] lines = File.ReadAllLines(inputFile);
                byte[] binary = Compile(lines);

                if (!string.IsNullOrEmpty(diskFile))
                {
                    PatchDisk(diskFile, binary);
                    Console.WriteLine($"[OK] Patched {diskFile} ({binary.Length} bytes)");
                }
                else
                {
                    File.WriteAllBytes(outputFile, binary);
                    Console.WriteLine($"[OK] Compiled to {outputFile} ({binary.Length} bytes)");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Compilation Error: {ex.Message}");
                Console.ResetColor();
            }
        }

        static byte[] Compile(string[] lines)
        {
            var labels = new Dictionary<string, byte>();
            var output = new List<byte>();

            // 1. Очистка строк
            var cleanLines = lines
                .Select(l => l.Split(';')[0].Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            // 2. Первый проход: Сбор меток и расчет адресов
            int currentPC = 0;
            foreach (var line in cleanLines)
            {
                if (line.EndsWith(":"))
                {
                    string labelName = line.TrimEnd(':').Trim();
                    labels[labelName] = (byte)currentPC;
                }
                else
                {
                    currentPC += GetInstructionSize(line);
                }
            }

            // 3. Второй проход: Генерация байт-кода
            foreach (var line in cleanLines)
            {
                if (line.EndsWith(":")) continue;

                var parts = line.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                string cmd = parts[0].ToUpper();

                if (!OpCodes.ContainsKey(cmd))
                    throw new Exception($"Unknown instruction: {cmd}");

                byte op = OpCodes[cmd];

                if (cmd == "SET") // SET Rn, imm8 (2 байта)
                {
                    byte reg = ParseRegister(parts[1]);
                    output.Add((byte)(op + reg));
                    output.Add(ParseLiteral(parts[2], labels));
                }
                else if (cmd == "MOV") // MOV Rn, Rm (2 байта)
                {
                    byte regDest = ParseRegister(parts[1]);
                    byte regSrc = ParseRegister(parts[2]);
                    output.Add((byte)(op + regDest));
                    output.Add(regSrc);
                }
                else if (cmd == "JMP" || cmd == "JZ" || cmd == "JNZ") // (2 байта)
                {
                    output.Add(op);
                    output.Add(ParseLiteral(parts[1], labels));
                }
                else
                {
                    // Все остальные команды (1 байт)
                    output.Add(op);
                    // Если вдруг у команды есть доп. литерал (напр. COLOR 12)
                    for (int i = 1; i < parts.Length; i++)
                    {
                        if (!parts[i].StartsWith("R", StringComparison.OrdinalIgnoreCase))
                            output.Add(ParseLiteral(parts[i], labels));
                    }
                }
            }

            return output.ToArray();
        }

        static int GetInstructionSize(string line)
        {
            var parts = line.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return 0;
            string cmd = parts[0].ToUpper();

            // Эти команды всегда занимают 2 байта (Op + Data)
            if (cmd == "SET" || cmd == "MOV" || cmd == "JMP" || cmd == "JZ" || cmd == "JNZ")
                return 2;

            return 1; // Остальные (HALT, READ, ADD, etc.) — 1 байт
        }

        static byte ParseRegister(string r)
        {
            string numPart = new string(r.Where(char.IsDigit).ToArray());
            if (byte.TryParse(numPart, out byte res) && res < 16) return res;
            throw new Exception($"Invalid register: {r}");
        }

        static byte ParseLiteral(string lit, Dictionary<string, byte> labels)
        {
            if (labels.ContainsKey(lit)) return labels[lit];

            // HEX: 0xFF
            if (lit.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return byte.Parse(lit.Substring(2), NumberStyles.HexNumber);

            // Char: 'A'
            if (lit.StartsWith("'") && lit.EndsWith("'") && lit.Length == 3)
                return (byte)lit[1];

            // Decimal: 48
            if (byte.TryParse(lit, out byte res)) return res;

            throw new Exception($"Invalid literal or label: {lit}");
        }

        static void PatchDisk(string path, byte[] code)
        {
            byte[] data = File.ReadAllBytes(path);
            int offset = 88; // Оффсет BootCode в твоем NBM
            Array.Copy(code, 0, data, offset, Math.Min(code.Length, 1024));
            File.WriteAllBytes(path, data);
        }

        static string GetArg(string[] args, string flag)
        {
            int idx = Array.IndexOf(args, flag);
            return (idx != -1 && idx < args.Length - 1) ? args[idx + 1] : null;
        }

        static void ShowHelp()
        {
            Console.WriteLine("NBasM - New Boot Assembly Compiler v1.2");
            Console.WriteLine("Usage: nbasm -i <src.asm> [-o <out.bin>] [-td <disk.xvd>]");
            Console.WriteLine("Commands: SET, MOV, ADD, SUB, MUL, DIV, JMP, CMP, JZ, JNZ, OUT_C, READ, COLOR, RAND, HALT");
        }
    }
}