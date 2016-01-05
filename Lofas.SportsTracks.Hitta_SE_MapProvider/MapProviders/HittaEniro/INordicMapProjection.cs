/*
Copyright (C) 2008, 2009, 2010 Peter Löfås
Copyright (C) 2008, 2009, 2015 Gerhard Olsson

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

using System.Drawing;
using ZoneFiveSoftware.Common.Data.GPS;
using System.Collections.Generic;
#if ST_2_1
using ZoneFiveSoftware.Common.Visuals.Fitness.GPS;
#else
using ZoneFiveSoftware.Common.Visuals.Mapping;
#endif

namespace Lofas.SportsTracks.Hitta_SE_MapProvider
{
    //The Tilehandling is kept with the projection, as it is dependent on the zoom
    public interface INordicMapProjection : IMapProjection
    {
        IEnumerable<HittaEniroMapProvider.MapTileInfo> GetTileInfo(Rectangle drawRect, double zoomST, IGPSLocation center);
        int TileSize(double zoomST);
    }
}
