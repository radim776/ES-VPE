using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EventScriptIDE.Dialogs
{
    public partial class TemplateDialog : Form
    {
        public int Result = 0;
        public TemplateDialog()
        {
            InitializeComponent();
            panel1.Click += Panel1_Click;
            panel2.Click += Panel2_Click;
            radioButton1.Click += RadioButton1_Click;
            radioButton2.Click += RadioButton2_Click;
        }

        private void RadioButton2_Click(object sender, EventArgs e)
        {
            Result = 2;
            radioButton1.Checked = false;
        }

        private void RadioButton1_Click(object sender, EventArgs e)
        {
            Result = 1;
            radioButton2.Checked = false;
        }

        private void Panel2_Click(object sender, EventArgs e)
        {
            Result = 2;
            radioButton2.Checked = true;
        }

        private void Panel1_Click(object sender, EventArgs e)
        {
            Result = 1;
            radioButton1.Checked = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
           
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Result = 0;
            Close();
        }
    }
}
