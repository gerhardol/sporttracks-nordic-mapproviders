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
using Lofas.SportsTracks.Hitta_SE_MapProvider.Common;
using Lofas.SportsTracks.Hitta_SE_MapProvider.MapProviders.Geodata;
using ZoneFiveSoftware.Common.Visuals.Fitness;
#if ST_2_1
using ZoneFiveSoftware.Common.Visuals.Fitness.GPS;
#else
using ZoneFiveSoftware.Common.Visuals.Mapping;
#endif

namespace Lofas.SportsTracks.Hitta_SE_MapProvider
{
#if ST_2_1
    public class Hitta_SE_ExtensionProviders : ZoneFiveSoftware.Common.Visuals.Fitness.IExtendMapProviders
    {
        static List<ZoneFiveSoftware.Common.Visuals.Fitness.GPS.IMapProvider> m_List = null;
        static Hitta_SE_ExtensionProviders()
        {
            m_List = new List<ZoneFiveSoftware.Common.Visuals.Fitness.GPS.IMapProvider>();
            m_List.Add(new Hitta_SE_MapProvider("Sat"));
            m_List.Add(new Hitta_SE_MapProvider("Map"));
            m_List.Add(new EniroMapProvider("map"));
            m_List.Add(new EniroMapProvider("aerial"));
            m_List.Add(new EniroMapProvider("nautical"));
            //m_List.Add(new Eniro_SE_MapProvider("Map", "FI_fi"));
        }
    #region IExtendMapProviders Members
        public IList<ZoneFiveSoftware.Common.Visuals.Fitness.GPS.IMapProvider> MapProviders
        {
            get { return m_List; }
        }
    #endregion
    }
#else

    public class Hitta_SE_ExtensionProviders : IExtendMapTileProviders
    {
        private static IList<IMapTileProvider> m_List = null;
        public IList<IMapTileProvider> MapTileProviders
        {
            get {
                if (null == m_List)
                {
                    m_List = new List<IMapTileProvider>();
                    m_List.Add(new HittaEniroMapProvider(SwedishMapProvider.Eniro, MapViewType.Map));
                    m_List.Add(new HittaEniroMapProvider(SwedishMapProvider.Eniro, MapViewType.Aerial));
                    m_List.Add(new HittaEniroMapProvider(SwedishMapProvider.Eniro, MapViewType.Nautical));
                    m_List.Add(new HittaEniroMapProvider(SwedishMapProvider.Hitta, MapViewType.Map));
                    m_List.Add(new HittaEniroMapProvider(SwedishMapProvider.Hitta, MapViewType.Aerial));
                }
                return m_List; 
            }
        }

    }
#endif

}
