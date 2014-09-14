using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ZoneFiveSoftware.Common.Visuals.Mapping;

namespace Lofas.SportsTracks.Hitta_SE_MapProvider.MapProviders.Geodata
{
    public class GeodataMapProvider : IMapTileProvider
    {
        private readonly Dictionary<string, string> m_DownloadQueueItems;
        private readonly Guid m_GUID;

        private readonly GeodataMapProjection m_MapProjection = new GeodataMapProjection();

        public GeodataMapProvider()
        {
            m_GUID = new Guid("5836b0ee-ff65-4bb3-8736-bd2eb764e446");
        }
       
        public int DrawMap(IMapImageReadyListener readyListener, System.Drawing.Graphics graphics, System.Drawing.Rectangle drawRect, System.Drawing.Rectangle clipRect, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation center, double zoom)
        {
            //throw new NotImplementedException();
            Debug.Print(zoom + center.ToString());
            return 0;
        }
        
        public int DownloadQueueSize
        {
            get { return m_DownloadQueueItems.Count; }
        }

        public Guid Id
        {
            get { return m_GUID; }
        }

        public IMapProjection MapProjection
        {
            get { return m_MapProjection; }
        }

        public double MaximumZoom
        {
            get { return 12; }
        }

        public double MinimumZoom
        {
            get { return 0; }
        }

        public string Name
        {
            get { return "Geodataportalen"; }
        }

        /// <summary>
        /// Implement code for handle refresh
        /// </summary>
        /// <param name="drawRectangle"></param>
        /// <param name="center"></param>
        /// <param name="zoomLevel"></param>
        public void Refresh(System.Drawing.Rectangle drawRectangle, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation center, double zoomLevel)
        {

            //throw new NotImplementedException();
        }

        public bool SupportsFractionalZoom
        {
            get { return false; }
        }
    }
}
