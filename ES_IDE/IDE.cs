using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using EventScriptIDE.Dialogs;
using Newtonsoft.Json;

namespace EventScriptIDE
{
    public class IDE : Form
    {
        // State
        ProjectModel _project = new ProjectModel();
        string _projectPath = null;
        AppSettings _settings;

        // Controls
        Label _infoLabel;
        Panel _groupsPanel;
        ListBox _varList;
        ListBox _ctrlList;
		Panel _insertLine;

		public static string ResourcePath = Path.Combine(Application.StartupPath, "Resource");
        public static class IDETheme
        {/*
            public static Color Back = Color.FromArgb(240, 240, 240);
            public static Color Fore = Color.FromArgb(0,0,0);
            public static Color CanvasBack = Helpers.FormBg;
            public static Color ListBack = Color.FromArgb(255,255,255);
            public static Color HeaderFore = Color.FromArgb(0,0,0);*/

			/*
			 * 
			public static Color Back = Color.FromArgb(30, 30, 30);
            public static Color Fore = Color.FromArgb(220, 220, 220);
            public static Color CanvasBack = Color.FromArgb(37, 37, 38);
            public static Color ListBack = Color.FromArgb(45, 45, 48);
			public static Color ListBack2 = Color.FromArgb(60, 60, 64);
			public static Color CanvasBack2 = Color.FromArgb(48, 48, 64);
			public static Color HeaderFore = Color.FromArgb(240, 240, 240);
			 * 
			 * 
			 * */
			public static Color Back = Color.FromArgb(20,20,20);
            public static Color Fore = Color.FromArgb(220, 220, 220);
            public static Color CanvasBack = Color.FromArgb(30, 30, 30);
            public static Color ListBack = Color.FromArgb(35, 35, 38);
			public static Color ListBack2 = Color.FromArgb(60, 60, 64);
			public static Color CanvasBack2 = Color.FromArgb(48, 48, 64);
			public static Color HeaderFore = Color.FromArgb(240, 240, 240);
            public static Color Accent = ColorTranslator.FromHtml("#5900ff");
            public static Color AccentDark = ColorTranslator.FromHtml("#2d0080");
        }
        // ---------------------------------------------------------------------
        public IDE(string[] args)
        {
            _settings = SettingsManager.Load();
            ExtensionRegistry.Reload();

            Text = "ES VPE";
            Size = new Size(1280, 720);
            MinimumSize = new Size(900, 600);
            Font = new Font("Segoe UI", 9f);
            BackColor = SystemColors.Control;
            StartPosition = FormStartPosition.CenterScreen;
            
            string exePath = Assembly.GetExecutingAssembly().Location;
            
            Icon exeIcon = Icon.ExtractAssociatedIcon(exePath);
            
            this.Icon = exeIcon;

            this.BackColor = IDETheme.Back;
            this.ForeColor = IDETheme.Fore;

			LoadFonts();

			BuildUI();
            BuildMenu();
            //Controls.SetChildIndex(MainMenuStrip, 0);
            Refresh2();

            if(args.Length>0)
            {
                if(File.Exists(args[0]))
                {
                    var project = ReadProjectFromFile(args[0]);
                    if (project == null) throw new Exception("Invalid file");

                    EnsureDefaults(project);

                    _project = project;
                    _projectPath = args[0];
                    Refresh2();
                }
            }
        }

        // ---------------------------------------------------------------------
        // UI Construction
        // ---------------------------------------------------------------------
        Image AddIcon = Image.FromFile(Path.Combine(ResourcePath,"Add.png"));
        Image SaveIcon = Image.FromFile(Path.Combine(ResourcePath, "Save.png"));
        Image OpenIcon = Image.FromFile(Path.Combine(ResourcePath, "Open.png"));
        public Image SettingsIcon = Image.FromFile(Path.Combine(ResourcePath, "Settings.png"));
        Image ExitIcon = Image.FromFile(Path.Combine(ResourcePath, "Exit.png"));
        public Image CompileIcon = Image.FromFile(Path.Combine(ResourcePath, "Compile.png"));
        Image GenerateIcon = Image.FromFile(Path.Combine(ResourcePath, "Generate.png"));
        public Image ExtensionIcon = Image.FromFile(Path.Combine(ResourcePath, "Extension.png"));
        Image ReloadIcon = Image.FromFile(Path.Combine(ResourcePath, "Reload.png"));
        public Image EventIcon = Image.FromFile(Path.Combine(ResourcePath, "Event.png"));
        public Image VariableIcon = Image.FromFile(Path.Combine(ResourcePath, "Variable.png"));
        public Image ControlIcon = Image.FromFile(Path.Combine(ResourcePath, "Control.png"));
        Image HelpIcon = Image.FromFile(Path.Combine(ResourcePath, "Help.png"));
        class DarkMenuColorTable : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => IDETheme.Back;

            public override Color ImageMarginGradientBegin => IDETheme.Back;
            public override Color ImageMarginGradientMiddle => IDETheme.Back;
            public override Color ImageMarginGradientEnd => IDETheme.Back;

            public override Color MenuItemPressedGradientBegin => Color.FromArgb(50, 50, 50);
            public override Color MenuItemPressedGradientMiddle => Color.FromArgb(50, 50, 50);
            public override Color MenuItemPressedGradientEnd => IDETheme.CanvasBack;

            public override Color MenuItemSelected
            {
                get { return IDETheme.AccentDark; }
            }

            public override Color MenuItemBorder
            {
                get { return IDETheme.Accent; }
            }

            public override Color MenuItemSelectedGradientBegin
            {
                get { return IDETheme.AccentDark; }
            }

