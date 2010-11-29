using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using ZoneFiveSoftware.Common.Data.GPS;
using ZoneFiveSoftware.Common.Visuals.Mapping;

namespace Lofas.SportsTracks.Hitta_SE_MapProvider    
{
    public class EniroMapProvider : IMapTileProvider
    {
        private Dictionary<string, string> m_DownloadQueueItems;
        private readonly string m_Name;
        private readonly Guid m_GUID;
        private readonly string m_ImageExt;
        private readonly string m_ViewTypeInUrl;
        private readonly EniroMapProjection m_Proj = new EniroMapProjection();        
        private readonly string m_CacheDirectory;
        private const int ENIRO_MAX_ZOOMLEVEL = 20;               

        public EniroMapProvider(string viewType)
        {
            if (viewType == "map")
            {
                m_ImageExt = ".png";
                m_GUID = new Guid("3E9661F9-8704-4868-9700-A668DF4C2C75");
                m_Name = "Eniro - Karta";
                m_ViewTypeInUrl = "map";
            }
            else
            {
                m_ImageExt = ".jpeg";
                m_GUID = new Guid("6B07A2D9-8394-496F-A843-751529BB88D9");
                m_Name = "Eniro - Flygfoto";
                m_ViewTypeInUrl = "aerial";
            }

            m_CacheDirectory = Path.Combine(Plugin.m_Application.Configuration.CommonWebFilesFolder, GUIDs.PluginMain.ToString() + Path.DirectorySeparatorChar + "Eniro_" + m_ViewTypeInUrl);
            m_DownloadQueueItems = new Dictionary<string, string>();     
        }

        #region IMapTileProvider Members

        public int DownloadQueueSize
        {
            get { Debug.Print(m_DownloadQueueItems.ToString()); 
                return m_DownloadQueueItems.Count; }
        }

        private const int TILE_SIZE = 256;

        public int DrawMap(IMapImageReadyListener readyListener, System.Drawing.Graphics graphics, System.Drawing.Rectangle drawRect, System.Drawing.Rectangle clipRect, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation center, double zoom)
        {
            zoom = ENIRO_MAX_ZOOMLEVEL - zoom;
            Debug.Print("DrawMap: " + zoom);
            int numberOfTilesQueued = 0;
            if (EniroMapProjection.isValidPoint(center))
            {
                try
                {
                    long xTile = m_Proj.XTile(center.LongitudeDegrees, zoom);
                    long yTile = m_Proj.YTile(center.LatitudeDegrees, zoom);
                    long xPixel = m_Proj.Xpixel(center.LongitudeDegrees, zoom);
                    long yPixel = m_Proj.Ypixel(center.LatitudeDegrees, zoom);
                    long nwx = m_Proj.Pixel_NW_OfTile(xTile);
                    long nwy = m_Proj.Pixel_NW_OfTile(yTile);

                    long xOffset = xPixel - nwx;
                    long yOffset = yPixel - nwy;

                    long localNWX = (drawRect.Width/2) - xOffset;
                    long localNWY = (drawRect.Height/2) - yOffset;

                    int noOfTilesOffsetX = (int) Math.Ceiling((double) localNWX/TILE_SIZE);
                    int noOfTilesOffsetY = (int) Math.Ceiling((double) localNWY/TILE_SIZE);

                    long startX = localNWX - (TILE_SIZE * noOfTilesOffsetX);
                    long startY = localNWY - (TILE_SIZE * noOfTilesOffsetY);

                    int noOfTilesToBeDrawnHorizontally = (int) Math.Ceiling((double) drawRect.Width/TILE_SIZE + 1);
                    int noOfTilesToBeDrawnVertically = (int) Math.Ceiling((double) drawRect.Height/TILE_SIZE + 1);
                    long startXTile = xTile - noOfTilesOffsetX;
                    long startYTile = yTile - noOfTilesOffsetY;

                    for (int x = 0; x < noOfTilesToBeDrawnHorizontally; x++)
                    {
                        for (int y = 0; y < noOfTilesToBeDrawnVertically; y++)
                        {
                            long tileXToBeDrawn = startXTile + x;
                            long tileYToBeDrawn = startYTile + y;
                            long tileYToBeDrawnEniro = (long)Math.Pow(2, zoom) - 1 - tileYToBeDrawn;

                            if (isCached(tileXToBeDrawn, tileYToBeDrawnEniro, zoom))
                            {
                                Debug.Print("Draw Eniro Tile: " + tileXToBeDrawn + ";" + tileYToBeDrawnEniro);
                                Image img = getImageFromCache(tileXToBeDrawn, tileYToBeDrawnEniro, zoom);
                                long ix = startX + x*TILE_SIZE;
                                long iy = startY + y*TILE_SIZE;
                                graphics.DrawImage(img, ix, iy, TILE_SIZE, TILE_SIZE);
                                img.Dispose();
                            }
                            else
                            {
                                queueDownload(tileXToBeDrawn, tileYToBeDrawnEniro, zoom, readyListener);
                                numberOfTilesQueued++;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.InnerException.ToString());
                }
            }
            Debug.Print("Eniro: " + numberOfTilesQueued);
            return numberOfTilesQueued;

        }

        STWebClient wc = new STWebClient();

        private void queueDownload(long tileXToBeDrawn, long tileYToBeDrawn, double zoom, IMapImageReadyListener listener)
        {
            try
            {
                string item = zoom + "/" + tileXToBeDrawn + "/" + tileYToBeDrawn;
            
                if (!m_DownloadQueueItems.ContainsKey(item))
                {
                    m_DownloadQueueItems.Add(item, "");
                    ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object o)
                                                                      {
                                                                          try
                                                                          {
                                                                              lock (wc)
                                                                              {
                                                                                  if (m_DownloadQueueItems.ContainsKey(item))
                                                                                  {
                                                                                      // TODO: Möjligen kan vi istället för att hårt skriva nedanstående url
                                                                                      // TODO: Växla mellan http://map01.eniro.com...- http://map04.eniro.com
                                                                                      Random rnd = new Random();
                                                                                      int serverIndex = rnd.Next(1, 4);
                                                                                      string baseUrl = string.Format("http://map0{0}.eniro.com/geowebcache/service/tms1.0.0/", serverIndex);
                                                                                      string url = baseUrl + m_ViewTypeInUrl + "/" + item + m_ImageExt;

                                                                                      Image img = Image.FromStream(wc.OpenRead(url));
                                                                                      img.Save(getFilePath(tileXToBeDrawn, tileYToBeDrawn, zoom, true));
                                                                                      img.Dispose();
                                                                                  }
                                                                              }

                                                                              // Invalidate region
                                                                              listener.InvalidateRegion(new GPSBounds(
                                                                                                            new GPSLocation((float)(61), (float)(16)),
                                                                                                            new GPSLocation((float)(59), (float)(18))));

                                                                          }
                                                                          catch (Exception e)
                                                                          {
                                                                              MessageBox.Show(e.InnerException.ToString());
                                                                          }
                                                                          finally
                                                                          {
                                                                          }
                                                                          m_DownloadQueueItems.Remove(item);
                                                                      }));
                }
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                throw e;
            }
        }
        
