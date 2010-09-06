/*
Copyright (C) 2008, 2009, 2010 Peter Löfås

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 3 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Text;
using ZoneFiveSoftware.Common.Visuals.Fitness;
using System.Runtime.InteropServices;
using System.Xml;

namespace Lofas.SportsTracks.Hitta_SE_MapProvider
{
    public class Plugin : IPlugin
    {

        #region IPlugin Members
        public static IApplication m_Application;
        public IApplication Application
        {
            set { m_Application = value; }
        }

        public Guid Id
        {
            get { return Lofas.SportsTracks.GUIDs.PluginMain; }
        }

        public string Name
        {
            get { return "Hitta.se / Eniro.se MapProvider (Sweden)"; }
        }

        public void ReadOptions(System.Xml.XmlDocument xmlDoc, System.Xml.XmlNamespaceManager nsmgr, System.Xml.XmlElement pluginNode)
        {
        }

        public string Version
        {
            get { return this.GetType().Assembly.GetName().Version.ToString(3); }
        }

        public void WriteOptions(System.Xml.XmlDocument xmlDoc, System.Xml.XmlElement pluginNode)
        {
        }

        #endregion
    }
}
