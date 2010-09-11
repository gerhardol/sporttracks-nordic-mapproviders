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
#if ST_2_1
using ZoneFiveSoftware.Common.Visuals.Fitness.GPS;
#else
using ZoneFiveSoftware.Common.Visuals.Mapping;
#endif

namespace Lofas.SportsTracks.Hitta_SE_MapProvider
{
    public class Eniro_SE_MapProjection : IMapProjection
    {
        const int tileX2 = 128;
        const int tileY2 = 128;
        const float dotSize = 0.0254F; // 0.000265 == meter/dpi
        private static readonly double[] scaleValues = { 1000, 2000, 4000, 8200, 16000, 57000, 240000, 1000000, 4000000, 1.6384E7 };
        //public readonly double[] old_scaleValues = { 1000, 2000, 4000, 8200, 20000, 57000, 240000, 500000, 4000000, 20800000 };
        private readonly string m_CacheDirectory;
        private readonly string m_reqUrlBase;
        private Dictionary<int, CacheTileInfo> m_TileInfoCache = new Dictionary<int, CacheTileInfo>();

        public Eniro_SE_MapProjection(string m_CacheDirectory, string m_reqUrlBase)
        {
            this.m_CacheDirectory = m_CacheDirectory;
            this.m_reqUrlBase = m_reqUrlBase;
        }

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
            b = b - (0.75 * F - 3 * Math.Pow(F, 3) / tileY2) * Math.Sin(2 * fi);
            b = b + (15 * Math.Pow(F, 2) / 64 + 15 * Math.Pow(F, 3) / tileY2) * Math.Sin(4 * fi);
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

        public static int getZoomIndex(double useZoomLevel)
        {
            return Convert.ToInt32(scaleValues.Length - Array.IndexOf(scaleValues, useZoomLevel));
        }

        private static double getMetersPerPixel(double zoomLevel, out double tileMeterPerPixel, out double eniroscale)
        {
            int zoomLevelIndex = (int)Math.Floor(zoomLevel);
            if (zoomLevelIndex < 0) { zoomLevelIndex = 0; }
            if (zoomLevelIndex >= scaleValues.Length) { zoomLevelIndex = scaleValues.Length - 1; }

            int level1 = zoomLevelIndex;
            int level2 = zoomLevelIndex + 1;

            double useScale = scaleValues[zoomLevelIndex];
            double useZoomLevel = useScale * dotSize / tileX2; 
            tileMeterPerPixel = useZoomLevel;

            //Possibly adjust the values from the "fractional" rest part of the zoom
            double zoomLevelRest = zoomLevel - zoomLevelIndex;
            if (zoomLevelRest > 0.5 && zoomLevelIndex < scaleValues.Length - 1)
            {
                useScale = scaleValues[zoomLevelIndex + 1];
                tileMeterPerPixel = useScale * 0.000265;
            }

            if (zoomLevelIndex < scaleValues.Length - 1 - 1 && zoomLevelRest > 1e-6)
                useZoomLevel += (zoomLevelRest * (scaleValues[level2] * dotSize / tileX2 - scaleValues[level1] * dotSize / tileX2));

            eniroscale = useScale;

            return useZoomLevel;
        }

        //This method contains info about providers, that is hard to separate
        public void getScaleInfo(double zoomLevel, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation gps, out double refTileULX, out double refTileULY, out double tileWMetersPerPixel, out double tileHMetersPerPixel, out int refTileXOffset, out int refTileYOffset, out double lengthDegreesX, out double lengthDegreesY, out double centerLat, out double centerLon)
        {
            double useScale;
            getScaleInfo(zoomLevel, gps, out useScale, out refTileULX, out refTileULY, out tileWMetersPerPixel, out tileHMetersPerPixel, out refTileXOffset, out refTileYOffset, out lengthDegreesX, out lengthDegreesY, out centerLat, out centerLon);
        }
        public void getScaleInfo(double zoomLevel, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation gps, out double useScale, out double refTileULX, out double refTileULY, out double tileWMetersPerPixel, out double tileHMetersPerPixel, out int refTileXOffset,out int refTileYOffset, out double lengthDegreesX, out double lengthDegreesY, out double centerLat, out double centerLon)
        {
            
            double tileMeterPerPixel;
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


                    string reqUrl = m_reqUrlBase + 
                        lat.ToString(CultureInfo.InvariantCulture) + "&c0-e2=number%3A" + 
                        lon.ToString(CultureInfo.InvariantCulture) + 
                        "&c0-e3=string%3Awgs84&c0-param3=Object_Wgs84GeoCoord%3A%7Bx%3Areference%3Ac0-e1%2C%20y%3Areference%3Ac0-e2%2C%20crs%3Areference%3Ac0-e3%7D&c0-param4=number%3A" + 
                        (scaleValues.Length - Array.IndexOf(scaleValues, useScale)) + 
                        "&c0-param5=string%3A&c0-param6=null%3Anull&c0-param7=number%3A0&c0-param8=number%3A0&batchId=0";
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
                    catch (Exception)
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

                    double wMeterPerPixel = (tileLRX_M - tileUlX_M) / (float)(2*tileX2);
                    double hMeterPerPixel = (tileUlY_M - tileLRY_M) / (float)(2*tileY2);
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
        }

        public System.Drawing.Point GPSToPixel(ZoneFiveSoftware.Common.Data.GPS.IGPSLocation origin, double zoomLevel, ZoneFiveSoftware.Common.Data.GPS.IGPSLocation gps)
        {
            int dx, dy;
            double lengthDegreesX, lengthDegreesY;
            try
            {
                double refTileULX, refTileULY, tileWMetersPerPixel, tileHMetersPerPixel, centerLat, centerLon;
                int refTileXOffset, refTileYOffset;
                getScaleInfo(zoomLevel, gps, out refTileULX, out refTileULY, out tileWMetersPerPixel, out tileHMetersPerPixel, out refTileXOffset, out refTileYOffset, out lengthDegreesX, out lengthDegreesY, out centerLat, out centerLon);

                dx = (int)Math.Round((gps.LongitudeDegrees - origin.LongitudeDegrees) / lengthDegreesX * (2 * tileX2));
                dy = (int)Math.Round((origin.LatitudeDegrees - gps.LatitudeDegrees) / lengthDegreesY * 2 * tileX2);
            }
            catch (Exception ee)
            {
                throw new ApplicationException("Eniro-Server changed!", ee);
            }

            return new System.Drawing.Point(dx, dy);
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

            double dx = pixel.X / (float)(2 * tileX2) * lengthDegreesX;
            double dy = pixel.Y / (float)(2 * tileY2) * lengthDegreesY;

            return new ZoneFiveSoftware.Common.Data.GPS.GPSLocation((float)(origin.LatitudeDegrees-dy),(float)(origin.LongitudeDegrees+dx));
        }

        #endregion
    }
}
