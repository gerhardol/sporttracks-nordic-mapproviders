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

//#define nontile
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Net;
using System.Globalization;
using Lofas.Projection;
#if ST_2_1
using ZoneFiveSoftware.Common.Visuals.Fitness.GPS;
#else
using ZoneFiveSoftware.Common.Visuals.Mapping;
#endif

namespace Lofas.SportsTracks.Hitta_SE_MapProvider
{
    public class Hitta_SE_MapProjection : IMapProjection
    {
        public double[] scaleValues = { 756, 1890, 7559, 15118, 37795, 94488, 264567, 755906, 2645669, 13228346 };
        public double[] ZOOM_LEVELS = { 0.2, 0.5, 2, 4, 10, 25, 70, 200, 700, 3500 };

        public double getMetersPerPixel(double zoomLevel, out double tileMeterPerPixel, out double hittascale)
        {
            double useZoomLevel = ZOOM_LEVELS[ZOOM_LEVELS.Length - 1];
            double useScale = scaleValues[scaleValues.Length - 1];
            //double minDist = Double.MaxValue;
            /*for (int i = ZOOM_LEVELS.Length - 1; i >= 0; i--)
            {
                if (Math.Abs(zoomLevel - ZOOM_LEVELS[i]) < minDist)
                {
                    useZoomLevel = ZOOM_LEVELS[i];
                    useScale = scaleValues[i];
                    minDist = Math.Abs(zoomLevel - ZOOM_LEVELS[i]);
                }
            }*/

            int zoomLevelInt = (int)Math.Floor(zoomLevel);
            int level1 = zoomLevelInt;
            int level2 = zoomLevelInt + 1;
            double zoomLevelRest = zoomLevel - zoomLevelInt;
            useZoomLevel = ZOOM_LEVELS[zoomLevelInt];
            tileMeterPerPixel = useZoomLevel;
            useScale = scaleValues[zoomLevelInt];
            if (zoomLevelRest > 0.5)
            {
                tileMeterPerPixel = ZOOM_LEVELS[zoomLevelInt + 1];
                useScale = scaleValues[zoomLevelInt + 1];
            }

            if (level2 < ZOOM_LEVELS.Length && zoomLevelRest > 1e-6)
                useZoomLevel += (zoomLevelRest * (ZOOM_LEVELS[level2] - ZOOM_LEVELS[level1]));

            hittascale = useScale;


            return useZoomLevel;
        }
        public System.Drawing.Point GPSToPixel(ZoneFiveSoftware.Common.Data.GPS.IGPSLocation origin, double zoomLevel, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation gps)
        {
            double hittascale;
            double tileMeterPerPixel;
            double metersPerPixel = getMetersPerPixel(zoomLevel, out tileMeterPerPixel, out hittascale);
            double x, y, origx, origy;
            CFProjection.WGS84ToRT90(gps.LatitudeDegrees, gps.LongitudeDegrees, 0, out x, out y);
            CFProjection.WGS84ToRT90(origin.LatitudeDegrees, origin.LongitudeDegrees, 0, out origx, out origy);

            int dx = (int)Math.Round((x - origx) / metersPerPixel);
            int dy = (int)Math.Round((origy - y) / metersPerPixel);

            return new System.Drawing.Point(dx, dy);
        }

        public ZoneFiveSoftware.Common.Data.GPS.IGPSLocation PixelToGPS(ZoneFiveSoftware.Common.Data.GPS.IGPSLocation origin, double zoomLevel, System.Drawing.Point pixel)
        {
            double hittascale;
            double tileMeterPerPixel;
            double metersPerPixel = getMetersPerPixel(zoomLevel, out tileMeterPerPixel, out hittascale);

            double lat, lon, origx, origy;
            CFProjection.WGS84ToRT90(origin.LatitudeDegrees, origin.LongitudeDegrees, 0, out origx, out origy);

            double x = origx + pixel.X * metersPerPixel;
            double y = origy - pixel.Y * metersPerPixel;

            CFProjection.RT90ToWGS84(x, y, out lat, out lon);
            return new ZoneFiveSoftware.Common.Data.GPS.GPSLocation((float)lon, (float)lat);
        }
    }
}