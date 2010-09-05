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
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Net;
using System.Globalization;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Lofas.SportsTracks.Hitta_SE_MapProvider
{
    [Guid("0BE4F711-316A-4d8a-B259-6B08BDD8438F")]
    public class Eniro_SE_MapProvider : ZoneFiveSoftware.Common.Visuals.Fitness.GPS.IMapProvider
    {
        #region IMapProvider Members

        string m_CacheDirectory;
        Dictionary<string, string> m_DownloadQueueItems;

        string m_View;
        string m_ImageExt;
        string m_ViewCountry = "SE_sv";
        string infoUrl = "http://kartor.eniro.se/mapapi/servlets/dwr-invoker/call/plaincall/TilesService.initializeEniMap.dwr";

        public Eniro_SE_MapProvider()
            : this("Sat")
        {
        }
        public Eniro_SE_MapProvider(string view)
            : this(view,"SE_sv")
        {
        }

        public Eniro_SE_MapProvider(string view, string country)
        {
            m_View = view;
            m_ViewCountry = country;

            if (country.StartsWith("FI"))
            {
                infoUrl = "http://kartat.eniro.fi/mapapi/servlets/dwr-invoker/call/plaincall/TilesService.getEniMapInfo.dwr";
            }


            m_CacheDirectory = Path.Combine(Plugin.m_Application.SystemPreferences.WebFilesFolder, "MapTiles" + Path.DirectorySeparatorChar + "Eniro_" + m_ViewCountry.Substring(0, 2) + "_" + view);
            //m_CacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ZoneFiveSoftware" + Path.DirectorySeparatorChar + "SportTracks" + Path.DirectorySeparatorChar + "2.0" + Path.DirectorySeparatorChar + "Web Files" + Path.DirectorySeparatorChar + "MapTiles" + Path.DirectorySeparatorChar + "Eniro_" + m_ViewCountry.Substring(0, 2) + "_" + view);
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
        double[] scaleValues = { 1000, 2000, 4000, 8200, 16000, 57000, 240000, 1000000, 4000000, 1.6384E7 };
        double[] old_scaleValues = { 1000, 2000, 4000, 8200, 20000, 57000, 240000, 500000, 4000000, 20800000 };

        int m_DownloadQueue = 0;
        public int DrawMap(ZoneFiveSoftware.Common.Visuals.Fitness.GPS.IMapImageReadyListener listener, System.Drawing.Graphics graphics, System.Drawing.Rectangle drawRectangle, System.Drawing.Rectangle clipRectangle, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation center, double zoomLevel)
        {
            //return 0;
            /*double x, y, origx, origy;
            GeoToUTM(center.LatitudeDegrees, center.LongitudeDegrees, out x, out y);

            int numQueued = DrawTiles(listener, graphics, ref drawRectangle, zoomLevel, x, y,center);

            */
            int numQueued = 0;
            try
            {
                double useScale;
                double tileMeterPerPixel;
                double metersPerPixel = getMetersPerPixel(zoomLevel, out tileMeterPerPixel, out useScale);


                double refTileULX, refTileULY, tileWMetersPerPixel, tileHMetersPerPixel, lengthDegreesX, lengthDegreesY, centerLat, centerLon;
                int refTileXOffset, refTileYOffset;
                getScaleInfo(zoomLevel, center, out refTileULX, out refTileULY, out tileWMetersPerPixel, out tileHMetersPerPixel, out refTileXOffset, out refTileYOffset, out lengthDegreesX, out lengthDegreesY, out centerLat, out centerLon);

                double dy = (center.LatitudeDegrees - centerLon) / lengthDegreesY;
                double dx = (center.LongitudeDegrees - centerLat) / lengthDegreesX;

                int numTilesDX = (int)Math.Round(dx);
                int numTilesDY = (int)Math.Round(dy);
                int startTileX = refTileXOffset + numTilesDX;
                int startTileY = refTileYOffset + numTilesDY;

                int tileDrawDX = (int)(-1 * (dx - numTilesDX) * 256) - 128;
                int tileDrawDY = (int)(1 * (dy - numTilesDY) * 256) - 128;

                int numTilesX = (int)Math.Ceiling(drawRectangle.Width / 256.0);
                int numTilesY = (int)Math.Ceiling(drawRectangle.Height / 256.0);

                Image img = null;

                for (int tileX = startTileX - numTilesX / 2; tileX <= startTileX + numTilesX / 2; tileX++)
                {
                    for (int tileY = startTileY + numTilesY / 2; tileY >= startTileY - numTilesY / 2; tileY--)
                    {

                        int col = tileX - startTileX;
                        int row = startTileY - tileY;
                        if (isCached(tileX, tileY, useScale))
                        {
                            img = getImageFromCache(tileX, tileY, useScale);
                            graphics.DrawImage(img, (int)Math.Floor((double)(drawRectangle.Width / 2.0 + tileDrawDX + col * 256)), (int)Math.Floor((double)(drawRectangle.Height / 2.0 + tileDrawDY + row * 256)));
                            //graphics.DrawRectangle(Pens.Red, (int)Math.Floor(drawRectangle.X + ddx + col * tileDrawWidth), (int)Math.Floor(drawRectangle.Y + ddy + row * tileDrawHeight), (int)tileDrawWidth, (int)tileDrawHeight);
                            img.Dispose();
                        }
                        else
                        {
                            //string url = "http://maps1.eniro.com/servlets/TilesDataServlet?id=SE_sv_" +
                            //    (m_View == "Sat" ? "aerial" : "standard") + "_" + Convert.ToInt32(scaleValues.Length - zoomLevel) + "_" + Convert.ToInt32(useScale) + ".0_58.0_256_128_" + tileX + "_" + tileY;
                            //img = Image.FromStream(wc.OpenRead(url));
                            //string downloadDir = Path.Combine(m_CacheDirectory, useScale.ToString());
                            //if (!Directory.Exists(downloadDir))
                            //    Directory.CreateDirectory(downloadDir);

                            //img.Save(Path.Combine(downloadDir, tileX + "_" + tileY + "." + m_ImageExt));
                            int cX = (int)Math.Floor((double)(tileDrawDX + col * 256)) + 128;
                            int cY = (int)Math.Floor((double)(tileDrawDY + row * 256)) + 128;
                            double latC = center.LongitudeDegrees + cX / 256.0 * lengthDegreesX;
                            double lonC = center.LatitudeDegrees + cY / 256.0 * lengthDegreesY;
                            queueDownload(latC, lonC, tileX, tileY, useScale, listener);
                            numQueued++;
                        }

                    }

                }
            }
            catch (Exception ee)
            {
                graphics.DrawString("Error in Eniro Mapprovider:" + ee.Message, new Font("Arial", 12f), Brushes.Black, new PointF(10, 10));
            }

            return numQueued;
        }
                        

        private double getMetersPerPixel(double zoomLevel, out double tileMeterPerPixel, out double eniroscale)
        {

            //double useZoomLevel = ZOOM_LEVELS[ZOOM_LEVELS.Length - 1];
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
            //useZoomLevel = ZOOM_LEVELS[zoomLevelInt];
            useScale = scaleValues[zoomLevelInt];
            double useZoomLevel = useScale * 0.0254/128; // 0.000265 == meter/dpi
            tileMeterPerPixel = useZoomLevel;
            
            if (zoomLevelRest > 0.5)
            {
                //tileMeterPerPixel = ZOOM_LEVELS[zoomLevelInt + 1];
                useScale = scaleValues[zoomLevelInt+1];
                tileMeterPerPixel = useScale * 0.000265;
            }


            if (level2 < scaleValues.Length - 1 && zoomLevelRest > 1e-6)
                useZoomLevel += (zoomLevelRest * (scaleValues[level2] * 0.0254 / 128 - scaleValues[level1] * 0.0254 / 128));

            

            eniroscale = useScale;

            
            return useZoomLevel;
        }
#if _OLD
        private int DrawTiles(ZoneFiveSoftware.Common.Visuals.Fitness.GPS.IMapImageReadyListener listener, System.Drawing.Graphics graphics, ref System.Drawing.Rectangle drawRectangle, double zoomLevel, double x, double y, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation locationWGS)
        {

            double useScale;
            double tileMeterPerPixel;
            double metersPerPixel = getMetersPerPixel(zoomLevel, out tileMeterPerPixel, out useScale);


            double refTileULX, refTileULY, tileWMetersPerPixel, tileHMetersPerPixel, lengthDegreesX, lengthDegreesY, centerLat, centerLon;
            int refTileXOffset, refTileYOffset;
            getScaleInfo(zoomLevel, locationWGS, out refTileULX, out refTileULY, out tileWMetersPerPixel, out tileHMetersPerPixel, out refTileXOffset, out refTileYOffset, out lengthDegreesX, out lengthDegreesY, out centerLat, out centerLon);
            tileWMetersPerPixel = tileHMetersPerPixel = metersPerPixel;


            double ulX = Math.Round(x - ((drawRectangle.Width / 2) * tileWMetersPerPixel),1);
            double ulY = Math.Round(y + ((drawRectangle.Height / 2) * tileHMetersPerPixel),1);
            double lrX = Math.Round(x + ((drawRectangle.Width / 2) * tileWMetersPerPixel),1);
            double lrY = Math.Round(y - ((drawRectangle.Height / 2) * tileHMetersPerPixel),1);

            double tileWidth = 256 * tileWMetersPerPixel;

            double tileDrawWidth = (int)(tileWidth / tileWMetersPerPixel);

          
            
            double tileDrawHeight = 256;// (int)(tileWidth / hMeterPerPixel);

            double diffX = (ulX - refTileULX) / tileWMetersPerPixel / 256;
            double diffY = (ulY - refTileULY) / tileHMetersPerPixel / 256;
            int numXTilesDiff = (int)Math.Round(diffX + Math.Sign(diffX) * 0.5);
            int numYTilesDiff = (int)Math.Round(diffY - Math.Sign(diffY)*0.5);
            int startTileX_IDX = refTileXOffset + numXTilesDiff;
            int startTileY_IDX = refTileYOffset + numYTilesDiff ;

            double tileUlX_M = refTileULX + (numXTilesDiff) * 256 * tileWMetersPerPixel;
            double tileUlY_M = refTileULY + numYTilesDiff * 256 * tileHMetersPerPixel;

            double ddx = (tileUlX_M - ulX) / tileWMetersPerPixel;
            double ddy = (ulY - tileUlY_M) / tileHMetersPerPixel;

            //double t = 256 * wMeterPerPixel + tileUlX_M;
            //double t2 = lengthDegreeX * 256;

           


            double rx = ulX;
            int numQueued = 0;
            
            int col = 0;

            for (int tileX = startTileX_IDX; tileX < startTileX_IDX+4; tileX++)
            {
                int row = 0; 
                for (int tileY = startTileY_IDX; tileY > startTileY_IDX-4; tileY--)
                {
                    Image img = null;
                    if (isCached(tileX, tileY, useScale))
                    {
                        img = getImageFromCache(tileX, tileY, useScale);
                    }
                    else
                    {
                        string ident = "standard";
                        switch (m_View)
                        {
                            case "Sat":
                                ident = "aerial";
                                break;
                            case "Nat":
                                ident = "nautical";
                                break;
                        }

                        string url = "http://maps1.eniro.com/servlets/TilesDataServlet?id=SE_sv_"+
                            ident +"_" + Convert.ToInt32(scaleValues.Length - zoomLevel) + "_" + Convert.ToInt32(useScale) + ".0_58.0_256_128_" + tileX + "_" + tileY;
                        img = Image.FromStream(wc.OpenRead(url));
                        string downloadDir = Path.Combine(m_CacheDirectory, useScale.ToString());
                        if (!Directory.Exists(downloadDir))
                            Directory.CreateDirectory(downloadDir);

                        img.Save(Path.Combine(downloadDir, tileX + "_" + tileY+ "." + m_ImageExt));
                    }
                    graphics.DrawImage(img, (int)Math.Floor(drawRectangle.X + ddx + col * tileDrawWidth), (int)Math.Floor(drawRectangle.Y + ddy + row * tileDrawHeight), (int)tileDrawWidth, (int)tileDrawHeight);
                    graphics.DrawRectangle(Pens.Red, (int)Math.Floor(drawRectangle.X + ddx + col * tileDrawWidth), (int)Math.Floor(drawRectangle.Y + ddy + row * tileDrawHeight), (int)tileDrawWidth, (int)tileDrawHeight);
                    img.Dispose();
                    row++;
                }
                col++;
            }

            
            
            /*double startTileX = Math.Round(((int)(ulX / (tileMeterPerPixel * 256)) + 0) * tileMeterPerPixel * 256 - 0.5, 1);
            double startTileY = Math.Round(((int)(ulY / (tileMeterPerPixel * 256)) + 1) * tileMeterPerPixel * 256 - 0.5, 1);

            int drawTileDX = (int)Math.Round((startTileX - ulX) / metersPerPixel);
            int drawTileDY = (int)Math.Round((ulY - startTileY) / metersPerPixel);

            rx = startTileX;

            int col = 0;
            
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
                        graphics.DrawImage(img, (float)(drawRectangle.X + col * tileDrawWidth + drawTileDX), (float)(drawRectangle.Y + row * tileDrawWidth + drawTileDY), (float)tileDrawWidth, (float)tileDrawWidth);
                        img.Dispose();

                    }
                    else
                    {
                        queueDownload(iRx/10.0, iRy/10.0,iRx,iRy, useScale, listener);
                        numQueued++;
                        m_DownloadQueue++;
                    }
                    ry -= 256 * tileMeterPerPixel;
                    row++;
                }
                col++;
                rx += 256 * tileMeterPerPixel;
            }*/


            return numQueued;
        }
    
#endif
        private class MapImageObj
        {
            public double cx;
            public double cy;
            public double Scale;
        }
        WebClient wc = new WebClient();
        Random rnd = new Random();
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

                                    int zoomLevelIdx = Array.IndexOf(scaleValues, useZoomLevel);
                                    int src = rnd.Next(0,4);

                                    string ident = "standard";
                                    switch (m_View)
                                    {
                                        case "Sat":
                                            ident = "aerial";
                                            break;
                                        case "Nat":
                                            ident = "nautical";
                                            break;
                                    }

                                    string url = "http://maps" + src + ".eniro.com/servlets/TilesDataServlet?id=" + m_ViewCountry + "_" +
                                        ident + "_" + Convert.ToInt32(scaleValues.Length - zoomLevelIdx) + "_" + Convert.ToInt32(useZoomLevel) + ".0_58.0_256_128_" + iRx + "_" + iRy;
                                    wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows; U; Windows NT 5.1; sv-SE; rv:1.9.0.4) Gecko/2008102920 Firefox/3.0.4");
                                    Image img = Image.FromStream(wc.OpenRead(url));

                                    img.Save(Path.Combine(downloadDir, iRx + "_" + iRy+ "." + m_ImageExt));
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
            get { return false; }
        }

        public Guid Id
        {
            get { 
                
                if (m_View == "Sat")
                    return this.GetType().GUID ;
                else if (m_View == "Nat")
                {
                    return new Guid("102C58BC-B78B-4f8c-95D2-4100773EE50F");
                }
                else
                {
                    if (m_ViewCountry == "FI_fi")
                    {
                        return new Guid("CE766975-7457-4036-BF11-EE4D5104E605");
                    }
                    else
                    {
                        return new Guid("FE0739E6-643D-4a0a-95B3-74CC1073D36E");
                    }
                }
            }
        }

        public System.Drawing.Rectangle MapImagePixelRect(object mapImage, System.Drawing.Rectangle drawRectangle, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation center, double zoomLevel)
        {
            if (mapImage is MapImageObj)
            {
                MapImageObj obj = mapImage as MapImageObj;

                double cX = obj.cx;
                double cY = obj.cy;

                double useScale;
                double tileMeterPerPixel;
                double metersPerPixel = getMetersPerPixel(zoomLevel, out tileMeterPerPixel, out useScale);


                double refTileULX, refTileULY, tileWMetersPerPixel, tileHMetersPerPixel, lengthDegreesX, lengthDegreesY, centerLat, centerLon;
                int refTileXOffset, refTileYOffset;
                getScaleInfo(zoomLevel, center, out refTileULX, out refTileULY, out tileWMetersPerPixel, out tileHMetersPerPixel, out refTileXOffset, out refTileYOffset, out lengthDegreesX, out lengthDegreesY, out centerLat, out centerLon);

                double dy = -1*(center.LatitudeDegrees - cY) / lengthDegreesY * 256;
                double dx = -1*(center.LongitudeDegrees - cX) / lengthDegreesX * 256;

                /*int numTilesDX = (int)Math.Round(dx);
                int numTilesDY = (int)Math.Round(dy);

                int startTileX = refTileXOffset + numTilesDX;
                int startTileY = refTileYOffset + numTilesDY;

                int tileDrawDX = (int)(-1 * (dx - numTilesDX) * 256) ;
                int tileDrawDY = (int)(1 * (dy - numTilesDY) * 256) ;*/

                return Rectangle.FromLTRB((int)(drawRectangle.Width / 2.0 + dx - 128), (int)(drawRectangle.Height / 2.0 + dy - 128),
                    (int)(drawRectangle.Width / 2.0 + dx + 128), (int)(drawRectangle.Height / 2.0 + dy + 128));
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
            get { return 0; }
        }

        public string Name
        {
            get
            {
                string name = "";
                switch (m_View)
                {
                    case "Sat":
                        name = "Eniro.se Flygfoto";
                        break;
                    case "Nat":
                         name = "Eniro.se Sjökort";
                         break;
                    default:
                        name = "Eniro.se Karta";
                        break;
                }

                if (m_ViewCountry == "FI_fi")
                    name = "Eniro.fi Kartat";

                return name;
            }
        }

        public void Refresh(System.Drawing.Rectangle drawRectangle, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation center, double zoomLevel)
        {
        }

        #endregion

        #region IMapProjection Members
        private class CacheTileInfo
        {
            public double refTileULX;
            public double refTileULY;
            public double tileWMetersPerPixel;
            public double tileHMetersPerPixel;
            public int refTileOffX;
            public int refTileOffY;
            public double lengthDegreesX;
            public double lengthDegreesY;
            public double centerLat;
            public double centerLon;

        }

        public static void GeoToUTM(double lat, double lon, out double x, out double y)
        {
            double fi, la, la0, n, e, m, b, e2;
            double A = 6378137;
            double F = 1 / 298.257223563;
            //4440var F=this.F;
            //4441var A=this.A;
            la0 = (33 - 30) * 6 - 3;
            la = lon - la0;
            la = la * Math.PI / 180;
            fi = lat * Math.PI / 180;
            e2 = F * (2 - F);
            n = A / Math.Sqrt(1 - e2 * Math.Pow(Math.Sin(fi), 2));
            e = Math.Sqrt(e2 * Math.Pow(Math.Cos(fi), 2) / (1 - e2));
            m = n / (1 + Math.Pow(e, 2));
            b = (1 - F / 2 + Math.Pow(F, 2) / 16 + Math.Pow(F, 3) / 32) * fi;
            b = b - (0.75 * F - 3 * Math.Pow(F, 3) / 128) * Math.Sin(2 * fi);
            b = b + (15 * Math.Pow(F, 2) / 64 + 15 * Math.Pow(F, 3) / 128) * Math.Sin(4 * fi);
            b = b - 35 * Math.Pow(F, 3) * Math.Sin(6 * fi) / 384;
            b = b * A;
            double _48d = b + Math.Pow(la, 2) * n * Math.Sin(fi) * Math.Cos(fi) / 2;
            _48d = _48d + Math.Pow(la, 4) * n * Math.Sin(fi) * Math.Pow(Math.Cos(fi), 3) * (5 - Math.Pow(Math.Tan(fi), 2) + 9 * Math.Pow(e, 2) + 4 * Math.Pow(e, 4)) / 24;
            _48d = _48d + Math.Pow(la, 6) * n * Math.Sin(fi) * Math.Pow(Math.Cos(fi), 5) * (61 - 58 * Math.Pow(Math.Tan(fi), 2) + Math.Pow(Math.Tan(fi), 4)) / 720;
            _48d = _48d * 0.9996;
            double _48e = la * n * Math.Cos(fi) + Math.Pow(la, 3) * n * Math.Pow(Math.Cos(fi), 3) * (1 - Math.Pow(Math.Tan(fi), 2) + Math.Pow(e, 2)) / 6;
            _48e = _48e + Math.Pow(la, 5) * n * Math.Pow(Math.Cos(fi), 5) * (5 - 18 * Math.Pow(Math.Tan(fi), 2) + Math.Pow(Math.Tan(fi), 4)) / 120;
            _48e = _48e * 0.9996 + 500000;
            x = _48e;
            y = _48d;
        }


        Dictionary<int, CacheTileInfo> m_TileInfoCache = new Dictionary<int, CacheTileInfo>();
        public System.Drawing.Point GPSToPixel(ZoneFiveSoftware.Common.Data.GPS.IGPSLocation origin, double zoomLevel, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation gps)
        {
            int dx, dy;
            double lengthDegreesX, lengthDegreesY;
            try
            {
                double refTileULX, refTileULY, tileWMetersPerPixel, tileHMetersPerPixel, centerLat, centerLon;
                int refTileXOffset, refTileYOffset;
                getScaleInfo(zoomLevel, gps, out refTileULX, out refTileULY, out tileWMetersPerPixel, out tileHMetersPerPixel, out refTileXOffset, out refTileYOffset, out lengthDegreesX, out lengthDegreesY, out centerLat, out centerLon);

                //double hittascale;
                //double tileMeterPerPixel;
                //double metersPerPixel = getMetersPerPixel(zoomLevel,out tileMeterPerPixel, out hittascale);
                ///double x, y, origx, origy;
                //Triona.Util.CFProjection.WGS84ToRT90(gps.LatitudeDegrees, gps.LongitudeDegrees, 0, out x, out y);
                //Triona.Util.CFProjection.WGS84ToRT90(origin.LatitudeDegrees, origin.LongitudeDegrees, 0, out origx, out origy);
                // GeoToUTM(gps.LatitudeDegrees, gps.LongitudeDegrees, out x, out y);
                //GeoToUTM(origin.LatitudeDegrees, origin.LongitudeDegrees, out origx, out origy);

                dx = (int)Math.Round((gps.LongitudeDegrees - origin.LongitudeDegrees) / lengthDegreesX * 256);
                dy = (int)Math.Round((origin.LatitudeDegrees - gps.LatitudeDegrees) / lengthDegreesY * 256);
            }
            catch (Exception ee)
            {
                throw new ApplicationException("Eniro-Server changed!",ee);
            }

            //int dx = (int)Math.Round((x - origx) / tileWMetersPerPixel);
            //int dy = (int)Math.Round((origy - y) / tileHMetersPerPixel);

            return new System.Drawing.Point(dx, dy);
        }

        private void getScaleInfo(double zoomLevel, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation gps, out double refTileULX, out double refTileULY, out double tileWMetersPerPixel, out double tileHMetersPerPixel, out int refTileXOffset,out int refTileYOffset, out double lengthDegreesX, out double lengthDegreesY, out double centerLat, out double centerLon)
        {
            
            double useScale, tileMeterPerPixel;
            getMetersPerPixel(zoomLevel, out tileMeterPerPixel, out useScale);
            if (m_TileInfoCache.ContainsKey((int)useScale))
            {
                CacheTileInfo cti = m_TileInfoCache[(int)useScale];
                refTileULX = cti.refTileULX;
                refTileULY = cti.refTileULY;
                refTileXOffset = cti.refTileOffX;
                refTileYOffset = cti.refTileOffY;
                tileWMetersPerPixel = cti.tileWMetersPerPixel;
                tileHMetersPerPixel = cti.tileHMetersPerPixel;
                lengthDegreesX = cti.lengthDegreesX;
                lengthDegreesY = cti.lengthDegreesY;
                centerLon = cti.centerLon;
                centerLat = cti.centerLat;

            }
                else
            {
                string infoFile = Path.Combine(m_CacheDirectory, useScale + "\\tileInfo.txt");
                if (!File.Exists(infoFile))
                {
                    double lat = gps.LongitudeDegrees;
                    double lon = gps.LatitudeDegrees;
                    //Fetch Info
                    //               "http://kartor.eniro.se/mapapi/servlets/dwr-invoker/exec/TilesService.getInfo.dwr"
                    //                http://kartor.eniro.se/mapapi/servlets/dwr-invoker/call/plaincall/TilesService.initializeEniMap.dwr?callCount=1&page=%2F&httpSessionId=&scriptSessionId=A5D21E9D83F2E4EA745B637926180464446&c0-scriptName=TilesService&c0-methodName=initializeEniMap&c0-id=0&c0-param0=string%3ASE&c0-param1=string%3A&c0-param2=string%3Astandard&c0-e1=number%3A0&c0-e2=number%3A0&c0-e3=string%3Awgs84&c0-param3=Object_Wgs84GeoCoord%3A%7Bx%3Areference%3Ac0-e1%2C%20y%3Areference%3Ac0-e2%2C%20crs%3Areference%3Ac0-e3%7D&c0-param4=number%3A1&c0-param5=string%3A&c0-param6=null%3Anull&c0-param7=number%3A0&c0-param8=number%3A0&batchId=1

                    string ident = "standard";
                    switch (m_View)
                    {
                        case "Sat":
                            ident = "aerial";
                            break;
                        case "Nat":
                            ident = "nautical";
                            break;
                    }

                    string reqUrl = infoUrl + "?callCount=1&page=%2F&httpSessionId=&scriptSessionId=A5D21E9D83F2E4EA745B637926180464446&c0-scriptName=TilesService&c0-methodName=initializeEniMap&c0-id=0&c0-param0=string%3ASE&c0-param1=string%3A&c0-param2=string%3A" + ident + "&c0-e1=number%3A" + lat.ToString(CultureInfo.InvariantCulture) + "&c0-e2=number%3A" + lon.ToString(CultureInfo.InvariantCulture) + "&c0-e3=string%3Awgs84&c0-param3=Object_Wgs84GeoCoord%3A%7Bx%3Areference%3Ac0-e1%2C%20y%3Areference%3Ac0-e2%2C%20crs%3Areference%3Ac0-e3%7D&c0-param4=number%3A" + (scaleValues.Length - Array.IndexOf(scaleValues, useScale)) + "&c0-param5=string%3A&c0-param6=null%3Anull&c0-param7=number%3A0&c0-param8=number%3A0&batchId=0";
                    WebRequest wq = HttpWebRequest.Create(reqUrl);
                    //wq.Method = "POST";
                    //string iurl = "callCount=1&c0-scriptName=TilesService&c0-methodName=getInfo&c0-id=5840_1217142424863&c0-param0=string:SE&c0-param1=string:sv&c0-param2=string:" + 
                    //    (m_View == "Sat" ? "aerial" : "standard") +"&c0-e1=number:" + lat.ToString(CultureInfo.InvariantCulture) + "&c0-e2=number:" + lon.ToString(CultureInfo.InvariantCulture) + "&c0-e3=string:wgs84&c0-param3=Object:{x:reference:c0-e1, y:reference:c0-e2, crs:reference:c0-e3, constructor:reference:c0-e4, toYMinuteDegrees:reference:c0-e5, toXMinuteDegrees:reference:c0-e6, convertValue:reference:c0-e7, toString:reference:c0-e8}&c0-param4=number:"
                    //    + (scaleValues.Length - Array.IndexOf(scaleValues, useScale))
                    //    + "&c0-param5=number:90&c0-param6=null:null&c0-param7=number:0&c0-param8=number:0&xml=true";
                    //byte[] data = System.Text.Encoding.ASCII.GetBytes(iurl);
                    //wq.ContentLength = data.Length;
                    //Stream stream = wq.GetRequestStream();
                    //stream.Write(data, 0, data.Length);
                    //stream.Flush();
                    WebResponse ws = wq.GetResponse();
                    StreamReader sr = new StreamReader(ws.GetResponseStream());
                    string resp = sr.ReadToEnd();
                    resp = resp.Replace("DWREngine._handleResponse('5840_1217142424863', s0);", "");
                    resp = resp.Replace("dwr.engine._remoteHandleCallback('0','0',{defaultCenter:s0,enimapTileHosts:s1,mapHost:\"maps.eniro.com\",obliqueThresholdZoomLevel:5,supportedLanguages:null,tileInfo:s2});", "");
                    resp = resp.Substring(0,resp.IndexOf("dwr.engine._remoteHandleCallback"));
                    //Utility.JScriptEvaluator eval = new Utility.JScriptEvaluator(resp);

                    CodeDomProvider compiler = new Microsoft.JScript.JScriptCodeProvider();
                    CompilerParameters parameters = new CompilerParameters();
                    parameters.GenerateInMemory = true;

                    string src = @"package Lofas
            {
               class LofasEval
               {
                    var myObj;
                    public function LoadParams()
{
" + 
  resp +
@"
myObj = s3;
}
                    
                    public function get_lengthDegreesX()
                    {
                        return myObj.lengthDegreesX
                    }   
                    public function get_lengthDegreesY()
                    {
                        return myObj.lengthDegreesY
                    }       
                    public function get_DPI()
                    {
                        return myObj.dpi
                    } 

                    public function get_UpperLeftX()
                    {
                        return myObj.upperLeft.x;
                    }
                    public function get_UpperLeftY()
                    {
                        return myObj.upperLeft.y;
                    }
                public function get_LowerRightX()
                    {
                        return myObj.lowerRight.x;
                    }
                    public function get_LowerRightY()
                    {
                        return myObj.lowerRight.y;
                    }  
                    public function get_CenterX()
                    {
                        return myObj.center.x;
                    }  
                    public function get_CenterY()
                    {
                        return myObj.center.y;
                    }   
                    public function get_OffsetX()
                    {
                        return myObj.offsetX;
                    }   
                    public function get_OffsetY()
                    {
                        return myObj.offsetY;
                    }   
                }
                }
                ";    

                    double lengthDegreeX,lengthDegreeY,tileUlX,tileUlY,tileLRX,tileLRY,tileCX,tileCY;
                    int dpi,startTileX_IDX,startTileY_IDX;
                    try
                    {
                        CompilerResults res = compiler.CompileAssemblyFromSource(parameters, src);
                        Assembly assm = res.CompiledAssembly;
                        Type mEvType = assm.GetType("Lofas.LofasEval");
                        object evaluator = Activator.CreateInstance(mEvType);

                        mEvType.GetMethod("LoadParams").Invoke(evaluator, null);

                        lengthDegreeX = Convert.ToDouble(mEvType.GetMethod("get_lengthDegreesX").Invoke(evaluator, null)); //Convert.ToDouble(op["lengthDegreesX"]);
                        lengthDegreeY = Convert.ToDouble(mEvType.GetMethod("get_lengthDegreesY").Invoke(evaluator, null));//Convert.ToDouble(op["lengthDegreesY"]);
                        dpi = Convert.ToInt32(mEvType.GetMethod("get_DPI").Invoke(evaluator, null));//Convert.ToInt32(op["dpi"]);


                        tileUlX = Convert.ToDouble(mEvType.GetMethod("get_UpperLeftX").Invoke(evaluator, null));//Convert.ToDouble(upperLeft["x"]);
                        tileUlY = Convert.ToDouble(mEvType.GetMethod("get_UpperLeftY").Invoke(evaluator, null));//Convert.ToDouble(upperLeft["y"]);


                        tileLRX = Convert.ToDouble(mEvType.GetMethod("get_LowerRightX").Invoke(evaluator, null));//Convert.ToDouble(lowerRight["x"]);
                        tileLRY = Convert.ToDouble(mEvType.GetMethod("get_LowerRightY").Invoke(evaluator, null));//Convert.ToDouble(lowerRight["y"]);

                        tileCX = Convert.ToDouble(mEvType.GetMethod("get_CenterX").Invoke(evaluator, null));//Convert.ToDouble(center["x"]);
                        tileCY = Convert.ToDouble(mEvType.GetMethod("get_CenterY").Invoke(evaluator, null));//Convert.ToDouble(center["y"]);

                        startTileX_IDX = Convert.ToInt32(mEvType.GetMethod("get_OffsetX").Invoke(evaluator, null));//Convert.ToInt32(op["offsetX"]);
                        startTileY_IDX = Convert.ToInt32(mEvType.GetMethod("get_OffsetY").Invoke(evaluator, null));//Convert.ToInt32(op["offsetY"]);
                    }
                    catch (Exception ee)
                    {
                        //Failed to du Javascript Lookup, try to fall back on core, lookup instead..
                        Regex rex = new Regex("s3\\.dpi=(?<dpi>\\d+);", RegexOptions.Singleline);
                        Match m = rex.Match(resp);
                        dpi = int.Parse(m.Groups["dpi"].Value);
                        rex = new Regex("s3\\.lengthDegreesX=(?<valx>.*?);s3\\.lengthDegreesY=(?<valy>.*?);", RegexOptions.Singleline);
                        m = rex.Match(resp);
                        lengthDegreeX = double.Parse(m.Groups["valx"].Value, CultureInfo.InvariantCulture);
                        lengthDegreeY = double.Parse(m.Groups["valy"].Value, CultureInfo.InvariantCulture);

                        rex = new Regex("s3\\.offsetX=(?<valx>.*?);s3\\.offsetY=(?<valy>.*?);", RegexOptions.Singleline);
                        m = rex.Match(resp);
                        startTileX_IDX = int.Parse(m.Groups["valx"].Value, CultureInfo.InvariantCulture);
                        startTileY_IDX = int.Parse(m.Groups["valy"].Value, CultureInfo.InvariantCulture);

                        rex = new Regex("s3\\.lowerRight=(?<ident>.*?);", RegexOptions.Singleline);
                        m = rex.Match(resp);
                        string iident = m.Groups["ident"].Value;

                        rex = new Regex(iident+"\\.x=(?<valx>.*?);" + iident + "\\.y=(?<valy>.*?);", RegexOptions.Singleline);
                        m = rex.Match(resp);
                        tileLRX = double.Parse(m.Groups["valx"].Value, CultureInfo.InvariantCulture);
                        tileLRY = double.Parse(m.Groups["valy"].Value, CultureInfo.InvariantCulture);

                        rex = new Regex("s3\\.upperLeft=(?<ident>.*?);", RegexOptions.Singleline);
                        m = rex.Match(resp);
                        iident = m.Groups["ident"].Value;

                        rex = new Regex(iident + "\\.x=(?<valx>.*?);" + iident + "\\.y=(?<valy>.*?);", RegexOptions.Singleline);
                        m = rex.Match(resp);
                        tileUlX = double.Parse(m.Groups["valx"].Value, CultureInfo.InvariantCulture);
                        tileUlY = double.Parse(m.Groups["valy"].Value, CultureInfo.InvariantCulture);

                        rex = new Regex("s3\\.center=(?<ident>.*?);", RegexOptions.Singleline);
                        m = rex.Match(resp);
                        iident = m.Groups["ident"].Value;

                        rex = new Regex(iident + "\\.x=(?<valx>.*?);" + iident + "\\.y=(?<valy>.*?);", RegexOptions.Singleline);
                        m = rex.Match(resp);
                        tileCX = double.Parse(m.Groups["valx"].Value, CultureInfo.InvariantCulture);
                        tileCY = double.Parse(m.Groups["valy"].Value, CultureInfo.InvariantCulture);
                    }

                    double tileUlX_M, tileUlY_M, tileLRX_M, tileLRY_M;
                    GeoToUTM(tileUlY, tileUlX, out tileUlX_M, out tileUlY_M);
                    GeoToUTM(tileLRY, tileLRX, out tileLRX_M, out tileLRY_M);



                    double wMeterPerPixel = (tileLRX_M - tileUlX_M) / 256.0;
                    double hMeterPerPixel = (tileUlY_M - tileLRY_M) / 256.0;
                    //object test = Result["zoomLevel"];
                    if (!Directory.Exists(Path.Combine(m_CacheDirectory, useScale.ToString())))
                    {
                        Directory.CreateDirectory(Path.Combine(m_CacheDirectory, useScale.ToString()));
                    }
                    File.WriteAllLines(infoFile, new string[] {tileUlX_M.ToString(CultureInfo.InvariantCulture), 
                        tileUlY_M.ToString(CultureInfo.InvariantCulture),
                    startTileX_IDX.ToString(),
                    startTileY_IDX.ToString(),
                    wMeterPerPixel.ToString(CultureInfo.InvariantCulture),
                    hMeterPerPixel.ToString(CultureInfo.InvariantCulture),
                    lengthDegreeX.ToString(CultureInfo.InvariantCulture),
                    lengthDegreeY.ToString(CultureInfo.InvariantCulture),
                    tileCX.ToString(CultureInfo.InvariantCulture),
                    tileCY.ToString(CultureInfo.InvariantCulture)});

                    refTileULX = tileUlX_M;
                    refTileULY = tileUlY_M;
                    refTileXOffset = startTileX_IDX;
                    refTileYOffset = startTileY_IDX;
                    tileHMetersPerPixel = hMeterPerPixel;
                    tileWMetersPerPixel = wMeterPerPixel;
                    lengthDegreesX = lengthDegreeX;
                    lengthDegreesY = lengthDegreeY;
                    centerLat = tileCX;
                    centerLon = tileCY;

                }
                else
                {
                    string[] cnt = File.ReadAllLines(infoFile);

                    refTileULX = double.Parse(cnt[0], CultureInfo.InvariantCulture);
                    refTileULY = double.Parse(cnt[1], CultureInfo.InvariantCulture);
                    refTileXOffset = int.Parse(cnt[2], CultureInfo.InvariantCulture);
                    refTileYOffset = int.Parse(cnt[3], CultureInfo.InvariantCulture);
                    tileWMetersPerPixel = double.Parse(cnt[4], CultureInfo.InvariantCulture);
                    tileHMetersPerPixel = double.Parse(cnt[5], CultureInfo.InvariantCulture);
                    lengthDegreesX= double.Parse(cnt[6], CultureInfo.InvariantCulture);
                    lengthDegreesY = double.Parse(cnt[7], CultureInfo.InvariantCulture);
                    centerLat = double.Parse(cnt[8], CultureInfo.InvariantCulture);
                    centerLon = double.Parse(cnt[9], CultureInfo.InvariantCulture);

                }

                if (!m_TileInfoCache.ContainsKey((int)useScale))
                {
                    CacheTileInfo cti = new CacheTileInfo();
                    cti.refTileOffX = refTileXOffset;
                    cti.refTileOffY = refTileYOffset;
                    cti.refTileULX = refTileULX;
                    cti.refTileULY = refTileULY;
                    cti.tileHMetersPerPixel = tileHMetersPerPixel;
                    cti.tileWMetersPerPixel = tileWMetersPerPixel;
                    cti.lengthDegreesX = lengthDegreesX;
                    cti.lengthDegreesY = lengthDegreesY;
                    cti.centerLat = centerLat;
                    cti.centerLon = centerLon;
                    m_TileInfoCache.Add((int)useScale, cti);
                }
            }
            //double t = 256 * wMeterPerPixel + tileUlX_M;
            //double t2 = lengthDegreeX * 256;
        }

        public ZoneFiveSoftware.Common.Data.GPS.IGPSLocation PixelToGPS(ZoneFiveSoftware.Common.Data.GPS.IGPSLocation origin, double zoomLevel, System.Drawing.Point pixel)
        {

            double refTileULX, refTileULY, tileWMetersPerPixel, tileHMetersPerPixel, lengthDegreesX, lengthDegreesY, centerLat, centerLon;
            int refTileXOffset, refTileYOffset;
            try
            {
                getScaleInfo(zoomLevel, origin, out refTileULX, out refTileULY, out tileWMetersPerPixel, out tileHMetersPerPixel, out refTileXOffset, out refTileYOffset, out lengthDegreesX, out lengthDegreesY, out centerLat, out centerLon);
            }
            catch (Exception ee)
            {
                throw new ApplicationException("Eniro-Server changed!",ee);
            }

            double dx = pixel.X / 256.0 * lengthDegreesX;
            double dy = pixel.Y / 256.0 * lengthDegreesY;

            //double hittascale;
            //double tileMeterPerPixel;
            //double metersPerPixel = getMetersPerPixel(zoomLevel, out tileMeterPerPixel, out hittascale);

            //double lat, lon, origx, origy;
            //Triona.Util.CFProjection.WGS84ToRT90(origin.LatitudeDegrees, origin.LongitudeDegrees, 0, out origx, out origy);

            //double x = origx + pixel.X*tileWMetersPerPixel;
            //double y = origy - pixel.Y*tileHMetersPerPixel;


            //Triona.Util.CFProjection.RT90ToWGS84(x, y, out lat, out lon);
            return new ZoneFiveSoftware.Common.Data.GPS.GPSLocation((float)(origin.LatitudeDegrees-dy),(float)(origin.LongitudeDegrees+dx));
        }

        #endregion
    }
}
