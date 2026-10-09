using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using System.Drawing;
using SmartContextMenu.Extensions;
using SmartContextMenu.Settings;
using SmartContextMenu.Controls;
using SmartContextMenu.Hooks;

namespace SmartContextMenu.Forms
{
    public partial class ApplicationSettingsForm : Form
    {
        private ApplicationSettings _settings;
        private LanguageManager _languageManager;

        private TabPage _tabpWindowBorder;
        private GroupBox _grpbWindowBorder;
        private CheckBox _chkWindowBorderEnabled;
        private CheckBox _chkWindowBorderAccentColor;
        private Label _lblWindowBorderColor;
        private TextBox _txtWindowBorderColor;
        private Button _btnWindowBorderColor;
        private Label _lblWindowBorderThickness;
        private NumericUpDown _nudWindowBorderThickness;
        private Label _lblWindowBorderOpacity;
        private NumericUpDown _nudWindowBorderOpacity;
        private CheckBox _chkWindowBorderRoundCorners;

        /// <summary>左侧导航栏宽度上限（实际按最长条目文字量出来，随字体/DPI 自动变化）。</summary>
        private const int SidebarMaxWidth = 240;

        /// <summary>侧栏与内容区之间分隔线的宽度。</summary>
        private const int SeparatorWidth = 1;

        /// <summary>选中项左侧强调条的宽度。</summary>
        private const int AccentBarWidth = 3;

        private ListBox _lstSections;
        private Panel _pnlContent;
        private Panel _pnlSeparator;
        private int _hotIndex = -1;

        public event EventHandler<EventArgs<ApplicationSettings>> OkClick;

        public ApplicationSettingsForm(ApplicationSettings settings)
        {
            _settings = settings;
            _languageManager = new LanguageManager(_settings.LanguageName);
            InitializeComponent();
            InitializeControls();
        }