        public Image OpenImage(string previewFile)
        {
            FileStream fs = new FileStream(previewFile, FileMode.Open, FileAccess.Read);
            return Image.FromStream(fs);
        }

        private bool isCached(long iRx, long iRy, double useZoomLevel)
        {
            return File.Exists(getFilePath(iRx, iRy, useZoomLevel));
        }

        private string getFilePath(long iRx, long iRy, double useZoomLevel, bool createDir)
        {
            string downloadDir = Path.Combine(m_CacheDirectory, useZoomLevel.ToString());
            if (createDir && !Directory.Exists(downloadDir))
                Directory.CreateDirectory(downloadDir);
            return Path.Combine(downloadDir, iRx + "_" + iRy + m_ImageExt);
        }

        private string getFilePath(long iRx, long iRy, double useZoomLevel)
        {
            return getFilePath(iRx, iRy, useZoomLevel, false);
        }

        private Image getImageFromCache(long iRx, long iRy, double useZoomLevel)
        {
            string str = getFilePath(iRx, iRy, useZoomLevel);

            try
            {
//                return Image.FromFile(str);
                return OpenImage(str);
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

                return new Bitmap(TILE_SIZE, TILE_SIZE);
            }
        }

//        public int Xpixel(double lng, double zoom)
//        {
//            // Instead of -180 to +180, we want 0 to 360
//            double dlng = lng + 180;
//            // 256 = tile Width
//            double dxpixel = dlng / 360.0 * 256 * Math.Pow(2, zoom);
//            int xpixel = Convert.ToInt32(Math.Floor(dxpixel));
//            return xpixel;
//        }
//
//        public int Ypixel(double lat, double zoom)
//        {
//            // The 25 comes from 17 + (256=&gt;2^8=&gt;8) 17+8 = 25
//            // ypixelcenter = the middle y pixel (the equator) at this zoom level
////            double ypixelcenter = Math.Pow(2, 28 - zoom - 1);
//            double ypixelcenter = Math.Pow(2, zoom - 1);
//
//            // PI/360 == degrees -&gt; radians
//            // The trig functions are done with radians
//            double dypixel = 256 * (ypixelcenter - Math.Log(Math.Tan(lat * Math.PI / 360 + Math.PI / 4)) * ypixelcenter / Math.PI);
//            int ypixel = Convert.ToInt32(Math.Floor(dypixel));
//            return ypixel;
//        }

//        public int XTile(double lng, double zoom)
//        {
//            return Convert.ToInt32(Math.Floor((double)Xpixel(lng, zoom) / 256));
//        }
//
//        public int YTile(double lat, double zoom)
//        {
//            return Convert.ToInt32(Math.Floor((double)Ypixel(lat, zoom) / 256));
//        }

//        public long Pixel_NW_OfTile(long tile)
//        {
//            return tile * 256;
//        }
        
        public void ClearDownloadQueue()
        {
            m_DownloadQueueItems.Clear();
        }

        public Guid Id
        {
            get { return m_GUID; }
        }

        public IMapProjection MapProjection
        {
            get { return m_Proj; }
        }

        // Översikt
        public double MaximumZoom
        {
            get { return 10; }
        }

        // Detaljrikt
        public double MinimumZoom
        {
            get { return 0; }
        }

        public string Name
        {
            get { return m_Name; }
        }

        public void Refresh(System.Drawing.Rectangle drawRectangle, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation center, double zoomLevel)
        {
            //TODO: Delete cached tiles
        }

        public bool SupportsFractionalZoom
        {
            get { return false; }
        }

        #endregion
    }
}
