/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services
{
    public class ServerConnector : IService, IGridRegistrationUrlModule
    {
        private IRegistryCore m_registry;
        private IConfigSource m_config;

        public string Name
        {
            get { return GetType().Name; }
        }

        #region IGridRegistrationUrlModule Members

        public string UrlName
        {
            get { return "ServerURI"; }
        }

        public bool DoMultiplePorts
        {
            get { return true; }
        }

        public void AddExistingUrlForClient(string SessionID, string url, uint port)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);

            server.AddStreamHandler(new ServerHandler(url, SessionID, m_registry));
        }

        public string GetUrlForRegisteringClient(string SessionID, uint port)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);
            string url = "/server" + UUID.Random();

            server.AddStreamHandler(new ServerHandler(url, SessionID, m_registry));

            return url;
        }

        public void RemoveUrlForClient(string sessionID, string url, uint port)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);
            server.RemoveHTTPHandler("POST", url);
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_config = config;
            IConfig handlerConfig = config.Configs["AuroraConnectors"];
            if (!handlerConfig.GetBoolean("AllowRemoteCalls", false))
                return;

            m_registry = registry;

            m_registry.RequestModuleInterface<IGridRegistrationService>().RegisterModule(this);
        }

        public void FinishedStartup()
        {
            if (m_registry != null)
            {
                uint port = m_config.Configs["Network"].GetUInt("http_listener_port", 8003);
                AddExistingUrlForClient("", "/", port);
                //AddUDPConector(8008);
            }
        }

        /*private void AddUDPConector(int port)
        {
            Thread thread = new Thread(delegate()
                {
                    UdpClient server = new UdpClient("127.0.0.1", port);
                    IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = server.Receive(ref sender);
                    OSDMap map = (OSDMap)OSDParser.DeserializeJson(new MemoryStream(data));
                    ServerHandler handler = new ServerHandler("", "", m_registry);
                    byte[] Data = handler.HandleMap(map);
                });
        }*/

        #endregion
    }
}