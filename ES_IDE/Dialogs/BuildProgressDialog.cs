using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace EventScriptIDE.Dialogs
{
    public class BuildProgressDialog : Form
    {
        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private readonly Label _statusLbl;
        private readonly RichTextBox _detailBox;
        private readonly ProgressBar _bar;
        private readonly Button _closeBtn;
        private readonly Button _runBtn;

        private readonly string _vbc, _vbPath, _exePath, _pname;
        private readonly List<string> _extraDlls, _embeddedFiles;
        private readonly string _icoFile;

        private bool _done = false;
        private FormClosingEventHandler _closingHandler;
        
        private const int Pad = 16;
        private const int StatusH = 20;
        private const int BarH = 20;
        private const int BtnPanelH = 44;
        private const int MinDetailH = 60;
        private const int MaxDetailH = 400;
        private const int MinWidth = 520;

        public BuildProgressDialog(IDE parent, string vbc, string vbPath, string exePath, string pname, List<string> extraDlls, List<string> embeddedFiles, string icoFile)
        {
            using (Bitmap bmp = new Bitmap(parent.CompileIcon))
            {
                IntPtr hIcon = bmp.GetHicon();

                using (Icon temp = Icon.FromHandle(hIcon))
                {
                    this.Icon = (Icon)temp.Clone();
                }

                DestroyIcon(hIcon);
            }

            _vbc = vbc;
            _vbPath = vbPath;
            _exePath = exePath;
            _pname = pname;
            _extraDlls = extraDlls;
            _embeddedFiles = embeddedFiles;
            _icoFile = icoFile;

            Text = "BUILDING " + pname + "…";
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(MinWidth, 180);
            Font = new Font("Segoe UI", 9f);
            StartPosition = FormStartPosition.Manual;
            ForeColor = IDE.IDETheme.Fore;
            BackColor = IDE.IDETheme.Back;

            _closingHandler = (s, e) => { if (!_done) e.Cancel = true; };
            FormClosing += _closingHandler;

            // Status label 
            _statusLbl = new Label
            {
                Text = "COMPILING " + pname + ".vb…",
                AutoSize = false,
                Left = Pad,
                Top = Pad,
                Height = StatusH,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };

            // Progress bar
            _bar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                Left = Pad,
                Top = Pad + StatusH + 8,
                Height = BarH,
                MarqueeAnimationSpeed = 20,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };

            // Detail box
            // Anchor Top only — do NOT anchor Bottom, otherwise the box grows
            // into the button panel when the form resizes.
            _detailBox = new RichTextBox
            {
                ReadOnly = true,
                BackColor = IDE.IDETheme.CanvasBack,
                BorderStyle = BorderStyle.None,
                ForeColor = Color.DimGray,
                Font = new Font("Consolas", 8.5f),
                Left = Pad,
                Top = Pad + StatusH + 8 + BarH + 8,
                Height = MinDetailH,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                WordWrap = true,
            };

            Controls.AddRange(new Control[] { _statusLbl, _bar, _detailBox });

            // Button panel (docked to bottom)
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = BtnPanelH,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8, 8, 8, 6),
                AutoSize = false,
                WrapContents = false,
            };

            _closeBtn = Helpers.MakeBtn("CLOSE", Color.DimGray, Color.White, (s, e) => Close(), 80);
            _runBtn = Helpers.MakeBtn("RUN EXE", Helpers.BtnGreen, Color.White, (s, e) => RunExe(), 90);

            _closeBtn.Enabled = false;
            _runBtn.Enabled = false;

            btnPanel.Controls.Add(_closeBtn);
            btnPanel.Controls.Add(_runBtn);
            Controls.Add(btnPanel);

            // Initial layout pass
            LayoutControls(MinDetailH);
            Helpers.CenterOnParent(this, parent);

            new Thread(Compile) { IsBackground = true }.Start();
        }

        //Resize / layout helper
        /// <summary>
        /// Resize the form so the detail box shows <paramref name="detailHeight"/>
        /// pixels of content, with the button panel always visible at the bottom.
        /// </summary>
        void LayoutControls(int detailHeight)
        {
            int clampedDetail = Math.Max(MinDetailH, Math.Min(MaxDetailH, detailHeight));

            // Width of content area
            int clientW = Math.Max(ClientSize.Width, MinWidth - SystemInformation.BorderSize.Width * 2);

            _statusLbl.Width = clientW - Pad * 2;
            _bar.Width = clientW - Pad * 2;
            _detailBox.Width = clientW - Pad * 2;
            _detailBox.Height = clampedDetail;
            
            int contentH = Pad + StatusH + 8 + BarH + 8 + clampedDetail + Pad;
            int totalClientH = contentH + BtnPanelH;

            ClientSize = new Size(clientW, totalClientH);
        }

        /// <summary>
        /// Measure how tall the RichTextBox needs to be to show all its text,
        /// then resize the form to fit (up to MaxDetailH before scrolling kicks in).
        /// </summary>
        void AutoResizeToContent()
        {
            int lastIndex = Math.Max(0, _detailBox.TextLength - 1);
            var lastPt = _detailBox.GetPositionFromCharIndex(lastIndex);
            int textHeight = lastPt.Y + _detailBox.Font.Height + 4;

            LayoutControls(textHeight);
        }

        // Thread helper
        void SafeInvoke(Action a)
        {
            try { if (IsHandleCreated && !IsDisposed) Invoke(a); }
            catch { }
        }

        // Build logic
        void Compile()
        {
            var projDir = Path.GetDirectoryName(_vbPath);

            var cmd = new List<string>
            {
                _vbc,
                "/target:winexe",
                "/out:" + _exePath,
                "/r:System.Windows.Forms.dll",
                "/r:System.Drawing.dll",
            };
            if (File.Exists(_icoFile)) cmd.Add("/resource:" + _icoFile);

            foreach (var dll in _extraDlls)
                if (!string.IsNullOrWhiteSpace(dll)) cmd.Add("/r:" + dll.Trim());

            foreach (var fp in _embeddedFiles)
            {
                var fp2 = fp.Trim();
                if (File.Exists(fp2)) cmd.Add("/resource:" + fp2 + "," + Path.GetFileName(fp2));
            }
            cmd.Add(_vbPath);

            var argParts = new List<string>();
            for (var i = 1; i < cmd.Count; i++)
                argParts.Add("\"" + cmd[i] + "\"");
            var arguments = string.Join(" ", argParts);

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = cmd[0],
                    Arguments = arguments,
                    WorkingDirectory = projDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using (var proc = new Process { StartInfo = psi })
                {
                    var stdoutSb = new StringBuilder();
                    var stderrSb = new StringBuilder();

                    proc.OutputDataReceived += (s2, e2) =>
                    {
                        if (e2.Data != null) stdoutSb.AppendLine(e2.Data);
                    };
                    proc.ErrorDataReceived += (s2, e2) =>
                    {
                        if (e2.Data != null) stderrSb.AppendLine(e2.Data);
                    };

                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    if (!proc.WaitForExit(120000))
                    {
                        proc.Kill();
                        SafeInvoke(() =>
                        {
                            MessageBox.Show(this, "Timedout 120", "vb compiler error", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                            OnFailure("Timedout 120 second");
                        });
                        return;
                    }
                    proc.WaitForExit();

                    var stdout = stdoutSb.ToString().Trim();
                    var stderr = stderrSb.ToString().Trim();
                    var parts = new List<string>();
                    if (!string.IsNullOrEmpty(stdout)) parts.Add(stdout);
                    if (!string.IsNullOrEmpty(stderr)) parts.Add(stderr);
                    var output = string.Join("\n", parts);

                    if (proc.ExitCode == 0 && File.Exists(_exePath))
                        SafeInvoke(() => OnSuccess(output));
                    else
                    {
                        var msg = output.Length > 0 ? output : "vbc exited with code " + proc.ExitCode;
                        SafeInvoke(() => OnFailure(msg));
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                SafeInvoke(() => OnFailure(
                    "vbc.exe not found at:\n" + _vbc +
                    "\n\nPlease set the correct Build Tools path in FILE > IDE Settings."));
            }
            catch (FileNotFoundException)
            {
                SafeInvoke(() => OnFailure(
                    "vbc.exe not found at:\n" + _vbc +
                    "\n\nPlease set the correct Build Tools path in FILE > IDE Settings."));
            }
            catch (Exception ex)
            {
                SafeInvoke(() => OnFailure(ex.Message));
            }
        }

        // Result handlers
        void OnSuccess(string output)
        {
            _done = true;
            FormClosing -= _closingHandler;

            _bar.Style = ProgressBarStyle.Continuous;
            _bar.Value = 100;
            _statusLbl.Text = "✔ BUILD SUCCESS: " + _pname + ".exe";
            _statusLbl.ForeColor = Color.Lime;

            if (!string.IsNullOrEmpty(output))
            {
                _detailBox.ForeColor = Color.DimGray;
                _detailBox.Text = output;
                AutoResizeToContent();
            }

            _closeBtn.Enabled = true;
            _runBtn.Enabled = true;
        }

        void OnFailure(string error)
        {
            _done = true;
            FormClosing -= _closingHandler;

            _bar.Style = ProgressBarStyle.Continuous;
            _bar.Value = 0;
            _statusLbl.Text = "✖ BUILD FAILED";
            _statusLbl.ForeColor = Helpers.BtnRed;

            _detailBox.ForeColor = Color.Salmon;
            _detailBox.Text = error;
            AutoResizeToContent();

            _closeBtn.Enabled = true;
            _runBtn.Enabled = true;
            Console.WriteLine(error);
        }

        // Run
        void RunExe()
        {
            try
            {
                Process.Start(new ProcessStartInfo(_exePath) { UseShellExecute = true });
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Run Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}