using System.Drawing;
using System.Windows.Forms;

namespace EventScriptIDE.Dialogs
{
    public class VariableDialog : Form
    {
        public VariableModel Result { get; private set; }

        private readonly TextBox  _nameBox;
        private readonly ComboBox _typeBox;
        private readonly TextBox  _defBox;

        public VariableDialog(Form parent, VariableModel initial = null)
        {
            Text            = "VARIABLE";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            StartPosition   = FormStartPosition.Manual;
            ClientSize      = new Size(340, 160);
            Font            = new Font("Segoe UI", 9f);
            BackColor = IDE.IDETheme.Back;
            ForeColor = IDE.IDETheme.Fore;

            var table = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount    = 3,
                Dock        = DockStyle.None,
                Left        = 12,
                Top         = 12,
                Width       = 310,
                Height      = 100,
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (var i = 0; i < 3; i++)
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _nameBox = new TextBox
            {
                Text = initial != null ? initial.Name : "",
                Dock = DockStyle.Fill,
                Font = Helpers.TbFont,
                BackColor = IDE.IDETheme.CanvasBack,
                ForeColor = IDE.IDETheme.HeaderFore,
                BorderStyle = BorderStyle.FixedSingle,
            };
            _defBox = new TextBox
            {
                Text = initial != null ? initial.Default : "",
                Dock = DockStyle.Fill,
                Font = Helpers.TbFont,
                BackColor = IDE.IDETheme.CanvasBack,
                ForeColor = IDE.IDETheme.HeaderFore,
                BorderStyle = BorderStyle.FixedSingle,
            };
            _typeBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill,
                Font = Helpers.TbFont,
                BackColor = IDE.IDETheme.CanvasBack,
                ForeColor = IDE.IDETheme.HeaderFore,
                FlatStyle = FlatStyle.Flat
            };
            _typeBox.Items.AddRange(new object[] { "String", "Integer", "Double", "Boolean", "Long", "Single","List(Of String)", "List(Of Integer)" });
            _typeBox.SelectedItem = initial != null ? initial.Type : "String";
            if (_typeBox.SelectedIndex < 0) _typeBox.SelectedIndex = 0;

            AddRow(table, 0, "Name:", _nameBox);
            AddRow(table, 1, "Type:", _typeBox);
            AddRow(table, 2, "Default Value:", _defBox);
            Controls.Add(table);

            var btnOk = Helpers.MakeBtn("OK",     Helpers.BtnBlue, Color.White, (s, e) => DoOk(),  80);
            var btnCx = Helpers.MakeBtn("CANCEL", Color.DimGray,   Color.White, (s, e) => Close(), 80);
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
                Padding = new Padding(0, 5, 6, 0),
            }, 0, r);
            t.Controls.Add(ctl, 1, r);
        }

        void DoOk()
        {
            var name = _nameBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                CustomMessageBox.Show("Variable name required", "Missing",MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Result = new VariableModel
            {
                Name    = name,
                Type    = _typeBox.SelectedItem != null ? _typeBox.SelectedItem.ToString() : "String",
                Default = _defBox.Text,
            };
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
