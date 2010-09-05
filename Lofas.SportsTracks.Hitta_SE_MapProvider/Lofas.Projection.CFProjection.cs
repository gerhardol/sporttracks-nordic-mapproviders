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



namespace Lofas.Projection
{
    /// <summary>
    /// Projection library for .NET Compact Framework and .NET Framework, uses no exernal libraries
    /// 
    /// Peter Löfås 2007
    /// Triona AB
    /// </summary>
    public class CFProjection
    {
        #region _CONSTANTS_
        private static double deg2rad = Math.PI / 180;
        private static double rad2deg = 180.0 / Math.PI;

        /* Ellipsoid parameters, default to WGS 84 */
        static double Geocent_a = 6378137.0;     /* Semi-major axis of ellipsoid in meters */
        static double Geocent_b = 6356752.3142;  /* Semi-minor axis of ellipsoid           */

        static double Geocent_a2 = 40680631590769.0;        /* Square of semi-major axis */
        static double Geocent_b2 = 40408299984087.05;       /* Square of semi-minor axis */
        static double Geocent_e2 = 0.0066943799901413800;   /* Eccentricity squared  */
        static double Geocent_ep2 = 0.00673949675658690300; /* 2nd eccentricity squared */


         private static double HALFPI	=	1.5707963267948966;
        private static double FORTPI	=	0.78539816339744833;
        private static double PI		=3.14159265358979323846;
        private static double TWOPI	=	6.2831853071795864769;
        private static double PI_OVER_2 =(PI / 2.0e0);
        private static double FALSE     = 0;
        private static double TRUE      = 1;
        private static double COS_67P5  = 0.38268343236508977;  /* cosine of 67.5 degrees */
        private static double AD_C      = 1.0026000;            /* Toms region 1 constant */
        private static double FC1 =1.0;
        private static double FC2 =.5;
        private static double FC3 =.16666666666666666666;
        private static double FC4 =.08333333333333333333;
        private static double FC5 =.05;
        private static double FC6 =.03333333333333333333;
        private static double FC7 =.02380952380952380952;
        private static double FC8 =.01785714285714285714;
        private static double SPI  =   3.14159265359;
        private static double ONEPI  = 3.14159265358979323846;

        private static double EPS = 1.0e-12;

        #endregion

        private static double[] rt90_DatumParams = {414.0978567149,
        41.3381489658,603.0627177516,
        -4.1453675348556307e-006,
        1.0381540791960210e-005,
        -3.4047111959502597e-005,
        1.0000009999999999};

        

        /*
        ** RES is fractional second figures
        ** RES60 = 60 * RES
        ** CONV = 180 * 3600 * RES / PI (radians to RES seconds)
        */
	    static double
            RES = 1000.0,
            RES60 = 60000.0,
            CONV = 206264806.24709635515796003417;

             private static double[] vm = {
	            .0174532925199433,
	            .0002908882086657216,
	            .0000048481368110953599
            };

        /// <summary>
        /// Transform from WGS84 Coordinates to Sweref99TM projection
        /// </summary>
        /// <param name="lat">Easting in WGS84</param>
        /// <param name="lon">Northing in WGS84</param>
        /// <param name="height">Height in WGS84</param>
        /// <param name="swref99X">Sweref99TM X (Easting)</param>
        /// <param name="sweref99Y">Sweref99TM Y (Northing)</param>
        public static void WGS84ToSWEREF99TM(double lat, double lon, double height, out double swref99X, out double sweref99Y)
        {
            double y = lat * deg2rad;
            double x = lon * deg2rad;
            double z = height;
            double lam = 0, phi = 0, h = 0;
            double src_a, src_es, dst_a, dst_es;
            double lam0, k0, esp, ml0, fr_meter, x0, y0;
            double[] en;

            src_a = 6378137.0000000000;
            src_es = 0.0066943799901413165;
            dst_a = 6378137.0000000000;
            dst_es = 0.0066943799901413165;

            lam0 = 0.26179938779914946;
            k0 = 0.99960000000000004;
            x0 = 500000.00000000000;
            y0 = 0.0;
            fr_meter = 1.0;
            esp = 0.0067192187991747592;
            en = new double[5];
            en[0] = 0.99832429843134363;
            en[1] = 0.0050186784214849862;
            en[2] = 2.1002980978867826e-005;
            en[3] = 1.0936603392140145e-007;
            en[4] = 6.1780588184216711e-010;

            ml0 = 0.0;

            phi = x;
            lam = y;
            project(ref x, ref y, ref phi, ref lam, lam0, dst_es, k0, esp, ml0, fr_meter, dst_a, x0, y0, en);
            swref99X = x;
            sweref99Y = y;
        }

        /// <summary>
        /// Convert from SWEREF99TM to RT90
        /// </summary>
        /// <param name="x">X (Easting)</param>
        /// <param name="y">Y (Northing)</param>
        public static void SWEREF99TMToRT90(ref double x, ref double y)
        {
            double lat, lon;
            CFProjection.SWEREF99TMToWGS84(x, y, out lat, out lon);
            CFProjection.WGS84ToRT90(lon, lat, 0, out x, out y);
        }
        /// <summary>
        /// Convert from SWEREF99TM to RT90
        /// </summary>
        /// <param name="x">X (Easting)</param>
        /// <param name="y">Y (Northing)</param>
        public static void RT90ToSWEREF99TM(ref double x, ref double y)
        {
            double lat, lon;
            CFProjection.RT90ToWGS84(x, y, out lat, out lon);
            CFProjection.WGS84ToSWEREF99TM(lat, lon, 0, out x, out y);
        }

