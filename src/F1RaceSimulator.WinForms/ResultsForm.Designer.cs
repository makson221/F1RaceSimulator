#nullable disable

namespace F1RaceSimulator.WinForms
{
    partial class ResultsForm
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
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblInfo = new System.Windows.Forms.Label();
            this.split = new System.Windows.Forms.SplitContainer();
            this.grpRace = new System.Windows.Forms.GroupBox();
            this.dgvRace = new System.Windows.Forms.DataGridView();
            this.grpChampionship = new System.Windows.Forms.GroupBox();
            this.dgvChampionship = new System.Windows.Forms.DataGridView();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.btnNext = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.split)).BeginInit();
            this.split.Panel1.SuspendLayout();
            this.split.Panel2.SuspendLayout();
            this.split.SuspendLayout();
            this.grpRace.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRace)).BeginInit();
            this.grpChampionship.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvChampionship)).BeginInit();
            this.panelBottom.SuspendLayout();
            this.SuspendLayout();
            //
            // lblTitle
            //
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(984, 34);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Результати";
            //
            // lblInfo
            //
            this.lblInfo.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblInfo.ForeColor = System.Drawing.Color.FromArgb(91, 104, 121);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(984, 24);
            this.lblInfo.TabIndex = 1;
            //
            // split
            //
            this.split.Dock = System.Windows.Forms.DockStyle.Fill;
            this.split.Name = "split";
            this.split.Panel1.Controls.Add(this.grpRace);
            this.split.Panel2.Controls.Add(this.grpChampionship);
            this.split.Size = new System.Drawing.Size(984, 480);
            this.split.SplitterDistance = 560;
            this.split.SplitterWidth = 8;
            this.split.TabIndex = 2;
            //
            // grpRace
            //
            this.grpRace.Controls.Add(this.dgvRace);
            this.grpRace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpRace.Name = "grpRace";
            this.grpRace.Padding = new System.Windows.Forms.Padding(6);
            this.grpRace.TabStop = false;
            this.grpRace.Text = "Протокол гонки";
            //
            // dgvRace
            //
            this.dgvRace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvRace.Name = "dgvRace";
            this.dgvRace.TabIndex = 0;
            //
            // grpChampionship
            //
            this.grpChampionship.Controls.Add(this.dgvChampionship);
            this.grpChampionship.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpChampionship.Name = "grpChampionship";
            this.grpChampionship.Padding = new System.Windows.Forms.Padding(6);
            this.grpChampionship.TabStop = false;
            this.grpChampionship.Text = "Чемпіонат пілотів";
            //
            // dgvChampionship
            //
            this.dgvChampionship.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvChampionship.Name = "dgvChampionship";
            this.dgvChampionship.TabIndex = 0;
            //
            // panelBottom
            //
            this.panelBottom.Controls.Add(this.btnNext);
            this.panelBottom.Controls.Add(this.btnClose);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(984, 46);
            this.panelBottom.TabIndex = 3;
            //
            // btnNext
            //
            this.btnNext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnNext.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnNext.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnNext.Location = new System.Drawing.Point(560, 8);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(310, 32);
            this.btnNext.TabIndex = 0;
            this.btnNext.Text = "Наступний етап ▶";
            this.btnNext.UseVisualStyleBackColor = true;
            //
            // btnClose
            //
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(880, 8);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(96, 32);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "Закрити";
            this.btnClose.UseVisualStyleBackColor = true;
            //
            // ResultsForm
            //
            this.AcceptButton = this.btnNext;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(1000, 600);
            this.Controls.Add(this.split);
            this.Controls.Add(this.panelBottom);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.lblTitle);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(800, 500);
            this.Name = "ResultsForm";
            this.Padding = new System.Windows.Forms.Padding(8);
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Результати етапу";
            this.split.Panel1.ResumeLayout(false);
            this.split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.split)).EndInit();
            this.split.ResumeLayout(false);
            this.grpRace.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvRace)).EndInit();
            this.grpChampionship.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvChampionship)).EndInit();
            this.panelBottom.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.SplitContainer split;
        private System.Windows.Forms.GroupBox grpRace;
        private System.Windows.Forms.DataGridView dgvRace;
        private System.Windows.Forms.GroupBox grpChampionship;
        private System.Windows.Forms.DataGridView dgvChampionship;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.Button btnClose;
    }
}
