using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace EventScriptIDE.Dialogs
{
    /// <summary>
    /// Simple visual form designer.
    /// Drag controls to move; drag the blue bottom-right handle to resize.
    /// </summary>
    public class VisualDesignerDialog : Form
    {
        const int HANDLE = 10;

        public List<ControlModel> Result { get; private set; }

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private readonly List<ControlModel> _controls;
        private readonly int   _fw, _fh;
        private readonly PictureBox _canvas;

        private int   _selected  = -1;
        private bool  _resizing  = false;
        private Point _dragStart;

        private readonly Brush _brushFillSelected = new SolidBrush(Color.FromArgb(0xe8, 0xf4, 0xff));
        private readonly Brush _brushFillNormal = new SolidBrush(Color.FromArgb(0xf0, 0xf0, 0xf0));
        private readonly Pen _penSelected = new Pen(Color.FromArgb(0x15, 0x65, 0xc0), 2);
        private readonly Pen _penNormal = new Pen(Color.FromArgb(0x88, 0x88, 0x88), 1);
        private readonly Font _fontLabel = new Font("Segoe UI", 8f);
        private AppSettings setting;
        private int _origX, _origY, _origW, _origH;

        public VisualDesignerDialog(IDE parent, ProjectModel project)
        {
            setting = SettingsManager.Load();
            Text = "VISUAL DESIGNER";
            MinimumSize = new Size(800, 600);
            Size = new Size(1060, 740);
            StartPosition = FormStartPosition.Manual;
            Font = new Font("Segoe UI", 9f);

            using (Bitmap bmp = new Bitmap(parent.ControlIcon))
            {
                IntPtr hIcon = bmp.GetHicon();

                using (Icon temp = Icon.FromHandle(hIcon))
                {
                    this.Icon = (Icon)temp.Clone();
                }

                DestroyIcon(hIcon);
            }
            
            var json = JsonConvert.SerializeObject(project.Controls);
            _controls = JsonConvert.DeserializeObject<List<ControlModel>>(json)
                           ?? new List<ControlModel>();
            _fw = project.FormWidth;
            _fh = project.FormHeight;

            // Toolbar
            var bar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = SystemColors.Control,
                Padding = new Padding(6, 4, 6, 0),
            };
            bar.Controls.Add(new Label
            {
                Text = "Visual Designer  ·  drag to move  ·  drag blue corner to resize",
                AutoSize = true,
                Left = 4, Top = 7,
            });
            var btnOk = Helpers.MakeBtn("OK", Helpers.BtnGreen, Color.White,
                (s, e) =>
                {
                    Result = _controls;

                    //update the project model directly
                    project.Controls.Clear();
                    project.Controls.AddRange(_controls);

                    Close();
                }, 80);
            var btnCx = Helpers.MakeBtn("CANCEL", Color.DimGray,Color.White,(s, e) => Close(), 80);
            btnOk.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnCx.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnOk.Top    = btnCx.Top = 4;
            bar.Controls.AddRange(new Control[] { btnOk, btnCx });
            bar.Resize += (s, e) =>
            {
                btnOk.Left = bar.Width - 172;
                btnCx.Left = bar.Width - 86;
            };

            // Scrollable canvas
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.DimGray,
            };
            _canvas = new PictureBox
            {
                Width = _fw,
                Height = _fh,
                Left = 10,
                Top = 10,
                BackColor = Color.White,
            };
            _canvas.Paint += DrawCanvas;
            _canvas.MouseDown += MouseDown2;
            _canvas.MouseMove+= MouseMove2;
            _canvas.MouseUp+= MouseUp2;
            _canvas.Cursor= Cursors.Arrow;
            scroll.Controls.Add(_canvas);

            Controls.Add(scroll);
            Controls.Add(bar);

            Helpers.CenterOnParent(this, parent);
            _canvas.Invalidate();

            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
              ControlStyles.OptimizedDoubleBuffer |
              ControlStyles.UserPaint, true);
            this.UpdateStyles();
        }

        // Drawing
        //Bitmap ICanvas = new Bitmap(800, 600);
        void DrawCanvas(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;

            // Form boundary
            g.FillRectangle(Brushes.White, 0, 0, _fw, _fh);
            g.DrawRectangle(new Pen(Color.DarkGray, 2), 0, 0, _fw, _fh);

            // Grid dots
            if(!setting.VDO)
            {
                using (var dotBrush = new SolidBrush(Color.LightGray))
                {
                    for (var gx = 0; gx < _fw; gx += 20)
                        for (var gy = 0; gy < _fh; gy += 20)
                            g.FillEllipse(dotBrush, gx - 1, gy - 1, 3, 3);
                }
            }

            for (var i = 0; i < _controls.Count; i++)
            {
                var c = _controls[i];
                if (BuiltinDefinitions.NonVisualControls.Contains(c.Type)) continue;
                DrawControl(g, i, c);
            }
        }

        void DrawControl(Graphics g, int i, ControlModel c)
        {
            var sel  = (i == _selected);

            var fill = sel ? _brushFillSelected : _brushFillNormal;
            var pen = sel ? _penSelected : _penNormal;

            g.FillRectangle(fill, c.X, c.Y, c.W, c.H);
            g.DrawRectangle(pen, c.X, c.Y, c.W, c.H);

            var label = c.Name + "\n(" + c.Type + ")";
            var fmt   = new StringFormat
            {
                Alignment     = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };
            g.DrawString(label, new Font("Verdana", 9f), Brushes.DimGray, new RectangleF(c.X, c.Y, c.W, c.H), fmt);

            if (sel)
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(0x15, 0x65, 0xc0)), c.X + c.W - HANDLE, c.Y + c.H - HANDLE, HANDLE, HANDLE);
            }
        }

        // Mouse interaction

        void MouseDown2(object sender, MouseEventArgs e)
        {
            if (_selected >= 0)
            {
                var c = _controls[_selected];
                if (!BuiltinDefinitions.NonVisualControls.Contains(c.Type))
                {
                    if (e.X >= c.X + c.W - HANDLE && e.X <= c.X + c.W &&
                        e.Y >= c.Y + c.H - HANDLE && e.Y <= c.Y + c.H)
                    {
                        _resizing  = true;
                        _dragStart = e.Location;
                        _origX = c.X; _origY = c.Y; _origW = c.W; _origH = c.H;
                        return;
                    }
                }
            }
            
            var hit = -1;
            for (var i = _controls.Count - 1; i >= 0; i--)
            {
                var c = _controls[i];
                if (BuiltinDefinitions.NonVisualControls.Contains(c.Type)) continue;
                if (e.X >= c.X && e.X <= c.X + c.W && e.Y >= c.Y && e.Y <= c.Y + c.H)
                { hit = i; break; }
            }

            _selected  = hit;
            _resizing  = false;
            if (hit >= 0)
            {
                var c = _controls[hit];
                _dragStart = e.Location;
                _origX = c.X; _origY = c.Y; _origW = c.W; _origH = c.H;
            }
            _canvas.Invalidate();
        }

        void MouseMove2(object sender, MouseEventArgs e)
        {
            if (_selected < 0 || (e.Button & MouseButtons.Left) == 0) return;
            var dx = e.X - _dragStart.X;
            var dy = e.Y - _dragStart.Y;
            var c  = _controls[_selected];
            if (_resizing)
            {
                c.W = Math.Max(10, _origW + dx);
                c.H = Math.Max(10, _origH + dy);
            }
            else
            {
                c.X = Math.Max(0, _origX + dx);
                c.Y = Math.Max(0, _origY + dy);
            }
            _canvas.Invalidate();
        }

        void MouseUp2(object sender, MouseEventArgs e)
        {
            
        }
    }
}
