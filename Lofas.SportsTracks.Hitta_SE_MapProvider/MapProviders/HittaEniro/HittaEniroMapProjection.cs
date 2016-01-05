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

using System;
using System.Diagnostics;
using System.Drawing;
using ZoneFiveSoftware.Common.Data.GPS;
using System.Collections.Generic;
#if ST_2_1
using ZoneFiveSoftware.Common.Visuals.Fitness.GPS;
#else
using ZoneFiveSoftware.Common.Visuals.Mapping;
#endif
using Lofas.Projection;

namespace Lofas.SportsTracks.Hitta_SE_MapProvider
{
    public interface INordicMapProjection : IMapProjection
    {
        IEnumerable<HittaEniroMapProvider.MapTileInfo> GetTileInfo(Rectangle drawRect, double zoomST, IGPSLocation center);
    }

    public class LantmaterietMapProjection : INordicMapProjection
    {
        private readonly int MAX_ZOOMLEVEL;
        private readonly int TILE_SIZE;
        public LantmaterietMapProjection(int maxZoom, int tileWidth)
        {
            MAX_ZOOMLEVEL = maxZoom;
            TILE_SIZE = tileWidth;
        }

        #region IMapProjection Members
        public Point GPSToPixel(IGPSLocation origin, double zoomST, IGPSLocation gps)
        {
            double originX, originY, gpsX, gpsY;
            FromGPSLocation(origin, out originX, out originY);
            FromGPSLocation(gps, out gpsX, out gpsY);
            double pxSz = pixelSize(zoomSTtoProvider(zoomST));
            int pixX = (int)Math.Round((gpsX - originX) / pxSz);
            int pixY = (int)Math.Round((originY - gpsY) / pxSz);

            return new Point(pixX, pixY);
        }

        public IGPSLocation PixelToGPS(IGPSLocation origin, double zoomST, Point pixel)
        {
            double originX, originY;
            FromGPSLocation(origin, out originX, out originY);
            double pxSz = pixelSize(zoomSTtoProvider(zoomST));
            double gpsX = originX + pxSz * pixel.X;
            double gpsY = originY - pxSz * pixel.Y;

            return ToGPSLocation(gpsX, gpsY);
        }
        #endregion

        static void FromGPSLocation(IGPSLocation gps, out double sweX, out double sweY)
        {
            CFProjection.WGS84ToSWEREF99TM(gps.LatitudeDegrees, gps.LongitudeDegrees, out sweX, out sweY);
            /*        MightyLittleGeodesy.Positions.WGS84Position wgsPos = new MightyLittleGeodesy.Positions.WGS84Position(gps.LatitudeDegrees, gps.LongitudeDegrees);
                    MightyLittleGeodesy.Positions.SWEREF99Position rtPos = new MightyLittleGeodesy.Positions.SWEREF99Position(wgsPos, MightyLittleGeodesy.Positions.SWEREF99Position.SWEREFProjection.sweref_99_tm);
                    sweY = rtPos.Latitude;
                    sweX = rtPos.Longitude;
              */
        }

        static IGPSLocation ToGPSLocation(double gpsX, double gpsY)
        {
            double lat, lon;
            CFProjection.SWEREF99TMToWGS84(gpsX, gpsY, out lat, out lon);
/*            MightyLittleGeodesy.Positions.SWEREF99Position swePos = new MightyLittleGeodesy.Positions.SWEREF99Position(gpsY, gpsX);
            MightyLittleGeodesy.Positions.WGS84Position wgsPos = swePos.ToWGS84();
            lat = wgsPos.Latitude;
            lon = wgsPos.Longitude;
            */
            return new GPSLocation((float)lat, (float)lon);
        }

        private double zoomSTtoProvider(double zoomST)
        {
            return MAX_ZOOMLEVEL - zoomST;
        }

