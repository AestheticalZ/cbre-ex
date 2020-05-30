﻿namespace CBRE.Editor.Logging
{
    partial class ExceptionWindow
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
			this.label1 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.FrameworkVersion = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.OperatingSystem = new System.Windows.Forms.TextBox();
			this.label6 = new System.Windows.Forms.Label();
			this.CBREVersion = new System.Windows.Forms.TextBox();
			this.label7 = new System.Windows.Forms.Label();
			this.FullError = new System.Windows.Forms.TextBox();
			this.CancelButton = new System.Windows.Forms.Button();
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(12, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(411, 25);
			this.label1.TabIndex = 0;
			this.label1.Text = "Oops! Something went horribly wrong.";
			this.label1.Click += new System.EventHandler(this.label1_Click);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(9, 46);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(70, 13);
			this.label4.TabIndex = 2;
			this.label4.Text = ".NET Version";
			// 
			// FrameworkVersion
			// 
			this.FrameworkVersion.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.FrameworkVersion.Enabled = false;
			this.FrameworkVersion.Location = new System.Drawing.Point(135, 43);
			this.FrameworkVersion.Name = "FrameworkVersion";
			this.FrameworkVersion.Size = new System.Drawing.Size(337, 20);
			this.FrameworkVersion.TabIndex = 3;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(12, 69);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(90, 13);
			this.label5.TabIndex = 2;
			this.label5.Text = "Operating System";
			// 
			// OperatingSystem
			// 
			this.OperatingSystem.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.OperatingSystem.Enabled = false;
			this.OperatingSystem.Location = new System.Drawing.Point(135, 66);
			this.OperatingSystem.Name = "OperatingSystem";
			this.OperatingSystem.Size = new System.Drawing.Size(337, 20);
			this.OperatingSystem.TabIndex = 3;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(12, 91);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(74, 13);
			this.label6.TabIndex = 2;
			this.label6.Text = "CBRE Version";
			// 
			// CBREVersion
			// 
			this.CBREVersion.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.CBREVersion.Enabled = false;
			this.CBREVersion.Location = new System.Drawing.Point(135, 88);
			this.CBREVersion.Name = "CBREVersion";
			this.CBREVersion.Size = new System.Drawing.Size(337, 20);
			this.CBREVersion.TabIndex = 3;
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(12, 115);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(94, 13);
			this.label7.TabIndex = 2;
			this.label7.Text = "Full Error Message";
			// 
			// FullError
			// 
			this.FullError.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.FullError.Enabled = false;
			this.FullError.Location = new System.Drawing.Point(135, 112);
			this.FullError.Multiline = true;
			this.FullError.Name = "FullError";
			this.FullError.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.FullError.Size = new System.Drawing.Size(337, 324);
			this.FullError.TabIndex = 3;
			// 
			// CancelButton
			// 
			this.CancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.CancelButton.Location = new System.Drawing.Point(8, 413);
			this.CancelButton.Name = "CancelButton";
			this.CancelButton.Size = new System.Drawing.Size(121, 23);
			this.CancelButton.TabIndex = 4;
			this.CancelButton.Text = "Close";
			this.CancelButton.UseVisualStyleBackColor = true;
			this.CancelButton.Click += new System.EventHandler(this.CancelButtonClicked);
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
			// 
			// ExceptionWindow
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(484, 448);
			this.Controls.Add(this.CancelButton);
			this.Controls.Add(this.FullError);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.CBREVersion);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.OperatingSystem);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.FrameworkVersion);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(500, 486);
			this.Name = "ExceptionWindow";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "pls fix";
			this.Load += new System.EventHandler(this.ExceptionWindow_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox FrameworkVersion;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox OperatingSystem;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox CBREVersion;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox FullError;
        private System.Windows.Forms.Button CancelButton;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
	}
}