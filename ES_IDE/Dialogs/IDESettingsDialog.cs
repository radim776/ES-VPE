using System.Drawing;
using System.Windows.Forms;

namespace EventScriptIDE.Dialogs
{
    public class IDESettingsDialog : Form
    {
        private readonly AppSettings _settings;
        private readonly TextBox     _btPath;
        private readonly CheckBox    _vdo;

        public IDESettingsDialog(IDE parent, AppSettings settings)
        {
            _settings = settings;

            Text = "VPE SETTINGS";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Size = new Size(560, 210);
            StartPosition = FormStartPosition.Manual;
            Font = new Font("Arial", 9f);
            BackColor = IDE.IDETheme.Back;
            ForeColor = IDE.IDETheme.Fore;

            var frm = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 10, 14, 10) };

            frm.Controls.Add(new Label
            {
                Text = "Build Tools Path:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true,
                Left = 0,
                Top = 4,
            });
            frm.Controls.Add(new Label
            {
                Text = @"e.g. C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools",
                ForeColor = Color.DimGray,
                AutoSize = true,
                Left = 0,
                Top = 24,
            });

            _btPath = new TextBox
            {
                Text = settings.BuildToolsPath,
                Left = 0,
                Top = 48,
                Width = 430,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            };
            frm.Controls.Add(_btPath);

            var browseBtn = Helpers.MakeBtn("Browse…", Color.DimGray, Color.White,
                                            (s, e) => Browse(), 80, 24);
            browseBtn.Left = 450; browseBtn.Top = 70;
            frm.Controls.Add(browseBtn);

            frm.Controls.Add(new Label
            {
                Text = "vbc.exe is looked up at: {buildtools_path}\\MSBuild\\Current\\Bin\\Roslyn\\vbc.exe",
                ForeColor = Color.Gray,
                AutoSize = true,
                Left = 0,
                Top = 76,
            });

            _vdo = new CheckBox
            {
                Text = "Visual Designer Optimizator",
                Checked = settings.VDO,
                AutoSize = true,
                Left = 0,
                Top = 104,
            };
            frm.Controls.Add(_vdo);
            Controls.Add(frm);

			var CuztomizeButton = Helpers.MakeBtn("CUZTOMIZE", Helpers.BtnPurple, IDE.IDETheme.Fore, (s, e) =>
			{
				using (var dlg = new CuztomizeDialog(parent,_settings))
				{
					Helpers.CenterOnOwner2(dlg, this);
					dlg.ShowDialog();
				}
			},0,24);
			CuztomizeButton.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
			

			var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 38, Padding = new Padding(6, 4, 6, 4) };
			var btnSave = Helpers.MakeBtn("SAVE", Helpers.BtnGreen, Color.White, (s, e) => DoSave(), 80);
			var btnCx = Helpers.MakeBtn("CANCEL", Color.DimGray, Color.White, (s, e) => Close(), 80);
			btnSave.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
			btnCx.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
			btnSave.Top = btnCx.Top = 6;
            btnPanel.Controls.AddRange(new Control[] { CuztomizeButton,btnSave, btnCx });
            btnPanel.Resize += (s, e) =>
            {
                btnSave.Left = btnPanel.Width - 172;
                btnCx.Left = btnPanel.Width - 86;
            };
            Controls.Add(btnPanel);

            Helpers.CenterOnParent(this, parent);
        }

        void Browse()
        {
            using (var dlg = new FolderBrowserDialog
            {
                Description  = "Select Build Tools root folder",
                SelectedPath = _btPath.Text,
            })
            {
                if (dlg.ShowDialog() == DialogResult.OK) _btPath.Text = dlg.SelectedPath;
            }
        }

        void DoSave()
        {
            /*if (_settings.DiscordRpc != _discordCb.Checked)
                MessageBox.Show("You may need to restart ES IDE to apply Discord RPC changes.",
                                "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);*/
            _settings.BuildToolsPath = _btPath.Text.Trim();
            _settings.VDO     = _vdo.Checked;
            SettingsManager.Save(_settings);
            //CustomMessageBox.Show("Settings saved.", "Saved",MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
    }
}
