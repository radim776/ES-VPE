using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EventScriptIDE.Dialogs
{

    public class ProjectSettingsDialog : Form
    {
        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public ProjectModel Result { get; private set; }

        private readonly TextBox  _nameBox;
        private readonly TextBox  _widthBox;
        private readonly TextBox  _heightBox;
        private readonly CheckBox _resizable;
        private readonly CheckBox _VStyles;
        private readonly ListBox  _dllList;
        private readonly TextBox  _dllEntry;
        private readonly ListBox  _embList;

        public ProjectSettingsDialog(IDE parent, ProjectModel project)
        {
            Text            = "PROJECT SETTINGS";
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize     = new Size(500, 420);
            Size            = new Size(560, 480);
            StartPosition   = FormStartPosition.Manual;
            Font            = new Font("Segoe UI", 9f);
            BackColor = IDE.IDETheme.Back;
            ForeColor = IDE.IDETheme.Fore;

            using (Bitmap bmp = new Bitmap(parent.SettingsIcon))
            {
                IntPtr hIcon = bmp.GetHicon();

                using (Icon temp = Icon.FromHandle(hIcon))
                {
                    this.Icon = (Icon)temp.Clone();
                }

                DestroyIcon(hIcon);
            }

            var nb = new TabControl
            {
                Dock = DockStyle.Fill
            };

            nb.DrawItem += (s, e) =>
            {
                var tab = nb.TabPages[e.Index];
                var g = e.Graphics;
                var bounds = e.Bounds;

                bool selected = (e.State & DrawItemState.Selected) != 0;

                // Background color
                Color backColor = selected ? Color.FromArgb(45, 45, 45) : Color.FromArgb(30, 30, 30);
                using (var brush = new SolidBrush(backColor))
                    g.FillRectangle(brush, bounds);

                // Border
                using (var pen = new Pen(Color.FromArgb(70, 70, 70))) // dark border
                    g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);

                // Text
                var textColor = selected ? Color.White : Color.LightGray;
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(tab.Text, tab.Font, new SolidBrush(textColor), bounds, sf);
            };

            // ── Tab 1: General ────────────────────────────────────────────────
            var tabGen  = new TabPage { Text = "  GENERAL  " };
            var genTable = new TableLayoutPanel
            {
                ColumnCount = 2, RowCount = 4,
                Dock        = DockStyle.Fill,
                Padding     = new Padding(12, 8, 12, 8),
            };
            genTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            genTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (var i = 0; i < 4; i++) genTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _nameBox   = new TextBox { Text = project.Name,                  Dock = DockStyle.Fill };
            _widthBox  = new TextBox { Text = project.FormWidth.ToString(),   Dock = DockStyle.Fill };
            _heightBox = new TextBox { Text = project.FormHeight.ToString(),  Dock = DockStyle.Fill };
            _resizable = new CheckBox { Text = "Allow window resizing", Checked = project.Resizable, AutoSize = true };
            _VStyles = new CheckBox { Text = "Modern", Checked = project.VStyle, AutoSize = true };

            AddGenRow(genTable, 0, "Project Name:", _nameBox);
            AddGenRow(genTable, 1, "Form Width:",   _widthBox);
            AddGenRow(genTable, 2, "Form Height:",  _heightBox);
            AddGenRow(genTable, 3, "Resizable:",    _resizable);
            AddGenRow(genTable, 4, "Visual Styles:", _VStyles);
            AddGenRow(genTable, 5, "", new Panel());
            tabGen.Controls.Add(genTable);
            nb.TabPages.Add(tabGen);

            // ── Tab 2: References ─────────────────────────────────────────────
            var tabRef  = new TabPage { Text = "  REFERENCES  " };
            var refPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 8, 12, 8) };

            var refLbl = new Label
            {
                Text      = "Extra DLL references (passed as /r: to vbc.exe)",
                ForeColor = Color.Gray,
                AutoSize  = true,
                Dock      = DockStyle.Top,
            };
            _dllList = new ListBox { Dock = DockStyle.Fill, Font = new Font("Courier New", 9f), BackColor = IDE.IDETheme.Back, ForeColor = IDE.IDETheme.Fore, BorderStyle = BorderStyle.None };
            foreach (var d in project.ExtraDlls) _dllList.Items.Add(d);

            var dllBot = new Panel { Dock = DockStyle.Bottom, Height = 32 };
            _dllEntry = new TextBox { Width = 260, Text = "e.g. System.Net.Http.dll", ForeColor = Color.Gray };
            _dllEntry.GotFocus  += (s, e) =>
            {
                if (_dllEntry.ForeColor == Color.Gray) { _dllEntry.Text = ""; _dllEntry.ForeColor = Color.Black; }
            };
            _dllEntry.LostFocus += (s, e) =>
            {
                if (_dllEntry.Text == "") { _dllEntry.Text = "e.g. System.Net.Http.dll"; _dllEntry.ForeColor = Color.Gray; }
            };
            var addDllBtn = Helpers.MakeBtn("Add",    Helpers.BtnBlue, Color.White, (s, e) => AddDll(),    50, 24);
            var remDllBtn = Helpers.MakeBtn("Remove", Helpers.BtnRed,  Color.White, (s, e) => RemoveDll(), 60, 24);
            _dllEntry.Left = 0;  _dllEntry.Top  = 4; _dllEntry.Height = 24;
            addDllBtn.Left = 268; addDllBtn.Top = 4;
            remDllBtn.Left = 324; remDllBtn.Top = 4;
            dllBot.Controls.AddRange(new Control[] { _dllEntry, addDllBtn, remDllBtn });

            refPanel.Controls.Add(_dllList);
            refPanel.Controls.Add(refLbl);
            refPanel.Controls.Add(dllBot);
            tabRef.Controls.Add(refPanel);
            nb.TabPages.Add(tabRef);

            // ── Tab 3: Embedded Files ─────────────────────────────────────────
            var tabEmb  = new TabPage { Text = "  EMBEDDED FILES  " };
            var embPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 8, 12, 8) };

            var embLbl = new Label
            {
                Text = "Files embedded as manifest resources.\n" +
                       "Access with: Assembly.GetExecutingAssembly().GetManifestResourceStream(\"filename\")",
                ForeColor = Color.Gray,
                AutoSize  = true,
                Dock      = DockStyle.Top,
            };
            _embList = new ListBox { Dock = DockStyle.Fill, Font = new Font("Courier New", 9f), BackColor = IDE.IDETheme.Back, ForeColor = IDE.IDETheme.Fore, BorderStyle = BorderStyle.None };
            foreach (var f in project.EmbeddedFiles) _embList.Items.Add(f);

            var embBot    = new Panel { Dock = DockStyle.Bottom, Height = 32 };
            var addEmbBtn = Helpers.MakeBtn("Add File…",      Helpers.BtnBlue, Color.White, (s, e) => BrowseEmbed(),   0, 24);
            var remEmbBtn = Helpers.MakeBtn("Remove Selected",   Helpers.BtnRed,  Color.White, (s, e) => RemoveEmbed(),   0, 24);
            addEmbBtn.Left = 0;                   addEmbBtn.Top = 4;
            remEmbBtn.Left = addEmbBtn.Width + 16; remEmbBtn.Top = 4;
            embBot.Controls.AddRange(new Control[] { addEmbBtn, remEmbBtn });

            embPanel.Controls.Add(_embList);
            embPanel.Controls.Add(embLbl);
            embPanel.Controls.Add(embBot);
            tabEmb.Controls.Add(embPanel);
            nb.TabPages.Add(tabEmb);

            foreach (TabPage page in nb.TabPages)
            {
                page.BackColor = IDE.IDETheme.CanvasBack;
                page.ForeColor = IDE.IDETheme.Fore;
            }

            // ── Buttons ───────────────────────────────────────────────────────
            var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 38, Padding = new Padding(6, 4, 6, 4) };
            var btnOk    = Helpers.MakeBtn("OK",     Helpers.BtnBlue, Color.White, (s, e) => DoOk(),  80);
            var btnCx    = Helpers.MakeBtn("CANCEL", Color.DimGray,   Color.White, (s, e) => Close(), 80);
            btnOk.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            btnCx.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            btnOk.Top    = btnCx.Top = 6;
            btnPanel.Controls.AddRange(new Control[] { btnOk, btnCx });
            btnPanel.Resize += (s, e) =>
            {
                btnOk.Left = btnPanel.Width - 172;
                btnCx.Left = btnPanel.Width - 86;
            };

            Controls.Add(nb);
            Controls.Add(btnPanel);
            Helpers.CenterOnParent(this, parent);
        }

        static void AddGenRow(TableLayoutPanel t, int r, string lbl, Control ctl)
        {
            t.Controls.Add(new Label
            {
                Text    = lbl,
                AutoSize = true,
                Padding = new Padding(0, 6, 6, 0),
                Anchor  = AnchorStyles.Left,
            }, 0, r);
            t.Controls.Add(ctl, 1, r);
        }

        void AddDll()
        {
            var v = _dllEntry.Text.Trim();
            if (!string.IsNullOrEmpty(v) && v != "e.g. System.Net.Http.dll")
            {
                _dllList.Items.Add(v);
                _dllEntry.Text = "";
            }
        }

        void RemoveDll()
        {
            if (_dllList.SelectedIndex >= 0) _dllList.Items.RemoveAt(_dllList.SelectedIndex);
        }

        void BrowseEmbed()
        {
            using (var dlg = new OpenFileDialog
            {
                Multiselect = true,
                Title       = "Select files to embed",
                Filter      = "All files|*.*",
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                var existing = new HashSet<string>(_embList.Items.Cast<string>());
                foreach (var f in dlg.FileNames)
                    if (existing.Add(f)) _embList.Items.Add(f);
            }
        }

        void RemoveEmbed()
        {
            if (_embList.SelectedIndex >= 0) _embList.Items.RemoveAt(_embList.SelectedIndex);
        }

        void DoOk()
        {
            int w, h;
            if (!int.TryParse(_widthBox.Text, out w) || !int.TryParse(_heightBox.Text, out h))
            {
                MessageBox.Show("Width and Height must be integers.", "Invalid",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var name = _nameBox.Text.Trim();
            if (string.IsNullOrEmpty(name)) name = "MyProject";
            Result = new ProjectModel
            {
                Name          = name,
                FormWidth     = w,
                FormHeight    = h,
                Resizable     = _resizable.Checked,
                VStyle        = _VStyles.Checked,
                ExtraDlls     = new List<string>(_dllList.Items.Cast<string>()),
                EmbeddedFiles = new List<string>(_embList.Items.Cast<string>()),
            };
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
