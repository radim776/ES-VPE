using System.Drawing;
using System.Windows.Forms;

namespace EventScriptIDE.Dialogs
{
    public class ControlDialog : Form
    {
        public ControlModel Result { get; private set; }

        private readonly TextBox   _nameBox;
        private readonly ComboBox  _typeBox;
        private readonly TextBox   _textBox;
        private readonly TextBox   _xBox;
        private readonly TextBox   _yBox;
        private readonly TextBox   _wBox;
        private readonly TextBox   _hBox;
        private readonly Control[] _visualOnlyControls;

        public ControlDialog(Form parent, ControlModel initial = null)
        {
            Text            = "CONTROL";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            StartPosition   = FormStartPosition.Manual;
            ClientSize      = new Size(340, 260);
            Font            = new Font("Segoe UI", 9f);
            BackColor = IDE.IDETheme.Back;
            ForeColor = IDE.IDETheme.Fore;

            var table = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount    = 7,
                Dock        = DockStyle.None,
                Left        = 12,
                Top         = 12,
                Width       = 310,
                Height      = 200,
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (var i = 0; i < 7; i++)
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _nameBox = new TextBox { Text = initial != null ? initial.Name : "",  Dock = DockStyle.Fill,
                Font = Helpers.TbFont,
                BackColor = IDE.IDETheme.CanvasBack,
                ForeColor = IDE.IDETheme.HeaderFore,
                BorderStyle = BorderStyle.FixedSingle,
            };
            _typeBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList,Dock = DockStyle.Fill,
                Font = Helpers.TbFont,
                BackColor = IDE.IDETheme.CanvasBack,
                ForeColor = IDE.IDETheme.HeaderFore,
                FlatStyle = FlatStyle.Flat
            };
            _typeBox.Items.AddRange(BuiltinDefinitions.ControlTypes);
            _typeBox.SelectedItem = initial != null ? initial.Type : "Button";
            if (_typeBox.SelectedIndex < 0 && _typeBox.Items.Count > 0) _typeBox.SelectedIndex = 0;
            _textBox = new TextBox { Text = initial != null ? initial.Text : "", Dock = DockStyle.Fill,
                BackColor = IDE.IDETheme.CanvasBack,
                ForeColor = IDE.IDETheme.HeaderFore,
                BorderStyle = BorderStyle.FixedSingle,
            };
            _xBox    = new TextBox { Text = (initial != null ? initial.X   : 10).ToString(), Width = 80, Font = Helpers.TbFont,
                BackColor = IDE.IDETheme.CanvasBack,
                ForeColor = IDE.IDETheme.HeaderFore,
                BorderStyle = BorderStyle.FixedSingle,
            };
            _yBox    = new TextBox { Text = (initial != null ? initial.Y   : 10).ToString(), Width = 80, Font = Helpers.TbFont,
                BackColor = IDE.IDETheme.CanvasBack,
                ForeColor = IDE.IDETheme.HeaderFore,
                BorderStyle = BorderStyle.FixedSingle,
            };
            _wBox    = new TextBox { Text = (initial != null ? initial.W   : 100).ToString(), Width = 80, Font = Helpers.TbFont,
                BackColor = IDE.IDETheme.CanvasBack,
                ForeColor = IDE.IDETheme.HeaderFore,
                BorderStyle = BorderStyle.FixedSingle,
            };
            _hBox    = new TextBox { Text = (initial != null ? initial.H   : 30).ToString(), Width = 80 ,Font=Helpers.TbFont,
                BackColor = IDE.IDETheme.CanvasBack,
                ForeColor = IDE.IDETheme.HeaderFore,
                BorderStyle = BorderStyle.FixedSingle,
            };

            AddRow(table, 0, "Name:",   _nameBox);
            AddRow(table, 1, "Type:",   _typeBox);
            AddRow(table, 2, "Text:",   _textBox);
            AddRow(table, 3, "X:",      _xBox);
            AddRow(table, 4, "Y:",      _yBox);
            AddRow(table, 5, "Width:",  _wBox);
            AddRow(table, 6, "Height:", _hBox);
            Controls.Add(table);

            _visualOnlyControls = new Control[] { _textBox, _xBox, _yBox, _wBox, _hBox };
            _typeBox.SelectedIndexChanged += (s, e) => UpdateVisualFields();
            UpdateVisualFields();

            var btnOk = Helpers.MakeBtn("OK",     Helpers.BtnBlue,  Color.White, (s, e) => DoOk(),   80);
            var btnCx = Helpers.MakeBtn("CANCEL", Color.DimGray,    Color.White, (s, e) => Close(),  80);
            btnOk.Left = ClientSize.Width - 170; btnOk.Top = ClientSize.Height - 36;
            btnCx.Left = ClientSize.Width - 85;  btnCx.Top = ClientSize.Height - 36;
            Controls.AddRange(new Control[] { btnOk, btnCx });

            Helpers.CenterOnParent(this, parent);
        }

        static void AddRow(TableLayoutPanel t, int r, string lbl, Control ctl)
        {
            t.Controls.Add(new Label
            {
                Text    = lbl,
                AutoSize = true,
                Anchor  = AnchorStyles.Left,
                Padding = new Padding(0, 5, 6, 0)
            }, 0, r);
            t.Controls.Add(ctl, 1, r);
        }

        void UpdateVisualFields()
        {
            var isNonVis = BuiltinDefinitions.NonVisualControls.Contains(
                _typeBox.SelectedItem != null ? _typeBox.SelectedItem.ToString() : "");
            foreach (var c in _visualOnlyControls) c.Enabled = !isNonVis;
        }

        void DoOk()
        {
            var name = _nameBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                CustomMessageBox.Show("control name required", "Missing Name",MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var ctype = _typeBox.SelectedItem != null ? _typeBox.SelectedItem.ToString() : "Button";
            if (BuiltinDefinitions.NonVisualControls.Contains(ctype))
            {
                Result = new ControlModel { Name = name, Type = ctype };
                DialogResult = DialogResult.OK;
                Close();
                return;
            }
            int x, y, w, h;
            if (!int.TryParse(_xBox.Text, out x) || !int.TryParse(_yBox.Text, out y)
                || !int.TryParse(_wBox.Text, out w) || !int.TryParse(_hBox.Text, out h))
            {
                CustomMessageBox.Show("X , Y , Width , Height must be integers", "Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (w <= 0 || h <= 0)
            {
                CustomMessageBox.Show("Width and Height must be positive", "Invalid Size", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Result = new ControlModel
            {
                Name = name, Type = ctype,
                Text = _textBox.Text,
                X = x, Y = y, W = w, H = h,
            };
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