        //Size of a pixel in SWEREF coordinates (m)
        private double pixelSize(double zoomProvider)
        {
            //One pixel for Zoom level 0 is 4096m, 9 is 8m
            return Math.Pow(2, 12-zoomProvider);
        }

        //The number of tiles are in the GetCapabilities wmts call
        //minTile is always 0, TileWidth/TileHeight is fixed to 256, BoundingBox coordinates fixed. ScaleDominator dont care
        private int maxTile(double zoomProvider)
        {
            return (int)Math.Pow(2, zoomProvider + 2) - 1;
        }

        //No need to request tiles not existing
        private int limitTile(int tile, double zoomProvider)
        {
            return tile < 0 ? 0 : tile > maxTile(zoomProvider) ? maxTile(zoomProvider) : tile;
        }

        public IEnumerable<HittaEniroMapProvider.MapTileInfo> GetTileInfo(Rectangle drawRect, double zoomST, IGPSLocation center)
        {
            IList<HittaEniroMapProvider.MapTileInfo> tiles = new List<HittaEniroMapProvider.MapTileInfo>();


            double zoomProvider = zoomSTtoProvider(zoomST);
            double pxSz = pixelSize(zoomProvider);

            double centerSweX, centerSweY;
            FromGPSLocation(center, out centerSweX, out centerSweY);
            //Pixel position in the BoundingBox, convert SweRef99 to pixels with the offset
            const int boundSweX = -1200000;
            const int boundSweY = 8500000;
            double pixWestX  = (centerSweX - boundSweX) / pxSz - drawRect.Width / 2;
            double pixEastX  = (centerSweX - boundSweX) / pxSz + drawRect.Width / 2;
            double pixNorthY = (boundSweY - centerSweY) / pxSz - drawRect.Height / 2;
            double pixSouthY = (boundSweY - centerSweY) / pxSz + drawRect.Height / 2;

            int tileWestX = (int)Math.Floor(pixWestX / TILE_SIZE);
            int tileEastX = (int)Math.Ceiling(pixEastX / TILE_SIZE);
            int tileNorthY = (int)Math.Floor(pixNorthY / TILE_SIZE);
            int tileSouthY = (int)Math.Ceiling(pixSouthY / TILE_SIZE);

            if (tileEastX < 0 || tileSouthY < 0 || 
                tileWestX > maxTile(zoomProvider) || tileNorthY > maxTile(zoomProvider))
            {
                //Not within range, nothing to fetch
                return tiles;
            }
            //The tile (normally) start outside the viewable frame, need the offset (before truncating)
            int pixTileX = drawRect.X + TILE_SIZE * tileWestX - (int)(pixWestX);
            int pixTileY = drawRect.Y + TILE_SIZE * tileNorthY - (int)(pixNorthY);
            tileWestX = limitTile(tileWestX, zoomProvider);
            tileEastX = limitTile(tileEastX, zoomProvider);
            tileNorthY = limitTile(tileNorthY, zoomProvider);
            tileSouthY = limitTile(tileSouthY, zoomProvider);

            // Calculation to find out which region to be invalidated
            //It seem natural to have each tile to cover its own area only, but all must be invalidatedfor this ti 
            Point northWestPoint = new Point(-drawRect.Width / 2, -drawRect.Height / 2);
            Point southEastPoint = new Point(drawRect.Width / 2, drawRect.Height / 2);
            IGPSLocation northWestLocation = PixelToGPS(center, zoomST, northWestPoint);
            IGPSLocation southEastLocation = PixelToGPS(center, zoomST, southEastPoint);
            IGPSBounds regionToBeInvalidated = new GPSBounds(northWestLocation, southEastLocation);

            for (int x = tileWestX; x <= tileEastX; x++)
            {
                for (int y = tileNorthY; y <= tileSouthY; y++)
                {
                    long ix = TILE_SIZE * (x - tileWestX) + pixTileX;
                    long iy = TILE_SIZE * (y - tileNorthY) + pixTileY;

                    tiles.Add(new HittaEniroMapProvider.MapTileInfo(zoomProvider, x, y, ix, iy, this.TILE_SIZE, this.TILE_SIZE, regionToBeInvalidated));
                }
            }
            return tiles;
        }

    }

