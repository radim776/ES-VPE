using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EventScriptIDE.Dialogs
{
	public partial class InstallNetSdk : Form
	{
		AppSettings appsettings;
		IDE parent;
		public InstallNetSdk(AppSettings a,IDE b)
		{
			InitializeComponent();
			BackColor = IDE.IDETheme.Back;
			ForeColor = IDE.IDETheme.Fore;
			appsettings = a;
			parent = b;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			Process proc = new Process();
			proc.StartInfo.UseShellExecute = true;
			proc.StartInfo.FileName = "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.423-windows-x64-installer";
			proc.Start();

		}

		private void button2_Click(object sender, EventArgs e)
		{
			var vbc = Path.Combine(appsettings.BuildToolsPath, "MSBuild", "Current", "Bin", "Roslyn", "vbc.exe");
			if (!File.Exists(vbc)) vbc = "vbc";

			try
			{
				var result = Process.Start(new ProcessStartInfo
				{
					FileName = vbc,
					Arguments = "-Version",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				});

				result?.WaitForExit();

				if (result == null || result.ExitCode != 0)
					throw new Exception("vbc check failed");

				Close();
			}
			catch
			{
				CustomMessageBox.Show("Compiler not found in this path !", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		private void button3_Click(object sender, EventArgs e)
		{
			using (var dlg = new IDESettingsDialog (parent,appsettings))
			{
				Helpers.CenterOnOwner2(dlg, this);
				dlg.ShowDialog(this);

				var vbc = Path.Combine(appsettings.BuildToolsPath, "MSBuild", "Current", "Bin", "Roslyn", "vbc.exe");
				if (!File.Exists(vbc)) vbc = "vbc";

				try
				{
					var result = Process.Start(new ProcessStartInfo
					{
						FileName = vbc,
						Arguments = "-Version",
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						UseShellExecute = false,
						CreateNoWindow = true
					});

					result?.WaitForExit();

					if (result == null || result.ExitCode != 0)
						throw new Exception("vbc check failed");

					Close();
				}
				catch
				{
					CustomMessageBox.Show("Compiler not found in this path !", "WARNING",MessageBoxButtons.OK,MessageBoxIcon.Warning);
				}

				
			}
		}
	}
}
