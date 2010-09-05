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

namespace Lofas.SportsTracks.Hitta_SE_MapProvider
{
    [Guid("23FBB14A-0949-4d42-BAA4-95C3AC3BC825")]
    public class Hitta_SE_MapProvider : ZoneFiveSoftware.Common.Visuals.Fitness.GPS.IMapProvider
    {
        #region IMapProvider Members

        string m_CacheDirectory;
        Dictionary<string, string> m_DownloadQueueItems;

        string m_View;
        string m_ImageExt;

        public Hitta_SE_MapProvider()
            : this("Sat")
        {
        }
        public Hitta_SE_MapProvider(string view)
        {
            m_View = view;
            m_CacheDirectory = Path.Combine(Plugin.m_Application.SystemPreferences.WebFilesFolder, "MapTiles" + Path.DirectorySeparatorChar + "Hitta_SE_" + view);
            //m_CacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ZoneFiveSoftware" + Path.DirectorySeparatorChar + "SportTracks" + Path.DirectorySeparatorChar + "2.0" + Path.DirectorySeparatorChar + "Web Files" + Path.DirectorySeparatorChar + "MapTiles" + Path.DirectorySeparatorChar + "Hitta_SE_" + view);
            m_DownloadQueueItems = new Dictionary<string, string>();
            if (m_View == "Sat")
                m_ImageExt = "jpg";
            else m_ImageExt = "gif";
        }

        public void ClearDownloadQueue()
        {
            m_DownloadQueueItems.Clear();
        }

        public int DownloadQueueSize
        {
            get { return m_DownloadQueueItems.Count; }
        }

        //double[] ZOOM_LEVELS = { 0.2, 0.8, 2, 4, 10, 25, 70, 200, 700, 3500 };
        //double[] scaleValues = { 756, 3024, 7559, 15118, 37795, 94488, 264567, 755906, 2645669, 13228346 };

        double[] ZOOM_LEVELS = { 0.2, 0.5, 2, 4, 10, 25, 70, 200, 700, 3500 };
        double[] scaleValues = { 756, 1890, 7559, 15118, 37795, 94488, 264567, 755906, 2645669, 13228346 };


        int m_DownloadQueue = 0;
        public int DrawMap(ZoneFiveSoftware.Common.Visuals.Fitness.GPS.IMapImageReadyListener listener, System.Drawing.Graphics graphics, System.Drawing.Rectangle drawRectangle, System.Drawing.Rectangle clipRectangle, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation center, double zoomLevel)
        {
            double x, y, origx, origy;

            if (center.LatitudeDegrees < 0 || center.LongitudeDegrees < 0 || center.LongitudeDegrees > 40)
                return 0;

            CFProjection.WGS84ToRT90(center.LatitudeDegrees, center.LongitudeDegrees, 0, out x, out y);

            int numQueued = DrawTiles(listener, graphics, ref drawRectangle, zoomLevel, x, y);



            return numQueued;
        }

        private double getMetersPerPixel(double zoomLevel, out double tileMeterPerPixel, out double hittascale)
        {
            double useZoomLevel = ZOOM_LEVELS[ZOOM_LEVELS.Length - 1];
            double useScale = scaleValues[scaleValues.Length - 1];
            double minDist = Double.MaxValue;
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
                useScale = scaleValues[zoomLevelInt+1];
            }

           
            if (level2 < ZOOM_LEVELS.Length && zoomLevelRest > 1e-6)
                useZoomLevel += (zoomLevelRest * (ZOOM_LEVELS[level2] - ZOOM_LEVELS[level1]));

            

            hittascale = useScale;

            
            return useZoomLevel;
        }