        private void InitializeControls()
        {
            tabpGeneral.Text = _languageManager.GetString("tab_settings_general");
            tabpMenuStart.Text = _languageManager.GetString("tab_settings_menu_start");
            tabpMenuSize.Text = _languageManager.GetString("tab_settings_menu_size");
            tabpMenuMoveTo.Text = _languageManager.GetString("tab_settings_menu_move_to");
            tabpMenuDimmer.Text = _languageManager.GetString("tab_settings_menu_dimmer");
            tabpMenu.Text = _languageManager.GetString("tab_settings_menu");
            grpbMouseHotkeys.Text = _languageManager.GetString("grpb_hotkeys");
            grpbLanguage.Text = _languageManager.GetString("grpb_language");
            grpbStartProgram.Text = _languageManager.GetString("grpb_start_program");
            grpbWindowSize.Text = _languageManager.GetString("grpb_window_size");
            grpbSizer.Text = _languageManager.GetString("grpb_sizer");
            grpbDisplay.Text = _languageManager.GetString("grpb_display");
            grpbDimmerColor.Text = _languageManager.GetString("grpb_dimmer_color");
            grpbDimmerTransparency.Text = _languageManager.GetString("grpb_dimmer_transparency");
            grpbNextHotkeys.Text = _languageManager.GetString("grpb_next_hotkeys");
            grpbPreviousHotkeys.Text = _languageManager.GetString("grpb_previous_hotkeys");
            chkEnableHighDPI.Text = _languageManager.GetString("chk_enable_high_dpi");
            clmStartProgramTitle.HeaderText = _languageManager.GetString("clm_start_program_title");
            clmStartProgramPath.HeaderText = _languageManager.GetString("clm_start_program_path");
            clmStartProgramArguments.HeaderText = _languageManager.GetString("clm_start_program_arguments");
            clmStartProgramEdit.ToolTipText = _languageManager.GetString("clm_start_program_edit");
            clmStartProgramDelete.ToolTipText = _languageManager.GetString("clm_start_program_delete");
            clmWindowSizeTitle.HeaderText = _languageManager.GetString("clm_window_size_title");
            clmWindowSizeLeft.HeaderText = _languageManager.GetString("clm_window_size_left");
            clmWindowSizeTop.HeaderText = _languageManager.GetString("clm_window_size_top");
            clmWindowSizeWidth.HeaderText = _languageManager.GetString("clm_window_size_width");
            clmWindowSizeHeight.HeaderText = _languageManager.GetString("clm_window_size_height");
            clmWindowSizeEdit.ToolTipText = _languageManager.GetString("clm_window_size_edit");
            clmWindowSizeDelete.ToolTipText = _languageManager.GetString("clm_window_size_delete");
            clmnMenuItemName.HeaderText = _languageManager.GetString("clm_hotkeys_name");
            clmnHotkeys.HeaderText = _languageManager.GetString("clm_hotkeys_keys");
            lblKey1.Text = _languageManager.GetString("lbl_key1");
            lblKey2.Text = _languageManager.GetString("lbl_key2");
            lblKey3.Text = _languageManager.GetString("lbl_key3");
            lblKey4.Text = _languageManager.GetString("lbl_key4");
            lblMouseButton.Text = _languageManager.GetString("lbl_mouse_button");
            chkShowOnlyOnTitlebar.Text = _languageManager.GetString("chk_show_only_on_titlebar");
            toolTipAddProcessName.SetToolTip(btnAddStartProgram, _languageManager.GetString("btn_add_start_program"));
            toolTipAddProcessName.SetToolTip(btnStartProgramDown, _languageManager.GetString("btn_start_program_down"));
            toolTipAddProcessName.SetToolTip(btnStartProgramUp, _languageManager.GetString("btn_start_program_up"));
            toolTipAddProcessName.SetToolTip(btnAddWindowSize, _languageManager.GetString("btn_add_window_size"));
            toolTipAddProcessName.SetToolTip(btnWindowSizeDown, _languageManager.GetString("btn_window_size_down"));
            toolTipAddProcessName.SetToolTip(btnWindowSizeUp, _languageManager.GetString("btn_window_size_up"));
            toolTipAddProcessName.SetToolTip(btnMenuItemDown, _languageManager.GetString("btn_menu_item_down"));
            toolTipAddProcessName.SetToolTip(btnMenuItemUp, _languageManager.GetString("btn_menu_item_up"));
            btnApply.Text = _languageManager.GetString("settings_btn_apply");
            btnCancel.Text = _languageManager.GetString("settings_btn_cancel");
            Text = _languageManager.GetString("settings_form");

            txtNextHotkeys.Text = _settings.NextMonitor.ToString();
            txtNextHotkeys.Tag = _settings.NextMonitor;
            txtPreviousHotkeys.Text = _settings.PreviousMonitor.ToString();
            txtPreviousHotkeys.Tag = _settings.PreviousMonitor;

            txtDimmerColor.Text = _settings.Dimmer.Color;
            trackbDimmerTransparency.Value = _settings.Dimmer.Transparency;
            lblTransparencyValue.Text = $"{_settings.Dimmer.Transparency}%";

            cmbKey1.ValueMember = "Id";
            cmbKey1.DisplayMember = "Text";
            cmbKey1.DataSource = EnumExtensions.AsEnumerable<VirtualKeyModifier>().Select(x => new { Id = x, Text = x.GetDescription() }).Where(x => !string.IsNullOrEmpty(x.Text)).ToList();
            cmbKey1.SelectedValue = _settings.Key1;

            cmbKey2.ValueMember = "Id";
            cmbKey2.DisplayMember = "Text";
            cmbKey2.DataSource = EnumExtensions.AsEnumerable<VirtualKeyModifier>().Select(x => new { Id = x, Text = x.GetDescription() }).Where(x => !string.IsNullOrEmpty(x.Text)).ToList();
            cmbKey2.SelectedValue = _settings.Key2;

            cmbKey3.ValueMember = "Id";
            cmbKey3.DisplayMember = "Text";
            cmbKey3.DataSource = EnumExtensions.AsEnumerable<VirtualKey>().Select(x => new { Id = x, Text = x.GetDescription() }).Where(x => !string.IsNullOrEmpty(x.Text)).ToList();
            cmbKey3.SelectedValue = _settings.Key3;

            cmbKey4.ValueMember = "Id";
            cmbKey4.DisplayMember = "Text";
            cmbKey4.DataSource = EnumExtensions.AsEnumerable<VirtualKey>().Select(x => new { Id = x, Text = x.GetDescription() }).Where(x => !string.IsNullOrEmpty(x.Text)).ToList();
            cmbKey4.SelectedValue = _settings.Key4;

            cmbMouseButton.ValueMember = "Id";
            cmbMouseButton.DisplayMember = "Text";
            cmbMouseButton.DataSource = EnumExtensions.AsEnumerable<MouseButton>().Select(x => new { Id = x, Text = x.GetDescription() }).Where(x => !string.IsNullOrEmpty(x.Text)).ToList();
            cmbMouseButton.SelectedValue = _settings.MouseButton;
            chkShowOnlyOnTitlebar.Checked = _settings.ShowOnlyOnTitlebar;

            listBoxLanguage.DisplayMember = "Text";
            listBoxLanguage.ValueMember = "Value";

            var languageItems = new[] {
                new { Text = "English", Value = "en" },
                new { Text = "Deutsch", Value = "de" },
                new { Text = "Français", Value = "fr" },
                new { Text = "Italiano", Value = "it" },
                new { Text = "Magyar", Value = "hu" },
                new { Text = "Español", Value = "es" },
                new { Text = "Português", Value = "pt" },
                new { Text = "Русский", Value = "ru" },
                new { Text = "Српски", Value = "sr" },
                new { Text = "Slovenščina", Value = "sl" },
                new { Text = "Tiếng Việt", Value = "vi" },
                new { Text = "Bahasa Indonesia", Value = "id" },
                new { Text = "עִברִית", Value = "he" },
                new { Text = "தமிழ்", Value = "ta" },
                new { Text = "简体中文", Value = "zh_cn" },
                new { Text = "繁體中文", Value = "zh_tw"},
                new { Text = "日本語", Value = "ja" },
                new { Text = "한국어", Value = "ko" }
            };

            listBoxLanguage.DataSource = languageItems;
            listBoxLanguage.SelectedValue = _settings.LanguageName;

            cmbSizer.Items.Add(_languageManager.GetString("sizer_window_with_margins"));
            cmbSizer.Items.Add(_languageManager.GetString("sizer_window_without_margins"));
            cmbSizer.Items.Add(_languageManager.GetString("sizer_window_client_area"));
            cmbSizer.SelectedIndex = (int)_settings.Sizer;
            chkEnableHighDPI.Checked = _settings.EnableHighDPI;

            var items = new List<Settings.MenuItem>();
            foreach(var item in _settings.MenuItems.Items)
            {
                items.Add((Settings.MenuItem)item.Clone());
            }

            FillGridViewByItems(gvHotkeys, items);
            FillGridViewByWindowSizeItems(gvWindowSize, _settings.MenuItems.WindowSizeItems);
            FillGridViewByStartProgramItems(gvStartProgram, _settings.MenuItems.StartProgramItems);

            InitializeWindowBorderControls();
            BuildSidebarNavigation();
        }

