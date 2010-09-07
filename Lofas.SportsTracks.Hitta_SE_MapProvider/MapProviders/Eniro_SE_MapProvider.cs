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
using ZoneFiveSoftware.Common.Data.GPS;
#if ST_2_1
using ZoneFiveSoftware.Common.Visuals.Fitness.GPS;
#else
using ZoneFiveSoftware.Common.Visuals.Mapping;
#endif

namespace Lofas.SportsTracks.Hitta_SE_MapProvider
{
    public class Eniro_SE_MapProvider : 
#if ST_2_1
        ZoneFiveSoftware.Common.Visuals.Fitness.GPS.IMapProvider
#else
        ZoneFiveSoftware.Common.Visuals.Mapping.IMapTileProvider
#endif
    {
        #region IMapProvider Members

        public const int tileX2 = 128;
        public const int tileY2 = 128;
        public string m_CacheDirectory;
        private Dictionary<string, string> m_DownloadQueueItems;

        public string m_View;
        string m_ImageExt;
        string m_ViewCountry = "SE_sv";
        public string infoUrl = "http://kartor.eniro.se/mapapi/servlets/dwr-invoker/call/plaincall/TilesService.initializeEniMap.dwr";
        private readonly string m_Name;
        private readonly Guid m_GUID;
        private readonly Eniro_SE_MapProjection m_Proj;

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


            m_CacheDirectory = Path.Combine(
#if ST_2_1
                Plugin.m_Application.SystemPreferences.WebFilesFolder, "MapTiles" + 
#else
                //TODO: Temporary use the ST2 folder, until Eniro works again
                //Plugin.m_Application.Configuration.CommonWebFilesFolder, GUIDs.PluginMain.ToString() +
                Plugin.m_Application.Configuration.CommonWebFilesFolder, "../../2.0/Web Files/MapTiles" +
#endif
                Path.DirectorySeparatorChar + "Eniro_" + m_ViewCountry.Substring(0, 2) + "_" + view);
            m_DownloadQueueItems = new Dictionary<string, string>();

            if (m_View == "Sat")
            {
                m_ImageExt = "jpg";
            }
            else
            {
                m_ImageExt = "gif";
            }

            //Name
            switch (m_View)
            {
                case "Sat":
                    m_Name = "Eniro.se Flygfoto";
                    break;
                case "Nat":
                    m_Name = "Eniro.se Sjökort";
                    break;
                default:
                    m_Name = "Eniro.se Karta";
                    break;
            }
            if (m_ViewCountry == "FI_fi")
                m_Name = "Eniro.fi Kartat";

            //GUID
            if (m_View == "Sat")
                m_GUID = new Guid("0BE4F711-316A-4d8a-B259-6B08BDD8438F");
            else if (m_View == "Nat")
            {
                m_GUID = new Guid("102C58BC-B78B-4f8c-95D2-4100773EE50F");
            }
            else
            {
                if (m_ViewCountry == "FI_fi")
                {
                    m_GUID = new Guid("CE766975-7457-4036-BF11-EE4D5104E605");
                }
                else
                {
                    m_GUID = new Guid("FE0739E6-643D-4a0a-95B3-74CC1073D36E");
                }
            }
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

            string reqUrlBase = infoUrl +
                "?callCount=1&page=%2F&httpSessionId=&scriptSessionId=A5D21E9D83F2E4EA745B637926180464446&c0-scriptName=TilesService&c0-methodName=initializeEniMap&c0-id=0&c0-param0=string%3ASE&c0-param1=string%3A&c0-param2=string%3A" +
                ident + "&c0-e1=number%3A";
            m_Proj = new Eniro_SE_MapProjection(m_CacheDirectory, reqUrlBase);

        }
 
        public void ClearDownloadQueue()
        {
            m_DownloadQueueItems.Clear();
        }

        public int DownloadQueueSize
        {
            get { return m_DownloadQueueItems.Count; }
        }

        public int DrawMap(IMapImageReadyListener listener, System.Drawing.Graphics graphics, System.Drawing.Rectangle drawRectangle, System.Drawing.Rectangle clipRectangle, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation center, double zoomLevel)
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
                double metersPerPixel = m_Proj.getMetersPerPixel(zoomLevel, out tileMeterPerPixel, out useScale);


                double refTileULX, refTileULY, tileWMetersPerPixel, tileHMetersPerPixel, lengthDegreesX, lengthDegreesY, centerLat, centerLon;
                int refTileXOffset, refTileYOffset;
                m_Proj.getScaleInfo(zoomLevel, center, out refTileULX, out refTileULY, out tileWMetersPerPixel, out tileHMetersPerPixel, out refTileXOffset, out refTileYOffset, out lengthDegreesX, out lengthDegreesY, out centerLat, out centerLon);

                double dy = (center.LatitudeDegrees - centerLon) / lengthDegreesY;
                double dx = (center.LongitudeDegrees - centerLat) / lengthDegreesX;

                int numTilesDX = (int)Math.Round(dx);
                int numTilesDY = (int)Math.Round(dy);
                int startTileX = refTileXOffset + numTilesDX;
                int startTileY = refTileYOffset + numTilesDY;

