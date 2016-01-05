/*
Copyright (C) 2016 Gerhard Olsson

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
using Lofas.Projection;

namespace Lofas.SportsTracks.Hitta_SE_MapProvider
{
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
        }

        static IGPSLocation ToGPSLocation(double gpsX, double gpsY)
        {
            double lat, lon;
            CFProjection.SWEREF99TMToWGS84(gpsX, gpsY, out lat, out lon);

            return new GPSLocation((float)lat, (float)lon);
        }

        private double zoomSTtoProvider(double zoomST)
        {
            return MAX_ZOOMLEVEL - zoomST;
        }

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
}