        /// <summary>
        /// 把原来的「顶部选项卡条」换成「左侧导航栏」。
        ///
        /// 为什么换：选项卡横向排列时，宽度是硬瓶颈，页面一多就放不下，
        /// 原生控件会弹出滚动箭头把后面的页面藏起来（本机实测第 7 个就溢出了）。
        /// 竖向导航的瓶颈是高度，而设置项本来就是竖着长的，加多少页面都不用改结构。
        ///
        /// 实现上刻意保守：
        /// 1) 现有 TabPage 和里面的控件一行不动，仍然由 TabControl 承载，只是把标签条藏起来；
        /// 2) Designer 生成的文件一个字不改，全部重排都在这里做；
        /// 3) 尺寸全部从控件本身量出来（标签条高度用 DisplayRectangle.Y，
        ///    侧栏宽度按最长条目文字量），不写死像素 —— 本机 AutoScale 会把窗体缩到设计的 76%，
        ///    任何写死的尺寸在别的机器或别的语言上都会失准。
        /// </summary>
        private void BuildSidebarNavigation()
        {
            SuspendLayout();

            // 标签条实际高度（随 DPI / 字体变化），后面靠它把标签条顶出可视区
            var stripHeight = tabMain.DisplayRectangle.Y;
            var sidebarWidth = MeasureSidebarWidth();
            var contentHeight = tabMain.Height;

            // 窗体加宽量 = 侧栏宽度 + 分隔线，这样内容区宽度和原来完全一致，
            // 页面里的分组框（721）和网格（705）不会被挤压
            ClientSize = new Size(ClientSize.Width + sidebarWidth + SeparatorWidth, ClientSize.Height);

            Controls.Remove(tabMain);
            tabMain.Dock = DockStyle.None;

            // 侧栏底色比窗体略深一点，形成"导航区/内容区"的层次，不用生硬的边框
            _lstSections = new ListBox
            {
                Location = new Point(0, 0),
                Size = new Size(sidebarWidth, contentHeight),
                IntegralHeight = false,
                BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = Font.Height + 16,
                BackColor = Shade(SystemColors.Control, -10),
                ForeColor = SystemColors.ControlText,
                TabStop = false
            };
            _lstSections.DrawItem += SidebarDrawItem;
            _lstSections.SelectedIndexChanged += SidebarSelectedIndexChanged;
            _lstSections.MouseMove += SidebarMouseMove;
            _lstSections.MouseLeave += SidebarMouseLeave;

            for (var i = 0; i < tabMain.TabCount; i++)
            {
                _lstSections.Items.Add(tabMain.TabPages[i].Text);
            }

            // 侧栏与内容区之间的分隔线，比侧栏底色再深一档
            _pnlSeparator = new Panel
            {
                Location = new Point(sidebarWidth, 0),
                Size = new Size(SeparatorWidth, contentHeight),
                BackColor = Shade(SystemColors.Control, -35)
            };

            _pnlContent = new Panel
            {
                Location = new Point(sidebarWidth + SeparatorWidth, 0),
                Size = new Size(ClientSize.Width - sidebarWidth - SeparatorWidth, contentHeight)
            };
            _pnlContent.Controls.Add(tabMain);

            // 先加内容区、再加分隔线和侧栏，保证侧栏压在内容区之上
            Controls.Add(_pnlContent);
            Controls.Add(_pnlSeparator);
            Controls.Add(_lstSections);

            LayoutContent(stripHeight);

            // 底部两个按钮在 Designer 里是固定坐标贴右的（整个窗体没有任何 Anchor），
            // 窗体加宽后必须跟着右移，否则会离右边框越来越远
            btnApply.Left += sidebarWidth + SeparatorWidth;
            btnCancel.Left += sidebarWidth + SeparatorWidth;
            btnApply.BringToFront();
            btnCancel.BringToFront();

            if (_lstSections.Items.Count > 0)
            {
                _lstSections.SelectedIndex = 0;
            }

            ResumeLayout();
        }

        /// <summary>
        /// 从系统控件色派生一个深浅档位。用派生色而不是写死 RGB，
        /// 这样在浅色/深色主题下都不会出现"一块突兀的灰"。
        /// </summary>
        private static Color Shade(Color color, int delta) => Color.FromArgb(
            Math.Min(255, Math.Max(0, color.R + delta)),
            Math.Min(255, Math.Max(0, color.G + delta)),
            Math.Min(255, Math.Max(0, color.B + delta)));

        /// <summary>侧栏宽度按最长条目文字实测并留出内边距，不写死像素。</summary>
        private int MeasureSidebarWidth()
        {
            var needed = 0;
            for (var i = 0; i < tabMain.TabCount; i++)
            {
                var size = TextRenderer.MeasureText(tabMain.TabPages[i].Text, Font);
                needed = Math.Max(needed, size.Width);
            }

            return Math.Min(needed + 28, SidebarMaxWidth);
        }

        /// <summary>
        /// 让 TabControl 顶部的原生标签条落到容器可视区之外，从而被容器裁掉。
        /// 只挪位置不改尺寸，所以页面内容完全不受影响。
        /// </summary>
        private void LayoutContent(int stripHeight)
        {
            tabMain.Bounds = new Rectangle(
                0,
                -stripHeight,
                _pnlContent.ClientSize.Width,
                _pnlContent.ClientSize.Height + stripHeight);
        }