        /// <summary>
        /// Convert from SWEREF99TM to WGS84 Lat Lon
        /// </summary>
        /// <param name="x">SWEREF99TM X (Easting)</param>
        /// <param name="y">SWEREF99TM Y (Northing)</param>
        /// <param name="lat">WGS84 Lat (easting)</param>
        /// <param name="lon">WGS84 Lon (northing)</param>
        public static void SWEREF99TMToWGS84(double x, double y, out double lat, out double lon)
        {
            double lam = 0, phi = 0, h = 0;
            double src_a, src_es, dst_a, dst_es;
            double lam0, k0, esp, ml0, fr_meter, to_meter, x0, y0;
            double[] en;
            double ra;

            lat = lon = 0;

            ra = 1.5678559428873979e-007;

            dst_a = 6378137.0000000000;
            dst_es = 0.0066943799901413165;
            src_a = 6378137.0000000000;
            src_es = 0.0066943799901413165;



            lam0 = 0.26179938779914946;
            k0 = 0.99960000000000004;
            x0 = 500000.0000000000;
            y0 = 0.0;
            fr_meter = 1.0;
            to_meter = 1.0;
            esp = 0.0067394967422764341;
            en = new double[5];
            en[0] = 0.99832429843134363;
            en[1] = 0.0050186784214849862;
            en[2] = 2.1002980978867826e-005;
            en[3] = 1.0936603392140145e-007;
            en[4] = 6.1780588184216711e-010;

            ml0 = 0;

            //Transform source to LatLong
            x = (x * to_meter - x0) * ra;
            y = (y * to_meter - y0) * ra;

            phi = inv_mlfn(ml0 + y / k0, src_es, en);
            lam = 0;
            if (Math.Abs(phi) >= HALFPI)
            {
                phi = y < 0 ? -HALFPI : HALFPI;
                lam = 0;
            }
            else
            {
                double sinphi = Math.Sin(phi);
                double cosphi = Math.Cos(phi);
                double t = Math.Abs(cosphi) > 1e-10 ? sinphi / cosphi : 0;
                double n = esp * cosphi * cosphi;
                double con = 1 - src_es * sinphi * sinphi;
                double d = x * Math.Sqrt(con) / k0;
                con *= t;
                t *= t;
                double ds = d * d;

                phi -= (con * ds / (1.0 - src_es)) * FC2 * (1.0 -
                    ds * FC4 * (5.0 + t * (3.0 - 9.0 * n) + n * (1.0 - 4 * n) -
                    ds * FC6 * (61.0 + t * (90.0 - 252.0 * n +
                    45.0 * t) + 46.0 * n
                    - ds * FC8 * (1385.0 + t * (3633.0 + t * (4095.0 + 1574.0 * t)))
                    )));
                lam = d * (FC1 -
                    ds * FC3 * (1.0 + 2.0 * t + n -
                    ds * FC5 * (5.0 + t * (28.0 + 24.0 * t + 8.0 * n) + 6.0 * n
                    - ds * FC7 * (61.0 + t * (662.0 + t * (1320.0 + 720.0 * t)))
                    ))) / cosphi;

            }

            lam += lam0;
            lam = adjlon(lam);

            x = lam;
            y = phi;

            lat = x * rad2deg;
            lon = y * rad2deg;
        }

