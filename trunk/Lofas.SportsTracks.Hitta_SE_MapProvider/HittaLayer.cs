using System;
using System.Collections.Generic;
using System.Text;
using SharpMap.Layers;
using System.Net;
using System.Drawing;
using System.IO;

namespace Lofas.SharpMapLayers
{
    public class HittaLayer : ILayer
    {
        private double m_MinVisible = 0;
        private double m_MaxVisible = Double.MaxValue;
        private bool m_Enabled = true;
        int m_SRID = -1;

        string m_LayerName;
        #region ILayer Members

        /*
         *  zoomLevels: [0.2,0.8,2,4,10,25,70,200,700,3500],
         * 
         * 
         * this.position = function(dx,dy) {
1195 var res = StarMap.zoomLevels[StarMap.zoomIndex];
1196 this.left = (StarMap.mapWidth >> 1) - ((StarMap.numTilesX << 8) >> 1) + (this.col << 8);
1197 this.top = (StarMap.mapHeight >> 1) - ((StarMap.numTilesY << 8) >> 1) + (this.row << 8);
1198
1199 var cTileX = Math.floor(StarMap.cx/(res*256))*(res*256);
1200 var cTileY = Math.ceil(StarMap.cy/(res*256))*(res*256);
1201
1202 var x = cTileX - (StarMap.numTilesX >> 1)*(res *256) + this.col*(res * 256);
1203 var y = cTileY + (StarMap.numTilesY >> 1)*(res * 256) - this.row*(res * 256);
1204
1205 this.sqX = Math.round(x / (res * 256));
1206 this.sqY = Math.round(y / (res * 256));
1207
1208 this.setImg();
1209 this.left += dx;
1210 this.top += dy;
1211 this.div.style.top = this.top + "px";
1212 this.div.style.left = this.left + "px";
1213 };
         */
        double[] starMapZoomLevels = new double[] { 0.2, 0.8, 2, 4, 10, 25, 70, 200, 700, 3500 };
        WebClient m_WebClient = new WebClient();
        public void Render(System.Drawing.Graphics g, global::SharpMap.Map map)
        {
            double res = 2;

            double ddRes = Math.Abs(map.PixelSize - 2);

            /* Optimize res*/
            foreach (double rrest in starMapZoomLevels)
            {
                double dpx = Math.Abs(map.PixelSize - rrest);
                if (dpx < ddRes)
                {
                    ddRes = dpx;
                    res = rrest;
                }
            }

            int ulTileX = (int)Math.Floor(map.Envelope.Left / (res * 256));
            int ulTileY = (int)Math.Ceiling(map.Envelope.Top / (res * 256));



            System.Globalization.CultureInfo cInfo = new System.Globalization.CultureInfo("en-US");
            int ty = ulTileY;
            while (ty * res * 256 > map.Envelope.Min.Y)
            {
                int tx = ulTileX;
                while (tx * res * 256 < map.Envelope.Max.X)
                {
                    PointF ul = map.WorldToImage(new SharpMap.Geometries.Point(tx * res * 256, ty * res * 256));
                    PointF lr = map.WorldToImage(new SharpMap.Geometries.Point((tx + 1) * res * 256, (ty - 1) * res * 256));

                    string url = "http://karta.hitta.se/starmap/ImgServlet?res=" + res.ToString(cInfo) + "&sqx=" + tx.ToString() + "&sqy=" + ty.ToString() + "&source=1";

                    Image img = getCachedImage(res, tx, ty);
                    if (img == null)
                    {
                        if (!Directory.Exists("cache"))
                            Directory.CreateDirectory("cache");
                        img = new System.Drawing.Bitmap(m_WebClient.OpenRead(url));
                        img.Save("cache\\res_" + res.ToString() + "tx_" + tx + "ty_" + ty + ".gif");
                    }
                    g.DrawImage(img, (float)Math.Floor(ul.X), (float)Math.Floor(ul.Y), (float)Math.Ceiling(lr.X - ul.X), (float)Math.Ceiling(lr.Y - ul.Y));
                    tx++;
                    img.Dispose();
                    //ul = map.WorldToImage(new SharpMap.Geometries.Point(tx * res * 256, ty * res * 256));
                    //lr = map.WorldToImage(new SharpMap.Geometries.Point((tx + 1) * res * 256, (ty - 1) * res * 256));
                }
                ty--;
            }

        }

        Image getCachedImage(double res, int tx, int ty)
        {
            Image img = null;
            if (Directory.Exists("cache"))
            {
                string filename = "cache\\" + "res_" + res.ToString() + "tx_" + tx + "ty_" + ty + ".gif";
                if (File.Exists(filename))
                {
                    return Image.FromFile(filename);
                }
            }
            return img;
        }

        public double MinVisible
        {
            get
            {
                return m_MinVisible;
            }
            set
            {
                m_MinVisible = value;
            }
        }

        public double MaxVisible
        {
            get
            {
                return m_MaxVisible;
            }
            set
            {
                m_MaxVisible = value;
            }
        }

        public bool Enabled
        {
            get
            {
                return m_Enabled;
            }
            set
            {
                m_Enabled = value;
            }
        }

        public string LayerName
        {
            get
            {
                return m_LayerName;
            }
            set
            {
                m_LayerName = value;
            }
        }

        public global::SharpMap.Geometries.BoundingBox Envelope
        {
            get { return new global::SharpMap.Geometries.BoundingBox(1158000, 6123000, 1795000,7691000); }
        }

        public int SRID
        {
            get
            {
                return m_SRID;
            }
            set
            {
                m_SRID = value;
            }
        }

        #endregion
    }
}
