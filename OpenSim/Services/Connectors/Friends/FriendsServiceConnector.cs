/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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

using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using FriendInfo = OpenSim.Services.Interfaces.FriendInfo;
using Aurora.Simulation.Base;
using OpenMetaverse;

namespace OpenSim.Services.Connectors
{
    public class FriendsServicesConnector : IFriendsService, IService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private List<string> m_ServerURIs = new List<string>();

        #region IFriendsService

        public FriendInfo[] GetFriends(UUID PrincipalID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["PRINCIPALID"] = PrincipalID.ToString();
            sendData["METHOD"] = "getfriends";

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/friends",
                        reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData != null)
                        {
                            if (replyData.ContainsKey("result") && (replyData["result"].ToString().ToLower() == "null"))
                            {
                                return new FriendInfo[0];
                            }

                            List<FriendInfo> finfos = new List<FriendInfo>();
                            Dictionary<string, object>.ValueCollection finfosList = replyData.Values;
                            //m_log.DebugFormat("[FRIENDS CONNECTOR]: get neighbours returned {0} elements", rinfosList.Count);
                            foreach (object f in finfosList)
                            {
                                if (f is Dictionary<string, object>)
                                {
                                    FriendInfo finfo = new FriendInfo((Dictionary<string, object>)f);
                                    finfos.Add(finfo);
                                }
                                else
                                    m_log.DebugFormat("[FRIENDS CONNECTOR]: GetFriends {0} received invalid response type {1}",
                                        PrincipalID, f.GetType());
                            }

                            // Success
                            return finfos.ToArray();
                        }

                        else
                            m_log.DebugFormat("[FRIENDS CONNECTOR]: GetFriends {0} received null response",
                                PrincipalID);
                    }
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[FRIENDS CONNECTOR]: Exception when contacting friends server: {0}", e.Message);
            }

            return new FriendInfo[0];

        }

        public bool StoreFriend(UUID PrincipalID, string Friend, int flags)
        {
            FriendInfo finfo = new FriendInfo();
            finfo.PrincipalID = PrincipalID;
            finfo.Friend = Friend;
            finfo.MyFlags = flags;

            Dictionary<string, object> sendData = finfo.ToKeyValuePairs();

            sendData["METHOD"] = "storefriend";

            string reply = string.Empty;
            try
            {
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                            m_ServerURI + "/friends",
                            WebUtils.BuildQueryString(sendData));
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[FRIENDS CONNECTOR]: Exception when contacting friends server: {0}", e.Message);
                return false;
            }

            if (reply != string.Empty)
            {
                Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                if ((replyData != null) && replyData.ContainsKey("Result") && (replyData["Result"] != null))
                {
                    bool success = false;
                    Boolean.TryParse(replyData["Result"].ToString(), out success);
                    return success;
                }
                else
                    m_log.DebugFormat("[FRIENDS CONNECTOR]: StoreFriend {0} {1} received null response",
                        PrincipalID, Friend);
            }
            else
                m_log.DebugFormat("[FRIENDS CONNECTOR]: StoreFriend received null reply");

            return false;

        }

        public bool Delete(UUID PrincipalID, string Friend)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            sendData["PRINCIPALID"] = PrincipalID.ToString();
            sendData["FRIEND"] = Friend;
            sendData["METHOD"] = "deletefriend";

            string reply = string.Empty;
            try
            {
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                            m_ServerURI + "/friends",
                            WebUtils.BuildQueryString(sendData));
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if ((replyData != null) && replyData.ContainsKey("Result") && (replyData["Result"] != null))
                        {
                            bool success = false;
                            Boolean.TryParse(replyData["Result"].ToString(), out success);
                            return success;
                        }
                        else
                            m_log.DebugFormat("[FRIENDS CONNECTOR]: DeleteFriend {0} {1} received null response",
                                PrincipalID, Friend);
                    }
                    else
                        m_log.DebugFormat("[FRIENDS CONNECTOR]: DeleteFriend received null reply");
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[FRIENDS CONNECTOR]: Exception when contacting friends server: {0}", e.Message);
                return false;
            }

            

            return false;
        }

        #endregion

        #region IService Members

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void PostInitialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("FriendsHandler", "") != Name)
                return;

            m_ServerURIs = registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
            registry.RegisterModuleInterface<IFriendsService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void PostStart(IConfigSource config, IRegistryCore registry)
        {
        }

        public void AddNewRegistry(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("FriendsHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IFriendsService>(this);
        }

        #endregion
    }
}