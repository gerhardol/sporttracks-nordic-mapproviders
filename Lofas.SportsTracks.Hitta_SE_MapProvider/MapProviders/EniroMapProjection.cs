using System;
using System.Diagnostics;
using System.Drawing;
using ZoneFiveSoftware.Common.Data.GPS;
using ZoneFiveSoftware.Common.Visuals.Mapping;

namespace Lofas.SportsTracks.Hitta_SE_MapProvider
{
    public class EniroMapProjection : IMapProjection
    {
        // Här ska vi konvertera från GPS till Pixel och vice versa. Vi får in en original GPS-position och en gps position och en zoomnivå. 
        // Då ska vi räkna ut vilken punkt gpspositionen med utgångspunkt från originalpositionen. 
        // Följande GPS-pos skickas in 60,63974;16,96524 och det ger punkten -186;-65

        #region IMapProjection Members

        private const int ENIRO_MAX_ZOOMLEVEL = 20;
        public Point GPSToPixel(IGPSLocation origin, double zoomLevel, IGPSLocation gps)
        {
            zoomLevel = ENIRO_MAX_ZOOMLEVEL - zoomLevel;
            Debug.Print("GPSToPixel: " + zoomLevel);
            long dx;
            long dy;
            try
            {
                long originX = Xpixel(origin.LongitudeDegrees, zoomLevel);
                long originY = Ypixel(origin.LatitudeDegrees, zoomLevel);
                long gpsX = Xpixel(gps.LongitudeDegrees, zoomLevel);
                long gpsY = Ypixel(gps.LatitudeDegrees, zoomLevel);

                dx = gpsX - originX;
                dy = gpsY - originY;
                Debug.Print("GPSToPixel:  " + dx + "/" + dy + "/" + gps.LongitudeDegrees + "/" + gps.LatitudeDegrees + "/" + zoomLevel);
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                throw e;
            }
            return new Point((int)dx, (int)dy);
        }

        public IGPSLocation PixelToGPS(IGPSLocation origin, double zoomLevel, Point pixel)
        {
            zoomLevel = ENIRO_MAX_ZOOMLEVEL - zoomLevel;
            Debug.Print("PixelToGPS: " + zoomLevel);
            float latitude;
            float longitude;
            try
            {
                long originX = Xpixel(origin.LongitudeDegrees, zoomLevel);
                long originY = Ypixel(origin.LatitudeDegrees, zoomLevel);

                long pixelX = originX + pixel.X;
                long pixelY = originY + pixel.Y;

                latitude = YToLat(pixelY, zoomLevel);
                longitude = XToLong(pixelX, zoomLevel);
                Debug.Print("PixelToGPS:  " + latitude + " / " + longitude + " / " + pixel.Y + "/" + pixel.X + "/" + zoomLevel);
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                throw e;
            }
            return new GPSLocation(latitude, longitude);
        }

        #endregion

        public long Xpixel(double lng, double zoom)
        {
            if (zoom > ENIRO_MAX_ZOOMLEVEL)
            {
                Debug.Print("För stor zoomnivå " + zoom);
                return 0;
            }

            // Instead of -180 to +180, we want 0 to 360
            double dlng = lng + 180;
            double dxpixel = dlng / 360.0 * 256 * Math.Pow(2, zoom);
            long xpixel = Convert.ToInt32(Math.Floor(dxpixel));
            return xpixel;
        }

        public long Ypixel(double lat, double zoom)
        {
            if (zoom > ENIRO_MAX_ZOOMLEVEL)
            {
                Debug.Print("För stor zoomnivå " + zoom);
                return 0;
            }
            // The 25 comes from 17 + (256=&gt;2^8=&gt;8) 17+8 = 25
            // ypixelcenter = the middle y pixel (the equator) at this zoom level
            double ypixelcenter = Math.Pow(2, zoom - 1);

            // PI/360 == degrees -&gt; radians
            // The trig functions are done with radians
            double dypixel = 256 * (ypixelcenter - Math.Log(Math.Tan(lat * Math.PI / 360 + Math.PI / 4)) * ypixelcenter / Math.PI);
            long ypixel = Convert.ToInt32(Math.Floor(dypixel));
            return ypixel;
        }

        public int XTile(double lng, double zoom)
        {
            return Convert.ToInt32(Math.Floor((double)Xpixel(lng, zoom) / 256));
        }

        public int YTile(double lat, double zoom)
        {
            return Convert.ToInt32(Math.Floor((double)Ypixel(lat, zoom) / 256));
        }

        public long Pixel_NW_OfTile(long tile)
        {
            return tile * 256;
        }

        public float YToLat(long y, double zoom)
        {
            double w = Math.Pow(2, zoom - 1);
            double dy = Convert.ToDouble(y);
            double num0 = Math.PI * (w - dy / 256);
            double num1 = Math.Atan(Math.Exp(num0 / w)) - Math.PI / 4;
            double latitude = 360 * num1 / Math.PI;
            return (float)latitude;
        }

        public float XToLong(long X, double zoom)
        {
            double dx = Convert.ToDouble(X);

            return (float) ((360*dx)/(256*Math.Pow(2, zoom)) - 180);
//            return (float) ((360*dx)/(256*Math.Pow(2, 28 - zoom)) - 180);
        }

        public static bool isValidPoint(IGPSLocation gps)
        {
            return isValidPoint(gps.LatitudeDegrees, gps.LongitudeDegrees);
        }

        public static bool isValidPoint(double lat, double lon)
        {
            //Approx Swedish coordinates
            //Hitta.se has a few overview maps for Scandinavia, but nothing useful
            if (lat < 55 || lat > 70 || lon < 10 || lon > 25)
            {
                return false;
            }
            return true;
        }
    }
}
