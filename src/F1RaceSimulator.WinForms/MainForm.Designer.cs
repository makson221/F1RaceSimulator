#nullable disable

namespace F1RaceSimulator.WinForms
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle headerStyle = new System.Windows.Forms.DataGridViewCellStyle();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.mnuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuSaveChampionship = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuLoadChampionship = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFileSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.mnuExit = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuRace = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuStartPause = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuRestart = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuSeason = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuStandings = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuResetSeason = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.tsbStartPause = new System.Windows.Forms.ToolStripButton();
            this.tsbRestart = new System.Windows.Forms.ToolStripButton();
            this.tsSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tslSpeed = new System.Windows.Forms.ToolStripLabel();
            this.tscSpeed = new System.Windows.Forms.ToolStripComboBox();
            this.tsSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tslTrack = new System.Windows.Forms.ToolStripLabel();
            this.tscTrack = new System.Windows.Forms.ToolStripComboBox();
            this.tsSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.tslWeather = new System.Windows.Forms.ToolStripLabel();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.tsslLap = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslLeader = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslPits = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslState = new System.Windows.Forms.ToolStripStatusLabel();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.tableLeft = new System.Windows.Forms.TableLayoutPanel();
            this.grpTrack = new System.Windows.Forms.GroupBox();
            this.trackPanel = new F1RaceSimulator.WinForms.TrackPanel();
            this.splitBottom = new System.Windows.Forms.SplitContainer();
            this.grpDriver = new System.Windows.Forms.GroupBox();
            this.telemetryPanel = new F1RaceSimulator.WinForms.TelemetryPanel();
            this.grpLog = new System.Windows.Forms.GroupBox();
            this.lstLog = new System.Windows.Forms.ListBox();
            this.grpStandings = new System.Windows.Forms.GroupBox();
            this.dgvStandings = new System.Windows.Forms.DataGridView();
            this.colColor = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPos = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDriver = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTeam = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colGap = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTyre = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPits = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.timerSim = new System.Windows.Forms.Timer(this.components);
            this.menuStrip.SuspendLayout();
            this.toolStrip.SuspendLayout();
            this.statusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.tableLeft.SuspendLayout();
            this.grpTrack.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitBottom)).BeginInit();
            this.splitBottom.Panel1.SuspendLayout();
            this.splitBottom.Panel2.SuspendLayout();
            this.splitBottom.SuspendLayout();
            this.grpDriver.SuspendLayout();
            this.grpLog.SuspendLayout();
            this.grpStandings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvStandings)).BeginInit();
            this.SuspendLayout();
            //
            // menuStrip
            //
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuFile,
            this.mnuRace,
            this.mnuSeason,
            this.mnuHelp});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(1280, 24);
            this.menuStrip.TabIndex = 0;
            //
            // mnuFile
            //
            this.mnuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuSaveChampionship,
            this.mnuLoadChampionship,
            this.mnuFileSeparator,
            this.mnuExit});
            this.mnuFile.Name = "mnuFile";
            this.mnuFile.Text = "&Файл";
            //
            // mnuSaveChampionship
            //
            this.mnuSaveChampionship.Name = "mnuSaveChampionship";
            this.mnuSaveChampionship.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.mnuSaveChampionship.Text = "Зберегти чемпіонат…";
            this.mnuSaveChampionship.Click += new System.EventHandler(this.MnuSaveChampionship_Click);
            //
            // mnuLoadChampionship
            //
            this.mnuLoadChampionship.Name = "mnuLoadChampionship";
            this.mnuLoadChampionship.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.mnuLoadChampionship.Text = "Відкрити чемпіонат…";
            this.mnuLoadChampionship.Click += new System.EventHandler(this.MnuLoadChampionship_Click);
            //
            // mnuFileSeparator
            //
            this.mnuFileSeparator.Name = "mnuFileSeparator";
            //
            // mnuExit
            //
            this.mnuExit.Name = "mnuExit";
            this.mnuExit.Text = "Вихід";
            this.mnuExit.Click += new System.EventHandler(this.MnuExit_Click);
            //
            // mnuRace
            //
            this.mnuRace.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuStartPause,
            this.mnuRestart});
            this.mnuRace.Name = "mnuRace";
            this.mnuRace.Text = "&Гонка";
            //
            // mnuStartPause
            //
            this.mnuStartPause.Name = "mnuStartPause";
            this.mnuStartPause.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.mnuStartPause.Text = "Старт";
            this.mnuStartPause.Click += new System.EventHandler(this.StartPause_Click);
            //
            // mnuRestart
            //
            this.mnuRestart.Name = "mnuRestart";
            this.mnuRestart.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this.mnuRestart.Text = "Почати знову";
            this.mnuRestart.Click += new System.EventHandler(this.Restart_Click);
            //
            // mnuSeason
            //
            this.mnuSeason.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuStandings,
            this.mnuResetSeason});
            this.mnuSeason.Name = "mnuSeason";
            this.mnuSeason.Text = "&Сезон";
            //
            // mnuStandings
            //
            this.mnuStandings.Name = "mnuStandings";
            this.mnuStandings.ShortcutKeys = System.Windows.Forms.Keys.F9;
            this.mnuStandings.Text = "Таблиця чемпіонату";
            this.mnuStandings.Click += new System.EventHandler(this.MnuStandings_Click);
            //
            // mnuResetSeason
            //
            this.mnuResetSeason.Name = "mnuResetSeason";
            this.mnuResetSeason.Text = "Скинути результати сезону";
            this.mnuResetSeason.Click += new System.EventHandler(this.MnuResetSeason_Click);
            //
            // mnuHelp
            //
            this.mnuHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuAbout});
            this.mnuHelp.Name = "mnuHelp";
            this.mnuHelp.Text = "&Довідка";
            //
            // mnuAbout
            //
            this.mnuAbout.Name = "mnuAbout";
            this.mnuAbout.Text = "Про програму";
            this.mnuAbout.Click += new System.EventHandler(this.MnuAbout_Click);
            //
            // toolStrip
            //
            this.toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbStartPause,
            this.tsbRestart,
            this.tsSeparator1,
            this.tslSpeed,
            this.tscSpeed,
            this.tsSeparator2,
            this.tslTrack,
            this.tscTrack,
            this.tsSeparator3,
            this.tslWeather});
            this.toolStrip.Location = new System.Drawing.Point(0, 24);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Padding = new System.Windows.Forms.Padding(6, 2, 6, 2);
            this.toolStrip.Size = new System.Drawing.Size(1280, 31);
            this.toolStrip.TabIndex = 1;
            //
            // tsbStartPause
            //
            this.tsbStartPause.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbStartPause.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.tsbStartPause.Name = "tsbStartPause";
            this.tsbStartPause.Text = "▶ Старт";
            this.tsbStartPause.Click += new System.EventHandler(this.StartPause_Click);
            //
            // tsbRestart
            //
            this.tsbRestart.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbRestart.Name = "tsbRestart";
            this.tsbRestart.Text = "⟲ Почати знову";
            this.tsbRestart.Click += new System.EventHandler(this.Restart_Click);
            //
            // tsSeparator1
            //
            this.tsSeparator1.Name = "tsSeparator1";
            //
            // tslSpeed
            //
            this.tslSpeed.Name = "tslSpeed";
            this.tslSpeed.Text = "Швидкість:";
            //
            // tscSpeed
            //
            this.tscSpeed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tscSpeed.Name = "tscSpeed";
            this.tscSpeed.Size = new System.Drawing.Size(70, 27);
            this.tscSpeed.ToolTipText = "У скільки разів симуляція швидша за реальний час";
            this.tscSpeed.SelectedIndexChanged += new System.EventHandler(this.TscSpeed_SelectedIndexChanged);
            //
            // tsSeparator2
            //
            this.tsSeparator2.Name = "tsSeparator2";
            //
            // tslTrack
            //
            this.tslTrack.Name = "tslTrack";
            this.tslTrack.Text = "Етап:";
            //
            // tscTrack
            //
            this.tscTrack.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tscTrack.DropDownWidth = 360;
            this.tscTrack.MaxDropDownItems = 24;
            this.tscTrack.Name = "tscTrack";
            this.tscTrack.Size = new System.Drawing.Size(330, 27);
            this.tscTrack.SelectedIndexChanged += new System.EventHandler(this.TscTrack_SelectedIndexChanged);
            //
            // tsSeparator3
            //
            this.tsSeparator3.Name = "tsSeparator3";
            //
            // tslWeather
            //
            this.tslWeather.Name = "tslWeather";
            this.tslWeather.Text = "Погода: —";
            //
            // statusStrip
            //
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsslLap,
            this.tsslLeader,
            this.tsslPits,
            this.tsslState});
            this.statusStrip.Location = new System.Drawing.Point(0, 756);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1280, 24);
            this.statusStrip.TabIndex = 3;
            //
            // tsslLap
            //
            this.tsslLap.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.tsslLap.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.tsslLap.Name = "tsslLap";
            this.tsslLap.Text = "Коло —";
            //
            // tsslLeader
            //
            this.tsslLeader.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.tsslLeader.Name = "tsslLeader";
            this.tsslLeader.Text = "Лідер: —";
            //
            // tsslPits
            //
            this.tsslPits.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.tsslPits.Name = "tsslPits";
            this.tsslPits.Text = "Піт-стопів: 0";
            //
            // tsslState
            //
            this.tsslState.Name = "tsslState";
            this.tsslState.Spring = true;
            this.tsslState.Text = "Пауза";
            this.tsslState.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // splitMain
            //
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Location = new System.Drawing.Point(0, 55);
            this.splitMain.Name = "splitMain";
            //
            // splitMain.Panel1
            //
            this.splitMain.Panel1.Controls.Add(this.tableLeft);
            this.splitMain.Panel1.Padding = new System.Windows.Forms.Padding(6, 6, 0, 6);
            //
            // splitMain.Panel2
            //
            this.splitMain.Panel2.Controls.Add(this.grpStandings);
            this.splitMain.Panel2.Padding = new System.Windows.Forms.Padding(0, 6, 6, 6);
            this.splitMain.Size = new System.Drawing.Size(1280, 701);
            this.splitMain.SplitterDistance = 720;
            this.splitMain.SplitterWidth = 6;
            this.splitMain.TabIndex = 2;
            //
            // tableLeft
            //
            this.tableLeft.ColumnCount = 1;
            this.tableLeft.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLeft.Controls.Add(this.grpTrack, 0, 0);
            this.tableLeft.Controls.Add(this.splitBottom, 0, 1);
            this.tableLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLeft.Location = new System.Drawing.Point(6, 6);
            this.tableLeft.Name = "tableLeft";
            this.tableLeft.RowCount = 2;
            this.tableLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 59F));
            this.tableLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 41F));
            this.tableLeft.Size = new System.Drawing.Size(714, 689);
            this.tableLeft.TabIndex = 0;
            //
            // grpTrack
            //
            this.grpTrack.Controls.Add(this.trackPanel);
            this.grpTrack.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpTrack.Name = "grpTrack";
            this.grpTrack.Padding = new System.Windows.Forms.Padding(6);
            this.grpTrack.TabIndex = 0;
            this.grpTrack.TabStop = false;
            this.grpTrack.Text = "Траса";
            //
            // trackPanel
            //
            this.trackPanel.BackColor = System.Drawing.Color.White;
            this.trackPanel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.trackPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.trackPanel.Name = "trackPanel";
            this.trackPanel.TabIndex = 0;
            this.trackPanel.CarClicked += new System.EventHandler<F1RaceSimulator.WinForms.CarEventArgs>(this.TrackPanel_CarClicked);
            //
            // splitBottom
            //
            this.splitBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitBottom.Name = "splitBottom";
            //
            // splitBottom.Panel1
            //
            this.splitBottom.Panel1.Controls.Add(this.grpDriver);
            //
            // splitBottom.Panel2
            //
            this.splitBottom.Panel2.Controls.Add(this.grpLog);
            this.splitBottom.Size = new System.Drawing.Size(708, 276);
            this.splitBottom.SplitterDistance = 390;
            this.splitBottom.SplitterWidth = 6;
            this.splitBottom.TabIndex = 1;
            //
            // grpDriver
            //
            this.grpDriver.Controls.Add(this.telemetryPanel);
            this.grpDriver.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpDriver.Name = "grpDriver";
            this.grpDriver.Padding = new System.Windows.Forms.Padding(6);
            this.grpDriver.TabIndex = 0;
            this.grpDriver.TabStop = false;
            this.grpDriver.Text = "Обраний пілот — телеметрія";
            //
            // telemetryPanel
            //
            this.telemetryPanel.BackColor = System.Drawing.Color.White;
            this.telemetryPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.telemetryPanel.Name = "telemetryPanel";
            this.telemetryPanel.TabIndex = 0;
            //
            // grpLog
            //
            this.grpLog.Controls.Add(this.lstLog);
            this.grpLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpLog.Name = "grpLog";
            this.grpLog.Padding = new System.Windows.Forms.Padding(6);
            this.grpLog.TabIndex = 0;
            this.grpLog.TabStop = false;
            this.grpLog.Text = "Журнал подій";
            //
            // lstLog
            //
            this.lstLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.lstLog.HorizontalScrollbar = true;
            this.lstLog.IntegralHeight = false;
            this.lstLog.Name = "lstLog";
            this.lstLog.TabIndex = 0;
            //
            // grpStandings
            //
            this.grpStandings.Controls.Add(this.dgvStandings);
            this.grpStandings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpStandings.Name = "grpStandings";
            this.grpStandings.Padding = new System.Windows.Forms.Padding(6);
            this.grpStandings.TabIndex = 0;
            this.grpStandings.TabStop = false;
            this.grpStandings.Text = "Позиції";
            //
            // dgvStandings
            //
            this.dgvStandings.AllowUserToAddRows = false;
            this.dgvStandings.AllowUserToDeleteRows = false;
            this.dgvStandings.AllowUserToResizeRows = false;
            this.dgvStandings.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvStandings.BackgroundColor = System.Drawing.Color.White;
            this.dgvStandings.BorderStyle = System.Windows.Forms.BorderStyle.None;
            headerStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            headerStyle.BackColor = System.Drawing.Color.FromArgb(237, 241, 246);
            headerStyle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            headerStyle.ForeColor = System.Drawing.Color.FromArgb(91, 104, 121);
            this.dgvStandings.ColumnHeadersDefaultCellStyle = headerStyle;
            this.dgvStandings.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvStandings.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colColor,
            this.colPos,
            this.colDriver,
            this.colTeam,
            this.colGap,
            this.colTyre,
            this.colPits});
            this.dgvStandings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvStandings.EnableHeadersVisualStyles = false;
            this.dgvStandings.MultiSelect = false;
            this.dgvStandings.Name = "dgvStandings";
            this.dgvStandings.ReadOnly = true;
            this.dgvStandings.RowHeadersVisible = false;
            this.dgvStandings.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvStandings.TabIndex = 0;
            this.dgvStandings.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DgvStandings_CellClick);
            //
            // colColor
            //
            this.colColor.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.colColor.HeaderText = "";
            this.colColor.Name = "colColor";
            this.colColor.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.colColor.Width = 6;
            //
            // colPos
            //
            this.colPos.FillWeight = 10F;
            this.colPos.HeaderText = "Поз";
            this.colPos.Name = "colPos";
            //
            // colDriver
            //
            this.colDriver.FillWeight = 30F;
            this.colDriver.HeaderText = "Пілот";
            this.colDriver.Name = "colDriver";
            //
            // colTeam
            //
            this.colTeam.FillWeight = 28F;
            this.colTeam.HeaderText = "Команда";
            this.colTeam.Name = "colTeam";
            //
            // colGap
            //
            this.colGap.FillWeight = 16F;
            this.colGap.HeaderText = "Відрив";
            this.colGap.Name = "colGap";
            //
            // colTyre
            //
            this.colTyre.FillWeight = 16F;
            this.colTyre.HeaderText = "Шини";
            this.colTyre.Name = "colTyre";
            //
            // colPits
            //
            this.colPits.FillWeight = 8F;
            this.colPits.HeaderText = "Піт";
            this.colPits.Name = "colPits";
            //
            // timerSim
            //
            this.timerSim.Interval = 30;
            this.timerSim.Tick += new System.EventHandler(this.TimerSim_Tick);
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1280, 780);
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.toolStrip);
            this.Controls.Add(this.menuStrip);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MainMenuStrip = this.menuStrip;
            this.MinimumSize = new System.Drawing.Size(1000, 650);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "F1 Race Simulator";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.tableLeft.ResumeLayout(false);
            this.grpTrack.ResumeLayout(false);
            this.splitBottom.Panel1.ResumeLayout(false);
            this.splitBottom.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitBottom)).EndInit();
            this.splitBottom.ResumeLayout(false);
            this.grpDriver.ResumeLayout(false);
            this.grpLog.ResumeLayout(false);
            this.grpStandings.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvStandings)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem mnuFile;
        private System.Windows.Forms.ToolStripMenuItem mnuSaveChampionship;
        private System.Windows.Forms.ToolStripMenuItem mnuLoadChampionship;
        private System.Windows.Forms.ToolStripSeparator mnuFileSeparator;
        private System.Windows.Forms.ToolStripMenuItem mnuExit;
        private System.Windows.Forms.ToolStripMenuItem mnuRace;
        private System.Windows.Forms.ToolStripMenuItem mnuStartPause;
        private System.Windows.Forms.ToolStripMenuItem mnuRestart;
        private System.Windows.Forms.ToolStripMenuItem mnuSeason;
        private System.Windows.Forms.ToolStripMenuItem mnuStandings;
        private System.Windows.Forms.ToolStripMenuItem mnuResetSeason;
        private System.Windows.Forms.ToolStripMenuItem mnuHelp;
        private System.Windows.Forms.ToolStripMenuItem mnuAbout;
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbStartPause;
        private System.Windows.Forms.ToolStripButton tsbRestart;
        private System.Windows.Forms.ToolStripSeparator tsSeparator1;
        private System.Windows.Forms.ToolStripLabel tslSpeed;
        private System.Windows.Forms.ToolStripComboBox tscSpeed;
        private System.Windows.Forms.ToolStripSeparator tsSeparator2;
        private System.Windows.Forms.ToolStripLabel tslTrack;
        private System.Windows.Forms.ToolStripComboBox tscTrack;
        private System.Windows.Forms.ToolStripSeparator tsSeparator3;
        private System.Windows.Forms.ToolStripLabel tslWeather;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel tsslLap;
        private System.Windows.Forms.ToolStripStatusLabel tsslLeader;
        private System.Windows.Forms.ToolStripStatusLabel tsslPits;
        private System.Windows.Forms.ToolStripStatusLabel tsslState;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.TableLayoutPanel tableLeft;
        private System.Windows.Forms.GroupBox grpTrack;
        private F1RaceSimulator.WinForms.TrackPanel trackPanel;
        private System.Windows.Forms.SplitContainer splitBottom;
        private System.Windows.Forms.GroupBox grpDriver;
        private F1RaceSimulator.WinForms.TelemetryPanel telemetryPanel;
        private System.Windows.Forms.GroupBox grpLog;
        private System.Windows.Forms.ListBox lstLog;
        private System.Windows.Forms.GroupBox grpStandings;
        private System.Windows.Forms.DataGridView dgvStandings;
        private System.Windows.Forms.DataGridViewTextBoxColumn colColor;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPos;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDriver;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTeam;
        private System.Windows.Forms.DataGridViewTextBoxColumn colGap;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTyre;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPits;
        private System.Windows.Forms.Timer timerSim;
    }
}
