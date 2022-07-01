using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MCRun
{
    public partial class Form1 : Form
    {
        string[] args;

        public Form1()
        {
            InitializeComponent();
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            txtCommand.ReadOnly = true;
            txtCommand.BackColor = Color.White;

            try
            {
                args = Environment.GetCommandLineArgs().Skip(1).ToArray();
                string uri = string.Join("/", args);
                if (string.IsNullOrWhiteSpace(uri))
                    uri = $"{Program.Protocol}://";

                txtURI.Text = uri.TrimStart('/');
                txtURI.Select(txtURI.Text.Length, 0);
            }
            catch { }
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            if (!Program.Execute(txtURI.Text, out string err))
                MessageBox.Show(this, "Failed to execute given command line:\n{err}", "Execute failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void btnParse_Click(object sender, EventArgs e)
        {
            Program.Parse(txtURI.Text, out var cmd, out var args);
            txtCommand.Text = $"{cmd} {args}";
        }

        private void btnTips_Click(object sender, EventArgs e)
        {
            new About().ShowDialog();
        }
    }
}
