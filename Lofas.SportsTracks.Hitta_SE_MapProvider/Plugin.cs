using System;
using System.Collections.Generic;
using System.Text;
using ZoneFiveSoftware.Common.Visuals.Fitness;
using System.Runtime.InteropServices;
using System.Xml;

namespace Lofas.SportsTracks.Hitta_SE_MapProvider
{
    [Guid("BA45D36B-CC00-4dcf-8768-E24237ADCA4B")]
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
            get { return new Guid("BA45D36B-CC00-4dcf-8768-E24237ADCA4B"); }
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