                int tileDrawDX = (int)(-1 * (dx - numTilesDX) * 2*tileX2) - tileY2;
                int tileDrawDY = (int)(1 * (dy - numTilesDY) * 2*tileY2) - tileY2;

                int numTilesX = (int)Math.Ceiling((float)drawRectangle.Width / (2*tileX2));
                int numTilesY = (int)Math.Ceiling((float)drawRectangle.Height / (2*tileY2));

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
                            graphics.DrawImage(img, (int)Math.Floor((double)(drawRectangle.Width / 2.0 + tileDrawDX + col * 2*tileX2)), (int)Math.Floor((double)(drawRectangle.Height / 2.0 + tileDrawDY + row * 2*tileY2)));
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
                            int cX = (int)Math.Floor((double)(tileDrawDX + col * 2*tileX2)) + tileY2;
                            int cY = (int)Math.Floor((double)(tileDrawDY + row * 2*tileY2)) + tileY2;
                            double latC = center.LongitudeDegrees + cX / (2*tileX2) * lengthDegreesX;
                            double lonC = center.LatitudeDegrees + cY / (2*tileY2) * lengthDegreesY;
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
                        

#if ENIRO_TODO_DOWNLOAD
        private class MapImageObj
        {
            public double cx;
            public double cy;
            public double Scale;
        }
#endif
        WebClient wc = new WebClient();
        Random rnd = new Random();
        private void queueDownload(double cx, double cy, int iRx, int iRy, double useZoomLevel, IMapImageReadyListener listener)
        {
            string item = iRx + "_" + iRy + "_" + useZoomLevel.ToString();
            if (!m_DownloadQueueItems.ContainsKey(item))
            {
#if ENIRO_TODO_DOWNLOAD
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

                                    int zoomLevelIdx = Array.IndexOf(m_Proj.scaleValues, useZoomLevel);
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
                                        ident + "_" + Convert.ToInt32(m_Proj.scaleValues.Length - zoomLevelIdx) + "_" + Convert.ToInt32(useZoomLevel) + ".0_58.0_256_128_" + iRx + "_" + iRy;
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
#if ST_2_1
                            listener.NotifyMapImageReady(obj);
#else
                            //TODO: Get bounds for tile, center is set
                            //listener.InvalidateRegion(new GPSBounds(
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
#endif
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
            catch (Exception)
            {
                try
                {
                    File.Delete(str);
                }
                catch (Exception)
                {
                }

                return new Bitmap(2*tileX2, 2*tileY2);
            }
        }

        private bool isCached(int iRx, int iRy, double useZoomLevel)
        {
            string downloadDir = Path.Combine(m_CacheDirectory, useZoomLevel.ToString());
            string str = Path.Combine(downloadDir, iRx + "_" + iRy + "." + m_ImageExt);
            return File.Exists(str);
        }


        public void Refresh(System.Drawing.Rectangle drawRectangle, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation center, double zoomLevel)
        {
#if ENIRO_TODO_DOWNLOAD
            //TODO: Delete cached tiles
#endif
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
            get { return 0; }
        }

        public bool SupportsFractionalZoom
        {
            get { return false; }
        }

        public string Name
        {
            get { return m_Name; }
        }
        #endregion

#if ST_2_1
        //A few methods differ ST2/ST3, the ST2 are separated
        public System.Drawing.Rectangle MapImagePixelRect(object mapImage, System.Drawing.Rectangle drawRectangle, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation center, double zoomLevel)
        {
            if (mapImage is MapImageObj)
            {
                MapImageObj obj = mapImage as MapImageObj;

                double cX = obj.cx;
                double cY = obj.cy;

                double useScale;
                double tileMeterPerPixel;
                double metersPerPixel = m_Proj.getMetersPerPixel(zoomLevel, out tileMeterPerPixel, out useScale);


                double refTileULX, refTileULY, tileWMetersPerPixel, tileHMetersPerPixel, lengthDegreesX, lengthDegreesY, centerLat, centerLon;
                int refTileXOffset, refTileYOffset;
                m_Proj.getScaleInfo(zoomLevel, center, out refTileULX, out refTileULY, out tileWMetersPerPixel, out tileHMetersPerPixel, out refTileXOffset, out refTileYOffset, out lengthDegreesX, out lengthDegreesY, out centerLat, out centerLon);

                double dy = -1*(center.LatitudeDegrees - cY) / lengthDegreesY * 2*tileY2;
                double dx = -1*(center.LongitudeDegrees - cX) / lengthDegreesX * 2*tileX2;

                /*int numTilesDX = (int)Math.Round(dx);
                int numTilesDY = (int)Math.Round(dy);

                int startTileX = refTileXOffset + numTilesDX;
                int startTileY = refTileYOffset + numTilesDY;

                int tileDrawDX = (int)(-1 * (dx - numTilesDX) * 2*tileX2) ;
                int tileDrawDY = (int)(1 * (dy - numTilesDY) * 2*tileY2) ;*/

                return Rectangle.FromLTRB((int)(drawRectangle.Width / 2.0 + dx - tileY2), (int)(drawRectangle.Height / 2.0 + dy - tileY2),
                    (int)(drawRectangle.Width / 2.0 + dx + tileY2), (int)(drawRectangle.Height / 2.0 + dy + tileY2));
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
