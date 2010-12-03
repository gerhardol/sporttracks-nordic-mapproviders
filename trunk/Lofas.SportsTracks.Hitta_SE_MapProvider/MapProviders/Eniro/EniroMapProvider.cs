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
using System.Drawing;
using System.IO;
using System.Threading;
using ZoneFiveSoftware.Common.Data.GPS;
#if ST_2_1
using ZoneFiveSoftware.Common.Visuals.Fitness.GPS;
#else
using ZoneFiveSoftware.Common.Visuals.Mapping;
#endif

namespace Lofas.SportsTracks.Hitta_SE_MapProvider
{
    public class EniroMapProvider :
#if ST_2_1
        ZoneFiveSoftware.Common.Visuals.Fitness.GPS.IMapProvider
#else
        ZoneFiveSoftware.Common.Visuals.Mapping.IMapTileProvider
#endif
    {
        #region Private members

        private const int ENIRO_MAX_ZOOMLEVEL = 20;
        private const int TILE_SIZE = 256;
        private readonly string m_CacheDirectory; // Directory where to store the cached tiles
        private readonly Dictionary<string, string> m_DownloadQueueItems;
        private readonly Guid m_GUID;
        private readonly string m_ImageExtension;

        private readonly EniroMapProjection m_MapProjection = new EniroMapProjection();
                                            // The projection to use within the EniroMapProvider   

        private readonly string m_Name;
        private readonly string m_ViewTypeInUrl;

        #endregion

        /// <summary>
        /// Constructor for EniroMapProvider. 
        /// Eniro map provider takes the mapViewType as a parameter and sets various private member variables.
        /// </summary>
        /// <param name="mapViewType"></param>
        public EniroMapProvider(string mapViewType)
        {
            switch (mapViewType)
            {
                case "map":
                    {
                        m_ImageExtension = ".png";
                        m_GUID = new Guid("3E9661F9-8704-4868-9700-A668DF4C2C75");
                        m_Name = "Eniro - Karta";
                        m_ViewTypeInUrl = "map";
                        break;
                    }
                case "aerial":
                    {
                        m_ImageExtension = ".jpeg";
                        m_GUID = new Guid("6B07A2D9-8394-496F-A843-751529BB88D9");
                        m_Name = "Eniro - Flygfoto";
                        m_ViewTypeInUrl = "aerial";
                        break;
                    }
                case "nautical":
                    {
                        m_ImageExtension = ".png";
                        m_GUID = new Guid("C7588CC1-AF51-497D-A6B8-AE18B9F600FD");
                        m_Name = "Eniro - Sjökort";
                        m_ViewTypeInUrl = "nautical";
                        break;
                    }
            }

            m_CacheDirectory = Path.Combine(
#if ST_2_1
                Plugin.m_Application.SystemPreferences.WebFilesFolder, "MapTiles" +
#else
                Plugin.m_Application.Configuration.CommonWebFilesFolder, GUIDs.PluginMain.ToString() + 
#endif
                 + Path.DirectorySeparatorChar + "Eniro_" +
                                            m_ViewTypeInUrl);
            m_DownloadQueueItems = new Dictionary<string, string>();
        }

        #region IMapTileProvider Members

        ///<summary>
        /// Return the number of images in the map image download queue. This is used to display the activity animation and the animation tooltip.
        ///</summary>
        public int DownloadQueueSize
        {
            get { return m_DownloadQueueItems.Count; }
        }

        /// <summary>
        /// Handles the drawing of the map.
        /// </summary>
        /// <param name="readyListener"></param>
        /// <param name="graphics"></param>
        /// <param name="drawRect"></param>
        /// <param name="clipRect"></param>
        /// <param name="center"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public int DrawMap(IMapImageReadyListener readyListener, Graphics graphics, Rectangle drawRect,
                           Rectangle clipRect, IGPSLocation center, double zoom)
        {
            // Convert the zoom level to match the zoom-levels of Eniro.
            double eniroZoomlevel = ENIRO_MAX_ZOOMLEVEL - zoom;

            int numberOfTilesQueued = 0;
            if (EniroMapProjection.IsValidLocation(center))
            {
                long xTileOfCenter = m_MapProjection.XTile(center.LongitudeDegrees, eniroZoomlevel);
                long yTileOfCenter = m_MapProjection.YTile(center.LatitudeDegrees, eniroZoomlevel);
                long xPixelOfCenter = m_MapProjection.Xpixel(center.LongitudeDegrees, eniroZoomlevel);
                long yPixelOfCenter = m_MapProjection.Ypixel(center.LatitudeDegrees, eniroZoomlevel);
                long xPixelOfNWCornerCenterTile = m_MapProjection.PixelOfNorthWestCornerOfTile(xTileOfCenter);
                long yPixelOfNWCornerCenterTile = m_MapProjection.PixelOfNorthWestCornerOfTile(yTileOfCenter);

                long xPixelOffsetCenterVsNWCornerOfCenterTile = xPixelOfCenter - xPixelOfNWCornerCenterTile;
                long yPixelOffsetCenterVsNWCornerOfCenterTile = yPixelOfCenter - yPixelOfNWCornerCenterTile;

                long xPixelsFromLeftEdgeOfDrawingAreaToLeftEdgeOfCenterTile = (drawRect.Width/2) -
                                                                              xPixelOffsetCenterVsNWCornerOfCenterTile;
                long yPixelsFromTopEdgeOfDrawingAreaToTopEdgeOfCenterTile = (drawRect.Height/2) -
                                                                            yPixelOffsetCenterVsNWCornerOfCenterTile;

                int noOfTilesToBeDrawnToTheLeftOfCenterTile =
                    (int) Math.Ceiling((double) xPixelsFromLeftEdgeOfDrawingAreaToLeftEdgeOfCenterTile/TILE_SIZE);
                int noOfTilesToBeDrawnAboveOfCenterTile =
                    (int) Math.Ceiling((double) yPixelsFromTopEdgeOfDrawingAreaToTopEdgeOfCenterTile/TILE_SIZE);

                long xNWStartPixel = xPixelsFromLeftEdgeOfDrawingAreaToLeftEdgeOfCenterTile -
                                     (TILE_SIZE*noOfTilesToBeDrawnToTheLeftOfCenterTile);
                long yNWStartPixel = yPixelsFromTopEdgeOfDrawingAreaToTopEdgeOfCenterTile -
                                     (TILE_SIZE*noOfTilesToBeDrawnAboveOfCenterTile);

                int noOfTilesToBeDrawnHorizontally = (int) Math.Ceiling((double) drawRect.Width/TILE_SIZE + 1);
                int noOfTilesToBeDrawnVertically = (int) Math.Ceiling((double) drawRect.Height/TILE_SIZE + 1);
                long startXTile = xTileOfCenter - noOfTilesToBeDrawnToTheLeftOfCenterTile;
                long startYTile = yTileOfCenter - noOfTilesToBeDrawnAboveOfCenterTile;

                // Calculation to find out which region to be invalidated
                Point northWestPoint = new Point(-drawRect.Width/2, -drawRect.Height/2);
                Point southEastPoint = new Point(drawRect.Width/2, drawRect.Height/2);
                IGPSLocation northWestLocation = m_MapProjection.PixelToGPS(center, zoom, northWestPoint);
                IGPSLocation southEastLocation = m_MapProjection.PixelToGPS(center, zoom, southEastPoint);
                GPSBounds regionToBeInvalidated = new GPSBounds(northWestLocation, southEastLocation);

                // We have calculated the start tile, that is the tile in the north-west corner of the drawing area. 
                // Now we will iterate left to right and top to bottom so that all tiles is either drawn or downloaded.
                for (int x = 0; x < noOfTilesToBeDrawnHorizontally; x++)
                {
                    for (int y = 0; y < noOfTilesToBeDrawnVertically; y++)
                    {
                        long tileXToBeDrawn = startXTile + x;
                        long tileYToBeDrawn = startYTile + y;
                        long tileYToBeDrawnEniro = (long) Math.Pow(2, eniroZoomlevel) - 1 - tileYToBeDrawn;

                        // Find out if the tile is cached on disk or needs to be downloaded from Eniro.
                        if (IsCached(tileXToBeDrawn, tileYToBeDrawnEniro, eniroZoomlevel))
                        {
                            Image img = getImageFromCache(tileXToBeDrawn, tileYToBeDrawnEniro, eniroZoomlevel);
                            long ix = xNWStartPixel + x*TILE_SIZE;
                            long iy = yNWStartPixel + y*TILE_SIZE;
                            graphics.DrawImage(img, ix, iy, TILE_SIZE, TILE_SIZE);
                            img.Dispose();
                        }
                        else
                        {
                            QueueDownload(tileXToBeDrawn, tileYToBeDrawnEniro, eniroZoomlevel, regionToBeInvalidated,
                                          readyListener);
                            numberOfTilesQueued++;
                        }
                    }
                }
            }
            return numberOfTilesQueued;
        }

