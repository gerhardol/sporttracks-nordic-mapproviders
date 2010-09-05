using System;
using System.Collections.Generic;
using System.Text;

namespace Lofas.SportsTracks.Hitta_SE_MapProvider
{
    public class Hitta_SE_ExtensionProviders : ZoneFiveSoftware.Common.Visuals.Fitness.IExtendMapProviders
    {
        #region IExtendMapProviders Members
        static List<ZoneFiveSoftware.Common.Visuals.Fitness.GPS.IMapProvider> m_List = null;
        static Hitta_SE_ExtensionProviders()
        {
            m_List = new List<ZoneFiveSoftware.Common.Visuals.Fitness.GPS.IMapProvider>();
            m_List.Add(new Hitta_SE_MapProvider());
            m_List.Add(new Hitta_SE_MapProvider("Map"));
            m_List.Add(new Eniro_SE_MapProvider());
            m_List.Add(new Eniro_SE_MapProvider("Map"));
            m_List.Add(new Eniro_SE_MapProvider("Nat"));
            //m_List.Add(new Eniro_SE_MapProvider("Map","FI_fi"));
        }
        public IList<ZoneFiveSoftware.Common.Visuals.Fitness.GPS.IMapProvider> MapProviders
        {
            get { return m_List; }
        }

        #endregion
    }
}