    public class HittaEniroMapProjection : INordicMapProjection
    {
        // Här ska vi konvertera från GPS till Pixel och vice versa. Vi får in en original GPS-position och en gps position och en zoomnivå. 
        // Då ska vi räkna ut vilken punkt gpspositionen med utgångspunkt från originalpositionen. 
        // Följande GPS-pos skickas in 60,63974;16,96524 och det ger punkten -186;-65

        private readonly int MAX_ZOOMLEVEL;
        private readonly int TILE_SIZE;

        public HittaEniroMapProjection(int maxZoom, int tileWidth)
        {
            MAX_ZOOMLEVEL = maxZoom;
            TILE_SIZE = tileWidth;
        }
        #region IMapProjection Members

        ///<summary>
        ///     Given a GPS location, translate it to a pixel relative to the origin.
        ///</summary>
        ///<param name="origin">The GPS location at pixel location (0,0).</param>
        ///<param name="zoomST">The current zoom level.</param>
        ///<param name="gps">The GPS location.</param>
        ///<returns>
        ///The pixel point.
        ///</returns>
        public Point GPSToPixel(IGPSLocation origin, double zoomST, IGPSLocation gps)
        {
            double zoom = MAX_ZOOMLEVEL - zoomST;
            long dx;
            long dy;
            try
            {
                long originX = Xpixel(origin.LongitudeDegrees, zoom);
                long originY = Ypixel(origin.LatitudeDegrees, zoom);
                long gpsX = Xpixel(gps.LongitudeDegrees, zoom);
                long gpsY = Ypixel(gps.LatitudeDegrees, zoom);

                dx = gpsX - originX;
                dy = gpsY - originY;
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                //throw e;
                //Avoid exception when the position is invalid
                dx = 0;
                dy = 0;
            }
            return new Point((int)dx, (int)dy);
        }


        ///<summary>
        ///     Given a pixel point in the control (offset from the origin), what is the GPS location ?
        ///</summary>
        ///
        ///<param name="origin">The GPS location at pixel location (0,0).</param>
        ///<param name="zoomST">The current zoom level.</param>
        ///<param name="pixel">The pixel point.</param>
        ///<returns>
        ///The GPS location.
        ///</returns>
        public IGPSLocation PixelToGPS(IGPSLocation origin, double zoomST, Point pixel)
        {
            double zoom = MAX_ZOOMLEVEL - zoomST;
            float latitude;
            float longitude;
            try
            {
                long originX = Xpixel(origin.LongitudeDegrees, zoom);
                long originY = Ypixel(origin.LatitudeDegrees, zoom);

                long pixelX = originX + pixel.X;
                long pixelY = originY + pixel.Y;

                latitude = YToLatitude(pixelY, zoom);
                longitude = XToLongitude(pixelX, zoom);
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                //throw e;
                //Avoid exception when 
                latitude = origin.LatitudeDegrees + (float)(pixel.Y*zoom/20000);
                longitude = origin.LongitudeDegrees + (float)(pixel.X * zoom / 20000);
            }
            return new GPSLocation(latitude, longitude);
        }

        #endregion

        /// <summary>
        /// Find out the x-pixel of a specific longitude
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public long Xpixel(double longitude, double zoom)
        {
            if (zoom > MAX_ZOOMLEVEL)
            {
                return 0;
            }

            // Instead of -180 to +180, we want 0 to 360
            double dlng = longitude + 180;
            double dxpixel = dlng / 360.0 * TILE_SIZE * Math.Pow(2, zoom);
            long xpixel = Convert.ToInt32(Math.Floor(dxpixel));
            return xpixel;
        }