            public override Color MenuItemSelectedGradientEnd
            {
                get { return IDETheme.AccentDark; }
            }
        }
        class GradientMenuRenderer : ToolStripProfessionalRenderer
        {
            public GradientMenuRenderer() : base(new DarkMenuColorTable())
            {
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using (var brush = new LinearGradientBrush(
                    e.AffectedBounds,
                    IDETheme.CanvasBack,
                    IDETheme.Back,
                    LinearGradientMode.Vertical))
                {
                    e.Graphics.FillRectangle(brush, e.AffectedBounds);
                }
            }
        }
        void BuildMenu()
        {
            var mb = new MenuStrip
            {
                Dock = DockStyle.Top,
                ForeColor = IDETheme.Fore
            };

            mb.Renderer = new GradientMenuRenderer();

            var MenusFont = new Font("Arial", 10f, FontStyle.Regular);

            // FILE
            var file = new ToolStripMenuItem("FILE");
            file.DropDownItems.Add(MItem("New Project",AddIcon, Keys.Control | Keys.N, (s, e) => NewProject2()));
            file.DropDownItems.Add(MItem("Open Project…",OpenIcon, Keys.Control | Keys.O, (s, e) => OpenProject()));
            file.DropDownItems.Add(MItem("Save Project",SaveIcon, Keys.Control | Keys.S, (s, e) => SaveProject(true)));
            file.DropDownItems.Add(MItem("Save Project As…",SaveIcon, Keys.None, (s, e) => SaveProjectAs()));
            file.DropDownItems.Add(new ToolStripSeparator());
            file.DropDownItems.Add(MItem("VPE Settings", SettingsIcon, Keys.None, (s, e) => OpenIdeSettings()));
            file.DropDownItems.Add(MItem("Refresh", ReloadIcon, Keys.F5, (s, e) => Refresh2()));
            file.DropDownItems.Add(new ToolStripSeparator());
            file.DropDownItems.Add(MItem("Exit",ExitIcon, Keys.None, (s, e) => Close()));

            // PROJECT
            var proj = new ToolStripMenuItem("PROJECT");
            proj.DropDownItems.Add(MItem("Settings",SettingsIcon, Keys.None, (s, e) => ProjectSettings()));
            proj.DropDownItems.Add(MItem("Add Event Group",EventIcon, Keys.None, (s, e) => AddEventGroup()));
            proj.DropDownItems.Add(MItem("Add Variable",VariableIcon, Keys.None, (s, e) => AddVariable()));
            proj.DropDownItems.Add(MItem("Add Control",ControlIcon, Keys.None, (s, e) => AddControl()));

            // GENERATE
            var gen = new ToolStripMenuItem("GENERATE");
            gen.DropDownItems.Add(MItem("Run (Build + Execute)",CompileIcon, Keys.F6, (s, e) => RunProject()));
            gen.DropDownItems.Add(new ToolStripSeparator());
            gen.DropDownItems.Add(MItem("Generate VB.NET Code",GenerateIcon, Keys.None, (s, e) => GenerateCode()));

            // EXTENSIONS
            var ext = new ToolStripMenuItem("EXTENSIONS");
            ext.DropDownItems.Add(MItem("Extension Browser",ExtensionIcon, Keys.None, (s, e) => OpenExtBrowser()));
            ext.DropDownItems.Add(MItem("Reload Extensions",ReloadIcon, Keys.None, (s, e) => ReloadExtensions()));
            ext.DropDownItems.Add(new ToolStripSeparator());
            ext.DropDownItems.Add(MItem("Open Extensions Folder",OpenIcon, Keys.None, (s, e) => OpenExtFolder()));
            ext.DropDownItems.Add(MItem("Install Extensions", AddIcon, Keys.None, (s, e) => InstallExtensions()));

            // HELP
            var hlp = new ToolStripMenuItem("HELP");
            hlp.DropDownItems.Add(MItem("About", HelpIcon, Keys.None, (s, e) => OpenAboutScreen()));

            // VERSION INDICATOR
            var ver = new ToolStripMenuItem("V" + Assembly.GetExecutingAssembly().GetName().Version.ToString())
            {
                Alignment = ToolStripItemAlignment.Right
            };
			ver.Click += (e, s) =>
			{
				OpenAboutScreen();
			};

            file.Font = MenusFont;
            proj.Font = MenusFont;
            gen.Font = MenusFont;
            ext.Font = MenusFont;
            hlp.Font = MenusFont;
            ver.Font = MenusFont;

            mb.Items.AddRange(new ToolStripItem[] { file, proj, gen, ext, hlp, ver });
            MainMenuStrip = mb;
            foreach (ToolStripMenuItem item in mb.Items)
            {
                if (item.HasDropDown)
                {
                    foreach (ToolStripItem item2 in item.DropDownItems)
                    {
                        if (item2 is ToolStripMenuItem menuItem)
                        {
                            menuItem.ForeColor = IDETheme.Fore;
                        }
                    }
                }
            }
            Controls.Add(mb);
        }

