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
using System.Diagnostics;
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
    public class Hitta_SE_MapProvider : 
#if ST_2_1
        ZoneFiveSoftware.Common.Visuals.Fitness.GPS.IMapProvider
#else
        ZoneFiveSoftware.Common.Visuals.Mapping.IMapTileProvider
#endif
    {
    #region IMapProvider Members

        const int tileX2 = 128;
        const int tileY2 = tileX2;
        private readonly string m_CacheDirectory;
        private readonly Dictionary<string, string> m_DownloadQueueItems;

        //private readonly string m_View;
        private readonly string m_ImageExt;
        private readonly string m_Name;
        private readonly Guid m_GUID;
        private readonly Hitta_SE_MapProjection m_Proj = new Hitta_SE_MapProjection();
        private readonly string m_BaseUrl;

        public Hitta_SE_MapProvider(string view)
        {
            //m_View = view;
            m_CacheDirectory = Path.Combine(
#if ST_2_1
                Plugin.m_Application.SystemPreferences.WebFilesFolder, "MapTiles" + 
#else
                Plugin.m_Application.Configuration.CommonWebFilesFolder, GUIDs.PluginMain.ToString() + 
#endif
                Path.DirectorySeparatorChar + "Hitta_SE_" + view);
            m_DownloadQueueItems = new Dictionary<string, string>();
            if (view == "Sat")
            {
                m_ImageExt = "jpg";
                m_GUID = new Guid("23FBB14A-0949-4d42-BAA4-95C3AC3BC825");
                m_Name = "Hitta.se Flygfoto";
            }
            else
            {
                m_ImageExt = "gif";
                m_GUID = new Guid("9BD470DD-3078-456f-8175-1A714D286B90");
                m_Name = "Hitta.se Karta";
            }
            var url = "http://static.hitta.se/tile/v2/";
            if (view == "Sat")
            {
                url += "1/";
            }
            else
            {
                url += "0/";
            }
            m_BaseUrl = url;
        }

        public void ClearDownloadQueue()
        {
            m_DownloadQueueItems.Clear();
        }

        public int DownloadQueueSize
        {
            get { return m_DownloadQueueItems.Count; }
        }

        /// <summary>
        /// Draw the map. Any map images which are immediately available (e.g. cached on disk) should be drawn immediately.  For missing map images, queue them for download and return the number missing.
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="graphics"></param>
        /// <param name="drawRectangle"></param>
        /// <param name="clipRectangle"></param>
        /// <param name="center"></param>
        /// <param name="zoomLevel">Zoom levels are passed from Sport Tracks from 0.5 up to</param>
        /// <returns></returns>
        public int DrawMap(IMapImageReadyListener listener, System.Drawing.Graphics graphics, System.Drawing.Rectangle drawRectangle, System.Drawing.Rectangle clipRectangle, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation center, double zoomLevel)
        {
            double x, y;

            int numQueued = 0;
            try
            {
                if (0 < Hitta_SE_MapProjection.isValidPoint(center))
                {
                    Hitta_SE_MapProjection.WGS84ToRT90(center, out x, out y);
                    numQueued = DrawTiles(listener, graphics, ref drawRectangle, zoomLevel, x, y);
                }
            }
            catch (Exception e)
            {
                Debug.Print(e.Message + Environment.NewLine + e.StackTrace);
            }
            return numQueued;
        }

        private int DrawTiles(IMapImageReadyListener listener, System.Drawing.Graphics graphics, ref System.Drawing.Rectangle drawRectangle, double zoomLevel, double x, double y)
        {
            int numQueued = 0;
            foreach (TileInfo t in getTileInfo(drawRectangle, zoomLevel, x, y))
            {
                if (isCached(t.iRx, t.iRy, t.useScale))
                {
                    Image img = getImageFromCache(t.iRx, t.iRy, t.useScale);
                    float ix = (float)(drawRectangle.X + t.col * t.tileDrawWidth + t.drawTileDX + t.offX);
                    float iy = (float)(drawRectangle.Y + t.row * t.tileDrawWidth + t.drawTileDY + t.offY);
                    float iw = (float)(drawRectangle.X + (t.col + 1) * t.tileDrawWidth + t.drawTileDX) - (float)(drawRectangle.X + (t.col) * t.tileDrawWidth + t.drawTileDX);
                    float ih = (float)(drawRectangle.Y + (t.row + 1) * t.tileDrawWidth + t.drawTileDY) - (float)(drawRectangle.Y + t.row * t.tileDrawWidth + t.drawTileDY);
                    graphics.DrawImage(img, (int)Math.Floor(ix), (int)Math.Floor(iy), (int)Math.Ceiling(iw), (int)Math.Ceiling(ih));
                    img.Dispose();

                }
                else
                {
                    queueDownload(t.rx, t.ry, t.iRx, t.iRy, t.useScale, listener);
                    numQueued++;
                }
            }
            return numQueued;
        }

        private void queueDownload(double cx, double cy, int iRx, int iRy, double useZoomLevel, IMapImageReadyListener listener)
        {
            string item = iRx + "_" + iRy + "_" + useZoomLevel.ToString();
            if (!m_DownloadQueueItems.ContainsKey(item))
            {
                m_DownloadQueueItems.Add(item, "");
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object o)
                {
                    try
                    {
                        lock (STWebClient.Instance)
                        {
                            if (m_DownloadQueueItems.ContainsKey(item))
                            {

#if nontile
                                    string url = "http://map.hitta.se/SpatialAceWMS/RWCInterface.axd?view=Hitta.MainView&format=" + m_ImageExt + "&transparent=false&layers=MainView/" + m_View + "Layer&width=256&height=256&scale=" + useZoomLevel + "&x=" + cx.ToString(CultureInfo.InvariantCulture) + "&y=" + cy.ToString(CultureInfo.InvariantCulture);
#else
                                int bottomLeftX = 451424;
                                int bottomLeftY = 5651424;
                                double resolution = Hitta_SE_MapProjection.getResolution(useZoomLevel);
                                //int column = (int)Math.Round(((cx  - bottomLeftX + ((2*tileX2 / 2) *resolution))) / (2*tileX2 * resolution), 0);
                                //int row = (int)Math.Round(((cy - bottomLeftY + ((2*tileY2 / 2) * resolution))) / (2*tileY2 * resolution), 0);

                                int column = (int)(((cx - bottomLeftX)) / (2 * tileX2 * resolution));
                                int row = (int)(((cy - bottomLeftY)) / (2 * tileY2 * resolution));
                                string geoCenterString = row + "/" + column;
                                string url = m_BaseUrl + resolution.ToString(CultureInfo.InvariantCulture) + "/" + geoCenterString;
#endif
                                Debug.Print(url + " ZOOM: " + useZoomLevel + "/" + resolution);
                                Image img = Image.FromStream(STWebClient.Instance.OpenRead(url));
                                img.Save(getFilePath(iRx, iRy, useZoomLevel, true));
                                img.Dispose();
                            }
                        }
                        MapImageObj obj = new MapImageObj();
                        obj.cx = cx;
                        obj.cy = cy;
                        obj.Scale = useZoomLevel;
#if ST_2_1
                            listener.NotifyMapImageReady(obj);
#else
                        double latN, latS, longW, longE;
                        double useScale;
                        double tileMeterPerPixel;
                        Hitta_SE_MapProjection.getMetersPerPixel(useZoomLevel, out tileMeterPerPixel, out useScale);
                        // Get offset in meters from the center position.
                        //The size invalidated is twice the actual RT90 area. This seem to be required to include the complete tile
                        //Otherwise may there be a row or column on the sides that do not display at all
                        double tileXOffsetFromCenter = 2 * tileX2 * tileMeterPerPixel;
                        double tileYOffsetFromCenter = 2 * tileY2 * tileMeterPerPixel;
                        // Find out upper left and bottom right corner of the tile.
                        Hitta_SE_MapProjection.RT90ToWGS84(cx - tileXOffsetFromCenter, cy + tileYOffsetFromCenter, out latN, out longW);
                        Hitta_SE_MapProjection.RT90ToWGS84(cx + tileXOffsetFromCenter, cy - tileYOffsetFromCenter, out latS, out longE);
                        // Invalidate region
                        listener.InvalidateRegion(new GPSBounds(
                                                        new GPSLocation((float)(latN), (float)(longW)),
                                                        new GPSLocation((float)(latS), (float)(longE))));

#endif
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                    }
                    m_DownloadQueueItems.Remove(item);
                }));
            }
        }

        private IList<TileInfo> getTileInfo(System.Drawing.Rectangle drawRectangle, double zoomLevel, double x, double y)
        {
            double useScale;
            double tileMeterPerPixel;
            double metersPerPixel = Hitta_SE_MapProjection.getMetersPerPixel(zoomLevel, out tileMeterPerPixel, out useScale);

            double ulX = Math.Round(x - ((drawRectangle.Width / 2) * metersPerPixel),1);
            double ulY = Math.Round(y + ((drawRectangle.Height / 2) * metersPerPixel),1);
            double lrX = Math.Round(x + ((drawRectangle.Width / 2) * metersPerPixel),1);
            double lrY = Math.Round(y - ((drawRectangle.Height / 2) * metersPerPixel),1);

            double tileWidth = 2*tileX2 * tileMeterPerPixel;

            double tileDrawWidth = (tileWidth / metersPerPixel);


            //double rx = ulX;

            double startTileX = Math.Round(((int)(ulX / (tileMeterPerPixel * 2*tileX2)) + 0) * tileMeterPerPixel * 2*tileX2 - 0.5, 1);
            double startTileY = Math.Round(((int)(ulY / (tileMeterPerPixel * 2*tileY2)) + 1) * tileMeterPerPixel * 2*tileY2 - 0.5, 1);

            ////beräkna offset vid utritning..
            //double drawTileDX = (startTileX - ulX) / metersPerPixel;
            //double drawTileDY = (ulY - startTileY) / metersPerPixel;
#if nontile
            double offX = 0;
            double offY = 0;
#else
            int bottomLeftX = 451424;
            int bottomLeftY = 5651424;

            double resolution = Hitta_SE_MapProjection.getResolution(useScale);
            int Tcolumn = (int)(((startTileX + tileMeterPerPixel * tileX2 - bottomLeftX)) / (2*tileX2 * resolution));
            int Trow = (int)(((startTileY - tileMeterPerPixel * tileY2 - bottomLeftY)) / (2*tileY2 * resolution));

            double colX = Tcolumn * 2*tileX2 * resolution + bottomLeftX;
            double rowY = Trow * 2*tileY2 * resolution + bottomLeftY;

            if (rowY < ulY)
            {
                Trow++;
                rowY = Trow * 2*tileY2 * resolution + bottomLeftY;
            }

            //double x_Diff = startTileX - colX;
            //double y_Diff = startTileY - rowY;

            startTileX = colX;
            startTileY = rowY;
            double offX = -1 * tileDrawWidth; // x_Diff / tileMeterPerPixel;
            double offY = 0; // y_Diff / tileMeterPerPixel;

            // Changed by Magnus Wallström. There is some problem with the zoom level 0.5 
            // which makes the tiles position vertically wrong. Doing this change seemed to solve it. 
            if (metersPerPixel == 0.75)
            {
                offY = -tileDrawWidth;                    
            }

            int endTcolumn = (int)Math.Round(((lrX + tileMeterPerPixel * tileX2 - bottomLeftX)) / (2*tileX2 * resolution), 0);
            lrX = endTcolumn * 2*tileX2 * resolution + bottomLeftX + tileDrawWidth;
            //ulY = rowY + tileDrawWidth;

            int lowerTrow = (int)Math.Round(((lrY - tileMeterPerPixel * tileY2 - bottomLeftY)) / (2*tileY2 * resolution), 0);
            lrY = lowerTrow * 2*tileY2 * resolution + bottomLeftY;
            //lrX += tileDrawWidth;
            double drawTileDX = (startTileX - ulX) / metersPerPixel;
            double drawTileDY = (ulY - startTileY) / metersPerPixel;
            startTileY = rowY + tileDrawWidth;
#endif
            
            double rx = startTileX;

            int col = 0;
            IList<TileInfo> tiles = new List<TileInfo>();
            while (rx <= lrX)
            {
                //int ry = (int)ulY;
                double ry = startTileY;
                int row = 0;
                while (ry >= lrY)
                {
                    int iRx = (int)Math.Round((rx + tileMeterPerPixel * tileX2) * 10);
                    int iRy = (int)Math.Round((ry - tileMeterPerPixel * tileY2) * 10);
                    tiles.Add(new TileInfo(rx, ry, iRx, iRy, useScale, col, tileDrawWidth, drawTileDX, offX, row, drawTileDY, offY));
                    ry -= 2*tileY2 * tileMeterPerPixel;
                    row++;
                }
                //break;
                col++;
                rx += 2*tileX2 * tileMeterPerPixel;
            }

            return tiles;
        }
            
        private class TileInfo
        {
                //more fields than needed, can be optimised
            //This is remains of having DrawTiles and getTileInfo in the same routine
            public TileInfo(double rx, double ry, int iRx, int iRy, double useScale,
                int col, double tileDrawWidth, double drawTileDX, double offX, 
                int row, double drawTileDY, double offY)
            {
                this.rx = rx;
                this.ry = ry;
                this.iRx = iRx;
                this.iRy = iRy;
                this.useScale = useScale;
                this.col = col;
                this.tileDrawWidth = tileDrawWidth;
                this.drawTileDX = drawTileDX;
                this.offX = offX;
                this.row = row;
                this.drawTileDY = drawTileDY;
                this.offY = offY;
            }
            public double rx;
            public double ry;
            public int iRx;
            public int iRy;
            public double useScale;
            public int  col;
            public double tileDrawWidth;
            public double drawTileDX;
            public double offX;
            public int row;
            public double drawTileDY;
            public double offY;
        }
        private class MapImageObj
        {
            public double cx;
            public double cy;
            public double Scale;
        }

        private string getFilePath(int iRx, int iRy, double useZoomLevel, bool createDir)
        {
            string downloadDir = Path.Combine(m_CacheDirectory, useZoomLevel.ToString());
            if (createDir && !Directory.Exists(downloadDir))
                Directory.CreateDirectory(downloadDir);
            return Path.Combine(downloadDir, iRx + "_" + iRy + "." + m_ImageExt);
        }
        private string getFilePath(int iRx, int iRy, double useZoomLevel)
        {
            return getFilePath(iRx, iRy, useZoomLevel, false);
        }
        private Image getImageFromCache(int iRx, int iRy, double useZoomLevel)
        {
            string str = getFilePath(iRx, iRy, useZoomLevel);

            try
            {
                return Image.FromFile(str);
            }
            catch (Exception)
            {
                try
                {
                    File.Delete(str);
                }
                catch (Exception)
                {
                }

                return new Bitmap(2 * tileX2, 2 * tileY2);
            }
        }

        private bool isCached(int iRx, int iRy, double useZoomLevel)
        {
            return File.Exists(getFilePath(iRx, iRy, useZoomLevel));
        }

        public Guid Id
        {
            get { return m_GUID; }
        }

        public IMapProjection MapProjection
        {
            get { return m_Proj; }
        }

        public double MaximumZoom
        {
            get { return 9; }
        }

        public double MinimumZoom
        {
            get { return 0.5; }
        }

        public bool SupportsFractionalZoom
        {
            get { return false; }
        }

        public string Name
        {
            get { return m_Name; }
        }

        public void Refresh(System.Drawing.Rectangle drawRectangle, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation center, double zoomLevel)
        {
            double x, y;

            if (0 < Hitta_SE_MapProjection.isValidPoint(center))
            {
                Hitta_SE_MapProjection.WGS84ToRT90(center, out x, out y);
                foreach (TileInfo t in getTileInfo(drawRectangle, zoomLevel, x, y))
                {
                    if (isCached(t.iRx, t.iRy, t.useScale))
                    {
                        string str = getFilePath(t.iRx, t.iRy, t.useScale);

                        try
                        {
                            File.Delete(str);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        }

    #endregion

#if ST_2_1
        //A few methods differ ST2/ST3, the ST2 methods are separated
        public System.Drawing.Rectangle MapImagePixelRect(object mapImage, System.Drawing.Rectangle drawRectangle, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation center, double zoomLevel)
        {
            if (mapImage is MapImageObj)
            {
                return drawRectangle;
            }
            else
            {
                return Rectangle.Empty;
            }
        }

        public double MaxZoomLevel
        {
            get { return MaximumZoom; }
        }

        public double MinZoomLevel
        {
            get { return MinimumZoom; }
        }
        public bool FractionalZoom
        {
            get { return SupportsFractionalZoom; }
        }

        #region IMapProjection Members
        public System.Drawing.Point GPSToPixel(ZoneFiveSoftware.Common.Data.GPS.IGPSLocation origin, double zoomLevel, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation gps)
        {
            return m_Proj.GPSToPixel(origin, zoomLevel, gps);
        }
        public ZoneFiveSoftware.Common.Data.GPS.IGPSLocation PixelToGPS(ZoneFiveSoftware.Common.Data.GPS.IGPSLocation origin, double zoomLevel, System.Drawing.Point pixel)
        {
            return m_Proj.PixelToGPS(origin, zoomLevel, pixel);
        }
        #endregion
#endif
    }
}