        /// <summary>
        /// Convert RT90 coordinates to WGS84
        /// Uses a iterative method to calculate datum tranformation
        /// </summary>
        /// <param name="x">RT90 Easting</param>
        /// <param name="y">RT90 Northing</param>
        /// <param name="lat">WGS84 Easting</param>
        /// <param name="lon">WGS84 Northing</param>
        public static void RT90ToWGS84(double x, double y, out double lat, out double lon)
        {
            double lam=0, phi=0, h=0;
            double src_a, src_es, dst_a, dst_es;
            double lam0, k0, esp, ml0, fr_meter, to_meter, x0, y0;
            double[] en;
            double ra;

            lat = lon = 0;

            ra = 1.5680378306312334e-007;

            dst_a = 6378137.0000000000;
            dst_es = 0.0066943799901413165;
            src_a = 6377397.1550000003;
            src_es = 0.0066743722318021448;
            

            lam0 = 0.27590649629595326;
            k0 = 1.0000000000000000;
            x0 = 1500000.0000000000;
            y0 = 0.0;
            fr_meter = 1.0;
            to_meter = 1.0;
            esp = 0.0067192187991747592;
            en = new double[5];
            en[0] = 0.99832931296163163;
            en[1] = 0.0050036851934337247;
            en[2] = 2.0877635399069992e-005;
            en[3] = 1.0838839586912615e-007;
            en[4] = 6.1045308393109063e-010;

            ml0 = 0.0;
            //Transform source to LatLong
            x = (x * to_meter - x0) * ra;
            y = (y * to_meter - y0) * ra;

            double s;
            phi = inv_mlfn(ml0 + y / k0, src_es, en);
            lam = 0;
            if (Math.Abs(phi) >= HALFPI)
            {
                phi = y < 0 ? -HALFPI : HALFPI;
                lam = 0;
            }
            else
            {
                double sinphi = Math.Sin(phi);
                double cosphi = Math.Cos(phi);
                double t = Math.Abs(cosphi) > 1e-10 ? sinphi / cosphi : 0;
                double n = esp * cosphi * cosphi;
                double con = 1 - src_es*sinphi*sinphi;
                double d = x * Math.Sqrt(con) / k0;
                con *= t;
                t *= t;
                double ds = d * d;

                phi -= (con * ds / (1.0 - src_es)) * FC2 * (1.0 -
                    ds * FC4 * (5.0 + t * (3.0 - 9.0 * n) + n * (1.0 - 4 * n) -
                    ds * FC6 * (61.0 + t * (90.0 - 252.0 * n +
                    45.0 * t) + 46.0 * n
                    - ds * FC8 * (1385.0 + t * (3633.0 + t * (4095.0 + 1574.0 * t)))
                    )));
                lam = d * (FC1 -
                    ds * FC3 * (1.0 + 2.0 * t + n -
                    ds * FC5 * (5.0 + t * (28.0 + 24.0 * t + 8.0 * n) + 6.0 * n
                    - ds * FC7 * (61.0 + t * (662.0 + t * (1320.0 + 720.0 * t)))
                    ))) / cosphi;

            }

            lam += lam0;
            lam = adjlon(lam);

            x = lam;
            y = phi;
            double z = 0;

            geodetic_to_geocentric(src_a, src_es, ref x, ref y, ref z);
            geocentric_to_wgs84(ref x, ref y, ref z, rt90_DatumParams);

            geocentric_to_geodetic(dst_a, dst_es, ref x, ref y, ref z);

            lat = x * rad2deg;
            lon = y * rad2deg;
        }

        /// <summary>
        /// Convert Coordinates in WGS84 (lat/long) to Swedish National Grid (RT90 2,5 gon West)
        /// Transforms 
        /// 1. geodetic->geocentric
        /// 2. Datum shift
        /// 3. geocentric -> geodetic (on new datum)
        /// 4. Projection to RT90 x,y
        /// </summary>
        /// <param name="lat">Latitude in decimal degrees</param>
        /// <param name="lon">Longitude in decimal degrees</param>
        /// <param name="height">Height over ellipsoid</param>
        /// <param name="rt90X">Outputs the corresponding x coordinate</param>
        /// <param name="rt90Y">Outputs the corresponding y coordinate</param>
        public static void WGS84ToRT90(double lat, double lon, double height, out double rt90X, out double rt90Y)
        {
            double y = lat * deg2rad;
            double x = lon * deg2rad;
            double z = height;
            double lam=0, phi=0, h=0;
            double src_a, src_es, dst_a, dst_es;
            double lam0, k0, esp, ml0, fr_meter, x0, y0;
            double[] en;

            src_a = 6378137.0000000000;
            src_es = 0.0066943799901413165;
            dst_a = 6377397.1550000003;
            dst_es = 0.0066743722318021448;

            lam0 = 0.27590649629595326;
            k0 = 1.0000000000000000;
            x0 = 1500000.0000000000;
            y0 = 0.0;
            fr_meter = 1.0;
            esp = 0.0067192187991747592;
            en = new double[5];
            en[0] = 0.99832931296163163;
            en[1] = 0.0050036851934337247;
            en[2] = 2.0877635399069992e-005;
            en[3] = 1.0838839586912615e-007;
            en[4] = 6.1045308393109063e-010;

            ml0 = 0.0;

            geodetic_to_geocentric(src_a, src_es, ref x,ref y,ref z);

            geocentric_from_wgs84(ref x, ref y, ref z, rt90_DatumParams);

            Convert_Geocentric_To_Geodetic(x, y, z, ref phi, ref lam, ref h, dst_a, dst_es);

            project(ref x, ref y, ref phi, ref lam, lam0, dst_es, k0, esp, ml0, fr_meter, dst_a, x0, y0, en);
            rt90X = x;
            rt90Y = y;
        }
        private static double inv_mlfn(double arg, double es, double[] en)
        {
            double s, t, phi, k = 1.0 / (1.0 - es);
            int i;
            phi = arg;
            for (i = 10; i > 0; --i)
            {
                s = Math.Sin(phi);
                t = 1.0 - es * s * s;
                t = (mlfn(phi, s, Math.Cos(phi), en) - arg) * (t * Math.Sqrt(t)) * k;
                phi -= t;
                if (Math.Abs(t) < EPS)
                {
                    return phi;
                }
            }
            return phi;
        }

        private static double mlfn(double phi, double sphi, double cphi, double[] en)
        {
            cphi *= sphi;
            sphi *= sphi;
            return (en[0] * phi - cphi * (en[1] + sphi * (en[2]
                + sphi * (en[3] + sphi * en[4]))));
        }

        private static double adjlon(double lon)
        {
            if (Math.Abs(lon) <= SPI) return (lon);
            lon += ONEPI;  /* adjust to 0..2pi rad */
            lon -= TWOPI * Math.Floor(lon / TWOPI); /* remove integral # of 'revolutions'*/
            lon -= ONEPI;  /* adjust back to -pi..pi rad */
            return (lon);
        }


