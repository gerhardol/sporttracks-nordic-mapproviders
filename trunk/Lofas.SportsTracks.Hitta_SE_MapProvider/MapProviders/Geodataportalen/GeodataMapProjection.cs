using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Remoting.Messaging;
using System.Text;
using ZoneFiveSoftware.Common.Data.GPS;
using ZoneFiveSoftware.Common.Visuals.Mapping;

namespace Lofas.SportsTracks.Hitta_SE_MapProvider.MapProviders.Geodata
{
    public class GeodataMapProjection : IMapProjection
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="zoomLevel"></param>
        /// <param name="gps"></param>
        /// <returns></returns>
        public System.Drawing.Point GPSToPixel(ZoneFiveSoftware.Common.Data.GPS.IGPSLocation origin, double zoomLevel, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation gps)
        {
            Point p = new Point(0,0);
            return p;
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="zoomLevel"></param>
        /// <param name="pixel"></param>
        /// <returns></returns>
        public ZoneFiveSoftware.Common.Data.GPS.IGPSLocation PixelToGPS(ZoneFiveSoftware.Common.Data.GPS.IGPSLocation origin, double zoomLevel, System.Drawing.Point pixel)
        {
            GPSLocation l = new GPSLocation(0,0);
            return l;
            //throw new NotImplementedException();
        }
    }
}