        /// <summary>
        /// The GUID of the Map provider
        /// </summary>
        public Guid Id
        {
            get { return m_GUID; }
        }

        /// <summary>
        /// The projection that is used for the EniroMapProvider
        /// </summary>
        public IMapProjection MapProjection
        {
            get { return m_MapProjection; }
        }

        /// <summary>
        /// Maximum zoom level. The zoom level does not correspond to the zoom level of Eniro.
        /// Maximum zoom level = not detailed
        /// </summary>
        public double MaximumZoom
        {
            get { return 10; }
        }

        /// <summary>
        /// Minimum zoom level. The zoom level does not correspond to the zoom level of Eniro.
        /// Minimum zoom level = detailed
        /// </summary>
        public double MinimumZoom
        {
            get { return 0; }
        }

        ///<summary>
        /// The name displayed in selection lists, menus, etc.
        ///</summary>
        public string Name
        {
            get { return m_Name; }
        }

        ///<summary>
        /// Refresh the map area by discarding any cached map images. After this call the control will be invalidated which will
        /// cause the appropriate calls to be made to refetch any images from the server.
        ///<param name="drawRectangle">The rectangle to draw the map at.</param>
        ///<param name="center">The GPS location at the center of the drawRectangle (width/2,height/2).</param>
        ///<param name="zoomLevel">The current zoom level.</param>
        public void Refresh(Rectangle drawRectangle, IGPSLocation center, double zoomLevel)
        {
            //TODO: Delete cached tiles
        }