        private static void project(ref double x, ref double y, ref double phi, ref double lam, double lam0, double es, double k0, double esp, double ml0, double fr_meter, double a, double x0, double y0, double[] en)
        {
            double al, als, n, cosphi, sinphi, t;

            if ((t = (double)Math.Abs(phi) - HALFPI) > EPS || (double)Math.Abs(lam) > 10.0)
            {

                /**ERROR, ej giltiga värden!**/

            }
            else
            { /* proceed with projection */
                if (Math.Abs(t) <= EPS)
                    phi = phi < 0.0 ? -HALFPI : HALFPI;

                lam -= lam0;	/* compute del lp.lam */
                lam = adjlon(lam); /* adjust del longitude */




                sinphi = Math.Sin(phi);
                cosphi = Math.Cos(phi);
                t = Math.Abs(cosphi) > 1e-10 ? sinphi / cosphi : 0.0;
                t *= t;
                al = cosphi * lam;
                als = al * al;
                al /= Math.Sqrt(1.0 - es * sinphi * sinphi);
                n = esp * cosphi * cosphi;
                x = k0 * al * (FC1 +
                    FC3 * als * (1.0 - t + n +
                    FC5 * als * (5.0 + t * (t - 18.0) + n * (14.0 - 58.0 * t)
                    + FC7 * als * (61.0 + t * (t * (179.0 - t) - 479.0))
                    )));
                y = k0 * (mlfn(phi, sinphi, cosphi, en) - ml0 +
                    sinphi * al * lam * FC2 * (1.0 +
                    FC4 * als * (5.0 - t + n * (9.0 + 4.0 * n) +
                    FC6 * als * (61.0 + t * (t - 58.0) + n * (270.0 - 330 * t)
                    + FC8 * als * (1385.0 + t * (t * (543.0 - t) - 3111.0))
                    ))));


                /* adjust for major axis and easting/northings */
                x = fr_meter * (a * x + x0);
                y = fr_meter * (a * y + y0);
            }
        }


