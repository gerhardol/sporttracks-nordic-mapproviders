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

using System.Net;
using ZoneFiveSoftware.Common.Visuals.Fitness;

namespace Lofas.SportsTracks.Hitta_SE_MapProvider
{
    public class STWebClient : WebClient
    {
        /// <summary>
        /// STWebClient extends the WebClient class so that it can take advantage of the Sport Tracks Proxy settings.
        /// </summary>
        public STWebClient()
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

        /// <summary>
        /// Returns IInternetSettings of Sport Tracks
        /// </summary>
        private static IInternetSettings InternetSettings
        {
            get { return Plugin.m_Application.SystemPreferences.InternetSettings; }
        } 
    }
}
