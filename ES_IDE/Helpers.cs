using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace EventScriptIDE
{
    public static class Helpers
    {
        // ── Dictionary helper (replaces .NET Core GetValueOrDefault) ──────────
        public static TValue GetOrDefault<TKey, TValue>(
            this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default(TValue))
        {
            TValue v;
            return dict.TryGetValue(key, out v) ? v : defaultValue;
        }

        // ── Auto-naming helpers ───────────────────────────────────────────────
        public static string AutoGroupName(ProjectModel project)
        {
            var existing = new HashSet<string>();
            foreach (var g in project.EventGroups) existing.Add(g.Name);
            for (var i = 1; ; i++)
            {
                var name = "Group " + i;
                if (!existing.Contains(name)) return name;
            }
        }

        public static string AutoEventName(EventGroup group)
        {
            var existing = new HashSet<string>();
            foreach (var e in group.Events) existing.Add(e.Name);
            for (var i = 1; ; i++)
            {
                var name = "Event " + i;
                if (!existing.Contains(name)) return name;
            }
        }

        // ── Dialog centering ──────────────────────────────────────────────────
        public static void CenterOnParent(Form dialog, Form parent)
        {
            var px = parent.Location.X + (parent.Width  - dialog.Width)  / 2;
            var py = parent.Location.Y + (parent.Height - dialog.Height) / 2;
            dialog.Location = new Point(Math.Max(0, px), Math.Max(0, py));
        }

        public static void CenterOnOwner2(Form dialog, Form owner)
        {
            if (owner == null)
            {
                dialog.StartPosition = FormStartPosition.CenterScreen;
                return;
            }

            dialog.StartPosition = FormStartPosition.Manual;

            // Wait until the dialog is loaded to get correct size
            dialog.Load += (s, e) =>
            {
                // Get the screen containing the owner
                Screen screen = Screen.FromControl(owner);

                int x = owner.Left + (owner.Width - dialog.Width) / 2;
                int y = owner.Top + (owner.Height - dialog.Height) / 2;

                // Make sure it stays fully on-screen
                x = Math.Max(screen.Bounds.X, Math.Min(x, screen.Bounds.Right - dialog.Width));
                y = Math.Max(screen.Bounds.Y, Math.Min(y, screen.Bounds.Bottom - dialog.Height));

                dialog.Location = new Point(x, y);
            };
        }
        public static Font BtnFont = new Font("Verdana", 9f, FontStyle.Bold);
        public static Font TbFont = new Font("Arial", 9f, FontStyle.Regular);
        // ── UI factory helpers ────────────────────────────────────────────────
        public static Button MakeBtn(string text, Color startColor, Color fg, EventHandler handler, int width = 0, int height = 25)
        {
            var btn = new Button
            {
                Text = text,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Font = BtnFont,
                Cursor = Cursors.Hand,
                Height = height,
                UseVisualStyleBackColor = false,
            };
            btn.FlatAppearance.BorderSize = 0;

            if (width > 0) btn.Width = width;
            else btn.AutoSize = true;

            btn.Click += handler;

            // Original end color
            Color endColor = ControlPaint.Dark(startColor, 0.2f);

            // Track hover and click state
            bool isHover = false;
            bool isClicked = false;

            btn.MouseEnter += (s, e) => { isHover = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { isHover = false; isClicked = false; btn.Invalidate(); };
            btn.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) isClicked = true; btn.Invalidate(); };
            btn.MouseUp += (s, e) => { if (e.Button == MouseButtons.Left) isClicked = false; btn.Invalidate(); };

            btn.Paint += (s, e) =>
            {
                var g = e.Graphics;
                var rect = btn.ClientRectangle;

                Color paintStart = startColor;
                Color paintEnd = endColor;

                if (isHover && !isClicked)
                {
                    paintStart = ControlPaint.Light(paintStart, 0.2f);
                    paintEnd = ControlPaint.Light(paintEnd, 0.2f);
                }

                if (isClicked)
                {
                    var temp = paintStart;
                    paintStart = paintEnd;
                    paintEnd = temp;
                }

                // Gradient background
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    rect,
                    paintStart,
                    paintEnd,
                    System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                {
                    g.FillRectangle(brush, rect);
                }

                // ----- 3D highlight/shadow -----

                Color topHighlight = ControlPaint.Light(startColor, 0.6f);
                Color bottomShadow = ControlPaint.Dark(endColor, 0.6f);

                if (isClicked)
                {
                    var tmp = topHighlight;
                    topHighlight = bottomShadow;
                    bottomShadow = tmp;
                }

                using (var topPen = new Pen(topHighlight))
                using (var bottomPen = new Pen(bottomShadow))
                {
                    // top highlight
                    g.DrawLine(topPen, 0, 0, rect.Width - 1, 0);

                    // bottom shadow
                    g.DrawLine(bottomPen, 0, rect.Height - 1, rect.Width - 1, rect.Height - 1);
                }

                // Optional side shading
                using (var sideShadow = new Pen(ControlPaint.Dark(endColor, 0.4f)))
                {
                    g.DrawLine(sideShadow, rect.Width - 1, 0, rect.Width - 1, rect.Height - 1);
                }

                // ----- pressed offset -----

                var textRect = rect;
                if (isClicked)
                    textRect.Offset(1, 1);

                TextRenderer.DrawText(
                    g,
                    btn.Text,
                    btn.Font,
                    textRect,
                    btn.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                );
            };

            return btn;
        }

        // ── Colour palette ────────────────────────────────────────────────────
        /*
        public static Color GroupHeaderBg = ColorTranslator.FromHtml("#0d2b6e");
        public static Color GroupBodyBg = ColorTranslator.FromHtml("#c9d6f5");
        public static Color EventHeaderBg = ColorTranslator.FromHtml("#1e40af");
        public static Color EventBodyBg = ColorTranslator.FromHtml("#e3eaff");
        public static Color CondBg = ColorTranslator.FromHtml("#e8f5e9");
        public static Color CondFg = ColorTranslator.FromHtml("#1b5e20");
        public static Color ActBg = ColorTranslator.FromHtml("#f3e5f5");
        public static Color ActFg = ColorTranslator.FromHtml("#4a148c");*/
        public static Color GroupHeaderBg = ColorTranslator.FromHtml("#1f3a8a");
        public static Color GroupBodyBg = ColorTranslator.FromHtml("#1e293b");

        public static Color EventHeaderBg = ColorTranslator.FromHtml("#1d4ed8");
        public static Color EventBodyBg = ColorTranslator.FromHtml("#1e293b");

        public static Color CondBg = ColorTranslator.FromHtml("#1b4332");
        public static Color CondFg = ColorTranslator.FromHtml("#a7f3d0");

        public static Color ActBg = ColorTranslator.FromHtml("#3b0764");
        public static Color ActFg = ColorTranslator.FromHtml("#e9d5ff");
        public static readonly Color FormBg = ColorTranslator.FromHtml("#f5f5f5");
        public static readonly Color BtnBlue = ColorTranslator.FromHtml("#1565c0");
        public static readonly Color BtnGreen = ColorTranslator.FromHtml("#1e7d22");
        public static readonly Color BtnRed = ColorTranslator.FromHtml("#c62828");
        public static readonly Color BtnPurple = ColorTranslator.FromHtml("#6a1b9a");
        public static readonly Color BtnDarkBlue = ColorTranslator.FromHtml("#1e3a8a");
        public static readonly Color BtnCondGreen = ColorTranslator.FromHtml("#388e3c");
        public static readonly Color BtnActPurple = ColorTranslator.FromHtml("#7b1fa2");
        public static readonly Color BtnDarkGray = ColorTranslator.FromHtml("#555555");

        // ── Param widget builder (shared between dialogs) ─────────────────────
        public static Dictionary<string, Func<string>> BuildParamWidgets(
            TableLayoutPanel panel, List<ParamDef> defs, Dictionary<string, string> initial)
        {
            panel.Controls.Clear();
            panel.RowCount = 0;
            panel.RowStyles.Clear();

            var getters = new Dictionary<string, Func<string>>();
            var visible = new List<ParamDef>();
            foreach (var pd in defs)
                if (string.IsNullOrEmpty(pd.Import)) visible.Add(pd);

            if (visible.Count == 0)
            {
                panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                panel.RowCount = 1;
                var lbl = new Label { Text = "(no parameters)", ForeColor = Color.Gray, AutoSize = true };
                panel.Controls.Add(lbl, 0, 0);
                panel.SetColumnSpan(lbl, 2);
                return getters;
            }

            panel.ColumnCount = 2;
            panel.ColumnStyles.Clear();
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            for (var i = 0; i < visible.Count; i++)
            {
                var pd = visible[i];
                var pname = pd.Name;
                string saved;
                if (!initial.TryGetValue(pname, out saved)) saved = "";
                var value = saved != "" ? saved : pd.Default;

                panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                panel.RowCount = i + 1;

                var lblText = pd.Label + (pd.Required ? "*" : "") + ":";
                var lbl = new Label
                {
                    Text      = lblText,
                    AutoSize  = true,
                    Anchor    = AnchorStyles.Left,
                    Padding   = new Padding(0, 4, 6, 0)
                };
                panel.Controls.Add(lbl, 0, i);

                if (pd.Kind == "select")
                {
                    var vals = pd.Values;
                    var displays = new List<string>(vals.Values);
                    string curDisp;
                    if (!vals.TryGetValue(value, out curDisp))
                        curDisp = displays.Count > 0 ? displays[0] : "";

                    var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, BackColor=IDE.IDETheme.ListBack,ForeColor=IDE.IDETheme.Fore };
                    combo.Items.AddRange(displays.ToArray());
                    if (!string.IsNullOrEmpty(curDisp) && combo.Items.Contains(curDisp))
                        combo.SelectedItem = curDisp;
                    else if (combo.Items.Count > 0)
                        combo.SelectedIndex = 0;
                    panel.Controls.Add(combo, 1, i);

                    var keyBox = value;
                    combo.SelectedIndexChanged += (s2, e2) =>
                    {
                        var sel = combo.SelectedItem != null ? combo.SelectedItem.ToString() : "";
                        foreach (var kvp in vals)
                        {
                            if (kvp.Value == sel) { keyBox = kvp.Key; break; }
                        }
                    };
                    var capturedKey = pname; // capture for closure
                    getters[pname] = () => keyBox;
                }
                else if (pd.Kind == "bool")
                {
                    var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80, BackColor = IDE.IDETheme.ListBack, ForeColor = IDE.IDETheme.Fore };
                    combo.Items.AddRange(new object[] { "True", "False" });
                    combo.SelectedItem = (value == "False") ? "False" : "True";
                    panel.Controls.Add(combo, 1, i);
                    getters[pname] = () => combo.SelectedItem != null ? combo.SelectedItem.ToString() : "True";
                }
                else if (pd.Kind == "num" || pd.Kind == "numint")
                {
                    var capturedKind = pd.Kind;
                    var tb = new TextBox
                    {
                        Text = value,
                        Dock = DockStyle.Fill,
                        BackColor = IDE.IDETheme.CanvasBack,
                        ForeColor = IDE.IDETheme.HeaderFore,
                        BorderStyle = BorderStyle.FixedSingle,
                        Font=Helpers.TbFont
                    };
                    tb.TextChanged += (s2, e2) =>
                    {
                        var v = tb.Text;
                        if (v != "" && v != "-")
                        {
                            bool ok;
                            if (capturedKind == "num")
                            {
                                double d;
                                ok = double.TryParse(v, out d);
                            }
                            else
                            {
                                int d;
                                ok = int.TryParse(v, out d);
                            }
                            if (!ok && tb.Text.Length > 0)
                                tb.Text = tb.Text.Substring(0, tb.Text.Length - 1);
                        }
                    };
                    panel.Controls.Add(tb, 1, i);
                    getters[pname] = () => tb.Text;
                }
                else
                {
                    var tb = new TextBox {
                        Text = value,
                        Dock = DockStyle.Fill,
                        BackColor = IDE.IDETheme.CanvasBack,
                        ForeColor = IDE.IDETheme.HeaderFore,
                        BorderStyle=BorderStyle.FixedSingle,
                        Font = Helpers.TbFont
                    };
                    panel.Controls.Add(tb, 1, i);
                    getters[pname] = () => tb.Text;
                }
            }
            panel.Controls.Add(new Panel());
            return getters;
        }
    }
}