        /// <summary>
        /// Get the y-pixel of a specific latitude
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public long Ypixel(double latitude, double zoom)
        {
            if (zoom > MAX_ZOOMLEVEL)
            {
                return 0;
            }
            // ypixelcenter = the middle y pixel (the equator) at this zoom level
            double ypixelcenter = Math.Pow(2, zoom - 1);

            // PI/360 == degrees -&gt; radians
            // The trig functions are done with radians
            double dypixel = TILE_SIZE * (ypixelcenter - Math.Log(Math.Tan(latitude * Math.PI / 360 + Math.PI / 4)) * ypixelcenter / Math.PI);
            long ypixel = Convert.ToInt32(Math.Floor(dypixel));
            return ypixel;
        }

        /// <summary>
        /// Get the x-tile of a specific longitude
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public int XTile(double longitude, double zoom)
        {
            return Convert.ToInt32(Math.Floor((double)Xpixel(longitude, zoom) / TILE_SIZE));
        }

        /// <summary>
        /// Get the y-tile of a specific latitude
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public int YTile(double latitude, double zoom)
        {
            return Convert.ToInt32(Math.Floor((double)Ypixel(latitude, zoom) / TILE_SIZE));
        }

        /// <summary>
        /// Get the pixel of the North-West corner of a certain tile.
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        public long PixelOfNorthWestCornerOfTile(long tile)
        {
            return tile * TILE_SIZE;
        }

        /// <summary>
        /// Converts an y-pixel to latitude
        /// </summary>
        /// <param name="y"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public float YToLatitude(long y, double zoom)
        {
            double w = Math.Pow(2, zoom - 1);
            double dy = Convert.ToDouble(y);
            double num0 = Math.PI * (w - dy / TILE_SIZE);
            double num1 = Math.Atan(Math.Exp(num0 / w)) - Math.PI / 4;
            double latitude = 360 * num1 / Math.PI;
            return (float)latitude;
        }

        /// <summary>
        /// Converts an x-pixel to longitude
        /// </summary>
        /// <param name="X"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public float XToLongitude(long X, double zoom)
        {
            double dx = Convert.ToDouble(X);

            return (float) ((360*dx)/(TILE_SIZE*Math.Pow(2, zoom)) - 180);
        }