        ///<summary>
        /// Is fractional zooming supported. If true, smoother zooming can be used, instead of a step size of 1, 0.25 is used.
        ///</summary>
        public bool SupportsFractionalZoom
        {
            get { return false; }
        }

        #endregion

        /// <summary>
        /// This method downloads a tile and saves it to disk.
        /// </summary>
        /// <param name="tileXToBeDrawn"></param>
        /// <param name="tileYToBeDrawn"></param>
        /// <param name="zoom"></param>
        /// <param name="regionToBeInvalidated"></param>
        /// <param name="listener"></param>
        private void QueueDownload(long tileXToBeDrawn, long tileYToBeDrawn, double zoom,
                                   IGPSBounds regionToBeInvalidated, IMapImageReadyListener listener)
        {
            string item = zoom + "/" + tileXToBeDrawn + "/" + tileYToBeDrawn;

            if (!m_DownloadQueueItems.ContainsKey(item))
            {
                m_DownloadQueueItems.Add(item, "");
                ThreadPool.QueueUserWorkItem(delegate
                                                 {
                                                     try
                                                     {
                                                         lock (STWebClient.Instance)
                                                         {
                                                             if (m_DownloadQueueItems.ContainsKey(item))
                                                             {
                                                                 // Eniro seems to randomly point against one of four different servers. 
                                                                 // Therefore I do the same and vary between an url of map01..., map02..., map03... and map04...
                                                                 Random rnd = new Random();
                                                                 int serverIndex = rnd.Next(1, 5);
                                                                 string baseUrl =
                                                                     string.Format(
                                                                         "http://map0{0}.eniro.com/geowebcache/service/tms1.0.0/",
                                                                         serverIndex);
                                                                 string url = baseUrl + m_ViewTypeInUrl + "/" + item +
                                                                              m_ImageExtension;

                                                                 Image img =
                                                                     Image.FromStream(
                                                                         STWebClient.Instance.OpenRead(url));
                                                                 img.Save(getFilePath(tileXToBeDrawn, tileYToBeDrawn,
                                                                                      zoom, true));
                                                                 img.Dispose();
                                                             }
                                                         }

                                                         // Invalidate the region of the drawing area
#if ST_2_1
                                                         listener.NotifyMapImageReady(regionToBeInvalidated);
#else
                                                         listener.InvalidateRegion(regionToBeInvalidated);
#endif
                                                     }
                                                     catch (Exception)
                                                     {
                                                     }
                                                     m_DownloadQueueItems.Remove(item);
                                                 });
            }
        }

