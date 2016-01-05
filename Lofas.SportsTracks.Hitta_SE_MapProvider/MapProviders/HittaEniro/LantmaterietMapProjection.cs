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
        private readonly int MAX_ST_ZOOMLEVEL;
        private const int MAX_LM_ZOOMLEVEL = 9;
        private readonly int TILE_SIZE = 256;
        public LantmaterietMapProjection(int maxZoom)
        {
            MAX_ST_ZOOMLEVEL = maxZoom;
        }

        #region IMapProjection Members
        public Point GPSToPixel(IGPSLocation origin, double zoomST, IGPSLocation gps)
        {
            double originX, originY, gpsX, gpsY;
            FromGPSLocation(origin, out originX, out originY);
            FromGPSLocation(gps, out gpsX, out gpsY);
            double pxSz = pixelSize(zoomST);
            int pixX = (int)Math.Round((gpsX - originX) / pxSz);
            int pixY = (int)Math.Round((originY - gpsY) / pxSz);

            return new Point(pixX, pixY);
        }

        public IGPSLocation PixelToGPS(IGPSLocation origin, double zoomST, Point pixel)
        {
            double originX, originY;
            FromGPSLocation(origin, out originX, out originY);
            double pxSz = pixelSize(zoomST);
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
            double zoomProvider = MAX_ST_ZOOMLEVEL - zoomST;
            if (zoomProvider > MAX_LM_ZOOMLEVEL) { zoomProvider = MAX_LM_ZOOMLEVEL; }
            return zoomProvider;
        }

        //Size of ST pixel, will be the same as physical tile up to MAX_LM_ZOOMLEVEL
        private double pixelSize(double zoomST)
        {
            //One pixel for LM Zoom level 0 is 4096m, 9 is 8m
            double zoom = MAX_ST_ZOOMLEVEL - zoomST;
            return Math.Pow(2, 12 - zoom);
        }

        //The number of tiles are in the GetCapabilities wmts call
        //minTile is always 0, TileWidth/TileHeight is fixed to 256, BoundingBox coordinates fixed. ScaleDominator dont care
        private int maxNoOfTiles(double zoomProvider)
        {
            return (int)Math.Pow(2, zoomProvider + 2) - 1;
        }

        //No need to request tiles not existing
        private int limitTile(int tile, double zoomProvider)
        {
            return tile < 0 ? 0 : tile > maxNoOfTiles(zoomProvider) ? maxNoOfTiles(zoomProvider) : tile;
        }

        public int TileSize(double zoomST)
        {
            int sz = TILE_SIZE;
            double zoom = MAX_ST_ZOOMLEVEL - zoomST;
            if (zoom > MAX_LM_ZOOMLEVEL)
            {
                sz *= (int)Math.Pow(2, zoom - MAX_LM_ZOOMLEVEL);
            }
            return sz;
        }

        //Fix for overlap in overzoom
        private int tileOverlap(double zoomST)
        {
            int sz = 0;
            double zoom = MAX_ST_ZOOMLEVEL - zoomST;
            if (zoom > MAX_LM_ZOOMLEVEL)
            {
                sz = (int)Math.Pow(2, zoom - MAX_LM_ZOOMLEVEL-1);
            }
            return sz;
        }

        public IEnumerable<HittaEniroMapProvider.MapTileInfo> GetTileInfo(Rectangle drawRect, double zoomST, IGPSLocation center)
        {
            IList<HittaEniroMapProvider.MapTileInfo> tiles = new List<HittaEniroMapProvider.MapTileInfo>();

            double zoomProvider = zoomSTtoProvider(zoomST);
            double pxSz = pixelSize(zoomST);
            int tileSize = this.TileSize(zoomST);

            double centerSweX, centerSweY;
            FromGPSLocation(center, out centerSweX, out centerSweY);
            //Pixel position in the BoundingBox, convert SweRef99 to pixels with the offset
            const int boundSweX = -1200000;
            const int boundSweY = 8500000;
            double pixWestX  = (centerSweX - boundSweX) / pxSz - drawRect.Width / 2;
            double pixEastX  = (centerSweX - boundSweX) / pxSz + drawRect.Width / 2;
            double pixNorthY = (boundSweY - centerSweY) / pxSz - drawRect.Height / 2;
            double pixSouthY = (boundSweY - centerSweY) / pxSz + drawRect.Height / 2;

            int tileWestX = (int)Math.Floor(pixWestX / tileSize);
            int tileEastX = (int)Math.Ceiling(pixEastX / tileSize);
            int tileNorthY = (int)Math.Floor(pixNorthY / tileSize);
            int tileSouthY = (int)Math.Ceiling(pixSouthY / tileSize);

            if (tileEastX < 0 || tileSouthY < 0 || 
                tileWestX > maxNoOfTiles(zoomProvider) || tileNorthY > maxNoOfTiles(zoomProvider))
            {
                //Not within range, nothing to fetch
                return tiles;
            }
            //The tile (normally) start outside the viewable frame, need the offset (before truncating)
            int pixTileX = drawRect.X + tileSize * tileWestX - (int)(pixWestX);
            int pixTileY = drawRect.Y + tileSize * tileNorthY - (int)(pixNorthY);
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

            int tileXtra = tileOverlap(zoomST);
            for (int x = tileWestX; x <= tileEastX; x++)
            {
                for (int y = tileNorthY; y <= tileSouthY; y++)
                {
                    long ix = tileSize * (x - tileWestX) + pixTileX;
                    long iy = tileSize * (y - tileNorthY) + pixTileY;

                    tiles.Add(new HittaEniroMapProvider.MapTileInfo(zoomProvider, x, y, ix, iy, tileSize + tileXtra, tileSize + tileXtra, regionToBeInvalidated));
                }
            }
            return tiles;
        }
    }
}
