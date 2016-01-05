/*
Copyright (C) 2016 Gerhard Olsson

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 3 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using ZoneFiveSoftware.Common.Data.Fitness;
using ZoneFiveSoftware.Common.Visuals;

namespace Lofas.SportsTracks.Settings
{
    public partial class SettingsPageControl : UserControl
    {
        public SettingsPageControl()
        {
            this.InitializeComponent();
            this.presentSettings();
        }

        private void presentSettings()
        {
            txtLantmaterietAccessKey.Text = Hitta_SE_MapProvider.HittaEniroMapProvider.LantmaterietAccessKey;
        }

        public void ThemeChanged(ITheme visualTheme)
        {
            this.PluginInfoBanner.ThemeChanged(visualTheme);
            this.PluginInfoPanel.ThemeChanged(visualTheme);
        }

        public void UICultureChanged(System.Globalization.CultureInfo culture)
        {
            this.lblLantmaterietAccessKey.Text = Properties.Resources.UI_Settings_LantmaterietAccessKey + ":";

            this.lblInfo.Text = Properties.Resources.UI_Settings_PageControl_linkInformativeUrl_Text;
            this.lblLicense.Text = Properties.Resources.UI_Settings_License;
            this.lblCopyright.Text = Properties.Resources.UI_Settings_Copyright + " " + "Peter Löfås 2010, Gerhard Olsson 2016";
            this.PluginInfoBanner.Text = Properties.Resources.UI_Settings_Title;
        }

        //private void precedeControl(Control a, Control b)
        //{
        //    a.Location = new Point(b.Location.X - a.Size.Width - 5, a.Location.Y);
        //}

        private void lblInfo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                "https://github.com/gerhardol/sporttracks-nordic-mapproviders/wiki"
            ));
        }

        private void txtLantmaterietAccessKey_LostFocus(object sender, EventArgs e)
        {
            Hitta_SE_MapProvider.HittaEniroMapProvider.LantmaterietAccessKey = txtLantmaterietAccessKey.Text;
            this.presentSettings();
        }
    }
}