        /// <summary>
        /// Opens an image using Image.FromStream
        /// </summary>
        /// <param name="previewFile"></param>
        /// <returns></returns>
        public Image OpenImage(string previewFile)
        {
            FileStream fs = new FileStream(previewFile, FileMode.Open, FileAccess.Read);
            return Image.FromStream(fs);
        }

        /// <summary>
        /// Checks if a tile is cached on disk or not.
        /// </summary>
        /// <param name="xTile"></param>
        /// <param name="yTile"></param>
        /// <param name="zoomLevel"></param>
        /// <returns></returns>
        private bool IsCached(long xTile, long yTile, double zoomLevel)
        {
            return File.Exists(getFilePath(xTile, yTile, zoomLevel));
        }

        /// <summary>
        /// Gets the path to the directory where the cached tiles are stored.
        /// </summary>
        /// <param name="xTile"></param>
        /// <param name="yTile"></param>
        /// <param name="zoomLevel"></param>
        /// <param name="createDir"></param>
        /// <returns></returns>
        private string getFilePath(long xTile, long yTile, double zoomLevel, bool createDir)
        {
            string downloadDir = Path.Combine(m_CacheDirectory, zoomLevel.ToString());
            if (createDir && !Directory.Exists(downloadDir))
                Directory.CreateDirectory(downloadDir);
            return Path.Combine(downloadDir, xTile + "_" + yTile + m_ImageExtension);
        }

        /// <summary>
        /// Gets the path to the directory where the cached tiles are stored.
        /// </summary>
        /// <param name="xTile"></param>
        /// <param name="yTile"></param>
        /// <param name="useZoomLevel"></param>
        /// <returns></returns>
        private string getFilePath(long xTile, long yTile, double useZoomLevel)
        {
            return getFilePath(xTile, yTile, useZoomLevel, false);
        }

        /// <summary>
        /// Gets an image that is cached on disk
        /// </summary>
        /// <param name="xTile"></param>
        /// <param name="yTile"></param>
        /// <param name="zoomLevel"></param>
        /// <returns></returns>
        private Image getImageFromCache(long xTile, long yTile, double zoomLevel)
        {
            string str = getFilePath(xTile, yTile, zoomLevel);

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

        /// <summary>
        /// Removes the items from the download queue
        /// </summary>
        public void ClearDownloadQueue()
        {
            m_DownloadQueueItems.Clear();
        }
#if ST_2_1
        //A few methods differ ST2/ST3, the ST2 methods are separated
        public System.Drawing.Rectangle MapImagePixelRect(object mapImage, System.Drawing.Rectangle drawRectangle, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation center, double zoomLevel)
        {
            if (mapImage is GPSBounds)
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
            return m_MapProjection.GPSToPixel(origin, zoomLevel, gps);
        }
        public ZoneFiveSoftware.Common.Data.GPS.IGPSLocation PixelToGPS(ZoneFiveSoftware.Common.Data.GPS.IGPSLocation origin, double zoomLevel, System.Drawing.Point pixel)
        {
            return m_MapProjection.PixelToGPS(origin, zoomLevel, pixel);
        }
        #endregion
#endif
    }
}