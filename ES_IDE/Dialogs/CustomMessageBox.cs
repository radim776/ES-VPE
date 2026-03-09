using System;
using System.Drawing;
using System.Windows.Forms;
using System.Media;
using EventScriptIDE;

public static class CustomMessageBox
{
    public static DialogResult Show(string text, string caption = "", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None)
    {
        using (var form = new Form())
        {
            form.Text = caption;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.ClientSize = new Size(400, 150);
            form.BackColor = IDE.IDETheme.Back;
            form.ForeColor = IDE.IDETheme.Fore;
            form.Font = new Font("Arial", 9F, FontStyle.Regular, GraphicsUnit.Point);
            
            var mainPanel = new Panel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Padding = new Padding(10);
            form.Controls.Add(mainPanel);

            PictureBox pic = null;
            int iconOffset = 0;

            if (icon != MessageBoxIcon.None)
            {
                pic = new PictureBox();
                pic.SizeMode = PictureBoxSizeMode.CenterImage;
                pic.Size = new Size(32, 32);
                pic.Location = new Point(15, 23);
                switch (icon)
                {
                    case MessageBoxIcon.Information:
                        pic.Image = SystemIcons.Information.ToBitmap();
                        SystemSounds.Asterisk.Play();
                        break;
                    case MessageBoxIcon.Warning:
                        pic.Image = SystemIcons.Warning.ToBitmap();
                        SystemSounds.Exclamation.Play();
                        break;
                    case MessageBoxIcon.Error:
                        pic.Image = SystemIcons.Error.ToBitmap();
                        SystemSounds.Hand.Play();
                        break;
                    case MessageBoxIcon.Question:
                        pic.Image = SystemIcons.Question.ToBitmap();
                        SystemSounds.Question.Play();
                        break;
                }
                mainPanel.Controls.Add(pic);
                iconOffset = pic.Right + 10;
            }
            else
            {
                iconOffset =  10;
            }
            
            var lbl = new Label();
            lbl.Text = text;
            lbl.ForeColor = form.ForeColor;
            lbl.AutoSize = false;
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            lbl.Location = new Point(iconOffset, 10);
            lbl.Size = new Size(form.ClientSize.Width - iconOffset - 10, form.ClientSize.Height - 60);
            mainPanel.Controls.Add(lbl);
            
            var panel = new FlowLayoutPanel();
            panel.FlowDirection = FlowDirection.RightToLeft;
            panel.Dock = DockStyle.Bottom;
            panel.Height = 40;
            panel.Padding = new Padding(10,7,10,10);
            panel.BackColor = IDE.IDETheme.CanvasBack;
            form.Controls.Add(panel);

            void AddButton(string btnText, DialogResult result)
            {
                var btn = Helpers.MakeBtn(btnText, Helpers.BtnDarkGray, Color.White, (object e, EventArgs a) => { }, 64, 26);
                //var btn = new Button();
                //btn.Text = btnText;
                btn.DialogResult = result;
                //btn.BackColor = Color.FromArgb(50, 50, 50);
                //btn.ForeColor = Color.White;
                //btn.FlatStyle = FlatStyle.Flat;
                //btn.FlatAppearance.BorderColor = Color.Gray;
                btn.Padding = new Padding(6);
                btn.Margin = new Padding(5, 0, 0, 0);
                panel.Controls.Add(btn);
                if (form.AcceptButton == null) form.AcceptButton = btn;
                if (form.CancelButton == null) form.CancelButton = btn;
            }
            
            switch (buttons)
            {
                case MessageBoxButtons.OK:
                    AddButton("OK", DialogResult.OK);
                    break;
                case MessageBoxButtons.OKCancel:
                    AddButton("CANCEL", DialogResult.Cancel);
                    AddButton("OK", DialogResult.OK);
                    break;
                case MessageBoxButtons.YesNo:
                    AddButton("NO", DialogResult.No);
                    AddButton("YES", DialogResult.Yes);
                    break;
                case MessageBoxButtons.YesNoCancel:
                    AddButton("CANCEL", DialogResult.Cancel);
                    AddButton("NO", DialogResult.No);
                    AddButton("YES", DialogResult.Yes);
                    break;
            }

            return form.ShowDialog();
        }
    }
}