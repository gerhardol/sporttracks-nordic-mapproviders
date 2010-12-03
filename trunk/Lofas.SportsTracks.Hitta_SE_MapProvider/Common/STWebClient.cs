/*
Copyright (C) 2010 Magnus Wallström

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

using System.Net;
using ZoneFiveSoftware.Common.Visuals.Fitness;

namespace Lofas.SportsTracks.Hitta_SE_MapProvider
{
    public sealed class STWebClient : WebClient
    {
        private static readonly STWebClient instance = new STWebClient();

        /// <summary>
        /// STWebClient extends the WebClient class so that it can take advantage of the Sport Tracks Proxy settings.
        /// </summary>
        private STWebClient()
        {
            if (InternetSettings.UseProxy)
            {
                WebProxy proxy = new WebProxy(InternetSettings.ProxyHost, InternetSettings.ProxyPort);
                string[] proxyUsername = InternetSettings.ProxyUsername.Split('\\');
                if (proxyUsername.Length > 1)
                {
                    string domain = proxyUsername[0];
                    string proxyUser = proxyUsername[1];
                    proxy.Credentials = new NetworkCredential(proxyUser, InternetSettings.ProxyPassword, domain);
                }
                else
                    proxy.Credentials = new NetworkCredential(InternetSettings.ProxyUsername,
                                                              InternetSettings.ProxyPassword);

                Proxy = proxy;
            }
        }

        public static STWebClient Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// Returns IInternetSettings of Sport Tracks
        /// </summary>
        private static IInternetSettings InternetSettings
        {
            get { return Plugin.m_Application.SystemPreferences.InternetSettings; }
        } 
    }
}
