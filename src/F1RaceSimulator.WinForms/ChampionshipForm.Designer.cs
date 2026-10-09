#nullable disable

namespace F1RaceSimulator.WinForms
{
    partial class ChampionshipForm
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
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabDrivers = new System.Windows.Forms.TabPage();
            this.dgvDrivers = new System.Windows.Forms.DataGridView();
            this.tabTeams = new System.Windows.Forms.TabPage();
            this.dgvTeams = new System.Windows.Forms.DataGridView();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.lblRaces = new System.Windows.Forms.Label();
            this.btnClose = new System.Windows.Forms.Button();
            this.tabControl.SuspendLayout();
            this.tabDrivers.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDrivers)).BeginInit();
            this.tabTeams.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTeams)).BeginInit();
            this.panelBottom.SuspendLayout();
            this.SuspendLayout();
            //
            // tabControl
            //
            this.tabControl.Controls.Add(this.tabDrivers);
            this.tabControl.Controls.Add(this.tabTeams);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(8, 8);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(624, 461);
            this.tabControl.TabIndex = 0;
            //
            // tabDrivers
            //
            this.tabDrivers.Controls.Add(this.dgvDrivers);
            this.tabDrivers.Name = "tabDrivers";
            this.tabDrivers.Padding = new System.Windows.Forms.Padding(4);
            this.tabDrivers.Text = "Пілоти";
            this.tabDrivers.UseVisualStyleBackColor = true;
            //
            // dgvDrivers
            //
            this.dgvDrivers.AllowUserToAddRows = false;
            this.dgvDrivers.AllowUserToDeleteRows = false;
            this.dgvDrivers.AllowUserToResizeRows = false;
            this.dgvDrivers.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvDrivers.BackgroundColor = System.Drawing.Color.White;
            this.dgvDrivers.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvDrivers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvDrivers.Name = "dgvDrivers";
            this.dgvDrivers.ReadOnly = true;
            this.dgvDrivers.RowHeadersVisible = false;
            this.dgvDrivers.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvDrivers.TabIndex = 0;
            //
            // tabTeams
            //
            this.tabTeams.Controls.Add(this.dgvTeams);
            this.tabTeams.Name = "tabTeams";
            this.tabTeams.Padding = new System.Windows.Forms.Padding(4);
            this.tabTeams.Text = "Кубок конструкторів";
            this.tabTeams.UseVisualStyleBackColor = true;
            //
            // dgvTeams
            //
            this.dgvTeams.AllowUserToAddRows = false;
            this.dgvTeams.AllowUserToDeleteRows = false;
            this.dgvTeams.AllowUserToResizeRows = false;
            this.dgvTeams.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvTeams.BackgroundColor = System.Drawing.Color.White;
            this.dgvTeams.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvTeams.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvTeams.Name = "dgvTeams";
            this.dgvTeams.ReadOnly = true;
            this.dgvTeams.RowHeadersVisible = false;
            this.dgvTeams.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvTeams.TabIndex = 0;
            //
            // panelBottom
            //
            this.panelBottom.Controls.Add(this.lblRaces);
            this.panelBottom.Controls.Add(this.btnClose);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Location = new System.Drawing.Point(8, 469);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(624, 44);
            this.panelBottom.TabIndex = 1;
            //
            // lblRaces
            //
            this.lblRaces.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRaces.Name = "lblRaces";
            this.lblRaces.TabIndex = 0;
            this.lblRaces.Text = "—";
            this.lblRaces.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // btnClose
            //
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(110, 44);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "Закрити";
            this.btnClose.UseVisualStyleBackColor = true;
            //
            // ChampionshipForm
            //
            this.AcceptButton = this.btnClose;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(640, 521);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.panelBottom);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(520, 400);
            this.Name = "ChampionshipForm";
            this.Padding = new System.Windows.Forms.Padding(8);
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Таблиця чемпіонату 2026";
            this.Load += new System.EventHandler(this.ChampionshipForm_Load);
            this.tabControl.ResumeLayout(false);
            this.tabDrivers.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvDrivers)).EndInit();
            this.tabTeams.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvTeams)).EndInit();
            this.panelBottom.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabDrivers;
        private System.Windows.Forms.DataGridView dgvDrivers;
        private System.Windows.Forms.TabPage tabTeams;
        private System.Windows.Forms.DataGridView dgvTeams;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Label lblRaces;
        private System.Windows.Forms.Button btnClose;
    }
}
