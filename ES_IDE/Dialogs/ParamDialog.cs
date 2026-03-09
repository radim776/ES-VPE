using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EventScriptIDE.Dialogs
{
    /// <summary>
    /// Three-panel dialog: [Category list] | [Action list] | [Parameters]
    /// Used for both conditions and actions.
    /// </summary>
    public class ParamDialog : Form
    {
        public ItemDefinition Result { get; private set; }

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private readonly Dictionary<string, Dictionary<string, List<ParamDef>>> _defs;
        private readonly ListBox             _catList;
        private readonly ListBox             _actList;
        private readonly TableLayoutPanel    _paramsPanel;
        private readonly ComboBox            _importsCombo;
        private Dictionary<string, Func<string>> _getters = new Dictionary<string, Func<string>>();

        public ParamDialog(IDE parent, string title, Dictionary<string, Dictionary<string, List<ParamDef>>> definitions, ItemDefinition initial = null)
        {
            Text            = title;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize     = new Size(720, 400);
            Size            = new Size(860, 480);
            StartPosition   = FormStartPosition.Manual;
            Font            = new Font("Segoe UI", 9f);
            BackColor = IDE.IDETheme.Back;
            ForeColor = IDE.IDETheme.Fore;

            _defs = definitions;

            using (Bitmap bmp = new Bitmap(parent.VariableIcon))
            {
                IntPtr hIcon = bmp.GetHicon();

                using (Icon temp = Icon.FromHandle(hIcon))
                {
                    this.Icon = (Icon)temp.Clone();
                }

                DestroyIcon(hIcon);
            }

            // ── Three-panel body ─────────────────────────────────────────────
            var body = new TableLayoutPanel
            {
                ColumnCount = 3,
                RowCount    = 1,
                Dock        = DockStyle.Fill,
                Padding     = new Padding(6, 6, 6, 0),
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var catBox = new GroupBox { Text = "CATEGORY", Dock = DockStyle.Fill, ForeColor=IDE.IDETheme.Fore };
            _catList = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f),BackColor=IDE.IDETheme.Back, ForeColor = IDE.IDETheme.Fore,BorderStyle=BorderStyle.None };
            catBox.Controls.Add(_catList);
            body.Controls.Add(catBox, 0, 0);

            var actBox = new GroupBox { Text = "ACTION", Dock = DockStyle.Fill, ForeColor = IDE.IDETheme.Fore };
            _actList = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f), BackColor = IDE.IDETheme.Back, ForeColor = IDE.IDETheme.Fore, BorderStyle = BorderStyle.None };
            actBox.Controls.Add(_actList);
            body.Controls.Add(actBox, 1, 0);

            var pBox = new GroupBox { Text = "PARAMETERS", Dock = DockStyle.Fill, ForeColor = IDE.IDETheme.Fore };
            _paramsPanel = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
            pBox.Controls.Add(_paramsPanel);
            body.Controls.Add(pBox, 2, 0);

            // ── Bottom bar ───────────────────────────────────────────────────
            var bot = new Panel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(6, 4, 6, 4) };
            bot.Controls.Add(new Label { Text = "Imports Key:", AutoSize = true, Left = 4, Top = 12 });

            _importsCombo = new ComboBox { Left = 90, Top = 8, Width = 160 };
            _importsCombo.Items.Add("");
            foreach (var k in ExtensionRegistry.Imports.Keys) _importsCombo.Items.Add(k);
            _importsCombo.SelectedIndex = 0;
            bot.Controls.Add(_importsCombo);
            bot.Controls.Add(new Label
            {
                Text      = "(optional — injects extra code)",
                ForeColor = Color.Gray,
                AutoSize  = true,
                Left = 258, Top = 12,
            });

            var btnOk = Helpers.MakeBtn("OK",     Helpers.BtnBlue, Color.White, (s, e) => DoOk(),  80);
            var btnCx = Helpers.MakeBtn("CANCEL", Color.DimGray,   Color.White, (s, e) => Close(), 80);
            btnOk.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            btnCx.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            btnOk.Top    = btnCx.Top = 8;
            bot.Controls.AddRange(new Control[] { btnOk, btnCx });
            bot.Resize += (s, e) =>
            {
                btnOk.Left = bot.Width - 172;
                btnCx.Left = bot.Width - 86;
            };

            Controls.Add(body);
            Controls.Add(bot);

            // ── Populate categories ──────────────────────────────────────────
            foreach (var cat in _defs.Keys) _catList.Items.Add(cat);
            _catList.SelectedIndexChanged += OnCat;
            _actList.SelectedIndexChanged += (s, e) => PopulateParams(null);

            // ── Pre-select ───────────────────────────────────────────────────
            var cats = new List<string>(_defs.Keys);
            if (initial != null)
            {
                if (_importsCombo.Items.Contains(initial.Imports))
                    _importsCombo.SelectedItem = initial.Imports;
                if (cats.Contains(initial.Category))
                {
                    _catList.SelectedIndex = cats.IndexOf(initial.Category);
                    PopulateActions(initial.Category, initial.Action);
                    PopulateParams(initial.Params);
                }
                else if (cats.Count > 0)
                {
                    _catList.SelectedIndex = 0;
                    PopulateActions(cats[0], null);
                }
            }
            else if (cats.Count > 0)
            {
                _catList.SelectedIndex = 0;
                PopulateActions(cats[0], null);
            }

            Helpers.CenterOnParent(this, parent);
        }

        void OnCat(object sender, EventArgs e)
        {
            var cat = _catList.SelectedItem != null ? _catList.SelectedItem.ToString() : "";
            if (!string.IsNullOrEmpty(cat))
            {
                PopulateActions(cat, null);
                PopulateParams(null);
            }
        }

        void PopulateActions(string cat, string select)
        {
            _actList.Items.Clear();
            Dictionary<string, List<ParamDef>> acts;
            if (!_defs.TryGetValue(cat, out acts)) return;
            foreach (var a in acts.Keys) _actList.Items.Add(a);
            if (_actList.Items.Count > 0)
            {
                if (select != null && _actList.Items.Contains(select))
                    _actList.SelectedItem = select;
                else
                    _actList.SelectedIndex = 0;
            }
        }

        void PopulateParams(Dictionary<string, string> initial)
        {
            var cat = _catList.SelectedItem != null ? _catList.SelectedItem.ToString() : "";
            var act = _actList.SelectedItem != null ? _actList.SelectedItem.ToString() : "";

            Dictionary<string, List<ParamDef>> catDefs;
            List<ParamDef> defs;
            if (!_defs.TryGetValue(cat, out catDefs) || !catDefs.TryGetValue(act, out defs))
                defs = new List<ParamDef>();

            _getters = Helpers.BuildParamWidgets(_paramsPanel, defs, initial ?? new Dictionary<string, string>());
        }

        void DoOk()
        {
            var cat = _catList.SelectedItem != null ? _catList.SelectedItem.ToString() : "";
            var act = _actList.SelectedItem != null ? _actList.SelectedItem.ToString() : "";
            if (string.IsNullOrEmpty(cat) || string.IsNullOrEmpty(act))
            {
                CustomMessageBox.Show("Select a category and action", "Missing",MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var parms = new Dictionary<string, string>();
            foreach (var kvp in _getters) parms[kvp.Key] = kvp.Value();
            Result = new ItemDefinition
            {
                Category = cat,
                Action   = act,
                Params   = parms,
                Imports  = _importsCombo.SelectedItem != null
                           ? _importsCombo.SelectedItem.ToString().Trim() : "",
            };
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
