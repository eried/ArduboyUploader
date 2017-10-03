namespace ArduboyUploader
{
    partial class FormMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.backgroundWorkerUploader = new System.ComponentModel.BackgroundWorker();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.tableLayoutPanelContents = new System.Windows.Forms.TableLayoutPanel();
            this.pictureBoxStatus = new System.Windows.Forms.PictureBox();
            this.buttonRetry = new System.Windows.Forms.Button();
            this.toolTipInfo = new System.Windows.Forms.ToolTip(this.components);
            this.tableLayoutPanelContents.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxStatus)).BeginInit();
            this.SuspendLayout();
            // 
            // backgroundWorkerUploader
            // 
            this.backgroundWorkerUploader.WorkerReportsProgress = true;
            this.backgroundWorkerUploader.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerUploader_DoWork);
            this.backgroundWorkerUploader.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorkerUploader_ProgressChanged);
            this.backgroundWorkerUploader.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorkerUploader_RunWorkerCompleted);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonCancel.FlatAppearance.BorderColor = System.Drawing.SystemColors.ControlText;
            this.buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonCancel.Image = ((System.Drawing.Image)(resources.GetObject("buttonCancel.Image")));
            this.buttonCancel.Location = new System.Drawing.Point(171, 348);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(156, 43);
            this.buttonCancel.TabIndex = 0;
            this.buttonCancel.TabStop = false;
            this.buttonCancel.Text = "&Cancel";
            this.buttonCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.buttonCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // tableLayoutPanelContents
            // 
            this.tableLayoutPanelContents.ColumnCount = 3;
            this.tableLayoutPanelContents.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33332F));
            this.tableLayoutPanelContents.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanelContents.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanelContents.Controls.Add(this.pictureBoxStatus, 0, 0);
            this.tableLayoutPanelContents.Controls.Add(this.buttonCancel, 1, 1);
            this.tableLayoutPanelContents.Controls.Add(this.buttonRetry, 0, 1);
            this.tableLayoutPanelContents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelContents.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelContents.Name = "tableLayoutPanelContents";
            this.tableLayoutPanelContents.Padding = new System.Windows.Forms.Padding(6);
            this.tableLayoutPanelContents.RowCount = 2;
            this.tableLayoutPanelContents.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelContents.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelContents.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelContents.Size = new System.Drawing.Size(500, 400);
            this.tableLayoutPanelContents.TabIndex = 2;
            // 
            // pictureBoxStatus
            // 
            this.pictureBoxStatus.BackColor = System.Drawing.Color.Black;
            this.tableLayoutPanelContents.SetColumnSpan(this.pictureBoxStatus, 3);
            this.pictureBoxStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBoxStatus.Location = new System.Drawing.Point(10, 9);
            this.pictureBoxStatus.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.pictureBoxStatus.Name = "pictureBoxStatus";
            this.pictureBoxStatus.Size = new System.Drawing.Size(480, 333);
            this.pictureBoxStatus.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxStatus.TabIndex = 1;
            this.pictureBoxStatus.TabStop = false;
            // 
            // buttonRetry
            // 
            this.buttonRetry.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonRetry.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonRetry.Image = ((System.Drawing.Image)(resources.GetObject("buttonRetry.Image")));
            this.buttonRetry.Location = new System.Drawing.Point(9, 348);
            this.buttonRetry.Name = "buttonRetry";
            this.buttonRetry.Size = new System.Drawing.Size(156, 43);
            this.buttonRetry.TabIndex = 2;
            this.buttonRetry.TabStop = false;
            this.buttonRetry.Text = "&Retry";
            this.buttonRetry.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.buttonRetry.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTipInfo.SetToolTip(this.buttonRetry, resources.GetString("buttonRetry.ToolTip"));
            this.buttonRetry.UseVisualStyleBackColor = true;
            this.buttonRetry.Visible = false;
            this.buttonRetry.Click += new System.EventHandler(this.buttonRetry_Click);
            // 
            // toolTipInfo
            // 
            this.toolTipInfo.AutoPopDelay = 20000;
            this.toolTipInfo.InitialDelay = 100;
            this.toolTipInfo.ReshowDelay = 100;
            this.toolTipInfo.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.toolTipInfo.ToolTipTitle = "Still can\'t upload?";
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(168F, 168F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(500, 400);
            this.ControlBox = false;
            this.Controls.Add(this.tableLayoutPanelContents);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Arduboy Uploader";
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.tableLayoutPanelContents.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxStatus)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.ComponentModel.BackgroundWorker backgroundWorkerUploader;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.PictureBox pictureBoxStatus;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelContents;
        private System.Windows.Forms.Button buttonRetry;
        private System.Windows.Forms.ToolTip toolTipInfo;
    }
}