        private void InstallExtensions()
        {
            using (var dlg = new OpenFileDialog
            {
                Filter = "ZIP Files|*.zip",
                Title = "Select Extension ZIP"
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

                string zipPath = dlg.FileName;
                
                using (ZipArchive zip = ZipFile.OpenRead(zipPath))
                {
                    ZipArchiveEntry infoEntry = zip.GetEntry("Info.INFO");
                    if (infoEntry == null)
                    {
                        CustomMessageBox.Show("This is not a extension archive", "Invalid Extension", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    
                    ExtensionInfo extInfo;
                    using (var reader = new StreamReader(infoEntry.Open()))
                    {
                        try
                        {
                            string json = reader.ReadToEnd();
                            extInfo = JsonConvert.DeserializeObject<ExtensionInfo>(json);
                            if (extInfo == null)
                            {
                                CustomMessageBox.Show("This extension archive is not valid", "Invalid Extension", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }
                        catch
                        {
                            CustomMessageBox.Show("This extension archive is not valid", "Invalid Extension", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(extInfo.Name) || string.IsNullOrWhiteSpace(extInfo.Developer))
                    {
                        CustomMessageBox.Show("Info file is invalid", "Invalid Extension", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    
                    string msg = $"Are you sure to install the extension '{extInfo.Name}' from {extInfo.Developer}?";
                    if (CustomMessageBox.Show(msg, "Install Extension", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                        return;
                    
                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {
                        if (entry.FullName.Equals("Info.INFO", StringComparison.OrdinalIgnoreCase))
                            continue;
                        
                        if (string.IsNullOrEmpty(entry.Name))
                            continue;

                        string targetPath = Path.Combine(SettingsManager.ExtensionsDir, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
                        
                        string dir = Path.GetDirectoryName(targetPath);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                            Directory.CreateDirectory(dir);
                        
                        if (File.Exists(targetPath))
                        {
                            DialogResult overwrite = CustomMessageBox.Show($"File '{entry.FullName}' already exists. Overwrite?", "File Exists", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                            if (overwrite == DialogResult.Cancel)
                                return;
                            if (overwrite == DialogResult.No)
                                continue;
                        }
                        
                        entry.ExtractToFile(targetPath, true);
                    }

                    CustomMessageBox.Show($"Extension '{extInfo.Name}' installed successfully.", "Installed", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    ReloadExtensions();
                }
            }
        }
        
        private class ExtensionInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Desc { get; set; } = string.Empty;
            public string Developer { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
        }

        private void OpenAboutScreen()
        {
            //throw new NotImplementedException();
            using (var dlg = new AboutBox1())
            {
                Helpers.CenterOnOwner2(dlg, this);
                dlg.ShowDialog(this);
            }
        }

        static ToolStripMenuItem MItem(string text, Image img, Keys shortcut, EventHandler handler)
        {
            var item = new ToolStripMenuItem(text, img, handler);

            if (shortcut != Keys.None)
                item.ShortcutKeys = shortcut;

            return item;
        }
        private SplitContainer split;
        void BuildUI()
        {
            // Top bar
            var topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                Padding = new Padding(4, 4, 4, 0),
            };

            _infoLabel = new Label
            {
                AutoSize = true,
                Left = 8,
                Top = 8,
                Font = new Font("Segoe UI", 9f),
            };
			_insertLine = new Panel
			{
				Height = 2,
				BackColor = Color.Red,
				Visible = false,
				Enabled = false
			};
			topBar.Controls.Add(_infoLabel);

            var settingsBtn = Helpers.MakeBtn("PROJECT SETTINGS", Helpers.BtnDarkGray, Color.White, (s, e) => ProjectSettings(), 0, 26);
            settingsBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            settingsBtn.Top = 4;
            topBar.Controls.Add(settingsBtn);

            var runBtn = Helpers.MakeBtn("▶ RUN", Helpers.BtnGreen, Color.White, (s, e) => RunProject(), 80, 26);
            runBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            runBtn.Top = 4;
            topBar.Controls.Add(runBtn);

            topBar.Resize += (s, e) =>
            {
                settingsBtn.Left = topBar.Width - settingsBtn.Width - runBtn.Width - 16;
                runBtn.Left = topBar.Width - runBtn.Width - 4;
            };

            Controls.Add(topBar);

            // Split container (left groups | right sidebar)
            split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 5,
                Panel1MinSize = 200,
            };
            
            Load += (s, e) =>
            {
                split.Panel2MinSize = 220;
                split.SplitterDistance = Math.Max(200, split.Width - 280);
            };

            // Left panel
            var leftTop = new Panel { Dock = DockStyle.Top, Height = 30, Padding = new Padding(4, 2, 4, 0) };
            leftTop.Controls.Add(new Label
            {
                Text = "GROUPS",
                Font = _fontSegoeBigBold,
                AutoSize = true,
                Left = 6,
                Top = 5,
            });

            var addGroupBtn = Helpers.MakeBtn("+ ADD GROUP", Helpers.BtnBlue, Color.White,
                                              (s, e) => AddEventGroup(), 0, 24);
            addGroupBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            addGroupBtn.Top = 3;
            leftTop.Controls.Add(addGroupBtn);
            leftTop.Resize += (s, e) => addGroupBtn.Left = leftTop.Width - addGroupBtn.Width - 4;

            _groupsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = IDETheme.CanvasBack,
            };

            var leftWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 30, 0, 0),
                AutoScroll = true
            };

            leftWrapper.Controls.Add(_groupsPanel);
            leftWrapper.Controls.Add(leftTop);

            split.Panel1.Controls.Add(leftWrapper);

            // Right sidebar
            BuildSidebar(split.Panel2);

            Controls.Add(split);
        }

        void BuildSidebar(Panel parent)
        {
            var spacer = new Panel
            {
                Height = 32,
                Dock = DockStyle.Top
            };
            var varBox = new GroupBox
            {
                Text = "VARIABLES",
                ForeColor=IDETheme.HeaderFore,
                Dock = DockStyle.Top,
                Height = 220,
                Padding = new Padding(8, 0, 8, 8),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            };

            var vfBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 30,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            
            vfBar.Controls.Add(Helpers.MakeBtn("ADD", Helpers.BtnGreen, Color.White, (s, e) => AddVariable(), 55, 22));
            vfBar.Controls.Add(Helpers.MakeBtn("EDIT", Helpers.BtnBlue, Color.White, (s, e) => EditVariable(), 55, 22));
            vfBar.Controls.Add(Helpers.MakeBtn("DELETE", Helpers.BtnRed, Color.White, (s, e) => DelVariable(), 60, 22));

            _varList = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Courier New", 10f),
                ForeColor=IDETheme.Fore,
                BackColor=IDETheme.ListBack,
                BorderStyle=BorderStyle.None,
                IntegralHeight = false
            };

            varBox.Controls.Add(_varList);
            varBox.Controls.Add(vfBar);
            
            parent.Controls.Add(varBox);
            parent.Controls.Add(spacer);

            var easdas = new Panel
            {
                Dock=DockStyle.Fill

            };

            var spacer2 = new Panel
            {
                Height = 260,
                Dock = DockStyle.Top,
            };

            var ctrlBox = new GroupBox
            {
                Text = "CONTROLS",
                ForeColor = IDETheme.HeaderFore,
                Dock = DockStyle.Fill,
                Padding = new Padding(8, 0, 8, 8),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            };

            var cfBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 30,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            cfBar.Controls.Add(Helpers.MakeBtn("ADD", Helpers.BtnGreen, Color.White, (s, e) => AddControl(), 55, 22));
            cfBar.Controls.Add(Helpers.MakeBtn("EDIT", Helpers.BtnBlue, Color.White, (s, e) => EditControl(), 55, 22));
            cfBar.Controls.Add(Helpers.MakeBtn("DELETE", Helpers.BtnRed, Color.White, (s, e) => DelControl(), 60, 22));

            var visBtn = Helpers.MakeBtn("VISUAL DESIGNER", Helpers.BtnPurple, Color.White,
                (s, e) => OpenVisualDesigner(), 0, 24);
            visBtn.Dock = DockStyle.Top;

            _ctrlList = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Courier New", 10f),
                ForeColor = IDETheme.Fore,
                BackColor = IDETheme.ListBack,
                BorderStyle = BorderStyle.None,
                IntegralHeight = false
            };

            ctrlBox.Controls.Add(_ctrlList);
            ctrlBox.Controls.Add(visBtn);
            ctrlBox.Controls.Add(cfBar);
            easdas.Controls.Add(ctrlBox);
            easdas.Controls.Add(spacer2);
            parent.Controls.Add(easdas);
            //parent.Controls.Add(spacer2);
        }

        // ---------------------------------------------------------------------
        // Refresh
        // ---------------------------------------------------------------------

        void Refresh2()
        {
            UpdateInfo();
            RefreshVarList();
            RefreshCtrlList();
            RefreshGroups();
        }

        void UpdateInfo()
        {
            var p = _project;
            var groups = p.EventGroups;
            int total = 0;
            foreach (var g in groups) total += g.Events.Count;

            _infoLabel.Text =
                "Project: " + p.Name +
                "  |  " + p.FormWidth + "×" + p.FormHeight +
                "  |  " + groups.Count + " group(s)  " + total + " event(s)" +
                "  |  " + p.Variables.Count + " variable(s)" +
                "  |  " + p.Controls.Count + " control(s)";
            _infoLabel.Font = _fontSegoeTinyBold;

            var title = "ES VPE - " + p.Name;
            if (_projectPath != null) title += "  [" + Path.GetFileName(_projectPath) + "]";
            Text = title;
        }

        void RefreshVarList()
        {
            _varList.Items.Clear();
            foreach (var v in _project.Variables)
                _varList.Items.Add(v.Name + " : " + v.Type + " = " + v.Default);
        }

        void RefreshCtrlList()
        {
            _ctrlList.Items.Clear();
            foreach (var c in _project.Controls)
                _ctrlList.Items.Add(c.Name + " (" + c.Type + ")");
        }

        void RefreshGroups()
        {
            var scrollPos = _groupsPanel.AutoScrollPosition;

            _groupsPanel.SuspendLayout();
            _groupsPanel.Controls.Clear();
            
            for (int gi = _project.EventGroups.Count - 1; gi >= 0; gi--)
                _groupsPanel.Controls.Add(BuildGroupCard(gi, _project.EventGroups[gi]));

            _groupsPanel.ResumeLayout();
            
            _groupsPanel.AutoScrollPosition = new Point(-scrollPos.X, -scrollPos.Y);
        }
		public Font[] DecodeFonts(string data)
		{
			var parts = data.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries); 
			var fonts = new Font[parts.Length];

			for (int i = 0; i < parts.Length; i++)
			{
				var fontParts = parts[i].Split(',');

				string name = fontParts[0];
				FontStyle style = (FontStyle)Enum.Parse(typeof(FontStyle), fontParts[1]);
				float size = float.Parse(fontParts[2]);

				fonts[i] = new Font(name, size, style);
			}

			return fonts;
		}
		void LoadFonts()
		{
			var Fonts = DecodeFonts(_settings.Fonts1);
			MonoFont = Fonts[0].FontFamily.Name;
			var VerdanaFont = Fonts[2].FontFamily.Name;
			_fontCb10 = new Font(MonoFont, 10f, FontStyle.Bold);
			_fontSegoeSmBold = Fonts[0];
			_fontSegoeTinyBold = Fonts[2];
			_fontSegoeBigBold = new Font(VerdanaFont, 12f, FontStyle.Bold);
			_fontSegoeBigRg = new Font(VerdanaFont, 12f, FontStyle.Regular);
			_fontCourier = Fonts[1];
		}
		// ---------------------------------------------------------------------
		// Dynamic Card Builders
		// ---------------------------------------------------------------------
		private static ItemDefinition _clipboard = null;
        private static string _clipboardKind = null;
		public static string MonoFont = "Courier New";
		static Font _fontCb10 = new Font(MonoFont, 10f, FontStyle.Bold);
		Image DeleteIcon = Image.FromFile(Path.Combine(ResourcePath,"Delete.png"));
        Image UpIcon = Image.FromFile(Path.Combine(ResourcePath,"Up.png"));
        Image DownIcon = Image.FromFile(Path.Combine(ResourcePath,"Down.png"));
        Image RenameIcon = Image.FromFile(Path.Combine(ResourcePath,"Rename.png"));
        Image EditIcon = Image.FromFile(Path.Combine(ResourcePath,"Edit.png"));
        Image CopyIcon = Image.FromFile(Path.Combine(ResourcePath,"Copy.png"));
        Panel BuildGroupCard(int gi, EventGroup group)
        {
            var outer = new Panel
            {
                Dock = DockStyle.Top,
                //BackColor = Helpers.GroupBodyBg,
                Padding = new Padding(0, 0, 0, 6),
				AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
				BorderStyle = BorderStyle.FixedSingle,
				BackColor = IDETheme.CanvasBack2
			};

			var hdr = new Panel
			{
				Dock = DockStyle.Top,
				Height = 30,
				BackColor = IDETheme.CanvasBack2,
			};

            var trig = group.Trigger;
            var ttype = trig.Type;
            string ctrl2; trig.Params.TryGetValue("control", out ctrl2);
            string subN2; trig.Params.TryGetValue("sub_name", out subN2);
            var param2 = !string.IsNullOrEmpty(ctrl2) ? ctrl2 : subN2;
            var trigLbl = ttype + (string.IsNullOrEmpty(param2) ? "" : " [" + param2 + "]");

            hdr.Controls.Add(new Label
            {
                Text = group.Name,
                ForeColor = Color.White,
                //BackColor = Helpers.GroupHeaderBg,
                Font = _fontCb10,
                AutoSize = true,
                Left = 8,
                Top = 6,
            });
            hdr.Controls.Add(new Label
            {
                Text = "⚡ " + trigLbl,
                ForeColor = Color.FromArgb(0x7e, 0xb4, 0xff),
                //BackColor = Helpers.GroupHeaderBg,
                Font = _fontSegoeTinyBold,
                AutoSize = true,
                Left = 8 + TextRenderer.MeasureText(group.Name, _fontCb10).Width + 8,
                Top = 8,
            });
            
            var captured_gi = gi;
            
            var btnDel = MakeHdrBtn("dl", Helpers.BtnRed, (s, e) => DelEventGroup(captured_gi),DeleteIcon);
            var btnDown = MakeHdrBtn("up", Helpers.BtnDarkBlue, (s, e) => MoveEventGroup(captured_gi, 1),UpIcon);
            var btnUp = MakeHdrBtn("dn", Helpers.BtnDarkBlue, (s, e) => MoveEventGroup(captured_gi, -1),DownIcon);
            var btnRename = MakeHdrBtn("rn", Helpers.BtnDarkBlue, (s, e) => RenameEventGroup(captured_gi),RenameIcon);
            var btnEdit = MakeHdrBtn("tr", Helpers.BtnPurple, (s, e) => EditGroupTrigger(captured_gi),EditIcon);
            var btnAdd = MakeHdrBtn("ev", Helpers.BtnBlue, (s, e) => AddEvent(captured_gi),AddIcon);
            //btnAdd.Width = 70;

            hdr.Controls.AddRange(new Control[] { btnDel, btnDown, btnUp, btnRename, btnEdit, btnAdd });
            hdr.Resize += (s, e) =>
            {
                int x = hdr.Width - 4;
                foreach (var b in new[] { btnDel, btnDown, btnUp, btnRename, btnEdit, btnAdd })
                {
                    x -= b.Width + 2;
                    b.Left = x;
                    b.Top = 3;
                }
            };

            // Events
            var inner = new Panel
            {
                Dock = DockStyle.Top,
				//BackColor = Helpers.GroupBodyBg,
				//BackColor = IDETheme.ListBack,
				BackColor = IDETheme.CanvasBack2,
				Padding = new Padding(4, 2, 4, 0),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
				AllowDrop = true,          // allow dropping even if empty
				MinimumSize = new Size(0, 30) // visible drop area
			};

			inner.DragOver += (s, de) =>
			{
				de.Effect = DragDropEffects.Move;

				var pt = inner.PointToClient(new Point(de.X, de.Y));

				if (de.Effect == DragDropEffects.None)
					HideInsertLine();
				else
					ShowInsertLine(inner, pt.Y);
			};

			inner.DragLeave += (s, e) =>
			{
				HideInsertLine();
			};

			inner.DragDrop += (s, de) =>
			{
				HideInsertLine();

				var raw = de.Data.GetData(DataFormats.StringFormat) as string;
				if (string.IsNullOrEmpty(raw)) return;

				var parts = raw.Split('|');
				if (parts[0] != "event") return;

				int srcGi = int.Parse(parts[1]);
				int srcEi = int.Parse(parts[2]);

				var ev = _project.EventGroups[srcGi].Events[srcEi];
				_project.EventGroups[srcGi].Events.RemoveAt(srcEi);

				var dstList = _project.EventGroups[gi].Events;

				var pt = inner.PointToClient(new Point(de.X, de.Y));

				// empty group? insert at 0
				int insertIndex = dstList.Count == 0 ? 0 : GetInsertIndexFromY(inner, pt.Y, null);
				insertIndex = dstList.Count - insertIndex;
				if (insertIndex < 0) insertIndex = 0;
				if (insertIndex > dstList.Count) insertIndex = dstList.Count;

				dstList.Insert(insertIndex, ev);

				RefreshGroups();
			};

			if (group.Events.Count == 0)
            {
				inner.AllowDrop = true;
				inner.MinimumSize = new Size(0, 30);
				/*inner.Controls.Add(new Label
				{
					Text = "  (no events)",
					ForeColor = Color.DimGray,
					BackColor = Helpers.GroupBodyBg,
					AutoSize = true,
					Font = new Font("Segoe UI", 8f, FontStyle.Italic),
					Padding = new Padding(0, 4, 0, 4),
					AllowDrop = false
				});*/
			}
            else
            {
                for (int ei = group.Events.Count - 1; ei >= 0; ei--)
                    inner.Controls.Add(BuildEventCard(gi, ei, group.Events[ei]));
            }

            outer.Controls.Add(inner);
            outer.Controls.Add(hdr);
            return outer;
        }
        static Font _fontSegoeSmBold = new Font(MonoFont, 11f, FontStyle.Bold);
        public static Font _fontSegoeTinyBold = new Font("Verdana", 8f, FontStyle.Bold);
        static Font _fontSegoeBigBold = new Font("Verdana", 12f, FontStyle.Bold);
        static Font _fontSegoeBigRg = new Font("Verdana", 12f, FontStyle.Regular);
		public Font _fontCourier = new Font(MonoFont, 8f);

		Panel BuildEventCard(int gi, int ei, EventModel ev)
		{
			var card = new Panel
			{
				Dock = DockStyle.Top,
				//BackColor = Helpers.EventBodyBg,
				BackColor = IDETheme.Back,
				Padding = new Padding(2),
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Margin = new Padding(0, 3, 0, 0),
				AllowDrop = true,
			};

			// Event header
			var ehdr = new Panel
			{
				Dock = DockStyle.Top,
				Height = 26,
				//BackColor = Helpers.EventHeaderBg
				BackColor = IDETheme.Back,
			};

			var evtHandle = new Panel
			{
				Width = 14,
				Dock = DockStyle.Left,
				Cursor = Cursors.SizeAll,
				BackColor = IDETheme.ListBack,
				BorderStyle = BorderStyle.FixedSingle,
				Tag = "event-handle"
			};
			evtHandle.MouseDown += (s, e) =>
			{
				if (e.Button != MouseButtons.Left) return;
				var data = $"event|{gi}|{ei}";
				evtHandle.DoDragDrop(data, DragDropEffects.Move);
			};

			ehdr.Controls.Add(evtHandle);

			ehdr.Controls.Add(new Label
			{
				Text = "  > " + ev.Name,
				ForeColor = Color.White,
				//BackColor = Helpers.EventHeaderBg,
				BackColor = IDETheme.Back,
				Font = _fontSegoeSmBold,
				AutoSize = true,
				Left = 2,
				Top = 5,
			});

			var btnX = MakeHdrBtn("✖", Helpers.BtnRed, (s, e) => DelEvent(gi, ei), DeleteIcon);
			//var btnDown = MakeHdrBtn("▼", Helpers.BtnGreen, (s, e) => MoveEvent(gi, ei, 1), DownIcon); // commented out - drag now used
			//var btnUp = MakeHdrBtn("▲", Helpers.BtnGreen, (s, e) => MoveEvent(gi, ei, -1), UpIcon); // commented out - drag now used
			var btnRename = MakeHdrBtn("REN", Helpers.BtnDarkBlue, (s, e) => RenameEvent(gi, ei), RenameIcon);

			int totalBtnW = 0;
			//var hdrBtns = new[] { btnX, btnDown, btnUp, btnRename };
			var hdrBtns = new[] { btnX, btnRename };
			foreach (var b in hdrBtns) totalBtnW += b.Width + 2;

			ehdr.Controls.AddRange(hdrBtns);
			ehdr.Resize += (s2, e2) =>
			{
				int x = ehdr.Width - 4;
				foreach (var b in hdrBtns)
				{
					x -= b.Width + 2;
					b.Left = x;
					b.Top = 2;
				}
			};

			// Conditions / Actions columns
			cols = new TableLayoutPanel
			{
				Dock = DockStyle.Top,
				ColumnCount = 2,
				RowCount = 1,
				//BackColor = Helpers.EventBodyBg,
				BackColor = IDETheme.Back,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink
			};
			cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
			cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
			cols.RowStyles.Add(new RowStyle(SizeType.AutoSize));

			cols.Controls.Add(BuildItemsPanel(gi, ei, ev.Conditions, "CONDITIONS", Helpers.CondBg, Helpers.CondFg, Helpers.BtnCondGreen, "conditions"), 0, 0);
			cols.Controls.Add(BuildItemsPanel(gi, ei, ev.Actions, "ACTIONS", Helpers.ActBg, Helpers.ActFg, Helpers.BtnActPurple, "actions"), 1, 0);

			card.Controls.Add(cols);
			card.Controls.Add(ehdr);

			card.DragOver += (s, de) =>
			{
				de.Effect = DragDropEffects.Move;

				var pt = card.Parent.PointToClient(new Point(de.X, de.Y));

				if (de.Effect == DragDropEffects.None)
					HideInsertLine();
				else
					ShowInsertLine(card.Parent, pt.Y);
			};

			card.DragLeave += (s, e) =>
			{
				HideInsertLine();
			};

			card.DragDrop += (s, de) =>
			{
				HideInsertLine();

				var raw = de.Data.GetData(DataFormats.StringFormat) as string;
				if (string.IsNullOrEmpty(raw)) return;

				var parts = raw.Split('|');
				if (parts[0] != "event") return;

				int srcGi = int.Parse(parts[1]);
				int srcEi = int.Parse(parts[2]);

				var pt = card.Parent.PointToClient(new Point(de.X, de.Y));
				int insertIndex = GetInsertIndexFromY(card.Parent, pt.Y, card);

				var srcList = _project.EventGroups[srcGi].Events;
				var ev2 = srcList[srcEi];
				srcList.RemoveAt(srcEi);

				var dstList = _project.EventGroups[gi].Events;

				if (insertIndex > dstList.Count) insertIndex = dstList.Count;

				dstList.Insert(insertIndex, ev2);

				RefreshGroups();
			};

			return card;
		}
		private TableLayoutPanel cols = null;
		Panel BuildItemsPanel(int gi, int ei, List<ItemDefinition> items, string title, Color bg, Color fg, Color addBtnColor, string key)
		{
			var panel = new Panel
			{
				Dock = DockStyle.Fill,
				BackColor = bg,
				Padding = new Padding(2),
				AutoSize = true,
				//BorderStyle = BorderStyle.FixedSingle,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
			};

			var lbl = new Label
			{
				Text = title,
				ForeColor = fg,
				BackColor = bg,
				Font = _fontSegoeTinyBold,
				AutoSize = true,
				Padding = new Padding(2),
			};

			var ButtonsHolder = new Panel
			{
				Dock = DockStyle.Bottom,
				Height = 22
			};

			var addBtn = Helpers.MakeBtn("＋ ADD " + title.TrimEnd('S'), addBtnColor, Color.White,
				(s2, e2) =>
				{
					if (key == "conditions") AddCondition(gi, ei);
					else AddAction(gi, ei);
				}, 0, 22);
			addBtn.Dock = DockStyle.Bottom;

			var pasteBtn = Helpers.MakeBtn("PASTE", addBtnColor, Color.White,
				(s2, e2) =>
				{
					if (_clipboard == null) return;
					if (_clipboardKind != key) return;
					var pasted = new ItemDefinition
					{
						Category = _clipboard.Category,
						Action = _clipboard.Action,
						Params = new Dictionary<string, string>(_clipboard.Params)
					};
					if (key == "conditions") PasteCondition(gi, ei, pasted);
					else PasteAction(gi, ei, pasted);
				}, 0, 22);
			pasteBtn.Dock = DockStyle.Left;

			var itemsHolder = new Panel
			{
				Dock = DockStyle.Top,
				BackColor = bg,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				AllowDrop = true,
			};

			itemsHolder.MinimumSize = new Size(0, 30);
			
			itemsHolder.DragOver += (s, de) =>
			{
				if ((de.KeyState & 8) != 0)
					de.Effect = DragDropEffects.None;
				else
					de.Effect = DragDropEffects.Move;

				var pt = itemsHolder.PointToClient(new Point(de.X, de.Y));
				
				if (de.Effect == DragDropEffects.None)
					HideInsertLine();
				else
					ShowInsertLine(itemsHolder, pt.Y);
			};

			itemsHolder.DragLeave += (s, e) =>
			{
				HideInsertLine();
			};

			itemsHolder.DragDrop += (s, de) =>
			{
				HideInsertLine();

				var raw = de.Data.GetData(DataFormats.StringFormat) as string;
				if (string.IsNullOrEmpty(raw)) return;

				var parts = raw.Split('|');
				if (parts[0] != "item") return;

				int srcGi = int.Parse(parts[1]);
				int srcEi = int.Parse(parts[2]);
				string srcKey = parts[3];
				int srcJ = int.Parse(parts[4]);

				var pt = itemsHolder.PointToClient(new Point(de.X, de.Y));
				int insertIndex = GetInsertIndexFromY(itemsHolder, pt.Y, null);

				List<ItemDefinition> srcList =
					srcKey == "conditions"
					? _project.EventGroups[srcGi].Events[srcEi].Conditions
					: _project.EventGroups[srcGi].Events[srcEi].Actions;

				var item = srcList[srcJ];
				srcList.RemoveAt(srcJ);

				List<ItemDefinition> dstList =
					key == "conditions"
					? _project.EventGroups[gi].Events[ei].Conditions
					: _project.EventGroups[gi].Events[ei].Actions;

				if (insertIndex > dstList.Count)
					insertIndex = dstList.Count;

				dstList.Insert(insertIndex, item);

				RefreshGroups();
			};

			itemsHolder.SuspendLayout();
			for (int j = items.Count - 1; j >= 0; j--)
			{
				var row = BuildItemRow(gi, ei, j, items[j], key, bg, fg, cols);
				itemsHolder.Controls.Add(row);
			}
			itemsHolder.ResumeLayout(false);
			itemsHolder.PerformLayout();

			ButtonsHolder.Controls.Add(pasteBtn);
			ButtonsHolder.Controls.Add(addBtn);
			panel.Controls.Add(ButtonsHolder);
			panel.Controls.Add(addBtn);
			panel.Controls.Add(itemsHolder);
			panel.Controls.Add(lbl);
			return panel;
		}
		void PasteCondition(int gi, int ei, ItemDefinition item)
        {
            _project.EventGroups[gi].Events[ei].Conditions.Add(item);
            RefreshGroups();
        }

        void PasteAction(int gi, int ei, ItemDefinition item)
        {
            _project.EventGroups[gi].Events[ei].Actions.Add(item);
            RefreshGroups();
        }
		int GetInsertIndexFromY(Control parent, int y, Control skip = null)
		{
			var list = parent.Controls.Cast<Control>()
				.Where(c => c != _insertLine && c != skip)
				.OrderBy(c => c.Top)
				.ToList();

			if (list.Count == 0)
				return 0;

			for (int i = 0; i < list.Count; i++)
			{
				var c = list[i];
				int mid = c.Top + c.Height / 2;

				if (y < mid)
					return i;
			}

			return list.Count;
		}

		void MoveEventInModel(int gi, int fromIndex, int toIndex)
		{
			var list = _project.EventGroups[gi].Events;
			if (fromIndex < 0 || fromIndex >= list.Count) return;
			if (toIndex < 0) toIndex = 0;
			if (toIndex > list.Count) toIndex = list.Count;
			if (fromIndex == toIndex || fromIndex + 1 == toIndex)
			{
				RefreshGroups();
				return;
			}
			var ev = list[fromIndex];
			list.RemoveAt(fromIndex);
			if (toIndex > fromIndex) toIndex--;
			if (toIndex > list.Count) toIndex = list.Count;
			list.Insert(toIndex, ev);
			RefreshGroups();
		}
		void ShowInsertLine(Control parent, int y)
		{
			if (_insertLine.Parent != parent)
			{
				_insertLine.Parent?.Controls.Remove(_insertLine);
				parent.Controls.Add(_insertLine);
				_insertLine.BringToFront();
			}
			
			var children = parent.Controls.Cast<Control>()
				.Where(c => c != _insertLine)
				.ToList();
			
			children.Sort((a, b) => a.Top.CompareTo(b.Top));

			int insertY;

			if (children.Count == 0)
			{
				insertY = 2;
			}
			else
			{
				int idx = GetInsertIndexFromY(parent, y, null);
				if (idx >= children.Count)
					insertY = children[children.Count - 1].Bottom;
				else
					insertY = children[idx].Top;
			}

			_insertLine.SetBounds(0, insertY - 1, parent.Width, 2);
			_insertLine.Visible = true;
		}

		void HideInsertLine()
		{
			_insertLine.Visible = false;
		}
		void MoveItemInModel(int gi, int ei, string key, int fromIndex, int toIndex)
		{
			List<ItemDefinition> list = key == "conditions"
				? _project.EventGroups[gi].Events[ei].Conditions
				: _project.EventGroups[gi].Events[ei].Actions;
			if (fromIndex < 0 || fromIndex >= list.Count) return;
			if (toIndex < 0) toIndex = 0;
			if (toIndex > list.Count) toIndex = list.Count;
			if (fromIndex == toIndex || fromIndex + 1 == toIndex)
			{
				RefreshGroups();
				return;
			}
			var el = list[fromIndex];
			list.RemoveAt(fromIndex);
			if (toIndex > fromIndex) toIndex--;
			if (toIndex > list.Count) toIndex = list.Count;
			list.Insert(toIndex, el);
			RefreshGroups();
		}
		Panel BuildItemRow(int gi, int ei, int j, ItemDefinition item, string key, Color bg, Color fg, Panel ih)
		{
			var row = new Panel
			{
				BackColor = bg,
				Height = 22,
				Dock = DockStyle.Top,
				Padding = new Padding(2, 1, 2, 1),
			};
			
			var itemHandle = new Panel
			{
				Width = 12,
				Dock = DockStyle.Left,
				Cursor = Cursors.SizeAll,
				BackColor = Color.FromArgb(Math.Min(row.BackColor.R + 24, 255), Math.Min(row.BackColor.G + 24, 255), Math.Min(row.BackColor.B + 24, 255)),
				Tag = "item-handle",
				BorderStyle = BorderStyle.FixedSingle,
			};
			itemHandle.MouseDown += (s, e) =>
			{
				if (e.Button != MouseButtons.Left) return;
				var data = $"item|{gi}|{ei}|{key}|{j}";
				itemHandle.DoDragDrop(data, DragDropEffects.Move);
			};
			row.Controls.Add(itemHandle);

			var sb = new System.Text.StringBuilder("[").Append(item.Category).Append("] ").Append(item.Action);
			bool first = true;
			foreach (var kvp in item.Params)
			{
				if (string.IsNullOrEmpty(kvp.Value)) continue;
				sb.Append(first ? "  →  " : "  ").Append(kvp.Key).Append('=').Append(kvp.Value);
				first = false;
			}

			var lbl = new Label
			{
				Text = sb.ToString(),
				ForeColor = fg,
				BackColor = bg,
				Font = _fontCourier,
				AutoSize = true,
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleLeft,
				Padding = new Padding(10, 0, 0, 0),
			};
			lbl.MaximumSize = new Size(((this.Width - split.Panel2.Width) / 3), 0);

			var btnEdit = MakeHdrBtn("✎", Helpers.BtnBlue, (s, e) => EditItem(gi, ei, key, j), EditIcon);
			var btnDel = MakeHdrBtn("✖", Helpers.BtnRed, (s, e) => DelItem(gi, ei, key, j), DeleteIcon);
			//	var btnDown = MakeHdrBtn("▼", Helpers.BtnDarkGray, (s, e) => MoveItem(gi, ei, key, j, 1), DownIcon);
			//	var btnUp = MakeHdrBtn("▲", Helpers.BtnDarkGray, (s, e) => MoveItem(gi, ei, key, j, -1), UpIcon);
			var btnCopy = MakeHdrBtn("⧉", Helpers.BtnDarkBlue, (s, e) =>
			{
				_clipboard = new ItemDefinition
				{
					Category = item.Category,
					Action = item.Action,
					Params = new Dictionary<string, string>(item.Params)
				};
				_clipboardKind = key;
			}, CopyIcon);

			foreach (var b in new[] { btnEdit, btnDel, btnCopy })
			{
				b.Width = 22;
				b.Height = 20;
				b.Dock = DockStyle.Right;
			}

			row.Controls.Add(lbl);
			//	row.Controls.Add(btnDown);
			//	row.Controls.Add(btnUp);
			row.Controls.Add(btnDel);
			row.Controls.Add(btnCopy);
			row.Controls.Add(btnEdit);
			row.Height = lbl.Height + row.Padding.Top + row.Padding.Bottom;
			return row;
		}

		static Button MakeHdrBtn(string text, Color bg, EventHandler handler, Image img = null)
        {
            var b = new Button
            {
                Text = img == null ? text : string.Empty,
                Image = img,
                ImageAlign = ContentAlignment.MiddleCenter,
                BackColor = bg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Width = 38,
                Height = 24,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
            };

            b.FlatAppearance.BorderSize = 0;
            b.Click += handler;

            return b;
        }

        // ---------------------------------------------------------------------
        // Event Group Operations
        // ---------------------------------------------------------------------

        void AddEventGroup()
        {
            using (var dlg = new TriggerDialog(this))
            {
                Helpers.CenterOnOwner2(dlg, this);
                dlg.ShowDialog(this);
                if (dlg.Result == null) return;
                var name = Helpers.AutoGroupName(_project);
                _project.EventGroups.Add(new EventGroup { Name = name, Trigger = dlg.Result });
            }
            Refresh2();
        }

        void DelEventGroup(int gi)
        {
            var g = _project.EventGroups[gi];
            if (CustomMessageBox.Show("Delete group \"" + g.Name + "\" and all " + g.Events.Count + " event(s)?", "Delete Group", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            _project.EventGroups.RemoveAt(gi);
            Refresh2();
        }

        void MoveEventGroup(int gi, int dir)
        {
            var ni = gi + dir;
            if (ni < 0 || ni >= _project.EventGroups.Count) return;
            var tmp = _project.EventGroups[gi];
            _project.EventGroups[gi] = _project.EventGroups[ni];
            _project.EventGroups[ni] = tmp;
            RefreshGroups(); UpdateInfo();
        }

        void RenameEventGroup(int gi)
        {
            var old = _project.EventGroups[gi].Name;
            var name = Microsoft.VisualBasic.Interaction.InputBox("new name:", "RENAME GROUP", old);
            if (string.IsNullOrEmpty(name)) return;
            _project.EventGroups[gi].Name = name;
            Refresh2();
        }

        void EditGroupTrigger(int gi)
        {
            using (var dlg = new TriggerDialog(this, _project.EventGroups[gi].Trigger))
            {
                Helpers.CenterOnOwner2(dlg, this);
                dlg.ShowDialog(this);
                if (dlg.Result == null) return;
                _project.EventGroups[gi].Trigger = dlg.Result;
            }
            RefreshGroups();
        }

        // ---------------------------------------------------------------------
        // Event Operations
        // ---------------------------------------------------------------------

        void AddEvent(int gi)
        {
            var group = _project.EventGroups[gi];
            var name = Helpers.AutoEventName(group);
            group.Events.Add(new EventModel { Name = name });
            Refresh2();
        }

        void DelEvent(int gi, int ei)
        {
            if (CustomMessageBox.Show("Delete this event?", "Delete Event", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            _project.EventGroups[gi].Events.RemoveAt(ei);
            Refresh2();
        }

        void MoveEvent(int gi, int ei, int dir)
        {
            var events = _project.EventGroups[gi].Events;
            var ni = ei + dir;
            if (ni < 0 || ni >= events.Count) return;
            var tmp = events[ei]; events[ei] = events[ni]; events[ni] = tmp;
            RefreshGroups();
        }

        void RenameEvent(int gi, int ei)
        {
            var old = _project.EventGroups[gi].Events[ei].Name;
            var name = Microsoft.VisualBasic.Interaction.InputBox("new name:", "RENAME EVENT", old);
            if (string.IsNullOrEmpty(name)) return;
            _project.EventGroups[gi].Events[ei].Name = name;
            RefreshGroups();
        }

        // ---------------------------------------------------------------------
        // Condition / Action Operations
        // ---------------------------------------------------------------------

        void AddCondition(int gi, int ei)
        {
            using (var dlg = new ParamDialog(this, "ADD CONDITION", ExtensionRegistry.GetAllConditions()))
            {
                Helpers.CenterOnOwner2(dlg,this);
                dlg.ShowDialog(this);
                if (dlg.Result == null) return;
                _project.EventGroups[gi].Events[ei].Conditions.Add(dlg.Result);
            }
            RefreshGroups();
        }

        void AddAction(int gi, int ei)
        {
            using (var dlg = new ParamDialog(this, "ADD ACTION", ExtensionRegistry.GetAllActions()))
            {
                Helpers.CenterOnOwner2(dlg, this);
                dlg.ShowDialog(this);
                if (dlg.Result == null) return;
                _project.EventGroups[gi].Events[ei].Actions.Add(dlg.Result);
            }
            RefreshGroups();
        }

        void EditItem(int gi, int ei, string key, int j)
        {
            var items = key == "conditions"
                ? _project.EventGroups[gi].Events[ei].Conditions
                : _project.EventGroups[gi].Events[ei].Actions;
            var defs = key == "conditions"
                ? ExtensionRegistry.GetAllConditions()
                : ExtensionRegistry.GetAllActions();

            using (var dlg = new ParamDialog(this, "Edit", defs, items[j]))
            {
                Helpers.CenterOnOwner2(dlg, this);
                dlg.ShowDialog(this);
                if (dlg.Result == null) return;
                items[j] = dlg.Result;
            }
            RefreshGroups();
        }

        void DelItem(int gi, int ei, string key, int j)
        {
            var items = key == "conditions"
                ? _project.EventGroups[gi].Events[ei].Conditions
                : _project.EventGroups[gi].Events[ei].Actions;
            items.RemoveAt(j);
            RefreshGroups();
        }

        void MoveItem(int gi, int ei, string key, int j, int dir)
        {
            var items = key == "conditions"
                ? _project.EventGroups[gi].Events[ei].Conditions
                : _project.EventGroups[gi].Events[ei].Actions;
            var ni = j + dir;
            if (ni < 0 || ni >= items.Count) return;
            var tmp = items[j]; items[j] = items[ni]; items[ni] = tmp;
            RefreshGroups();
        }

        // ---------------------------------------------------------------------
        // Variable Operations
        // ---------------------------------------------------------------------

        void AddVariable()
        {
            using (var dlg = new VariableDialog(this))
            {
                Helpers.CenterOnOwner2(dlg, this);
                dlg.ShowDialog(this);
                if (dlg.Result == null) return;
                _project.Variables.Add(dlg.Result);
            }
            RefreshVarList(); UpdateInfo();
        }

        void EditVariable()
        {
            if (_varList.SelectedIndex < 0) return;
            var idx = _varList.SelectedIndex;
            using (var dlg = new VariableDialog(this, _project.Variables[idx]))
            {
                Helpers.CenterOnOwner2(dlg, this);
                dlg.ShowDialog(this);
                if (dlg.Result == null) return;
                _project.Variables[idx] = dlg.Result;
            }
            RefreshVarList();
        }

        void DelVariable()
        {
            if (_varList.SelectedIndex < 0) return;
            if (CustomMessageBox.Show("Delete this variable?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            _project.Variables.RemoveAt(_varList.SelectedIndex);
            RefreshVarList(); UpdateInfo();
        }

        // ---------------------------------------------------------------------
        // Control Operations
        // ---------------------------------------------------------------------

        void AddControl()
        {
            using (var dlg = new ControlDialog(this))
            {
                Helpers.CenterOnOwner2(dlg, this);
                dlg.ShowDialog(this);
                if (dlg.Result == null) return;
                _project.Controls.Add(dlg.Result);
            }
            RefreshCtrlList(); UpdateInfo();
        }

        void EditControl()
        {
            if (_ctrlList.SelectedIndex < 0) return;
            var idx = _ctrlList.SelectedIndex;
            using (var dlg = new ControlDialog(this, _project.Controls[idx]))
            {
                Helpers.CenterOnOwner2(dlg, this);
                dlg.ShowDialog(this);
                if (dlg.Result == null) return;
                _project.Controls[idx] = dlg.Result;
            }
            RefreshCtrlList();
        }

        void DelControl()
        {
            if (_ctrlList.SelectedIndex < 0) return;
            if (CustomMessageBox.Show("Delete this control?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            _project.Controls.RemoveAt(_ctrlList.SelectedIndex);
            RefreshCtrlList(); UpdateInfo();
        }

        void OpenVisualDesigner()
        {
            if (_project.Controls.Count == 0)
            {
                CustomMessageBox.Show("Add some controls first, then open the Visual Designer", "Visual Designer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var dlg = new VisualDesignerDialog(this, _project))
            {
                Helpers.CenterOnOwner2(dlg, this);
                dlg.ShowDialog(this);
                if (dlg.Result != null) RefreshCtrlList();
            }
        }

        // ---------------------------------------------------------------------
        // Project Settings
        // ---------------------------------------------------------------------

        void ProjectSettings()
        {
            using (var dlg = new ProjectSettingsDialog(this, _project))
            {
                Helpers.CenterOnOwner2(dlg, this);

                dlg.ShowDialog(this);

                if (dlg.Result == null) return;
                _project.Name = dlg.Result.Name;
                _project.FormWidth = dlg.Result.FormWidth;
                _project.FormHeight = dlg.Result.FormHeight;
                _project.Resizable = dlg.Result.Resizable;
                _project.VStyle = dlg.Result.VStyle;
                _project.ExtraDlls = dlg.Result.ExtraDlls;
                _project.EmbeddedFiles = dlg.Result.EmbeddedFiles;
            }
            Refresh2();
        }

        // ---------------------------------------------------------------------
        // File I/O
        // ---------------------------------------------------------------------

        void NewProject()
        {
            if (CustomMessageBox.Show("Discard current project and start fresh?", "New Project",MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            _project = new ProjectModel();
            _projectPath = null;
            Refresh2();
        }

        void NewProject2()
        {
            //if (CustomMessageBox.Show("Discard current project and start fresh?", "New Project", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            using (var dlg = new TemplateDialog())
            {
                Helpers.CenterOnOwner2(dlg, this);
                dlg.ShowDialog();
                var Result = dlg.Result;
                Console.WriteLine(Result);
                if(Result!=0)
                {
                    
                    switch(Result)
                    {
                        case 1:
                            _project = new ProjectModel();
                            _projectPath = null;
                            Refresh2();
                            break;

                        case 2:
                            //_project = new ProjectModel();
                            try
                            {
                                _project = ReadProjectFromFile(Path.Combine(ResourcePath,"ExampleProjects","CustomRender.ESPRJ"));
                            }
                            catch(Exception e)
                            {
                                CustomMessageBox.Show("Can not load TempLate:"+e.Message, "Error When Loading TempLate", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            _projectPath = null;
                            Refresh2();
                            break;

                        default:
                            CustomMessageBox.Show("Unknown TempLate ID:" + Result, "CRITICAL FATAIL FAILURE",MessageBoxButtons.OK,MessageBoxIcon.Error);

                            break;
                    }
                }
                else
                {
                    //CustomMessageBox.Show("cancelled", "debug");
                }
            }
        }

        const string BinaryHeader = "ESPRJ";
        const int BinaryVersion = 1;

		void OpenProject()
		{
			using (var dlg = new OpenFileDialog
			{
				DefaultExt = ".ESPRJ",
				Filter = "ES Project|*.ESPRJ|ES VPE Project (legacy)|*.ESP|JSON|*.JSON|All files|*.*"
			})
			{
				if (dlg.ShowDialog() != DialogResult.OK) return;

				try
				{
					var project = ReadProjectFromFile(dlg.FileName);
					if (project == null) throw new Exception("Invalid file");

					EnsureDefaults(project);

					_project = project;
					_projectPath = dlg.FileName;
					Refresh2();
				}
				catch (Exception ex)
				{
					CustomMessageBox.Show("Failed to open:\n" + ex.Message, "Error",MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

        public void SaveProject(bool ShowPopup)
        {
            if (_projectPath != null) WriteProject(_projectPath,ShowPopup);
            else SaveProjectAs();
        }

        void SaveProjectAs()
        {
            using (var dlg = new SaveFileDialog
            {
                DefaultExt = ".ESPRJ",
                Filter = "ES Project|*.ESPRJ|ES VPE Project (legacy)|*.ESP|JSON|*.JSON|All files|*.*"
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                _projectPath = dlg.FileName;
                WriteProject(_projectPath,true);
            }
        }

        void WriteProject(string path, bool ShowPopup)
        {
            try
            {
                var ext = Path.GetExtension(path)?.ToLowerInvariant();
                if (ext == ".esprj")
                {
                    var bytes = SerializeProjectToBinary(_project);
                    File.WriteAllBytes(path, bytes);
                }
                else
                {
                    File.WriteAllText(path, JsonConvert.SerializeObject(_project, Formatting.None), Encoding.UTF8);
                }

                UpdateInfo();
                if(ShowPopup)
				{
					CustomMessageBox.Show("Saved :\n" + path, "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("error :\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        ProjectModel ReadProjectFromFile(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (fs.Length >= BinaryHeader.Length + sizeof(int))
                {
                    var headerBytes = new byte[BinaryHeader.Length];
                    fs.Read(headerBytes, 0, headerBytes.Length);
                    var header = Encoding.ASCII.GetString(headerBytes);

                    if (header == BinaryHeader)
                    {
                        var verBytes = new byte[4];
                        fs.Read(verBytes, 0, 4);
                        int version = BitConverter.ToInt32(verBytes, 0);
                        
                        var remaining = new byte[fs.Length - fs.Position];
                        fs.Read(remaining, 0, remaining.Length);

                        var json = DecompressToString(remaining);
                        var project = JsonConvert.DeserializeObject<ProjectModel>(json);
                        return project;
                    }
                }
                
                fs.Seek(0, SeekOrigin.Begin);
                using (var sr = new StreamReader(fs, Encoding.UTF8))
                {
                    var text = sr.ReadToEnd();
                    var project = JsonConvert.DeserializeObject<ProjectModel>(text);
                    return project;
                }
            }
        }

        byte[] SerializeProjectToBinary(ProjectModel project)
        {
            var json = JsonConvert.SerializeObject(project, Formatting.None);
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            byte[] compressed;
            using (var ms = new MemoryStream())
            {
                using (var gzip = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                {
                    gzip.Write(jsonBytes, 0, jsonBytes.Length);
                }
                compressed = ms.ToArray();
            }

            using (var outMs = new MemoryStream())
            {
                var headerBytes = Encoding.ASCII.GetBytes(BinaryHeader);
                outMs.Write(headerBytes, 0, headerBytes.Length);
                
                var verBytes = BitConverter.GetBytes(BinaryVersion);
                outMs.Write(verBytes, 0, verBytes.Length);
                
                outMs.Write(compressed, 0, compressed.Length);

                return outMs.ToArray();
            }
        }

        string DecompressToString(byte[] compressed)
        {
            using (var inMs = new MemoryStream(compressed))
            using (var gzip = new GZipStream(inMs, CompressionMode.Decompress))
            using (var sr = new StreamReader(gzip, Encoding.UTF8))
            {
                return sr.ReadToEnd();
            }
        }

        void EnsureDefaults(ProjectModel data)
        {
            if (data.ExtraDlls == null) data.ExtraDlls = new List<string>();
            if (data.EmbeddedFiles == null) data.EmbeddedFiles = new List<string>();
            if (data.Variables == null) data.Variables = new List<VariableModel>();
            if (data.Controls == null) data.Controls = new List<ControlModel>();
            if (data.EventGroups == null) data.EventGroups = new List<EventGroup>();
        }

        // ---------------------------------------------------------------------
        // Code Generation / Build
        // ---------------------------------------------------------------------

        void GenerateCode()
        {
            var code = CodeGen.GenerateVbNet(_project);
            using (var dlg = new CodeViewDialog(this, code))
            {
                Helpers.CenterOnOwner2(dlg, this);
                dlg.ShowDialog(this);
            }
        }

        void RunProject()
        {
            var pname = _project.Name;
            var projDir = Path.Combine(SettingsManager.ProjectsRoot, pname);
            var appdatadir = Path.Combine(SettingsManager.AppDataRoot);
            var vbPath = Path.Combine(projDir, pname + ".vb");
            var exePath = Path.Combine(projDir, pname + ".exe");

            var code = CodeGen.GenerateVbNet(_project);
            try
            {
                Directory.CreateDirectory(projDir);
                File.WriteAllText(vbPath, code, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Could not write VB file:\n" + ex.Message, "Write Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var bt = _settings.BuildToolsPath;
            var vbc = Path.Combine(bt, "MSBuild", "Current", "Bin", "Roslyn", "vbc.exe");
            if (!File.Exists(vbc)) vbc = "vbc";

            var icoFile = Path.Combine(appdatadir, "ico.ico");
            //Console.WriteLine(icoFile);
            using (var dlg = new BuildProgressDialog(this, vbc, vbPath, exePath, pname, _project.ExtraDlls, _project.EmbeddedFiles, icoFile))
            {
                Helpers.CenterOnOwner2(dlg, this);
                dlg.ShowDialog(this);
            }
        }

        // ---------------------------------------------------------------------
        // Extensions
        // ---------------------------------------------------------------------

        void OpenExtBrowser()
        {
            using (var dlg = new ExtensionBrowserDialog(this))
            {
                Helpers.CenterOnOwner2(dlg, this);
                dlg.ShowDialog(this);
            }
        }

        void ReloadExtensions()
        {
            ExtensionRegistry.Reload();
            CustomMessageBox.Show("Reloaded : " + ExtensionRegistry.Metadata.Count + " extension(s) active.", "Extensions", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Refresh2();
        }

        void OpenExtFolder()
        {
            Directory.CreateDirectory(SettingsManager.ExtensionsDir);
            try
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(SettingsManager.ExtensionsDir)
                    { UseShellExecute = true });
            }
            catch
            {
                CustomMessageBox.Show(SettingsManager.ExtensionsDir, "Extensions Folder");
            }
        }

        // ---------------------------------------------------------------------
        // IDE Settings
        // ---------------------------------------------------------------------

        void OpenIdeSettings()
        {
            using (var dlg = new IDESettingsDialog(this, _settings))
            {
                Helpers.CenterOnOwner2(dlg, this);
                dlg.ShowDialog(this);
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // IDE
            // 
            this.ClientSize = new System.Drawing.Size(292, 269);
            this.Name = "VPE";
            this.ResumeLayout(false);

        }
    }
}