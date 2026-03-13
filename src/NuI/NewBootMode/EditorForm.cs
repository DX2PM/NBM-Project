using System;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace NbmPatcher
{
    public partial class EditorForm : Form
    {
        string filePath;
        RichTextBox codeBox;

        public EditorForm(string path)
        {
            this.filePath = path;
            this.Text = $"Editing: {Path.GetFileName(path)}";
            this.Size = new System.Drawing.Size(500, 450);

            codeBox = new RichTextBox { Top = 10, Left = 10, Width = 460, Height = 300, Font = new System.Drawing.Font("Courier New", 12) };

            // Пытаемся загрузить существующий код из файла
            try
            {
                byte[] fullFile = File.ReadAllBytes(path);
                byte[] bootCode = new byte[1024];
                Array.Copy(fullFile, NbmLib.BootCodeOffset, bootCode, 0, 1024);
                codeBox.Text = BitConverter.ToString(bootCode).Replace("-", " ");
            }
            catch { }

            Button btnSave = new Button { Text = "Save", Top = 330, Left = 10, Width = 100 };
            Button btnRun = new Button { Text = "Save & Run", Top = 330, Left = 120, Width = 100 };

            btnSave.Click += (s, e) => SaveCode();
            btnRun.Click += (s, e) => {
                SaveCode();
                RunEmulator();
            };

            this.Controls.Add(codeBox);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnRun);
        }

        private void SaveCode()
        {
            try
            {
                byte[] code = NbmLib.HexToBytes(codeBox.Text);
                NbmLib.PatchBootCode(filePath, code);
                MessageBox.Show("Disk patched successfully!", "OK");
            }
            catch (Exception ex)
            {
                MessageBox.Show("HEX Error: " + ex.Message);
            }
        }

        private void RunEmulator()
        {
            string enginePath = Path.Combine(Application.StartupPath, "Resources", "nbm.exe");
            if (!File.Exists(enginePath))
            {
                MessageBox.Show("nbm.exe not found in Resources!");
                return;
            }

            Process.Start(enginePath, $"\"{filePath}\"");
        }
    }
}