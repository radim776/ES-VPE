using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EventScriptIDE.Dialogs
{
    public class ExtensionBrowserDialog : Form
    {
        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private readonly Panel _listPanel;
        private readonly Label _countLbl;

        private static readonly Dictionary<string, Color> TypeColors = new Dictionary<string, Color>
        {
            { "Imports", ColorTranslator.FromHtml("#1b5e20") },
            { "Triggers", ColorTranslator.FromHtml("#4a148c") },
            { "Conditions", ColorTranslator.FromHtml("#0d47a1") },
            { "Actions", ColorTranslator.FromHtml("#b71c1c") },
        };

        public ExtensionBrowserDialog(IDE parent)
        {
            Text = "EXTENSION BROWSER";
            Size = new Size(800, 540);
            MinimumSize = new Size(640, 400);
            StartPosition = FormStartPosition.Manual;
            Font = new Font("Segoe UI", 9f);
            BackColor = IDE.IDETheme.Back;
            ForeColor = IDE.IDETheme.Fore;

            using (Bitmap bmp = new Bitmap(parent.ExtensionIcon))
            {
                IntPtr hIcon = bmp.GetHicon();

                using (Icon temp = Icon.FromHandle(hIcon))
                {
                    this.Icon = (Icon)temp.Clone();
                }

                DestroyIcon(hIcon);
            }

            // Top bar
            var top = new Panel { Dock = DockStyle.Top, Height = 36 };
            top.Controls.Add(new Label
            {
                Text = "INSTALLED EXTENSIONS",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                AutoSize = true,
                Left = 8,
                Top = 6,
            });
            var reloadBtn = Helpers.MakeBtn("RELOAD ALL",  Helpers.BtnBlue, Color.White, (s, e) => Reload(), 0, 26);
            var folderBtn = Helpers.MakeBtn("OPEN FOLDER", Color.DimGray,  Color.White, (s, e) => OpenFolder(), 0, 26);
            reloadBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            folderBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            top.Controls.AddRange(new Control[] { reloadBtn, folderBtn });
            top.Resize += (s, e) =>
            {
                reloadBtn.Left = top.Width - 120; reloadBtn.Top = 5;
                folderBtn.Left = top.Width - 240; folderBtn.Top = 5;
            };

            // Scrollable list
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(0xf0, 0xf0, 0xf0)
            };
            _listPanel = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = IDE.IDETheme.CanvasBack,
            };
            scroll.Controls.Add(_listPanel);

            // Footer
            var foot = new Panel { Dock = DockStyle.Bottom, Height = 34, Padding = new Padding(6, 4, 6, 4) };
            _countLbl = new Label { AutoSize = true, Left = 6, Top = 8 };
            foot.Controls.Add(_countLbl);
            foot.Controls.Add(new Label
            {
                Text = "Extensions path: " + SettingsManager.ExtensionsDir,
                ForeColor = Color.Gray,
                AutoSize = true,
                Left = 200,
                Top = 8,
            });
            /*var closeBtn = new Button
            {
                Text   = "CLOSE",
                Width  = 70,
                Height = 24,
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
            };*/
            var closeBtn = Helpers.MakeBtn("CLOSE", Helpers.BtnDarkGray, Color.White, (s, e) => Close());
            //closeBtn.Click += (s, e) => Close();
            closeBtn.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            closeBtn.Top = 5;
            foot.Controls.Add(closeBtn);
            foot.Resize += (s, e) => closeBtn.Left = foot.Width - 85;

            Controls.Add(scroll);
            Controls.Add(top);
            Controls.Add(foot);

            Populate();
            Helpers.CenterOnParent(this, parent);
        }

        // Private helper struct

        struct ExtFile
        {
            public string Subdir, Folder, Fname;
        }

        void Populate()
        {
            _listPanel.Controls.Clear();
            var allFiles = new List<ExtFile>();

            foreach (var sub in new[] { "Imports", "Triggers", "Conditions", "Actions" })
            {
                var folder = Path.Combine(SettingsManager.ExtensionsDir, sub);
                if (!Directory.Exists(folder)) continue;
                foreach (var f in Directory.GetFiles(folder).OrderBy(x => x))
                {
                    var fl = f.ToLower();
                    if (fl.EndsWith(".json") || fl.EndsWith(".disabled"))
                        allFiles.Add(new ExtFile { Subdir = sub, Folder = folder, Fname = Path.GetFileName(f) });
                }
            }

            if (allFiles.Count == 0)
            {
                _listPanel.Controls.Add(new Label
                {
                    Text    = "No extensions found.\n\nPlace extension JSON files in:\n" + SettingsManager.ExtensionsDir,
                    ForeColor = Color.Gray,
                    Dock=DockStyle.Fill,
                    BackColor=IDE.IDETheme.CanvasBack,
                    AutoSize  = true,
                    Padding   = new Padding(20),
                });
                _countLbl.Text = "0 extensions";
                return;
            }

            _countLbl.Text = allFiles.Count + " file(s) found";
            foreach (var ef in allFiles)
            {
                var path    = Path.Combine(ef.Folder, ef.Fname);
                var enabled = ef.Fname.ToLower().EndsWith(".json");
                RenderRow(ef.Subdir, ef.Folder, ef.Fname, path, enabled);
            }
        }

        void RenderRow(string subdir, string folder, string fname, string path, bool enabled)
        {
            var meta = new Dictionary<string, string>
            {
                { "Name",      fname },
                { "Version",   "?" },
                { "Developer", "?" },
            };
            var itemCount = 0;
            try
            {
                var arr = JArray.Parse(File.ReadAllText(path));
                if (arr.Count > 0)
                {
                    var hdr = arr[0] as JObject;
                    if (hdr != null && hdr.Value<string>("SpecialType") == "ExtensionData")
                    {
                        meta["Name"] = hdr.Value<string>("Name") ?? fname;
                        meta["Version"] = hdr.Value<string>("Version") ?? "?";
                        meta["Developer"] = hdr.Value<string>("Developer") ?? "?";
                    }
                }
                itemCount = Math.Max(0, arr.Count - 1);
            }
            catch { }

            Color typeColor = TypeColors.TryGetValue(subdir, out var c) ? c : Color.Gray;
            //original
            //var bg = enabled ? Color.FromArgb(0xe8, 0xff, 0xe8) : Color.FromArgb(0xf5, 0xf5, 0xf5);
            // dark mode
            var bg = enabled ? Color.FromArgb(20, 60, 20) : Color.FromArgb(40, 40, 40);

            var row = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = bg,
                Padding = new Padding(0),
            };
            
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = bg,
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)) ; 
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));

            // Type panel
            var typePanel = new Panel { Dock = DockStyle.Fill, BackColor = typeColor };
            var typeLabel = new Label
            {
                Text = subdir,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            typePanel.MaximumSize = new Size(0,50);
            typePanel.Controls.Add(typeLabel);
            table.Controls.Add(typePanel, 0, 0);

            // Info panel
            var infoPanel = new Panel { Dock = DockStyle.Fill, BackColor = bg, Padding = new Padding(6, 2, 6, 2) };
            var nameLabel = new Label
            {
                Text = meta["Name"],
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true,
                BackColor = bg
            };
            var detailLabel = new Label
            {
                Text = $"v{meta["Version"]}  by {meta["Developer"]}  |  {itemCount} item(s){(enabled ? "" : "  [DISABLED]")}",
                ForeColor = Color.DimGray,
                AutoSize = true,
                Top = 22,
                BackColor = bg
            };
            infoPanel.Controls.Add(nameLabel);
            infoPanel.Controls.Add(detailLabel);
            table.Controls.Add(infoPanel, 1, 0);

            // Buttons panel
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = bg,
                Padding = new Padding(0),
                AutoSize = true
            };
            var editBtn = Helpers.MakeBtn("✎ Edit", Helpers.BtnBlue, Color.White, (s, e) => EditFile(path), 0, 24);
            var uninstallBtn = Helpers.MakeBtn("✖ Uninstall", Helpers.BtnRed, Color.White, (s, e) => Uninstall(path, meta["Name"]), 0, 24);
            var toggleBtn = enabled
                ? Helpers.MakeBtn("II Disable", Color.FromArgb(0xf5, 0x7c, 0x00), Color.White, (s, e) => Toggle(path, folder, fname, true), 0, 24)
                : Helpers.MakeBtn("▶ Enable", Helpers.BtnGreen, Color.White, (s, e) => Toggle(path, folder, fname, false), 0, 24);

            btnPanel.Controls.AddRange(new Control[] { editBtn, uninstallBtn, toggleBtn });
            table.Controls.Add(btnPanel, 2, 0);

            row.Controls.Add(table);
            _listPanel.Controls.Add(row);
            _listPanel.Controls.SetChildIndex(row, 0);
        }

        void Toggle(string path, string folder, string fname, bool disable)
        {
            string newPath;
            if (disable)
                newPath = Path.Combine(folder, Path.GetFileNameWithoutExtension(fname) + ".disabled");
            else
                newPath = Path.Combine(folder, Path.GetFileNameWithoutExtension(fname) + ".json");
            try { File.Move(path, newPath); }
            catch (Exception ex) { CustomMessageBox.Show(ex.Message, "Error"); return; }
            Reload();
        }

        void EditFile(string path)
        {
            try
            {
                var psi = new ProcessStartInfo(path) { UseShellExecute = true };
                Process.Start(psi);
            }
            catch { CustomMessageBox.Show("Open this file in a text editor:\n" + path, "Edit"); }
        }

        void Uninstall(string path, string name)
        {
            if (CustomMessageBox.Show("Delete extension \"" + name + "\"?\n\n" + path, "Uninstall", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try { File.Delete(path); }
            catch (Exception ex) { CustomMessageBox.Show(ex.Message, "Error"); return; }
            Reload();
        }

        void Reload()
        {
            ExtensionRegistry.Reload();
            Populate();
            _countLbl.Text = ExtensionRegistry.Metadata.Count + " active";
        }

        void OpenFolder()
        {
            Directory.CreateDirectory(SettingsManager.ExtensionsDir);
            try
            {
                var psi = new ProcessStartInfo(SettingsManager.ExtensionsDir) { UseShellExecute = true };
                Process.Start(psi);
            }
            catch { CustomMessageBox.Show(SettingsManager.ExtensionsDir, "Extensions Folder"); }
        }
    }
}