        private static void Convert_Geocentric_To_Geodetic(double X,
                                     double Y,
                                     double Z,
                                     ref double Latitude,
                                     ref double Longitude,
                                     ref double Height, double a, double es)
        {

            double Geocent_a, Geocent_b, Geocent_a2, Geocent_b2, Geocent_e2, Geocent_ep2;

            double b;
            if (es == 0.0)
                b = a;
            else
                b = a * Math.Sqrt(1 - es);

            Geocent_a = a;
            Geocent_b = b;
            Geocent_a2 = a * a;
            Geocent_b2 = b * b;
            Geocent_e2 = (Geocent_a2 - Geocent_b2) / Geocent_a2;
            Geocent_ep2 = (Geocent_a2 - Geocent_b2) / Geocent_b2;

#if DONOTUSEITERATIVEMETHOD
    /* BEGIN Convert_Geocentric_To_Geodetic */
/*
 * The method used here is derived from 'An Improved Algorithm for
 * Geocentric to Geodetic Coordinate Conversion', by Ralph Toms, Feb 1996
 */

/* Note: Variable names follow the notation used in Toms, Feb 1996 */

    double W;        /* distance from Z axis */
    double W2;       /* square of distance from Z axis */
    double T0;       /* initial estimate of vertical component */
    double T1;       /* corrected estimate of vertical component */
    double S0;       /* initial estimate of horizontal component */
    double S1;       /* corrected estimate of horizontal component */
    double Sin_B0;   /* sin(B0), B0 is estimate of Bowring aux variable */
    double Sin3_B0;  /* cube of sin(B0) */
    double Cos_B0;   /* cos(B0) */
    double Sin_p1;   /* sin(phi1), phi1 is estimated latitude */
    double Cos_p1;   /* cos(phi1) */
    double Rn;       /* Earth radius at location */
    double Sum;      /* numerator of cos(phi1) */
    int At_Pole;     /* indicates location is in polar region */



    At_Pole = FALSE;
    if (X != 0.0)
    {
        Longitude = atan2(Y,X);
    }
    else
    {
        if (Y > 0)
        {
            Longitude = PI_OVER_2;
        }
        else if (Y < 0)
        {
            Longitude = -PI_OVER_2;
        }
        else
        {
            At_Pole = TRUE;
            Longitude = 0.0;
            if (Z > 0.0)
            {  /* north pole */
                Latitude = PI_OVER_2;
            }
            else if (Z < 0.0)
            {  /* south pole */
                Latitude = -PI_OVER_2;
            }
            else
            {  /* center of earth */
                Latitude = PI_OVER_2;
                Height = -Geocent_b;
                return;
            } 
        }
    }
    W2 = X*X + Y*Y;
    W = sqrt(W2);
    T0 = Z * AD_C;
    S0 = sqrt(T0 * T0 + W2);
    Sin_B0 = T0 / S0;
    Cos_B0 = W / S0;
    Sin3_B0 = Sin_B0 * Sin_B0 * Sin_B0;
    T1 = Z + Geocent_b * Geocent_ep2 * Sin3_B0;
    Sum = W - Geocent_a * Geocent_e2 * Cos_B0 * Cos_B0 * Cos_B0;
    S1 = sqrt(T1*T1 + Sum * Sum);
    Sin_p1 = T1 / S1;
    Cos_p1 = Sum / S1;
    Rn = Geocent_a / sqrt(1.0 - Geocent_e2 * Sin_p1 * Sin_p1);
    if (Cos_p1 >= COS_67P5)
    {
        Height = W / Cos_p1 - Rn;
    }
    else if (Cos_p1 <= -COS_67P5)
    {
        Height = W / -Cos_p1 - Rn;
    }
    else
    {
        Height = Z / Sin_p1 + Rn * (Geocent_e2 - 1.0);
    }
    if (At_Pole == FALSE)
    {
        Latitude = atan(Sin_p1 / Cos_p1);
    }
#else
            /*
* Reference...
* ============
* Wenzel, H.-G.(1985): Hochauflösende Kugelfunktionsmodelle für
* das Gravitationspotential der Erde. Wiss. Arb. Univ. Hannover
* Nr. 137, p. 130-131.

* Programmed by GGA- Leibniz-Institue of Applied Geophysics
*               Stilleweg 2
*               D-30655 Hannover
*               Federal Republic of Germany
*               Internet: www.gga-hannover.de
*
*               Hannover, March 1999, April 2004.
*               see also: comments in statements
* remarks:
* Mathematically exact and because of symmetry of rotation-ellipsoid,
* each point (X,Y,Z) has at least two solutions (Latitude1,Longitude1,Height1) and
* (Latitude2,Longitude2,Height2). Is point=(0.,0.,Z) (P=0.), so you get even
* four solutions,	every two symmetrical to the semi-minor axis.
* Here Height1 and Height2 have at least a difference in order of
* radius of curvature (e.g. (0,0,b)=> (90.,0.,0.) or (-90.,0.,-2b);
* (a+100.)*(sqrt(2.)/2.,sqrt(2.)/2.,0.) => (0.,45.,100.) or
* (0.,225.,-(2a+100.))).
* The algorithm always computes (Latitude,Longitude) with smallest |Height|.
* For normal computations, that means |Height|<10000.m, algorithm normally
* converges after to 2-3 steps!!!
* But if |Height| has the amount of length of ellipsoid's axis
* (e.g. -6300000.m),	algorithm needs about 15 steps.
*/

            /* local defintions and variables */
            /* end-criterium of loop, accuracy of sin(Latitude) */
            double genau = 1.0E-12;
            double genau2 = (genau * genau);
            int maxiter = 30;

            double P;        /* distance between semi-minor axis and location */
            double RR;       /* distance between center and location */
            double CT;       /* sin of geocentric latitude */
            double ST;       /* cos of geocentric latitude */
            double RX;
            double RK;
            double RN;       /* Earth radius at location */
            double CPHI0;    /* cos of start or old geodetic latitude in iterations */
            double SPHI0;    /* sin of start or old geodetic latitude in iterations */
            double CPHI;     /* cos of searched geodetic latitude */
            double SPHI;     /* sin of searched geodetic latitude */
            double SDPHI;    /* end-criterium: addition-theorem of sin(Latitude(iter)-Latitude(iter-1)) */
            bool At_Pole;     /* indicates location is in polar region */
            int iter;        /* # of continous iteration, max. 30 is always enough (s.a.) */

            At_Pole = false;
            P = Math.Sqrt(X * X + Y * Y);
            RR = Math.Sqrt (X * X + Y * Y + Z * Z);

            /*	special cases for latitude and longitude */
            if (P / Geocent_a < genau)
            {

                /*  special case, if P=0. (X=0., Y=0.) */
                At_Pole = true;
                Longitude = 0.0;

                /*  if (X,Y,Z)=(0.,0.,0.) then Height becomes semi-minor axis
                 *  of ellipsoid (=center of mass), Latitude becomes PI/2 */
                if (RR / Geocent_a < genau)
                {
                    Latitude = PI_OVER_2;
                    Height = -Geocent_b;
                    return;

                }
            }
            else
            {
                /*  ellipsoidal (geodetic) longitude
                 *  interval: -PI < Longitude <= +PI */
                Longitude = Math.Atan2(Y, X);
            }

            /* --------------------------------------------------------------
             * Following iterative algorithm was developped by
             * "Institut für Erdmessung", University of Hannover, July 1988.
             * Internet: www.ife.uni-hannover.de
             * Iterative computation of CPHI,SPHI and Height.
             * Iteration of CPHI and SPHI to 10**-12 radian resp.
             * 2*10**-7 arcsec.
             * --------------------------------------------------------------
             */
            CT = Z / RR;
            ST = P / RR;
            RX = 1.0 / Math.Sqrt(1.0 - Geocent_e2 * (2.0 - Geocent_e2) * ST * ST);
            CPHI0 = ST * (1.0 - Geocent_e2) * RX;
            SPHI0 = CT * RX;
            iter = 0;

            /* loop to find sin(Latitude) resp. Latitude
             * until |sin(Latitude(iter)-Latitude(iter-1))| < genau */
            do
            {
                iter++;
                RN = Geocent_a / Math.Sqrt(1.0 - Geocent_e2 * SPHI0 * SPHI0);

                /*  ellipsoidal (geodetic) height */
                Height = P * CPHI0 + Z * SPHI0 - RN * (1.0 - Geocent_e2 * SPHI0 * SPHI0);

                RK = Geocent_e2 * RN / (RN + Height);
                RX = 1.0 / Math.Sqrt(1.0 - RK * (2.0 - RK) * ST * ST);
                CPHI = ST * (1.0 - RK) * RX;
                SPHI = CT * RX;
                SDPHI = SPHI * CPHI0 - CPHI * SPHI0;
                CPHI0 = CPHI;
                SPHI0 = SPHI;
            }
            while (SDPHI * SDPHI > genau2 && iter < maxiter);

            /*	ellipsoidal (geodetic) latitude */
            Latitude = Math.Atan(SPHI / (double)Math.Abs(CPHI));

            return;
#endif
        }