        private void SidebarSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_lstSections.SelectedIndex >= 0 && _lstSections.SelectedIndex < tabMain.TabCount)
            {
                tabMain.SelectedIndex = _lstSections.SelectedIndex;
            }
        }

        /// <summary>
        /// 自绘条目。默认 ListBox 的选中态是一整块高饱和蓝，在设置窗口里很扎眼；
        /// 这里改成现代导航栏的常见做法：底色深浅区分 + 选中项左侧一条强调条。
        /// 不用粗体字是为了避免额外 Font 对象的释放问题（Designer 已经重写了 Dispose）。
        /// </summary>
        private void SidebarDrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _lstSections.Items.Count)
            {
                return;
            }

            var selected = e.Index == _lstSections.SelectedIndex;
            var hot = !selected && e.Index == _hotIndex;

            var background = selected
                ? Shade(SystemColors.Control, -30)
                : (hot ? Shade(SystemColors.Control, -20) : _lstSections.BackColor);

            using (var brush = new SolidBrush(background))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            // 选中项左侧的强调条
            if (selected)
            {
                using (var accent = new SolidBrush(SystemColors.Highlight))
                {
                    e.Graphics.FillRectangle(
                        accent,
                        new Rectangle(e.Bounds.X, e.Bounds.Y, AccentBarWidth, e.Bounds.Height));
                }
            }

            var textBounds = new Rectangle(
                e.Bounds.X + AccentBarWidth + 10,
                e.Bounds.Y,
                e.Bounds.Width - AccentBarWidth - 16,
                e.Bounds.Height);

            TextRenderer.DrawText(
                e.Graphics,
                _lstSections.Items[e.Index].ToString(),
                e.Font,
                textBounds,
                SystemColors.ControlText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        /// <summary>ListBox 自身没有悬停态，这里自己跟踪鼠标位置来重绘。</summary>
        private void SidebarMouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            var index = _lstSections.IndexFromPoint(e.Location);
            if (index == ListBox.NoMatches)
            {
                index = -1;
            }

            if (index != _hotIndex)
            {
                _hotIndex = index;
                _lstSections.Invalidate();
            }
        }

        private void SidebarMouseLeave(object sender, EventArgs e)
        {
            if (_hotIndex != -1)
            {
                _hotIndex = -1;
                _lstSections.Invalidate();
            }
        }

        /// <summary>
        /// 语言文件里没有对应条目时返回空串，这里统一回落到英文，
        /// 避免未翻译的语言出现空白标签。
        /// </summary>
        private string GetBorderString(string name, string fallback)
        {
            var value = _languageManager.GetString(name);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        /// <summary>
        /// 构建「置顶边框」选项卡。
        /// 为了让新增功能不侵入 Designer 生成的文件，控件全部用代码创建。
        /// </summary>
        private void InitializeWindowBorderControls()
        {
            var border = _settings.WindowBorder;
            border.Normalize();

            _tabpWindowBorder = new TabPage
            {
                Location = new Point(4, 25),
                Padding = new Padding(3),
                Size = new Size(745, 483),
                UseVisualStyleBackColor = true,
                Text = GetBorderString("tab_settings_window_border", "Always On Top Border")
            };

            _grpbWindowBorder = new GroupBox
            {
                // 和其余页面第一个分组框保持同一边距（原来这里是 12,12，别的页是 11,20）
                Location = new Point(11, 20),
                Size = new Size(721, 256),
                TabStop = false,
                // 窗体为了容纳 7 个选项卡会被撑宽，分组框跟着变宽才不会右侧留白
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = GetBorderString("grpb_window_border", "Border appearance")
            };

            _chkWindowBorderEnabled = new CheckBox
            {
                AutoSize = true,
                Location = new Point(20, 32),
                Text = GetBorderString("chk_window_border_enabled", "Show an accent color border when a window is always on top"),
                Checked = border.Enabled
            };
            _chkWindowBorderEnabled.CheckedChanged += WindowBorderEnabledChanged;

            _chkWindowBorderAccentColor = new CheckBox
            {
                AutoSize = true,
                Location = new Point(20, 68),
                Text = GetBorderString("chk_window_border_accent", "Use the system accent color"),
                Checked = border.UseAccentColor
            };
            _chkWindowBorderAccentColor.CheckedChanged += WindowBorderAccentColorChanged;

            _lblWindowBorderColor = new Label
            {
                AutoSize = true,
                Location = new Point(20, 108),
                Text = GetBorderString("lbl_window_border_color", "Border color")
            };

            _txtWindowBorderColor = new TextBox
            {
                Location = new Point(240, 104),
                Size = new Size(120, 23),
                Text = border.Color
            };

            _btnWindowBorderColor = new Button
            {
                Location = new Point(368, 103),
                Size = new Size(90, 25),
                Text = GetBorderString("btn_window_border_color", "Choose...")
            };
            _btnWindowBorderColor.Click += WindowBorderColorClick;

            _lblWindowBorderThickness = new Label
            {
                AutoSize = true,
                Location = new Point(20, 148),
                Text = GetBorderString("lbl_window_border_thickness", "Thickness (px)")
            };

            _nudWindowBorderThickness = new NumericUpDown
            {
                Location = new Point(240, 144),
                Size = new Size(80, 23),
                Minimum = WindowBorderSettings.MinThickness,
                Maximum = WindowBorderSettings.MaxThickness
            };
            _nudWindowBorderThickness.Value = Math.Min(Math.Max(border.Thickness, WindowBorderSettings.MinThickness), WindowBorderSettings.MaxThickness);

            _lblWindowBorderOpacity = new Label
            {
                AutoSize = true,
                Location = new Point(20, 188),
                Text = GetBorderString("lbl_window_border_opacity", "Opacity (%)")
            };

            _nudWindowBorderOpacity = new NumericUpDown
            {
                Location = new Point(240, 184),
                Size = new Size(80, 23),
                Minimum = WindowBorderSettings.MinOpacity,
                Maximum = WindowBorderSettings.MaxOpacity
            };
            _nudWindowBorderOpacity.Value = Math.Min(Math.Max(border.Opacity, WindowBorderSettings.MinOpacity), WindowBorderSettings.MaxOpacity);

            _chkWindowBorderRoundCorners = new CheckBox
            {
                AutoSize = true,
                Location = new Point(20, 222),
                Text = GetBorderString("chk_window_border_round_corners", "Follow the window corner rounding"),
                Checked = border.RoundCorners
            };

            _grpbWindowBorder.Controls.Add(_chkWindowBorderEnabled);
            _grpbWindowBorder.Controls.Add(_chkWindowBorderAccentColor);
            _grpbWindowBorder.Controls.Add(_lblWindowBorderColor);
            _grpbWindowBorder.Controls.Add(_txtWindowBorderColor);
            _grpbWindowBorder.Controls.Add(_btnWindowBorderColor);
            _grpbWindowBorder.Controls.Add(_lblWindowBorderThickness);
            _grpbWindowBorder.Controls.Add(_nudWindowBorderThickness);
            _grpbWindowBorder.Controls.Add(_lblWindowBorderOpacity);
            _grpbWindowBorder.Controls.Add(_nudWindowBorderOpacity);
            _grpbWindowBorder.Controls.Add(_chkWindowBorderRoundCorners);

            _tabpWindowBorder.Controls.Add(_grpbWindowBorder);
            tabMain.Controls.Add(_tabpWindowBorder);

            UpdateWindowBorderControlState();
        }

        private void UpdateWindowBorderControlState()
        {
            var enabled = _chkWindowBorderEnabled.Checked;
            var useAccentColor = _chkWindowBorderAccentColor.Checked;

            _chkWindowBorderAccentColor.Enabled = enabled;
            _chkWindowBorderRoundCorners.Enabled = enabled;
            _lblWindowBorderColor.Enabled = enabled && !useAccentColor;
            _txtWindowBorderColor.Enabled = enabled && !useAccentColor;
            _btnWindowBorderColor.Enabled = enabled && !useAccentColor;
            _lblWindowBorderThickness.Enabled = enabled;
            _nudWindowBorderThickness.Enabled = enabled;
            _lblWindowBorderOpacity.Enabled = enabled;
            _nudWindowBorderOpacity.Enabled = enabled;
        }

        private void WindowBorderEnabledChanged(object sender, EventArgs e) => UpdateWindowBorderControlState();

        private void WindowBorderAccentColorChanged(object sender, EventArgs e) => UpdateWindowBorderControlState();

        private void WindowBorderColorClick(object sender, EventArgs e)
        {
            var color = Color.Black;
            try
            {
                color = ColorTranslator.FromHtml(_txtWindowBorderColor.Text);
            }
            catch
            {
            }

            var dialog = new ColorDialog
            {
                AllowFullOpen = true,
                AnyColor = true,
                FullOpen = true,
                Color = color
            };

            if (dialog.ShowDialog() != DialogResult.Cancel)
            {
                _txtWindowBorderColor.Text = ColorTranslator.ToHtml(dialog.Color);
            }
        }

        private void GridViewStartProgramCellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var grid = (DataGridView)sender;
            if (grid.Columns[e.ColumnIndex] is DataGridViewButtonColumn && e.RowIndex >= 0)
            {
                var row = grid.Rows[e.RowIndex];
                if (e.ColumnIndex == 3 && !row.ReadOnly && row.Tag is StartProgramMenuItem menuItem)
                {
                    var dialog = new StartProgramForm(_languageManager, menuItem);
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        row.Cells[0].Value = dialog.MenuItem.Title;
                        row.Cells[1].Value = dialog.MenuItem.FileName;
                        row.Cells[2].Value = dialog.MenuItem.Arguments;
                        row.Tag = dialog.MenuItem;
                    }
                }

                if (e.ColumnIndex == 4)
                {
                    grid.Rows.RemoveAt(e.RowIndex);
                }
            }
        }

        private void GridViewWindowSizeCellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var grid = (DataGridView)sender;

            if (grid.Columns[e.ColumnIndex] is DataGridViewButtonColumn && e.RowIndex >= 0)
            {
                var row = grid.Rows[e.RowIndex];
                if (e.ColumnIndex == 6 && !row.ReadOnly && row.Tag is WindowSizeMenuItem menuItem)
                {
                    var dialog = new SizeSettingsForm(_languageManager, menuItem);
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        row.Cells[0].Value = dialog.MenuItem.Title;
                        row.Cells[1].Value = dialog.MenuItem.Left.HasValue ? dialog.MenuItem.Left.ToString() : string.Empty;
                        row.Cells[2].Value = dialog.MenuItem.Top.HasValue ? dialog.MenuItem.Top.ToString() : string.Empty;
                        row.Cells[3].Value = dialog.MenuItem.Width.ToString();
                        row.Cells[4].Value = dialog.MenuItem.Height.ToString();
                        row.Cells[5].Value = dialog.MenuItem.Shortcut.ToString();

                        menuItem.Title = dialog.MenuItem.Title;
                        menuItem.Left = dialog.MenuItem.Left;
                        menuItem.Top = dialog.MenuItem.Top;
                        menuItem.Width = dialog.MenuItem.Width;
                        menuItem.Height = dialog.MenuItem.Height;
                        menuItem.Shortcut.Key1 = dialog.MenuItem.Shortcut.Key1;
                        menuItem.Shortcut.Key2 = dialog.MenuItem.Shortcut.Key2;
                        menuItem.Shortcut.Key3 = dialog.MenuItem.Shortcut.Key3;
                    }
                }

                if (e.ColumnIndex == 7)
                {
                    grid.Rows.RemoveAt(e.RowIndex);
                }
            }
        }

        private void GridViewHotkeysCellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var grid = (DataGridView)sender;
            if (grid.Columns[e.ColumnIndex] is DataGridViewButtonColumn && e.RowIndex >= 0)
            {
                var row = grid.Rows[e.RowIndex];
                if (!row.ReadOnly)
                {
                    ShowHotkeysForm(row);
                }
            }

            if (grid.Columns[e.ColumnIndex] is DataGridViewCheckBoxColumn && e.RowIndex >= 0)
            {
                var row = grid.Rows[e.RowIndex];
                var cell = (DataGridViewCheckBoxCell)row.Cells[e.ColumnIndex];
                cell.Value = !(bool)cell.Value;
                var menuItem = (Settings.MenuItem)row.Tag;
                menuItem.Show = (bool)cell.Value;
            }
        }

        private void GridViewHotkeysCellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            var grid = (DataGridView)sender;
            if ((e.ColumnIndex == 0 || e.ColumnIndex == 1) && e.RowIndex >= 0)
            {
                var row = grid.Rows[e.RowIndex];
                if (!row.ReadOnly)
                {
                    ShowHotkeysForm(row);
                }
            }
        }

        private void GridViewStartProgramCellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            var grid = (DataGridView)sender;
            if ((e.ColumnIndex == 0 || e.ColumnIndex == 1 || e.ColumnIndex == 2) && e.RowIndex >= 0 && grid.Rows[e.RowIndex].Tag is StartProgramMenuItem menuItem)
            {
                var row = grid.Rows[e.RowIndex];
                if (!row.ReadOnly)
                {
                    var dialog = new StartProgramForm(_languageManager, menuItem);
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        row.Cells[0].Value = dialog.MenuItem.Title;
                        row.Cells[1].Value = dialog.MenuItem.FileName;
                        row.Cells[2].Value = dialog.MenuItem.Arguments;
                        row.Tag = dialog.MenuItem;
                    }
                }
            }
        }

        private void GridViewWindowSizeCellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            var grid = (DataGridView)sender;
            if ((e.ColumnIndex == 0 || e.ColumnIndex == 1 || e.ColumnIndex == 2 || e.ColumnIndex == 3 || e.ColumnIndex == 4 || e.ColumnIndex == 5) && e.RowIndex >= 0 && grid.Rows[e.RowIndex].Tag is WindowSizeMenuItem menuItem)
            {
                var row = grid.Rows[e.RowIndex];
                if (!row.ReadOnly)
                {
                    var dialog = new SizeSettingsForm(_languageManager, menuItem);
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        row.Cells[0].Value = dialog.MenuItem.Title;
                        row.Cells[1].Value = dialog.MenuItem.Left.HasValue ? dialog.MenuItem.Left.ToString() : string.Empty;
                        row.Cells[2].Value = dialog.MenuItem.Top.HasValue ? dialog.MenuItem.Top.ToString() : string.Empty;
                        row.Cells[3].Value = dialog.MenuItem.Width.ToString();
                        row.Cells[4].Value = dialog.MenuItem.Height.ToString();
                        row.Cells[5].Value = dialog.MenuItem.Shortcut.ToString();

                        menuItem.Title = dialog.MenuItem.Title;
                        menuItem.Left = dialog.MenuItem.Left;
                        menuItem.Top = dialog.MenuItem.Top;
                        menuItem.Width = dialog.MenuItem.Width;
                        menuItem.Height = dialog.MenuItem.Height;
                        menuItem.Shortcut.Key1 = dialog.MenuItem.Shortcut.Key1;
                        menuItem.Shortcut.Key2 = dialog.MenuItem.Shortcut.Key2;
                        menuItem.Shortcut.Key3 = dialog.MenuItem.Shortcut.Key3;
                    }
                }
            }
        }

        private void ButtonAddStartProgramClick(object sender, EventArgs e)
        {
            var dialog = new StartProgramForm(_languageManager, null);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                var index = gvStartProgram.Rows.Add();
                var row = gvStartProgram.Rows[index];
                row.Cells[0].Value = dialog.MenuItem.Title;
                row.Cells[1].Value = dialog.MenuItem.FileName;
                row.Cells[2].Value = dialog.MenuItem.Arguments;
                row.Cells[3].ToolTipText = _languageManager.GetString("clm_start_program_edit");
                row.Cells[4].ToolTipText = _languageManager.GetString("clm_start_program_delete");
                row.Tag = dialog.MenuItem;
            }
        }

        private void ButtonAddWindowSizeClick(object sender, EventArgs e)
        {
            var dialog = new SizeSettingsForm(_languageManager, new WindowSizeMenuItem { Width = 1, Height = 1 });
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                var index = gvWindowSize.Rows.Add();
                var row = gvWindowSize.Rows[index];
                row.Cells[0].Value = dialog.MenuItem.Title;
                row.Cells[1].Value = dialog.MenuItem.Left.HasValue ? dialog.MenuItem.Left.ToString() : string.Empty;
                row.Cells[2].Value = dialog.MenuItem.Top.HasValue ? dialog.MenuItem.Top.ToString() : string.Empty;
                row.Cells[3].Value = dialog.MenuItem.Width.ToString();
                row.Cells[4].Value = dialog.MenuItem.Height.ToString();
                row.Cells[5].Value = dialog.MenuItem.Shortcut.ToString();
                row.Cells[6].ToolTipText = _languageManager.GetString("clm_window_size_edit");
                row.Cells[7].ToolTipText = _languageManager.GetString("clm_window_size_delete");
                row.Tag = dialog.MenuItem;
            }
        }

        private void ButtonArrowUpClick(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var grid = button.Name == "btnWindowSizeUp" ? gvWindowSize : gvStartProgram;
            if (grid.SelectedRows.Count > 0)
            {
                var index = grid.SelectedRows[0].Index;
                var newIndex = index > 0 ? index - 1 : 0;
                var selectedRow = grid.SelectedRows[0];
                grid.Rows.RemoveAt(index);
                grid.Rows.Insert(newIndex, selectedRow);
                grid.Rows[newIndex].Selected = true;
                grid.CurrentCell = grid.Rows[newIndex].Cells[0];
            }
        }

        private void ButtonArrowDownClick(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var grid = button.Name == "btnWindowSizeDown" ? gvWindowSize : gvStartProgram;
            if (grid.SelectedRows.Count > 0)
            {
                var index = grid.SelectedRows[0].Index;
                var newIndex = index < grid.Rows.Count - 1 ? index + 1 : grid.Rows.Count - 1;
                var selectedRow = grid.SelectedRows[0];
                grid.Rows.RemoveAt(index);
                grid.Rows.Insert(newIndex, selectedRow);
                grid.Rows[newIndex].Selected = true;
                grid.CurrentCell = grid.Rows[newIndex].Cells[0];
            }
        }

        private void ButtonMenuItemUpClick(object sender, EventArgs e)
        {
            if (gvHotkeys.SelectedRows.Count > 0)
            {
                var items = (IList<Settings.MenuItem>)gvHotkeys.Tag;
                var item = (Settings.MenuItem)gvHotkeys.SelectedRows[0].Tag;
                var list = FindList(items, item);
                if (list != null && list.Count > 1)
                {
                    var index = list.IndexOf(item);
                    if (index > 0)
                    {
                        ((List<Settings.MenuItem>)list).Reverse(index - 1, 2);
                        gvHotkeys.Rows.Clear();
                        FillGridViewByItems(gvHotkeys, items);
                        foreach (DataGridViewRow row in gvHotkeys.Rows)
                        {
                            if (row.Tag == item)
                            {
                                row.Selected = true;
                                gvHotkeys.CurrentCell = row.Cells[0];
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void ButtonMenuItemDownClick(object sender, EventArgs e)
        {
            if (gvHotkeys.SelectedRows.Count > 0)
            {
                var items = (IList<Settings.MenuItem>)gvHotkeys.Tag;
                var item = (Settings.MenuItem)gvHotkeys.SelectedRows[0].Tag;
                var list = FindList(items, item);
                if (list != null && list.Count > 1)
                {
                    var index = list.IndexOf(item);
                    if (index < list.Count - 1)
                    {
                        ((List<Settings.MenuItem>)list).Reverse(index, 2);
                        gvHotkeys.Rows.Clear();
                        FillGridViewByItems(gvHotkeys, items);
                        foreach (DataGridViewRow row in gvHotkeys.Rows)
                        {
                            if (row.Tag == item)
                            {
                                row.Selected = true;
                                gvHotkeys.CurrentCell = row.Cells[0];
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void TrackbDimmerTransparencyValueChanged(object sender, EventArgs e)
        {
            lblTransparencyValue.Text = $"{trackbDimmerTransparency.Value}%";
        }

        private void ButtonNextHotkeysClick(object sender, EventArgs e)
        {
            var shortcut = txtNextHotkeys.Tag as KeyboardShortcut ?? _settings.NextMonitor;
            var form = new HotkeysForm(_languageManager, shortcut);
            var result = form.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                txtNextHotkeys.Tag = form.Shortcut;
                txtNextHotkeys.Text = form.Shortcut.ToString();
            }
        }

        private void ButtonPreviousHotkeysClick(object sender, EventArgs e)
        {
            var shortcut = txtPreviousHotkeys.Tag as KeyboardShortcut ?? _settings.PreviousMonitor;
            var form = new HotkeysForm(_languageManager, shortcut);
            var result = form.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                txtPreviousHotkeys.Tag = form.Shortcut;
                txtPreviousHotkeys.Text = form.Shortcut.ToString();
            }
        }

        private void ButtonChooseDimmerColorClick(object sender, EventArgs e)
        {
            var color = Color.Black;
            try
            {
                color = ColorTranslator.FromHtml(txtDimmerColor.Text);
            }
            catch
            {
            }

            var dialog = new ColorDialog
            {
                AllowFullOpen = true,
                AnyColor = true,
                FullOpen = true,
                Color = color
            };

            if (dialog.ShowDialog() != DialogResult.Cancel)
            {
                txtDimmerColor.Text = ColorTranslator.ToHtml(dialog.Color);
            }
        }

        private void ButtonApplyClick(object sender, EventArgs e)
        {
            var settings = new ApplicationSettings();

            foreach (DataGridViewRow row in gvWindowSize.Rows)
            {
                if (row.Tag is WindowSizeMenuItem item)
                {
                    settings.MenuItems.WindowSizeItems.Add((WindowSizeMenuItem)item.Clone());
                }
            }

            foreach (DataGridViewRow row in gvStartProgram.Rows)
            {
                if (row.Tag is StartProgramMenuItem item)
                {
                    settings.MenuItems.StartProgramItems.Add((StartProgramMenuItem)item.Clone());
                }
            }

            settings.Key1 = (VirtualKeyModifier)cmbKey1.SelectedValue;
            settings.Key2 = (VirtualKeyModifier)cmbKey2.SelectedValue;
            settings.Key3 = (VirtualKey)cmbKey3.SelectedValue;
            settings.Key4 = (VirtualKey)cmbKey4.SelectedValue;
            settings.MouseButton = (MouseButton)cmbMouseButton.SelectedValue;
            settings.ShowOnlyOnTitlebar = chkShowOnlyOnTitlebar.Checked;
            settings.MenuItems.Items = (IList<Settings.MenuItem>)gvHotkeys.Tag;
            settings.Dimmer.Color = txtDimmerColor.Text;
            settings.Dimmer.Transparency = trackbDimmerTransparency.Value;
            settings.Sizer = (WindowSizerType)cmbSizer.SelectedIndex;
            settings.EnableHighDPI = chkEnableHighDPI.Checked;
            settings.MenuDisabledInterval = _settings.MenuDisabledInterval;
            settings.LowLevelHooksTimeout = _settings.LowLevelHooksTimeout;
            settings.LanguageName = listBoxLanguage.SelectedValue == null ? string.Empty : listBoxLanguage.SelectedValue.ToString();

            settings.WindowBorder.Enabled = _chkWindowBorderEnabled.Checked;
            settings.WindowBorder.UseAccentColor = _chkWindowBorderAccentColor.Checked;
            settings.WindowBorder.Color = _txtWindowBorderColor.Text;
            settings.WindowBorder.Thickness = (int)_nudWindowBorderThickness.Value;
            settings.WindowBorder.Opacity = (int)_nudWindowBorderOpacity.Value;
            settings.WindowBorder.RoundCorners = _chkWindowBorderRoundCorners.Checked;
            settings.WindowBorder.Normalize();

            if (txtNextHotkeys.Tag is KeyboardShortcut nextShortcut)
            {
                settings.NextMonitor.Key1 = nextShortcut.Key1;
                settings.NextMonitor.Key2 = nextShortcut.Key2;
                settings.NextMonitor.Key3 = nextShortcut.Key3;
            }

            if (txtPreviousHotkeys.Tag is KeyboardShortcut previousShortcut)
            {
                settings.PreviousMonitor.Key1 = previousShortcut.Key1;
                settings.PreviousMonitor.Key2 = previousShortcut.Key2;
                settings.PreviousMonitor.Key3 = previousShortcut.Key3;
            }

            if (!settings.Equals(_settings))
            {
                OkClick?.Invoke(this, new EventArgs<ApplicationSettings>(settings));
            }

            Close();
        }

        private void ButtonCancelClick(object sender, EventArgs e)
        {
            Close();
        }

        private void KeyDownClick(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == 13)
            {
                ButtonApplyClick(sender, e);
            }

            if (e.KeyValue == 27)
            {
                Close();
            }
        }

        private void ShowHotkeysForm(DataGridViewRow row)
        {
            var menuItem = (Settings.MenuItem)row.Tag;
            var form = new HotkeysForm(_languageManager, menuItem.Shortcut);
            var result = form.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                menuItem.Shortcut = form.Shortcut;
                row.Cells[1].Value = menuItem.Shortcut.ToString();
                row.Tag = menuItem;
            }
        }

        private void FillGridViewByItems(DataGridView gridView, IList<Settings.MenuItem> items)
        {
            gridView.Tag = items;
            foreach (var item in items)
            {
                if (item.Type == MenuItemType.Item)
                {
                    var index = gridView.Rows.Add();
                    var row = gridView.Rows[index];
                    var title = GetTransparencyTitle(item.Name);
                    title = title != null ? title : _languageManager.GetString(item.Name);
                    row.Tag = item;
                    row.Cells[0].Value = title;
                    row.Cells[1].Value = item == null ? string.Empty : item.Shortcut.ToString();
                    ((DataGridViewCheckBoxCell)row.Cells[2]).Value = item.Show;
                    ((DataGridViewCheckBoxCell)row.Cells[2]).ToolTipText = _languageManager.GetString("clm_hotkeys_show_tooltip");
                }

                if (item.Type == MenuItemType.Separator)
                {
                    var index = gridView.Rows.Add();
                    var row = gridView.Rows[index];
                    var title = _languageManager.GetString("separator");
                    row.Tag = item;
                    row.ReadOnly = true;
                    row.Cells[0].Value = title;
                    row.Cells[1].Value = item == null ? string.Empty : item.Shortcut.ToString();
                    ((DataGridViewCheckBoxCell)row.Cells[2]).Value = item.Show;
                    ((DataGridViewCheckBoxCell)row.Cells[2]).ToolTipText = _languageManager.GetString("clm_hotkeys_show_tooltip");
                    ((DataGridViewDisableButtonCell)row.Cells[3]).Enabled = false;
                }

                if (item.Type == MenuItemType.Group)
                {
                    var index = gridView.Rows.Add();
                    var row = gridView.Rows[index];
                    row.Tag = item;
                    row.ReadOnly = true;
                    row.Cells[0].Value = _languageManager.GetString(item.Name);
                    ((DataGridViewCheckBoxCell)row.Cells[2]).Value = item.Show;
                    ((DataGridViewCheckBoxCell)row.Cells[2]).ToolTipText = _languageManager.GetString("clm_hotkeys_show_tooltip");
                    ((DataGridViewDisableButtonCell)row.Cells[3]).Enabled = false;

                    foreach (var subItem in item.Items)
                    {
                        if (subItem.Type == MenuItemType.Item)
                        {
                            var subItemIndex = gridView.Rows.Add();
                            var subItemRow = gridView.Rows[subItemIndex];
                            var title = GetTransparencyTitle(subItem.Name);
                            title = title != null ? title : _languageManager.GetString(subItem.Name);
                            subItemRow.Tag = subItem;
                            subItemRow.Cells[0].Value = title;
                            subItemRow.Cells[1].Value = subItem == null ? string.Empty : subItem.Shortcut.ToString();
                            ((DataGridViewCheckBoxCell)subItemRow.Cells[2]).Value = subItem.Show;
                            ((DataGridViewCheckBoxCell)subItemRow.Cells[2]).ToolTipText = _languageManager.GetString("clm_hotkeys_show_tooltip");
                            var padding = subItemRow.Cells[0].Style.Padding;
                            subItemRow.Cells[0].Style.Padding = new Padding(20, padding.Top, padding.Right, padding.Bottom);
                        }

                        if (subItem.Type == MenuItemType.Separator)
                        {
                            var subItemIndex = gridView.Rows.Add();
                            var subItemRow = gridView.Rows[subItemIndex];
                            var title = _languageManager.GetString("separator");
                            subItemRow.Tag = subItem;
                            subItemRow.ReadOnly = true;
                            subItemRow.Cells[0].Value = title;
                            subItemRow.Cells[1].Value = subItem == null ? string.Empty : subItem.Shortcut.ToString();
                            ((DataGridViewCheckBoxCell)subItemRow.Cells[2]).Value = subItem.Show;
                            ((DataGridViewCheckBoxCell)subItemRow.Cells[2]).ToolTipText = _languageManager.GetString("clm_hotkeys_show_tooltip");
                            ((DataGridViewDisableButtonCell)subItemRow.Cells[3]).Enabled = false;
                            var padding = subItemRow.Cells[0].Style.Padding;
                            subItemRow.Cells[0].Style.Padding = new Padding(20, padding.Top, padding.Right, padding.Bottom);
                        }
                    }
                }
            }
        }

        private void FillGridViewByWindowSizeItems(DataGridView gridView, IList<WindowSizeMenuItem> items)
        {
            foreach (var item in items)
            {
                if (item.Type == MenuItemType.Item)
                {
                    var index = gridView.Rows.Add();
                    var row = gridView.Rows[index];
                    row.Tag = (WindowSizeMenuItem)item.Clone();
                    row.Cells[0].Value = item.Title;
                    row.Cells[1].Value = item.Left.HasValue ? item.Left.ToString() : string.Empty;
                    row.Cells[2].Value = item.Top.HasValue ? item.Top.ToString() : string.Empty;
                    row.Cells[3].Value = item.Width.ToString();
                    row.Cells[4].Value = item.Height.ToString();
                    row.Cells[5].Value = item.Shortcut.ToString();
                    row.Cells[6].ToolTipText = _languageManager.GetString("clm_window_size_edit");
                    row.Cells[7].ToolTipText = _languageManager.GetString("clm_window_size_delete");
                }

                if (item.Type == MenuItemType.Separator)
                {
                    var index = gridView.Rows.Add();
                    var row = gridView.Rows[index];
                    row.Tag = (WindowSizeMenuItem)item.Clone();
                    row.ReadOnly = true;
                    row.Cells[0].Value = _languageManager.GetString("separator");
                    row.Cells[6].ToolTipText = _languageManager.GetString("clm_window_size_edit");
                    row.Cells[7].ToolTipText = _languageManager.GetString("clm_window_size_delete");
                    ((DataGridViewDisableButtonCell)row.Cells[6]).Enabled = false;
                }
            }
        }

        private void FillGridViewByStartProgramItems(DataGridView gridView, IList<StartProgramMenuItem> items)
        {
            foreach (var item in items)
            {
                if (item.Type == MenuItemType.Item)
                {
                    var cloneItem = (StartProgramMenuItem)item.Clone();
                    var index = gridView.Rows.Add();
                    var row = gridView.Rows[index];
                    row.Tag = cloneItem;
                    row.Cells[0].Value = cloneItem.Title;
                    row.Cells[1].Value = cloneItem.FileName;
                    row.Cells[2].Value = cloneItem.Arguments;
                    row.Cells[3].ToolTipText = _languageManager.GetString("clm_start_program_edit");
                    row.Cells[4].ToolTipText = _languageManager.GetString("clm_start_program_delete");
                }

                if (item.Type == MenuItemType.Separator)
                {
                    var index = gridView.Rows.Add();
                    var row = gridView.Rows[index];
                    row.Tag = (StartProgramMenuItem)item.Clone();
                    row.ReadOnly = true;
                    row.Cells[0].Value = _languageManager.GetString("separator");
                    row.Cells[3].ToolTipText = _languageManager.GetString("clm_start_program_edit");
                    row.Cells[4].ToolTipText = _languageManager.GetString("clm_start_program_delete");
                    ((DataGridViewDisableButtonCell)row.Cells[3]).Enabled = false;
                }
            }
        }

        private string GetTransparencyTitle(string name) => name switch
        {
            MenuItemName.TransparencyOpaque => $"0%{_languageManager.GetString(name)}",
            MenuItemName.Transparency10 => "10%",
            MenuItemName.Transparency20 => "20%",
            MenuItemName.Transparency30 => "30%",
            MenuItemName.Transparency40 => "40%",
            MenuItemName.Transparency50 => "50%",
            MenuItemName.Transparency60 => "60%",
            MenuItemName.Transparency70 => "70%",
            MenuItemName.Transparency80 => "80%",
            MenuItemName.Transparency90 => "90%",
            MenuItemName.TransparencyInvisible => $"100%{_languageManager.GetString(name)}",
            _ => null
        };

        private IList<Settings.MenuItem> FindList(IList<Settings.MenuItem> list, Settings.MenuItem element)
        {
            foreach (var item in list)
            {
                if (item == element)
                {
                    return list;
                }

                if (item.Items.Any(x => x == element))
                {
                    return item.Items;
                }
            }
            return null;
        }
    }
}
