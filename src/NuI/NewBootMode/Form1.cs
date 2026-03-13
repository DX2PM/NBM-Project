using System;
using System.Windows.Forms;
using System.IO;

namespace NbmPatcher
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            this.Text = "New Boot Mode v0.1 (UI)";
            this.Size = new System.Drawing.Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;

            Button btnCreate = new Button { Text = "Create a new XVirtual Disk", Top = 50, Left = 50, Width = 280, Height = 50 };
            Button btnEdit = new Button { Text = "Edit an existing xvd", Top = 120, Left = 50, Width = 280, Height = 50 };

            btnCreate.Click += (s, e) => {
                SaveFileDialog sfd = new SaveFileDialog { Filter = "XVD Disk|*.xvd" };
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    NbmLib.CreateEmptyXvd(sfd.FileName);
                    new EditorForm(sfd.FileName).Show();
                }
            };

            btnEdit.Click += (s, e) => {
                OpenFileDialog ofd = new OpenFileDialog { Filter = "XVD Disk|*.xvd" };
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    new EditorForm(ofd.FileName).Show();
                }
            };

            this.Controls.Add(btnCreate);
            this.Controls.Add(btnEdit);
        }
    }
}