        /// <summary>
        /// Get information about map tiles
        /// </summary>
        public IEnumerable<HittaEniroMapProvider.MapTileInfo> GetTileInfo(Rectangle drawRect, double zoomST, IGPSLocation center)
        {
            IList<HittaEniroMapProvider.MapTileInfo> tiles = new List<HittaEniroMapProvider.MapTileInfo>();

            if (HittaEniroMapProjection.IsValidLocation(center))
            {
                // Convert the zoom level to match the zoom-levels of the map provider.
                double zoomProvider = this.MAX_ZOOMLEVEL - zoomST;

                long xTileOfCenter = XTile(center.LongitudeDegrees, zoomProvider);
                long yTileOfCenter = YTile(center.LatitudeDegrees, zoomProvider);
                var xPixelOfCenter = Xpixel(center.LongitudeDegrees, zoomProvider);
                var yPixelOfCenter = Ypixel(center.LatitudeDegrees, zoomProvider);
                var xPixelOfNWCornerCenterTile = PixelOfNorthWestCornerOfTile(xTileOfCenter);
                var yPixelOfNWCornerCenterTile = PixelOfNorthWestCornerOfTile(yTileOfCenter);

                var xPixelOffsetCenterVsNWCornerOfCenterTile = xPixelOfCenter - xPixelOfNWCornerCenterTile;
                var yPixelOffsetCenterVsNWCornerOfCenterTile = yPixelOfCenter - yPixelOfNWCornerCenterTile;

                var xPixelsFromLeftEdgeOfDrawingAreaToLeftEdgeOfCenterTile = (drawRect.Width / 2) -
                                                                              xPixelOffsetCenterVsNWCornerOfCenterTile;
                var yPixelsFromTopEdgeOfDrawingAreaToTopEdgeOfCenterTile = (drawRect.Height / 2) -
                                                                            yPixelOffsetCenterVsNWCornerOfCenterTile;

                var noOfTilesToBeDrawnToTheLeftOfCenterTile =
                    (int)Math.Ceiling((double)xPixelsFromLeftEdgeOfDrawingAreaToLeftEdgeOfCenterTile / this.TILE_SIZE);
                var noOfTilesToBeDrawnAboveOfCenterTile =
                    (int)Math.Ceiling((double)yPixelsFromTopEdgeOfDrawingAreaToTopEdgeOfCenterTile / this.TILE_SIZE);

                var xNWStartPixel = xPixelsFromLeftEdgeOfDrawingAreaToLeftEdgeOfCenterTile -
                                     (this.TILE_SIZE * noOfTilesToBeDrawnToTheLeftOfCenterTile);
                var yNWStartPixel = yPixelsFromTopEdgeOfDrawingAreaToTopEdgeOfCenterTile -
                                     (this.TILE_SIZE * noOfTilesToBeDrawnAboveOfCenterTile);

                var noOfTilesToBeDrawnHorizontally = (int)Math.Ceiling((double)drawRect.Width / this.TILE_SIZE + 1);
                var noOfTilesToBeDrawnVertically = (int)Math.Ceiling((double)drawRect.Height / this.TILE_SIZE + 1);
                var startXTile = xTileOfCenter - noOfTilesToBeDrawnToTheLeftOfCenterTile;
                var startYTile = yTileOfCenter - noOfTilesToBeDrawnAboveOfCenterTile;

                // Calculation to find out which region to be invalidated
                //TBD it would have been natural to have each tile to cover its own area only, but it aint so
                Point northWestPoint = new Point(-drawRect.Width / 2, -drawRect.Height / 2);
                Point southEastPoint = new Point(drawRect.Width / 2, drawRect.Height / 2);
                IGPSLocation northWestLocation = PixelToGPS(center, zoomST, northWestPoint);
                IGPSLocation southEastLocation = PixelToGPS(center, zoomST, southEastPoint);
                IGPSBounds regionToBeInvalidated = new GPSBounds(northWestLocation, southEastLocation);

                // We have calculated the start tile, that is the tile in the north-west corner of the drawing area. 
                // Now we will iterate left to right and top to bottom so that all tiles is either drawn or downloaded.
                for (int x = 0; x < noOfTilesToBeDrawnHorizontally; x++)
                {
                    for (int y = 0; y < noOfTilesToBeDrawnVertically; y++)
                    {
                        long tileXToBeDrawn = startXTile + x;
                        long tileYToBeDrawn = startYTile + y;
                        long tileYToBeDrawnProvider = (long)Math.Pow(2, zoomProvider) - 1 - tileYToBeDrawn;
                        long ix = xNWStartPixel + x * this.TILE_SIZE;
                        long iy = yNWStartPixel + y * this.TILE_SIZE;

                        tiles.Add(new HittaEniroMapProvider.MapTileInfo(zoomProvider, tileXToBeDrawn, tileYToBeDrawnProvider, ix, iy, this.TILE_SIZE, this.TILE_SIZE, regionToBeInvalidated));
                    }
                }
            }

            return tiles;
        }

        /// <summary>
        /// Validates if the location falls within nordic range.
        /// </summary>
        /// <param name="gps"></param>
        /// <returns></returns>
        private static bool IsValidLocation(IGPSLocation gps)
        {
            return IsValidLocation(gps.LatitudeDegrees, gps.LongitudeDegrees);
        }

        /// <summary>
        /// Validates if location falls within nordic range.
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <returns></returns>
        private static bool IsValidLocation(double lat, double lon)
        {
            //Approx Swedish coordinates, really 10.5700, 55.2000, 24.1800, 69.1000, but some stretch
            if (lat < 55 || lat > 72 || lon < 4 || lon > 32)
            {
                return false;
            }
            return true;
        }
    }
}
