using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EventScriptIDE.Dialogs
{
    public class TriggerDialog : Form
    {
        public TriggerInfo Result { get; private set; }

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private readonly ListBox          _trigList;
        private readonly TableLayoutPanel _paramsPanel;
        private readonly Dictionary<string, List<ParamDef>> _allTriggers;
        private Dictionary<string, Func<string>> _getters = new Dictionary<string, Func<string>>();

        public TriggerDialog(IDE parent, TriggerInfo initial = null)
        {
            Text            = "SET TRIGGER";
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize     = new Size(540, 280);
            Size            = new Size(620, 340);
            StartPosition   = FormStartPosition.Manual;
            Font            = new Font("Segoe UI", 9f);
            BackColor = IDE.IDETheme.Back;
            ForeColor = IDE.IDETheme.Fore;

            _allTriggers = ExtensionRegistry.GetAllTriggers();

            using (Bitmap bmp = new Bitmap(parent.EventIcon))
            {
                IntPtr hIcon = bmp.GetHicon();

                using (Icon temp = Icon.FromHandle(hIcon))
                {
                    this.Icon = (Icon)temp.Clone();
                }

                DestroyIcon(hIcon);
            }

            // ── Layout ───────────────────────────────────────────────────────
            var body = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount    = 1,
                Dock        = DockStyle.Fill,
                Padding     = new Padding(6),
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var trigBox = new GroupBox { Text = "TRIGGER", Dock = DockStyle.Fill, ForeColor = IDE.IDETheme.Fore };
            _trigList = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f), BackColor = IDE.IDETheme.Back, ForeColor = IDE.IDETheme.Fore, BorderStyle = BorderStyle.None };
            trigBox.Controls.Add(_trigList);
            body.Controls.Add(trigBox, 0, 0);

            var paramBox  = new GroupBox { Text = "PARAMETERS", Dock = DockStyle.Fill, ForeColor = IDE.IDETheme.Fore };
            _paramsPanel  = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
            paramBox.Controls.Add(_paramsPanel);
            body.Controls.Add(paramBox, 1, 0);

            // ── Buttons ──────────────────────────────────────────────────────
            var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 38, Padding = new Padding(4) };
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

            Controls.Add(body);
            Controls.Add(btnPanel);

            // ── Populate ─────────────────────────────────────────────────────
            foreach (var t in _allTriggers.Keys) _trigList.Items.Add(t);
            _trigList.SelectedIndexChanged += (s, e) => PopulateParams(null);

            var triggers = new List<string>(_allTriggers.Keys);
            if (initial != null && triggers.Contains(initial.Type))
            {
                _trigList.SelectedIndex = triggers.IndexOf(initial.Type);
                PopulateParams(initial.Params);
            }
            else if (triggers.Count > 0)
            {
                _trigList.SelectedIndex = 0;
                PopulateParams(null);
            }

            Helpers.CenterOnParent(this, parent);
        }

        void PopulateParams(Dictionary<string, string> initial)
        {
            var ttype = _trigList.SelectedItem != null ? _trigList.SelectedItem.ToString() : "";
            List<ParamDef> defs;
            if (!_allTriggers.TryGetValue(ttype, out defs))
                defs = new List<ParamDef>();
            _getters = Helpers.BuildParamWidgets(_paramsPanel, defs, initial ?? new Dictionary<string, string>());
        }

        void DoOk()
        {
            var ttype = _trigList.SelectedItem != null ? _trigList.SelectedItem.ToString() : "";
            if (string.IsNullOrEmpty(ttype))
            {
                CustomMessageBox.Show("Select a trigger.", "Missing",MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var parms = new Dictionary<string, string>();
            foreach (var kvp in _getters) parms[kvp.Key] = kvp.Value();
            Result       = new TriggerInfo { Type = ttype, Params = parms };
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
