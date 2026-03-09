using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace EventScriptIDE.Dialogs
{
    public class CodeViewDialog : Form
    {
        public CodeViewDialog(Form parent, string code)
        {
            Text = "Generated VB.NET Code";
            Size = new Size(860, 680);
            MinimumSize = new Size(600, 400);
            StartPosition = FormStartPosition.Manual;
            Font = new Font("Segoe UI", 9f);
            BackColor = IDE.IDETheme.Back;
            ForeColor = IDE.IDETheme.Fore;

            // ── Toolbar ──────────────────────────────────────────────────────
            var bar = new Panel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(4, 8, 4, 0) };

            var copyBtn  = Helpers.MakeBtn("COPY TO CLIPBOARD", Color.DimGray, Color.White,(s, e) => DoCopy(code), 0, 26);
            var saveBtn  = Helpers.MakeBtn("SAVE AS...", Color.DimGray, Color.White,(s, e) => DoSave(code), 0, 26);
            saveBtn.Left = copyBtn.Width + 96;

            /*var closeBtn = new Button
            {
                Text   = "CLOSE",
                Width  = 70,
                Height = 26,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
            };
            closeBtn.Click += (s, e) => Close();*/
            var closeBtn = Helpers.MakeBtn("CLOSE", Color.DimGray, Color.White, (s, e) => Close(), 0, 26);
            closeBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;

            bar.Controls.AddRange(new Control[] { copyBtn, saveBtn, closeBtn });
            bar.Resize += (s, e) => closeBtn.Left = bar.Width - 78;
            bar.Top = 4;
            bar.Left = 4;

            // ── Code area ────────────────────────────────────────────────────
            var txt = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Courier New", 10f),
                WordWrap = false,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Both,
                Text = code,
                BackColor = IDE.IDETheme.CanvasBack,
                ForeColor = IDE.IDETheme.HeaderFore
            };

            Controls.Add(txt);
            Controls.Add(bar);

            Helpers.CenterOnParent(this, parent);
        }

        void DoCopy(string code)
        {
            Clipboard.SetText(code);
            MessageBox.Show("Code copied to clipboard.", "Copied",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        void DoSave(string code)
        {
            using (var dlg = new SaveFileDialog
            {
                DefaultExt = ".vb",
                Filter     = "VB.NET source|*.vb|All files|*.*",
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                File.WriteAllText(dlg.FileName, code, Encoding.UTF8);
                MessageBox.Show("Saved to:\n" + dlg.FileName, "Saved",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