        private static int geocentric_from_wgs84(ref double x, ref double y, ref double z, double[] datumParams)
        {
            double x_tmp, y_tmp, z_tmp;

            double Rx_BF, Ry_BF, Rz_BF, Dx_BF, Dy_BF, Dz_BF, M_BF;
            Dx_BF = datumParams[0];
            Dy_BF = datumParams[1];
            Dz_BF = datumParams[2];
            Rx_BF = datumParams[3];
            Ry_BF = datumParams[4];
            Rz_BF = datumParams[5];
            M_BF = datumParams[6];

            x_tmp = (x - Dx_BF) / M_BF;
            y_tmp = (y - Dy_BF) / M_BF;
            z_tmp = (z - Dz_BF) / M_BF;

            x = x_tmp + Rz_BF * y_tmp - Ry_BF * z_tmp;
            y = -Rz_BF * x_tmp + y_tmp + Rx_BF * z_tmp;
            z = Ry_BF * x_tmp - Rx_BF * y_tmp + z_tmp;

            return 0;
        }

        private static int geocentric_to_wgs84(ref double x, ref double y, ref double z, double[] datumParams)
        {
            int i;

            double x_out, y_out, z_out;
            double Rx_BF, Ry_BF, Rz_BF, Dx_BF, Dy_BF, Dz_BF, M_BF;
            Dx_BF = datumParams[0];
            Dy_BF = datumParams[1];
            Dz_BF = datumParams[2];
            Rx_BF = datumParams[3];
            Ry_BF = datumParams[4];
            Rz_BF = datumParams[5];
            M_BF = datumParams[6];

            //Dx_BF = 414.0978567149,Dy_BF = 41.3381489658,Dz_BF=603.0627177516,Rx_BF=-4.1453675348556307e-006,Ry_BF=1.0381540791960210e-005,Rz_BF=-3.4047111959502597e-005,M_BF=1.0000009999999999;



            x_out = M_BF * (x - Rz_BF * y + Ry_BF * z) + Dx_BF;
            y_out = M_BF * (Rz_BF * x + y - Rx_BF * z) + Dy_BF;
            z_out = M_BF * (-Ry_BF * x + Rx_BF * y + z) + Dz_BF;

            x = x_out;
            y = y_out;
            z = z_out;

            return 0;
        }

        private static int geocentric_to_geodetic(double a, double es, ref double x, ref double y, ref double z)
        {
            double b;
            int i;
            if (es == 0)
                b = a;
            else
                b = a * Math.Sqrt(1 - es);

            double lat=0, lon=0, h=0;
            Convert_Geocentric_To_Geodetic(x, y, z, ref lat, ref lon, ref h);
            x = lon;
            y = lat;
            z = h;
            return 0;
        }

        private static int geodetic_to_geocentric(double a, double es,
                               ref double x, ref double y, ref double z)
        {
            double b;
            int i;

            if (es == 0.0)
                b = a;
            else
                b = a * Math.Sqrt(1 - es);


            double lat = 0, lon= 0, h = 0;
            if (Convert_Geodetic_To_Geocentric(y, x, z,
                                                    ref lat, ref lon, ref h, a, b) != 0)
            {
                //        fputs("Error converting to geocentric coordinates!");
                return -1;
            }

            x = lat;
            y = lon;
            z = h;

            return 0;
        }

