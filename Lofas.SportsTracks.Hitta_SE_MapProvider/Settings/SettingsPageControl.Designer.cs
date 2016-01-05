using System;

namespace Lofas.SportsTracks.Settings {
    partial class SettingsPageControl {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.PluginInfoPanel = new ZoneFiveSoftware.Common.Visuals.Panel();
            this.PluginInfoBanner = new ZoneFiveSoftware.Common.Visuals.ActionBanner();
            this.lblInfo = new System.Windows.Forms.LinkLabel();
            this.lblLantmaterietAccessKey = new System.Windows.Forms.Label();
            this.txtLantmaterietAccessKey = new ZoneFiveSoftware.Common.Visuals.TextBox();
            this.lblCopyright = new System.Windows.Forms.Label();
            this.lblLicense = new System.Windows.Forms.Label();
            this.PluginInfoPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // PluginInfoPanel
            // 
            this.PluginInfoPanel.AutoSize = true;
            this.PluginInfoPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.PluginInfoPanel.BackColor = System.Drawing.Color.Transparent;
            this.PluginInfoPanel.BorderColor = System.Drawing.Color.Gray;
            this.PluginInfoPanel.Controls.Add(this.PluginInfoBanner);
            this.PluginInfoPanel.Controls.Add(this.lblInfo);
            this.PluginInfoPanel.Controls.Add(this.lblLantmaterietAccessKey);
            this.PluginInfoPanel.Controls.Add(this.txtLantmaterietAccessKey);
            this.PluginInfoPanel.Controls.Add(this.lblCopyright);
            this.PluginInfoPanel.Controls.Add(this.lblLicense);
            this.PluginInfoPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PluginInfoPanel.HeadingBackColor = System.Drawing.Color.LightBlue;
            this.PluginInfoPanel.HeadingFont = null;
            this.PluginInfoPanel.HeadingLeftMargin = 0;
            this.PluginInfoPanel.HeadingText = null;
            this.PluginInfoPanel.HeadingTextColor = System.Drawing.Color.Black;
            this.PluginInfoPanel.HeadingTopMargin = 3;
            this.PluginInfoPanel.Location = new System.Drawing.Point(0, 0);
            this.PluginInfoPanel.Name = "PluginInfoPanel";
            this.PluginInfoPanel.Size = new System.Drawing.Size(365, 250);
            this.PluginInfoPanel.TabIndex = 0;
            // 
            // PluginInfoBanner
            // 
            this.PluginInfoBanner.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PluginInfoBanner.BackColor = System.Drawing.Color.Transparent;
            this.PluginInfoBanner.HasMenuButton = false;
            this.PluginInfoBanner.Location = new System.Drawing.Point(0, 0);
            this.PluginInfoBanner.Margin = new System.Windows.Forms.Padding(0);
            this.PluginInfoBanner.Name = "PluginInfoBanner";
            this.PluginInfoBanner.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.PluginInfoBanner.Size = new System.Drawing.Size(365, 20);
            this.PluginInfoBanner.Style = ZoneFiveSoftware.Common.Visuals.ActionBanner.BannerStyle.Header2;
            this.PluginInfoBanner.TabIndex = 0;
            this.PluginInfoBanner.Text = "Plugin Information";
            this.PluginInfoBanner.UseStyleFont = true;
            // 
            // lblInfo
            // 
            this.lblInfo.AutoSize = true;
            this.lblInfo.Location = new System.Drawing.Point(6, 33);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(30, 13);
            this.lblInfo.TabIndex = 1;
            this.lblInfo.TabStop = true;
            this.lblInfo.Text = "<info";
            this.lblInfo.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblInfo_LinkClicked);
            // 
            // lblLantmaterietAccessKey
            // 
            this.lblLantmaterietAccessKey.AutoSize = true;
            this.lblLantmaterietAccessKey.Location = new System.Drawing.Point(6, 55);
            this.lblLantmaterietAccessKey.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.lblLantmaterietAccessKey.Name = "lblLantmaterietAccessKey";
            this.lblLantmaterietAccessKey.Size = new System.Drawing.Size(137, 13);
            this.lblLantmaterietAccessKey.TabIndex = 0;
            this.lblLantmaterietAccessKey.Text = "<lblLantmaterietAccessKey:";
            // 
            // txtLantmaterietAccessKey
            // 
            this.txtLantmaterietAccessKey.AcceptsReturn = false;
            this.txtLantmaterietAccessKey.AcceptsTab = false;
            this.txtLantmaterietAccessKey.BackColor = System.Drawing.Color.White;
            this.txtLantmaterietAccessKey.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(123)))), ((int)(((byte)(114)))), ((int)(((byte)(108)))));
            this.txtLantmaterietAccessKey.ButtonImage = null;
            this.txtLantmaterietAccessKey.Location = new System.Drawing.Point(159, 55);
            this.txtLantmaterietAccessKey.Margin = new System.Windows.Forms.Padding(0);
            this.txtLantmaterietAccessKey.MaxLength = 32767;
            this.txtLantmaterietAccessKey.Multiline = false;
            this.txtLantmaterietAccessKey.Name = "txtLantmaterietAccessKey";
            this.txtLantmaterietAccessKey.ReadOnly = false;
            this.txtLantmaterietAccessKey.ReadOnlyColor = System.Drawing.SystemColors.Control;
            this.txtLantmaterietAccessKey.ReadOnlyTextColor = System.Drawing.SystemColors.ControlLight;
            this.txtLantmaterietAccessKey.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txtLantmaterietAccessKey.Size = new System.Drawing.Size(195, 19);
            this.txtLantmaterietAccessKey.TabIndex = 1;
            this.txtLantmaterietAccessKey.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.txtLantmaterietAccessKey.LostFocus += new System.EventHandler(this.txtLantmaterietAccessKey_LostFocus);
            // 
            // lblCopyright
            // 
            this.lblCopyright.AutoSize = true;
            this.lblCopyright.Location = new System.Drawing.Point(6, 91);
            this.lblCopyright.Name = "lblCopyright";
            this.lblCopyright.Size = new System.Drawing.Size(109, 13);
            this.lblCopyright.TabIndex = 1;
            this.lblCopyright.Text = "<Copyright lofas 2009";
            // 
            // lblLicense
            // 
            this.lblLicense.AutoSize = true;
            this.lblLicense.Location = new System.Drawing.Point(6, 115);
            this.lblLicense.Name = "lblLicense";
            this.lblLicense.Size = new System.Drawing.Size(356, 39);
            this.lblLicense.TabIndex = 3;
            this.lblLicense.Text = "<Trails Plugin is distributed under the GNU Lesser General Public Licence.\r\nThe L" +
    "icense is included in the plugin installation directory and at:\r\nhttp://www.gnu." +
    "org/licenses/lgpl.html.";
            // 
            // SettingsPageControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.PluginInfoPanel);
            this.MinimumSize = new System.Drawing.Size(350, 250);
            this.Name = "SettingsPageControl";
            this.Size = new System.Drawing.Size(365, 250);
            this.PluginInfoPanel.ResumeLayout(false);
            this.PluginInfoPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ZoneFiveSoftware.Common.Visuals.Panel PluginInfoPanel;
        private ZoneFiveSoftware.Common.Visuals.ActionBanner PluginInfoBanner;
        private System.Windows.Forms.LinkLabel lblInfo;
        private System.Windows.Forms.Label lblLantmaterietAccessKey;
        private ZoneFiveSoftware.Common.Visuals.TextBox txtLantmaterietAccessKey;
        private System.Windows.Forms.Label lblCopyright;
        private System.Windows.Forms.Label lblLicense;
    }
}
