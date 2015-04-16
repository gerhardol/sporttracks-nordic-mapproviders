﻿/*
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
using Lofas.SportsTracks.Hitta_SE_MapProvider.Common;
using ZoneFiveSoftware.Common.Data.GPS;
#if ST_2_1
using ZoneFiveSoftware.Common.Visuals.Fitness.GPS;
#else
using ZoneFiveSoftware.Common.Visuals.Mapping;
using System.ComponentModel;
#endif

namespace Lofas.SportsTracks.Hitta_SE_MapProvider
{
    public class HittaEniroMapProvider :
#if ST_2_1
        ZoneFiveSoftware.Common.Visuals.Fitness.GPS.IMapProvider
#else
        ZoneFiveSoftware.Common.Visuals.Mapping.IMapTileProvider
#endif
    {
        #region Private members

        private const int MAX_ZOOMLEVEL = 20;
        private const int TILE_SIZE = 256;
        private readonly string m_CacheDirectory; // Directory where to store the cached tiles
        private readonly Dictionary<string, string> m_DownloadQueueItems;
        private readonly Guid m_GUID;
        private readonly string m_ImageExtension;
        private readonly SwedishMapProvider m_SwedishMapProvider;
        private readonly string m_MapProviderAbbreviation;
        private readonly HittaEniroMapProjection m_MapProjection = new HittaEniroMapProjection();
                                            // The projection to use within the EniroMapProvider
        //Invalidating the regions for fetched tiles must be done on the main thread
        private AsyncOperation m_operOnMainThread = null;

        private readonly string m_Name;
        private readonly string m_ViewTypeInUrl;
        private readonly int m_minimumZoom;
        private readonly int m_maximumZoom;


        #endregion

        private class QueueInfo
        {
            public MapTileInfo tileInfo;
            public IMapImageReadyListener listener;

            public QueueInfo(MapTileInfo t, IMapImageReadyListener listener)
            {
                this.listener = listener;
                this.tileInfo = t;
            }
        }

        /// <summary>
        /// Constructor for EniroMapProvider. 
        /// The map provider takes the mapViewType as a parameter and sets various private member variables.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="mapViewType"></param>
        public HittaEniroMapProvider(SwedishMapProvider provider, MapViewType mapViewType)
        {
            m_SwedishMapProvider = provider;
            m_operOnMainThread = AsyncOperationManager.CreateOperation(null); 
            switch (provider)
            {
                case SwedishMapProvider.Eniro:
                {
                    switch (mapViewType)
                    {
                        case MapViewType.Map:
                        {
                            m_ImageExtension = ".png";
                            m_GUID = new Guid("3E9661F9-8704-4868-9700-A668DF4C2C75");
                            m_Name = "Eniro - Karta";
                            m_ViewTypeInUrl = "map";
                            m_MapProviderAbbreviation = "EniroMap";
                            m_maximumZoom = 15;
                            m_minimumZoom = 3;
                            break;
                        }
                        case MapViewType.Aerial:
                        {
                            m_ImageExtension = ".jpeg";
                            m_GUID = new Guid("6B07A2D9-8394-496F-A843-751529BB88D9");
                            m_Name = "Eniro - Flygfoto";
                            m_ViewTypeInUrl = "aerial";
                            m_MapProviderAbbreviation = "EniroAer";
                            m_maximumZoom = 15;
                            m_minimumZoom = 2;
                            break;
                        }
                        case MapViewType.Nautical:
                        {
                            m_ImageExtension = ".png";
                            m_GUID = new Guid("C7588CC1-AF51-497D-A6B8-AE18B9F600FD");
                            m_Name = "Eniro - Sjökort";
                            m_ViewTypeInUrl = "nautical";
                            m_MapProviderAbbreviation = "EniroNau";
                            m_maximumZoom = 15;
                            m_minimumZoom = 3;
                            break;
                        }
                    }
                    break;
                }
                case SwedishMapProvider.Hitta:
                {
                    switch (mapViewType)
                    {
                        case MapViewType.Map:
                        {
                            m_ImageExtension = ".png";
                            m_GUID = new Guid("23FBB14A-0949-4d42-BAA4-95C3AC3BC825");
                            m_Name = "Hitta - Karta";
                            m_ViewTypeInUrl = "0";
                            m_MapProviderAbbreviation = "HittaMap";
                            m_maximumZoom = 15;
                            m_minimumZoom = 3;
                            break;
                        }
                        case MapViewType.Aerial:
                        {
                            m_ImageExtension = ".jpeg";
                            m_GUID = new Guid("9BD470DD-3078-456f-8175-1A714D286B90");
                            m_Name = "Hitta - Flygfoto";
                            m_ViewTypeInUrl = "1";
                            m_MapProviderAbbreviation = "HittaAer";
                            m_maximumZoom = 15;
                            m_minimumZoom = 2;
                            break;
                        }
                    }
                    break;
                }
            }

           

            m_CacheDirectory = Path.Combine(
#if ST_2_1
                Plugin.m_Application.SystemPreferences.WebFilesFolder, "MapTiles" +
#else
                Plugin.m_Application.Configuration.CommonWebFilesFolder, GUIDs.PluginMain.ToString() + 
#endif
                 Path.DirectorySeparatorChar + m_MapProviderAbbreviation);
            m_DownloadQueueItems = new Dictionary<string, string>();
        }

        private string tileUrl(MapTileInfo tile)
        {
            var url = "";
            string baseUrl;
            switch (m_SwedishMapProvider)
            {
                case SwedishMapProvider.Eniro:
                    // Eniro seems to randomly point against one of four different servers. 
                    // Therefore I do the same and vary between an url of map01..., map02..., map03... and map04...
                    var rnd = new Random();
                    int serverIndex = rnd.Next(1, 5);
                    baseUrl =
                        string.Format(
                            "http://map0{0}.eniro.com/geowebcache/service/tms1.0.0/",
                            serverIndex);

                    url = baseUrl + m_ViewTypeInUrl + "/" + tileId(tile) + m_ImageExtension;
                    break;

                case SwedishMapProvider.Hitta:
                    baseUrl = "http://static.hitta.se/tile/v3/";
                    url = baseUrl + m_ViewTypeInUrl + "/" + tileId(tile);  //+m_ImageExtension;
                    break;
            }
            return url;
        }

        //Eniro/Hitta has the same type of identification
        private string tileId(MapTileInfo tile){ return tile.zoomlevel + "/" + tile.pixTileX + "/" + tile.pixTileY; }

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
            int numberOfTilesQueued = 0;
            if (HittaEniroMapProjection.IsValidLocation(center))
            {
                foreach (MapTileInfo t in GetTileInfo(drawRect, zoom, center))
                {
                    // Find out if the tile is cached on disk or needs to be downloaded from the map provider.
                    if (IsCached(t.pixTileX, t.pixTileY, t.zoomlevel))
                    {
                        Image img = GetImageFromCache(t.pixTileX, t.pixTileY, t.zoomlevel);
                        graphics.DrawImage(img, t.iTileX, t.iTileY, t.widthX, t.heightY);
                        img.Dispose();
                    }
                    else
                    {
                        if(QueueDownload(new QueueInfo(t, readyListener)))
                        {
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
        /// The projection that is used for the MapProvider
        /// </summary>
        public IMapProjection MapProjection
        {
            get { return m_MapProjection; }
        }

        /// <summary>        
        /// Maximum zoom level = not detailed
        /// </summary>
        public double MaximumZoom
        {
            get { return m_maximumZoom; }
        }

        /// <summary>        
        /// Minimum zoom level = detailed
        /// </summary>
        public double MinimumZoom
        {
            get { return m_minimumZoom; }
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
        /// </summary>
        public void Refresh(Rectangle drawRectangle, IGPSLocation center, double zoomLevel)
        {
            if (HittaEniroMapProjection.IsValidLocation(center))
            {
                foreach (MapTileInfo t in GetTileInfo(drawRectangle, zoomLevel, center))
                {
                    if (IsCached(t.pixTileX, t.pixTileY, t.zoomlevel))
                    {
                        string str = GetFilePath(t.pixTileX, t.pixTileY, t.zoomlevel);

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

        ///<summary>
        /// Is fractional zooming supported. If true, smoother zooming can be used, instead of a step size of 1, 0.25 is used.
        ///</summary>
        public bool SupportsFractionalZoom
        {
            get { return false; }
        }

        #endregion

        /// <summary>
        /// Wrap information about tiles
        /// </summary>
        private class MapTileInfo
        {
            public MapTileInfo(long iTileX, long iTileY, long pixTileX, long pixTileY, double zoomlevel, int widthX, int heightY, IGPSBounds regionToBeInvalidated)
            {
                this.iTileX = iTileX;
                this.iTileY = iTileY;
                this.pixTileX = pixTileX;
                this.pixTileY = pixTileY;
                this.zoomlevel = zoomlevel;
                this.widthX = widthX;
                this.heightY = heightY;
                this.regionToBeInvalidated = regionToBeInvalidated;
            }
            public readonly long iTileX;
            public readonly long iTileY;
            public readonly long pixTileX;
            public readonly long pixTileY;
            public readonly double zoomlevel;
            public readonly int widthX;
            public readonly int heightY;
            public readonly IGPSBounds regionToBeInvalidated;
       }

        /// <summary>
        /// Get information about map tiles
        /// </summary>
        private IEnumerable<MapTileInfo> GetTileInfo(Rectangle drawRect, double zoom, IGPSLocation center)
        {
            IList<MapTileInfo> tiles = new List<MapTileInfo>();

            // Convert the zoom level to match the zoom-levels of the map provider.
            double zoomLevel = MAX_ZOOMLEVEL - zoom;

            if (HittaEniroMapProjection.IsValidLocation(center))
            {
                long xTileOfCenter = m_MapProjection.XTile(center.LongitudeDegrees, zoomLevel);
                long yTileOfCenter = m_MapProjection.YTile(center.LatitudeDegrees, zoomLevel);
                var xPixelOfCenter = m_MapProjection.Xpixel(center.LongitudeDegrees, zoomLevel);
                var yPixelOfCenter = m_MapProjection.Ypixel(center.LatitudeDegrees, zoomLevel);
                var xPixelOfNWCornerCenterTile = m_MapProjection.PixelOfNorthWestCornerOfTile(xTileOfCenter);
                var yPixelOfNWCornerCenterTile = m_MapProjection.PixelOfNorthWestCornerOfTile(yTileOfCenter);

                var xPixelOffsetCenterVsNWCornerOfCenterTile = xPixelOfCenter - xPixelOfNWCornerCenterTile;
                var yPixelOffsetCenterVsNWCornerOfCenterTile = yPixelOfCenter - yPixelOfNWCornerCenterTile;

                var xPixelsFromLeftEdgeOfDrawingAreaToLeftEdgeOfCenterTile = (drawRect.Width / 2) -
                                                                              xPixelOffsetCenterVsNWCornerOfCenterTile;
                var yPixelsFromTopEdgeOfDrawingAreaToTopEdgeOfCenterTile = (drawRect.Height / 2) -
                                                                            yPixelOffsetCenterVsNWCornerOfCenterTile;

                var noOfTilesToBeDrawnToTheLeftOfCenterTile =
                    (int)Math.Ceiling((double)xPixelsFromLeftEdgeOfDrawingAreaToLeftEdgeOfCenterTile / TILE_SIZE);
                var noOfTilesToBeDrawnAboveOfCenterTile =
                    (int)Math.Ceiling((double)yPixelsFromTopEdgeOfDrawingAreaToTopEdgeOfCenterTile / TILE_SIZE);

                var xNWStartPixel = xPixelsFromLeftEdgeOfDrawingAreaToLeftEdgeOfCenterTile -
                                     (TILE_SIZE * noOfTilesToBeDrawnToTheLeftOfCenterTile);
                var yNWStartPixel = yPixelsFromTopEdgeOfDrawingAreaToTopEdgeOfCenterTile -
                                     (TILE_SIZE * noOfTilesToBeDrawnAboveOfCenterTile);

                var noOfTilesToBeDrawnHorizontally = (int)Math.Ceiling((double)drawRect.Width / TILE_SIZE + 1);
                var noOfTilesToBeDrawnVertically = (int)Math.Ceiling((double)drawRect.Height / TILE_SIZE + 1);
                var startXTile = xTileOfCenter - noOfTilesToBeDrawnToTheLeftOfCenterTile;
                var startYTile = yTileOfCenter - noOfTilesToBeDrawnAboveOfCenterTile;

                // Calculation to find out which region to be invalidated
                var northWestPoint = new Point(-drawRect.Width / 2, -drawRect.Height / 2);
                var southEastPoint = new Point(drawRect.Width / 2, drawRect.Height / 2);
                var northWestLocation = m_MapProjection.PixelToGPS(center, zoom, northWestPoint);
                var southEastLocation = m_MapProjection.PixelToGPS(center, zoom, southEastPoint);
                var regionToBeInvalidated = new GPSBounds(northWestLocation, southEastLocation);

                // We have calculated the start tile, that is the tile in the north-west corner of the drawing area. 
                // Now we will iterate left to right and top to bottom so that all tiles is either drawn or downloaded.
                for (var x = 0; x < noOfTilesToBeDrawnHorizontally; x++)
                {
                    for (var y = 0; y < noOfTilesToBeDrawnVertically; y++)
                    {
                        long tileXToBeDrawn = startXTile + x;
                        long tileYToBeDrawn = startYTile + y;
                        long tileYToBeDrawnProvider = (long)Math.Pow(2, zoomLevel) - 1 - tileYToBeDrawn;
                        long ix = xNWStartPixel + x * TILE_SIZE;
                        long iy = yNWStartPixel + y * TILE_SIZE;

                        tiles.Add(new MapTileInfo(ix, iy, tileXToBeDrawn, tileYToBeDrawnProvider, zoomLevel, TILE_SIZE, TILE_SIZE, regionToBeInvalidated));
                    }
                }
            }

            return tiles;
        }

        /// <summary>
        /// This method downloads a tile and saves it to disk.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="regionToBeInvalidated"></param>
        /// <param name="listener"></param>
        private bool QueueDownload(QueueInfo queueInfo)
        {
            bool queued = false;

            if (!m_DownloadQueueItems.ContainsKey(tileId(queueInfo.tileInfo)))
            {
                queued = true;
                m_DownloadQueueItems.Add(tileId(queueInfo.tileInfo), "");
                ThreadPool.QueueUserWorkItem(new WaitCallback(DownloadWorker), queueInfo);
            }
            return queued;
        }

        private void DownloadWorker(object queueInfoObject)
        {
            QueueInfo queueInfo = queueInfoObject as QueueInfo;
            try
            {
                lock (STWebClient.Instance)
                {
                    if (m_DownloadQueueItems.ContainsKey(tileId(queueInfo.tileInfo)))
                    {
                        string url = tileUrl(queueInfo.tileInfo);

                        STWebClient.Instance.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:37.0) Gecko/20100101 Firefox/37.0";
                        Image img = Image.FromStream(STWebClient.Instance.OpenRead(url));
                        img.Save(GetFilePath(queueInfo.tileInfo.pixTileX, queueInfo.tileInfo.pixTileY, queueInfo.tileInfo.zoomlevel, true));
                        img.Dispose();
                    }
                }

                // Invalidate the region of the drawing area
                m_operOnMainThread.Post(new SendOrPostCallback(InvalidateRegion), queueInfo);
            }
            catch (Exception e)
            {
            }
            m_DownloadQueueItems.Remove(tileId(queueInfo.tileInfo));
        }

        private void InvalidateRegion(object queueInfoObject)
        {
            QueueInfo queueInfo = queueInfoObject as QueueInfo;
#if ST_2_1
            queueInfo.listener.NotifyMapImageReady(queueInfo.tileInfo.regionToBeInvalidated);
#else
            queueInfo.listener.InvalidateRegion(queueInfo.tileInfo.regionToBeInvalidated);
#endif
        }

        /// <summary>
        /// Opens an image using Image.FromStream
        /// </summary>
        /// <param name="previewFile"></param>
        /// <returns></returns>
        public Image OpenImage(string previewFile)
        {
            var fs = new FileStream(previewFile, FileMode.Open, FileAccess.Read);
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
            return File.Exists(GetFilePath(xTile, yTile, zoomLevel));
        }

        /// <summary>
        /// Gets the path to the directory where the cached tiles are stored.
        /// </summary>
        /// <param name="xTile"></param>
        /// <param name="yTile"></param>
        /// <param name="zoomLevel"></param>
        /// <param name="createDir"></param>
        /// <returns></returns>
        private string GetFilePath(long xTile, long yTile, double zoomLevel, bool createDir)
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
        private string GetFilePath(long xTile, long yTile, double useZoomLevel)
        {
            return GetFilePath(xTile, yTile, useZoomLevel, false);
        }

        /// <summary>
        /// Gets an image that is cached on disk
        /// </summary>
        /// <param name="xTile"></param>
        /// <param name="yTile"></param>
        /// <param name="zoomLevel"></param>
        /// <returns></returns>
        private Image GetImageFromCache(long xTile, long yTile, double zoomLevel)
        {
            var str = GetFilePath(xTile, yTile, zoomLevel);

            try
            {
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