        private static void Convert_Geocentric_To_Geodetic(double X,
                                     double Y,
                                     double Z,
                                     ref double Latitude,
                                     ref double Longitude,
                                     ref double Height)
        {
            /*
             * The method used here is derived from 'An Improved Algorithm for
             * Geocentric to Geodetic Coordinate Conversion', by Ralph Toms, Feb 1996
             */

            /* Note: Variable names follow the notation used in Toms, Feb 1996 */

            double W;        /* distance from Z axis */
            double W2;       /* square of distance from Z axis */
            double T0;       /* initial estimate of vertical component */
            double T1;       /* corrected estimate of vertical component */
            double S0;       /* initial estimate of horizontal component */
            double S1;       /* corrected estimate of horizontal component */
            double Sin_B0;   /* sin(B0), B0 is estimate of Bowring aux variable */
            double Sin3_B0;  /* cube of sin(B0) */
            double Cos_B0;   /* cos(B0) */
            double Sin_p1;   /* sin(phi1), phi1 is estimated latitude */
            double Cos_p1;   /* cos(phi1) */
            double Rn;       /* Earth radius at location */
            double Sum;      /* numerator of cos(phi1) */
            bool At_Pole;     /* indicates location is in polar region */

            At_Pole = false;
            if (X != 0.0)
            {
                Longitude = Math.Atan2(Y, X);
            }
            else
            {
                if (Y > 0)
                {
                    Longitude = PI_OVER_2;
                }
                else if (Y < 0)
                {
                    Longitude = -PI_OVER_2;
                }
                else
                {
                    At_Pole = true;
                    Longitude = 0.0;
                    if (Z > 0.0)
                    {  /* north pole */
                        Latitude = PI_OVER_2;
                    }
                    else if (Z < 0.0)
                    {  /* south pole */
                        Latitude = -PI_OVER_2;
                    }
                    else
                    {  /* center of earth */
                        Latitude = PI_OVER_2;
                        Height = -Geocent_b;
                        return;
                    }
                }
            }
            W2 = X * X + Y * Y;
            W = Math.Sqrt(W2);
            T0 = Z * AD_C;
            S0 = Math.Sqrt(T0 * T0 + W2);
            Sin_B0 = T0 / S0;
            Cos_B0 = W / S0;
            Sin3_B0 = Sin_B0 * Sin_B0 * Sin_B0;
            T1 = Z + Geocent_b * Geocent_ep2 * Sin3_B0;
            Sum = W - Geocent_a * Geocent_e2 * Cos_B0 * Cos_B0 * Cos_B0;
            S1 = Math.Sqrt(T1 * T1 + Sum * Sum);
            Sin_p1 = T1 / S1;
            Cos_p1 = Sum / S1;
            Rn = Geocent_a / Math.Sqrt(1.0 - Geocent_e2 * Sin_p1 * Sin_p1);
            if (Cos_p1 >= COS_67P5)
            {
                Height = W / Cos_p1 - Rn;
            }
            else if (Cos_p1 <= -COS_67P5)
            {
                Height = W / -Cos_p1 - Rn;
            }
            else
            {
                Height = Z / Sin_p1 + Rn * (Geocent_e2 - 1.0);
            }
            if (At_Pole == false)
            {
                Latitude = Math.Atan(Sin_p1 / Cos_p1);
            }
        }

        private static long Convert_Geodetic_To_Geocentric(double Latitude,
                                     double Longitude,
                                     double Height,
                                     ref double X,
                                     ref double Y,
                                     ref double Z, double a, double b)
        { /* BEGIN Convert_Geodetic_To_Geocentric */
            /*
             * The function Convert_Geodetic_To_Geocentric converts geodetic coordinates
             * (latitude, longitude, and height) to geocentric coordinates (X, Y, Z),
             * according to the current ellipsoid parameters.
             *
             *    Latitude  : Geodetic latitude in radians                     (input)
             *    Longitude : Geodetic longitude in radians                    (input)
             *    Height    : Geodetic height, in meters                       (input)
             *    X         : Calculated Geocentric X coordinate, in meters    (output)
             *    Y         : Calculated Geocentric Y coordinate, in meters    (output)
             *    Z         : Calculated Geocentric Z coordinate, in meters    (output)
             *
             */

            double Geocent_a, Geocent_b, Geocent_a2, Geocent_b2, Geocent_e2, Geocent_ep2;

            Geocent_a = a;
            Geocent_b = b;
            Geocent_a2 = a * a;
            Geocent_b2 = b * b;
            Geocent_e2 = (Geocent_a2 - Geocent_b2) / Geocent_a2;
            Geocent_ep2 = (Geocent_a2 - Geocent_b2) / Geocent_b2;

            long Error_Code = 0;
            double Rn;            /*  Earth radius at location  */
            double Sin_Lat;       /*  sin(Latitude)  */
            double Sin2_Lat;      /*  Square of sin(Latitude)  */
            double Cos_Lat;       /*  cos(Latitude)  */

            /*
            ** Don't blow up if Latitude is just a little out of the value
            ** range as it may just be a rounding issue.  Also removed longitude
            ** test, it should be wrapped by cos() and sin().  NFW for PROJ.4, Sep/2001.
            */
            if (Latitude < -PI_OVER_2 && Latitude > -1.001 * PI_OVER_2)
                Latitude = -PI_OVER_2;
            else if (Latitude > PI_OVER_2 && Latitude < 1.001 * PI_OVER_2)
                Latitude = PI_OVER_2;
            else if ((Latitude < -PI_OVER_2) || (Latitude > PI_OVER_2))
            { /* Latitude out of range */
                //    fputs("Latitude out of range!");
            }

            if (Error_Code == 0)
            { /* no errors */
                if (Longitude > PI)
                    Longitude -= (2 * PI);
                Sin_Lat = Math.Sin(Latitude);
                Cos_Lat = Math.Cos(Latitude);
                Sin2_Lat = Sin_Lat * Sin_Lat;
                Rn = Geocent_a / (Math.Sqrt(1.0e0 - Geocent_e2 * Sin2_Lat));
                X = (Rn + Height) * Cos_Lat * Math.Cos(Longitude);
                Y = (Rn + Height) * Cos_Lat * Math.Sin(Longitude);
                Z = ((Rn * (1 - Geocent_e2)) + Height) * Sin_Lat;

            }
            return (Error_Code);
        } /* END OF Convert_Geodetic_To_Geocentric */

