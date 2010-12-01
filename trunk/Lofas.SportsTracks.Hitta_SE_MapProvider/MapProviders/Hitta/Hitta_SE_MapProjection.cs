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
using ZoneFiveSoftware.Common.Data.GPS;
#if ST_2_1
using ZoneFiveSoftware.Common.Visuals.Fitness.GPS;
#else
using ZoneFiveSoftware.Common.Visuals.Mapping;
#endif

namespace Lofas.SportsTracks.Hitta_SE_MapProvider
{
    public class Hitta_SE_MapProjection : IMapProjection
    {
        private static readonly double[] scaleValues = { 756, 1890, 7559, 15118, 37795, 94488, 264567, 755906, 2645669, 13228346 };
        private static readonly double[] ZOOM_LEVELS = { 0.2, 0.5, 2, 4, 10, 25, 70, 200, 700, 3500 };

        public static double getResolution(double useZoomLevel)
        {
            return ZOOM_LEVELS[Array.IndexOf(scaleValues, useZoomLevel)];
        }

        public static double getMetersPerPixel(double zoomLevel)
        {
            double tileMeterPerPixel, hittascale;
            return getMetersPerPixel(zoomLevel, out tileMeterPerPixel, out hittascale);
        }
        public static double getMetersPerPixel(double zoomLevel, out double tileMeterPerPixel, out double hittascale)
        {
            //double useZoomLevel = ZOOM_LEVELS[ZOOM_LEVELS.Length - 1];
            //double useScale = scaleValues[scaleValues.Length - 1];
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

            int zoomLevelIndex = (int)Math.Floor(zoomLevel);
            if (zoomLevelIndex < 0) { zoomLevelIndex = 0; }
            if (zoomLevelIndex >= scaleValues.Length) { zoomLevelIndex = scaleValues.Length - 1; }

            double useScale = scaleValues[zoomLevelIndex];
            double useZoomLevel = ZOOM_LEVELS[zoomLevelIndex];
            tileMeterPerPixel = useZoomLevel;

            //Possibly adjust the values from the "fractional" rest part of the zoom
            double zoomLevelRest = zoomLevel - zoomLevelIndex;
            if (zoomLevelRest > 0.5 && zoomLevelIndex < ZOOM_LEVELS.Length - 1)
            {
                //The ST zoom is larger than the current, use next level
                tileMeterPerPixel = ZOOM_LEVELS[zoomLevelIndex + 1];
                useScale = scaleValues[zoomLevelIndex + 1];
            }

            if (zoomLevelIndex < ZOOM_LEVELS.Length - 1 - 1 && zoomLevelRest > 1e-6)
                useZoomLevel += (zoomLevelRest * (ZOOM_LEVELS[zoomLevelIndex + 1] - ZOOM_LEVELS[zoomLevelIndex]));

            hittascale = useScale;

            return useZoomLevel;
        }

        public static int isValidPoint(IGPSLocation gps)
        {
            return isValidPoint(gps.LatitudeDegrees, gps.LongitudeDegrees);
        }
        public static int isValidPoint(double lat, double lon)
        {
            //Approx Swedish coordinates
            //Hitta.se has a few overview maps for Scandinavia, but nothing useful
            if (lat < 55 || lat > 70 || lon < 10 || lon > 25)
            {
                return 0;
            }
            return 1;
        }
        public static int WGS84ToRT90(IGPSLocation gps, out double x, out double y)
        {
            int result = isValidPoint(gps);
            CFProjection.WGS84ToRT90(gps.LatitudeDegrees, gps.LongitudeDegrees, 0, out x, out y);
            
            return result;
        }
        public static int RT90ToWGS84(double x, double y, out double lat, out double lon)
        {
            CFProjection.RT90ToWGS84(x, y, out lat, out lon);
            int result = isValidPoint(lat, lon);

            return result;
        }
        public System.Drawing.Point GPSToPixel(IGPSLocation origin, double zoomLevel, IGPSLocation gps)
        {
            double metersPerPixel = getMetersPerPixel(zoomLevel);
            double x, y, origx, origy;
            WGS84ToRT90(gps, out x, out y); 
            WGS84ToRT90(origin, out origx, out origy);

            int dx = (int)Math.Round((x - origx) / metersPerPixel);
            int dy = (int)Math.Round((origy - y) / metersPerPixel);

            return new System.Drawing.Point(dx, dy);
        }

        public IGPSLocation PixelToGPS(IGPSLocation origin, double zoomLevel, System.Drawing.Point pixel)
        {
            double metersPerPixel = getMetersPerPixel(zoomLevel);

            double lat, lon, origx, origy;
            WGS84ToRT90(origin, out origx, out origy);

            double x = origx + pixel.X * metersPerPixel;
            double y = origy - pixel.Y * metersPerPixel;

            RT90ToWGS84(x, y, out lat, out lon);
            return new GPSLocation((float)lat, (float)lon);
        }
    }
}