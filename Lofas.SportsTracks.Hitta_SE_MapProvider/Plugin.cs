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
            get { return Properties.Resources.ApplicationName; }
        }

        public void ReadOptions(System.Xml.XmlDocument xmlDoc, System.Xml.XmlNamespaceManager nsmgr, System.Xml.XmlElement pluginNode)
        {
            String attr;
            //attr = pluginNode.GetAttribute(xmlTags.settingsVersion);
            //if (attr.Length > 0) { settingsVersion = (Int16)XmlConvert.ToInt16(attr); }
            attr = pluginNode.GetAttribute(xmlTags.sLantmaterietAccessKey);
            if (attr.Length > 0) {
                HittaEniroMapProvider.LantmaterietAccessKey = attr;
            }
            else
            {
                //A default key with user sporttracks-plugin
                HittaEniroMapProvider.LantmaterietAccessKey = "ff3e84bbf924c3348d6460ec99751888";
            }
        }

        public string Version
        {
            get { return this.GetType().Assembly.GetName().Version.ToString(3); }
        }

        public void WriteOptions(System.Xml.XmlDocument xmlDoc, System.Xml.XmlElement pluginNode)
        {
            pluginNode.SetAttribute(xmlTags.settingsVersion, XmlConvert.ToString(1));
            pluginNode.SetAttribute(xmlTags.sLantmaterietAccessKey, HittaEniroMapProvider.LantmaterietAccessKey);
        }

        #endregion
        private class xmlTags
        {
            public const string settingsVersion = "settingsVersion";
            public const string sLantmaterietAccessKey = "sLantmaterietAccessKey";
        }
    }
}
