using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EventScriptIDE.Dialogs
{
	public partial class CuztomizeDialog : Form
	{
		public Font MonoFont1 = new Font(IDE.MonoFont, 11f, FontStyle.Bold);
		public Font MonoFont2 = new Font(IDE.MonoFont, 8f, FontStyle.Regular);
		public Font VFont1 = IDE._fontSegoeTinyBold;
		private IDE ParentIde;
		private AppSettings ASettings;
		private List<Control> MonoFont1Controls = new List<Control> () ;
		private List<Control> MonoFont2Controls = new List<Control>();
		public CuztomizeDialog(IDE Parent,AppSettings Settings)
		{
			InitializeComponent();
			MonoFont1Controls.Add(label1);
			MonoFont1Controls.Add(label6);
			MonoFont2Controls.Add(label4);
			MonoFont2Controls.Add(label3);
			ParentIde = Parent;
			ASettings = Settings;
			var Fonts = Parent.DecodeFonts(Settings.Fonts1);
			MonoFont1 = Fonts[0];
			MonoFont2 = Fonts[1];
			VFont1 = Fonts[2];
			foreach (Control c in MonoFont1Controls)
			{
				c.Font = MonoFont1;
			}
			foreach (Control c in MonoFont2Controls)
			{
				c.Font = MonoFont2;
			}
			label2.Font = VFont1;
		}

		private void MonoFont1Change()
		{
			fontDialog1.Font = MonoFont1;
			fontDialog1.ShowDialog();
			Console.WriteLine(fontDialog1.Font);
			MonoFont1 = fontDialog1.Font;
			foreach(Control c in MonoFont1Controls)
			{
				c.Font = MonoFont1;
			}
		}

		private void MonoFont2Change()
		{
			fontDialog1.Font = MonoFont2;
			fontDialog1.ShowDialog();
			Console.WriteLine(fontDialog1.Font);
			MonoFont2 = fontDialog1.Font;
			foreach (Control c in MonoFont2Controls)
			{
				c.Font = MonoFont2;
			}
		}


		private void label6_Click(object sender, EventArgs e)
		{
			MonoFont1Change();
		}

		private void label1_Click(object sender, EventArgs e)
		{
			MonoFont1Change();
		}

		private void label4_Click(object sender, EventArgs e)
		{
			MonoFont2Change();
		}

		private void label3_Click(object sender, EventArgs e)
		{
			MonoFont2Change();
		}

		private void label2_Click(object sender, EventArgs e)
		{
			fontDialog1.Font = VFont1;
			fontDialog1.ShowDialog();
			VFont1 = fontDialog1.Font;
			label2.Font = VFont1;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if(CustomMessageBox.Show("Save and Restart ES VPE ? make sure your project has been saved !","WARNING",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.Yes)
			{
				ParentIde.SaveProject(false);
				var save = $"{MonoFont1.FontFamily.Name},{MonoFont1.Style.ToString()},{MonoFont1.Size.ToString()};" +
						   $"{MonoFont2.FontFamily.Name},{MonoFont2.Style.ToString()},{MonoFont2.Size.ToString()};" +
						   $"{VFont1   .FontFamily.Name},{VFont1   .Style.ToString()},{VFont1   .Size.ToString()};";
				//MessageBox.Show(save);
				ASettings.Fonts1 = save;
				SettingsManager.Save(ASettings);
				//somehow open .\ES_IDE.exe and stop this process
				var NewProces = new ProcessStartInfo
				{
					UseShellExecute = false,
					CreateNoWindow = false,
					WindowStyle = ProcessWindowStyle.Normal,
					FileName = @".\ES_IDE.exe"
				};
				Process.Start(NewProces);
				Environment.Exit(0);
			}
		}
	}
}