        private static void old2_WGS84ToRT90(double lat, double lon, double height)
        {
            double radWGlat = lat * deg2rad;
            double radWGlon = lon * deg2rad;
            double h = height;  

            double a = 6378137;
            double f = 1/298.257222101;

            double a2 = 6377397.155;
            double e2 = 0.003342773182175;
            
            double e = f * (2 - f);

            double v = a / (Math.Sqrt(1 - (e * (Math.Sin(radWGlat) * Math.Sin(radWGlat)))));
            double x = (v + h) * Math.Cos(radWGlat) * Math.Cos(radWGlon);
            double y = (v + h) * Math.Cos(radWGlat) * Math.Sin(radWGlon);
            double z = ((1 - e) * v + h) * Math.Sin(radWGlat);

            double xp = -414.0978567149;
            double yp = -41.3381489658;
            double zp = -603.0627177516;
            double xr = 0.8550434314;
            double yr = -2.1413465;
            double zr = 7.0227209516;
            double s = 1;

            double sf = s * 0.000001;
            double xrot = (xr / 3600) * deg2rad;
            double yrot = (yr / 3600) * deg2rad;
            double zrot = (zr / 3600) * deg2rad;
            double hx = x + (x * sf) - (y * zrot) + (z * yrot) + xp;
            double hy = (x * zrot) + y + (y * sf) - (z * xrot) + yp;
            double hz = (-1 * x * yrot) + (y * xrot) + z + (z * sf) + zp;

            // Convert back to lat, lon
            double newLon = Math.Atan(hy / hx);
            double p = Math.Sqrt((hx * hx) + (hy * hy));
            double newLat = Math.Atan(hz / (p * (1 - e2)));
            v = a2 / (Math.Sqrt(1 - e2 * (Math.Sin(newLat) * Math.Sin(newLat))));
            double errvalue = 1.0;
            double lat0 = 0;
            while (errvalue > 0.001)
            {
                lat0 = Math.Atan((hz + e2 * v * Math.Sin(newLat)) / p);
                errvalue = Math.Abs(lat0 - newLat);
                newLat = lat0;
            }

            //convert back to degrees
            newLat = newLat * rad2deg;
            newLon = newLon * rad2deg;


        }

        private static void old_WGS84ToRT90Coorect(double lat, double lon, double height)
        {
            //first off convert to radians
            double radWGlat = lat * deg2rad;
            double radWGlon = lon * deg2rad;
            //these are the values for WGS86(GRS80) to OSGB36(Airy)
            double a = 6378137;              // WGS84_AXIS
            double e = 0.00669438037928458;  // WGS84_ECCENTRIC
            double h = height;               // height above datum  (from $GPGGA sentence)
            //double a2 = 6377563.396;         // OSGB_AXIS
            //double e2 = 0.0066705397616;     // OSGB_ECCENTRIC 
            double a2 = 6377397.155;
            double e2 = 0.003342773182175;
            //double xp = -446.448;
            //double yp = 125.157;
            //double zp = -542.06;
            //double xr = -0.1502;
            //double yr = -0.247;
            //double zr = -0.8421;
            //double s = 20.4894;

            //414.0978567149,41.3381489658,603.0627177516,-0.8550434314,2.1413465,-7.0227209516,1
            double xp = -414.0978567149;
            double yp = -41.3381489658;
            double zp = -603.0627177516;
            double xr = 0.8550434314;
            double yr = -2.1413465;
            double zr = 7.0227209516;
            double s = 1;

            // convert to cartesian geocentric coordinates; lat, lon are in radians
            double sf = s * 0.000001;
            double v = a / (Math.Sqrt(1 - (e * (Math.Sin(radWGlat) * Math.Sin(radWGlat)))));
            double x = (v + h) * Math.Cos(radWGlat) * Math.Cos(radWGlon);
            double y = (v + h) * Math.Cos(radWGlat) * Math.Sin(radWGlon);
            double z = ((1 - e) * v + h) * Math.Sin(radWGlat);

            // transform cartesian
            double xrot = (xr / 3600) * deg2rad;
            double yrot = (yr / 3600) * deg2rad;
            double zrot = (zr / 3600) * deg2rad;
            double hx = x + (x * sf) - (y * zrot) + (z * yrot) + xp;
            double hy = (x * zrot) + y + (y * sf) - (z * xrot) + yp;
            double hz = (-1 * x * yrot) + (y * xrot) + z + (z * sf) + zp;

            // Convert back to lat, lon
            double newLon = Math.Atan(hy / hx);
            double p = Math.Sqrt((hx * hx) + (hy * hy));
            double newLat = Math.Atan(hz / (p * (1 - e2)));
            v = a2 / (Math.Sqrt(1 - e2 * (Math.Sin(newLat) * Math.Sin(newLat))));
            double errvalue = 1.0;
            double lat0 = 0;
            while (errvalue > 0.001)
            {
                lat0 = Math.Atan((hz + e2 * v * Math.Sin(newLat)) / p);
                errvalue = Math.Abs(lat0 - newLat);
                newLat = lat0;
            }

            //convert back to degrees
            newLat = newLat * rad2deg;
            newLon = newLon * rad2deg;

            //convert lat and lon (OSGB36)  to OS 6 figure northing and easting
            //     LLtoNE(newLat, newLon);


        }
    }
}