        private int DrawTiles(ZoneFiveSoftware.Common.Visuals.Fitness.GPS.IMapImageReadyListener listener, System.Drawing.Graphics graphics, ref System.Drawing.Rectangle drawRectangle, double zoomLevel, double x, double y)
        {
            double useScale;
            double tileMeterPerPixel;
            double metersPerPixel = getMetersPerPixel(zoomLevel, out tileMeterPerPixel, out useScale);

            double ulX = Math.Round(x - ((drawRectangle.Width / 2) * metersPerPixel),1);
            double ulY = Math.Round(y + ((drawRectangle.Height / 2) * metersPerPixel),1);
            double lrX = Math.Round(x + ((drawRectangle.Width / 2) * metersPerPixel),1);
            double lrY = Math.Round(y - ((drawRectangle.Height / 2) * metersPerPixel),1);

            double tileWidth = 256 * tileMeterPerPixel;

            double tileDrawWidth = (tileWidth / metersPerPixel);


            double rx = ulX;

            double startTileX = Math.Round(((int)(ulX / (tileMeterPerPixel * 256)) + 0) * tileMeterPerPixel * 256 - 0.5, 1);
            double startTileY = Math.Round(((int)(ulY / (tileMeterPerPixel * 256)) + 1) * tileMeterPerPixel * 256 - 0.5, 1);

            ////beräkna offset vid utritning..
            //double drawTileDX = (startTileX - ulX) / metersPerPixel;
            //double drawTileDY = (ulY - startTileY) / metersPerPixel;
#if nontile
            double offX = 0;
            double offY = 0;
#else
            int bottomLeftX = 451424;
            int bottomLeftY = 5651424;

            double resolution = ZOOM_LEVELS[Array.IndexOf(scaleValues, useScale)];
            int Tcolumn = (int)(((startTileX + tileMeterPerPixel * 128 - bottomLeftX)) / (256 * resolution));
            int Trow = (int)(((startTileY - tileMeterPerPixel * 128 - bottomLeftY)) / (256 * resolution));

            double colX = Tcolumn * 256 * resolution + bottomLeftX;
            double rowY = Trow * 256 * resolution + bottomLeftY;

            

            if (rowY < ulY)
            {
                Trow++;
                rowY = Trow * 256 * resolution + bottomLeftY;
            }

            //double x_Diff = startTileX - colX;
            //double y_Diff = startTileY - rowY;

            startTileX = colX;
            startTileY = rowY;
            double offX = -1 * tileDrawWidth; // x_Diff / tileMeterPerPixel;
            double offY = 0; // y_Diff / tileMeterPerPixel;

            if (metersPerPixel < 1)
            {
                if (Math.Abs(metersPerPixel-0.65) < 1e-6)
                {
                        offY = -tileDrawWidth;
                }
                else if (metersPerPixel == 0.455)
                {
                 
                        offY = -tileDrawWidth * 2;
                        //offX -= tileDrawWidth;
                }
                else if (metersPerPixel == 0.38)
                {

                    offY = -tileDrawWidth * 2;
                    //offX -= tileDrawWidth;
                }
                else if (Math.Abs(metersPerPixel-0.305)<1e-6)
                {
                    offY = -tileDrawWidth*3;
                    offX -= tileDrawWidth;
                }
                else if (metersPerPixel == 0.275)
                {
                    offY = -tileDrawWidth * 3;
                }
                else if (metersPerPixel == 0.2)
                {
                    offY = -tileDrawWidth * 2;
                    offX -= tileDrawWidth;
                }
                
            }

            int endTcolumn = (int)Math.Round(((lrX + tileMeterPerPixel * 128 - bottomLeftX)) / (256 * resolution), 0);
            lrX = endTcolumn * 256 * resolution + bottomLeftX + tileDrawWidth;
            //ulY = rowY + tileDrawWidth;

            int lowerTrow = (int)Math.Round(((lrY - tileMeterPerPixel * 128 - bottomLeftY)) / (256 * resolution), 0);
            lrY = lowerTrow * 256 * resolution + bottomLeftY;
            //lrX += tileDrawWidth;
            double drawTileDX = (startTileX - ulX) / metersPerPixel;
            double drawTileDY = (ulY - startTileY) / metersPerPixel;
            startTileY = rowY + tileDrawWidth;

#endif

            


            rx = startTileX;

            int col = 0;
            int numQueued = 0;

            while (rx <= lrX)
            {
                //int ry = (int)ulY;
                double ry = startTileY;
                int row = 0;
                while (ry >= lrY)
                {
                    int iRx = (int)Math.Round((rx + tileMeterPerPixel * 128) * 10);
                    int iRy = (int)Math.Round((ry - tileMeterPerPixel * 128) * 10);

                    if (isCached(iRx, iRy, useScale))
                    {
                        Image img = getImageFromCache(iRx, iRy, useScale);
                        float ix = (float)(drawRectangle.X + col * tileDrawWidth + drawTileDX + offX);
                        float iy = (float)(drawRectangle.Y + row * tileDrawWidth + drawTileDY + offY);
                        float iw = (float)(drawRectangle.X + (col+1) * tileDrawWidth + drawTileDX) - (float)(drawRectangle.X + (col) * tileDrawWidth + drawTileDX);
                        float ih = (float)(drawRectangle.Y + (row+1) * tileDrawWidth + drawTileDY) - (float)(drawRectangle.Y + row * tileDrawWidth + drawTileDY);
                        graphics.DrawImage(img,(int)Math.Floor(ix) , (int)Math.Floor(iy), (int)Math.Ceiling(iw), (int)Math.Ceiling(ih));
                        img.Dispose();

                    }
                    else
                    {
                        queueDownload(rx, ry,iRx,iRy, useScale, listener);
                        numQueued++;
                        m_DownloadQueue++;
                    }
                    ry -= 256 * tileMeterPerPixel;
                    row++;
                }
                //break;
                col++;
                rx += 256 * tileMeterPerPixel;
            }


            return numQueued;
        }

        private class MapImageObj
        {
            public double cx;
            public double cy;
            public double Scale;
        }
        WebClient wc = new WebClient();
        private void queueDownload(double cx, double cy, int iRx, int iRy, double useZoomLevel,ZoneFiveSoftware.Common.Visuals.Fitness.GPS.IMapImageReadyListener listener)
        {
            string item = iRx + "_" + iRy + "_" + useZoomLevel.ToString();
            if (!m_DownloadQueueItems.ContainsKey(item))
            {
                m_DownloadQueueItems.Add(item,"");
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object o)
                    {
                        
                        try
                        {
                            lock (wc)
                            {
                                if (m_DownloadQueueItems.ContainsKey(item))
                                {
                                    string downloadDir = Path.Combine(m_CacheDirectory, useZoomLevel.ToString());
                                    if (!Directory.Exists(downloadDir))
                                        Directory.CreateDirectory(downloadDir);

#if nontile
                                    string url = "http://map.hitta.se/SpatialAceWMS/RWCInterface.axd?view=Hitta.MainView&format=" + m_ImageExt + "&transparent=false&layers=MainView/" + m_View + "Layer&width=256&height=256&scale=" + useZoomLevel + "&x=" + cx.ToString(CultureInfo.InvariantCulture) + "&y=" + cy.ToString(CultureInfo.InvariantCulture);
#else
                                    int bottomLeftX = 451424;
                                    int bottomLeftY = 5651424;
                                    double resolution = ZOOM_LEVELS[Array.IndexOf(scaleValues, useZoomLevel)];
                                    //int column = (int)Math.Round(((cx  - bottomLeftX + ((256 / 2) *resolution))) / (256 * resolution), 0);
                                    //int row = (int)Math.Round(((cy - bottomLeftY + ((256 / 2) * resolution))) / (256 * resolution), 0);


                                    int column = (int)(((cx - bottomLeftX)) / (256 * resolution));
                                    int row = (int)(((cy - bottomLeftY)) / (256 * resolution));
                                    string geoCenterString = row + "/" + column;
                                    string url = "http://karta.hitta.se/mapstore/service/tile/";
                                    if (m_View == "Sat")
                                    {
                                        url += "1/";
                                    }
                                    else
                                    {
                                        url += "0/";
                                    }

                                    url += resolution.ToString(CultureInfo.InvariantCulture) + "/";
                                    url += geoCenterString;
#endif

                                    Image img = Image.FromStream(wc.OpenRead(url));
                                    //wc.DownloadFile(url, "cache\\sat_" + iRx + "_" + iRy + "_" + useZoomLevel + ".jpg");
                                    img.Save(Path.Combine(downloadDir, iRx + "_" + iRy + "." + m_ImageExt));
                                    img.Dispose();
                                }
                            }
                            MapImageObj obj = new MapImageObj();
                            obj.cx = cx;
                            obj.cy = cy;
                            obj.Scale = useZoomLevel;
                            listener.NotifyMapImageReady(obj);
                        }
                        catch (Exception ee)
                        {
                        }
                        finally
                        {
                        }
                        m_DownloadQueue--;
                        m_DownloadQueueItems.Remove(item);

                    }));
            }
        }

        private Image getImageFromCache(int iRx, int iRy, double useZoomLevel)
        {
            //GC.Collect();
                            string downloadDir = Path.Combine(m_CacheDirectory, useZoomLevel.ToString());
                string str = Path.Combine(downloadDir, iRx + "_" + iRy + "." + m_ImageExt);

            try
            {
                return Image.FromFile(str);
            }
            catch (Exception ee)
            {
                try
                {
                    File.Delete(str);
                }
                catch (Exception)
                {
                }

                return new Bitmap(256, 256);
            }
        }

        private bool isCached(int iRx, int iRy, double useZoomLevel)
        {
            string downloadDir = Path.Combine(m_CacheDirectory, useZoomLevel.ToString());
            string str = Path.Combine(downloadDir, iRx + "_" + iRy + "." + m_ImageExt);
            return File.Exists(str);
        }

        public bool FractionalZoom
        {
            get { return true; }
        }

        public Guid Id
        {
            get { return m_View == "Sat" ? this.GetType().GUID : new Guid("9BD470DD-3078-456f-8175-1A714D286B90"); }
        }

        public System.Drawing.Rectangle MapImagePixelRect(object mapImage, System.Drawing.Rectangle drawRectangle, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation center, double zoomLevel)
        {
            if (mapImage is MapImageObj)
            {
                //MapImageObj obj = mapImage as MapImageObj;

                //double cX = obj.cx;
                //double cY = obj.cy;

                //double x, y;
                //Triona.Util.CFProjection.WGS84ToRT90(center.LatitudeDegrees, center.LongitudeDegrees, 0, out x, out y);

                //double hittaScale;
                //double tileMeterPerPixel;

                //int bottomLeftX = 451424;
                //int bottomLeftY = 5651424;
                //double resolution = ZOOM_LEVELS[Array.IndexOf(scaleValues, obj.Scale)];
                //int column = (int)Math.Round(((cX - bottomLeftX)) / (256 * resolution), 0);
                //int row = (int)Math.Round(((cY - bottomLeftY)) / (256 * resolution), 0);
                //double colX = column * 256 * resolution + bottomLeftX;
                //double rowY = row * 256 * resolution + bottomLeftY;

                //double metersPerPixel = getMetersPerPixel(zoomLevel, out tileMeterPerPixel, out hittaScale);

                //double tileWidth = tileMeterPerPixel * 256;

                //double drawTileWith = tileWidth / metersPerPixel;

                //double dx = ((cX - 256*metersPerPixel - x)/metersPerPixel);
                //double dy = ((cY-256 - y) / metersPerPixel);


                //int centerPxX = drawRectangle.Width / 2;
                //int centerPxY = drawRectangle.Height / 2;
                //double minX = (centerPxX + dx);
                //double maxY = (centerPxY - dy );

                //Rectangle rect = Rectangle.FromLTRB(
                //    (int)(minX-1), 
                //    (int)Math.Floor((float)maxY)-1, 
                //    (int)Math.Ceiling(minX + drawTileWith)+1, 
                //    (int)Math.Ceiling(maxY + drawTileWith)+1);

                //rect = Rectangle.FromLTRB(
                //    (int)(minX - 1),
                //    0,
                //    (int)Math.Ceiling(minX + drawTileWith) + 1,
                //    drawRectangle.Height);

                return drawRectangle;
            }
            else
            {
                return Rectangle.Empty;
            }
        }

        public double MaxZoomLevel
        {
            get { return 9; }
        }

        public double MinZoomLevel
        {
            get { return 0.35; }
        }

        public string Name
        {
            get { return m_View == "Sat" ? "Hitta.se Flygfoto" : "Hitta.se Karta"; }
        }

        public void Refresh(System.Drawing.Rectangle drawRectangle, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation center, double zoomLevel)
        {
        }

        #endregion

        #region IMapProjection Members

        public System.Drawing.Point GPSToPixel(ZoneFiveSoftware.Common.Data.GPS.IGPSLocation origin, double zoomLevel, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation gps)
        {
            double hittascale;
            double tileMeterPerPixel;
            double metersPerPixel = getMetersPerPixel(zoomLevel,out tileMeterPerPixel, out hittascale);
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

            double x = origx + pixel.X*metersPerPixel;
            double y = origy - pixel.Y*metersPerPixel;


            CFProjection.RT90ToWGS84(x, y, out lat, out lon);
            return new ZoneFiveSoftware.Common.Data.GPS.GPSLocation((float)lon, (float)lat);
        }

        #endregion
    }
}
