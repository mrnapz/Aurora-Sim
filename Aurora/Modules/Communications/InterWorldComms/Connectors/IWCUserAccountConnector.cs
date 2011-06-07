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
using System.Linq;
using System.Text;
using OpenSim.Services.Connectors;
using OpenSim.Services.UserAccountService;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Framework;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Modules 
{
    public class IWCUserAccountConnector : IUserAccountService, IService
    {
        protected UserAccountService m_localService;
        protected UserAccountServicesConnector m_remoteService;
        protected IRegistryCore m_registry;

        #region IService Members

        public string Name
        {
            get { return GetType().Name; }
        }

        public IUserAccountService InnerService
        {
            get
            {
                //If we are getting URls for an IWC connection, we don't want to be calling other things, as they are calling us about only our info
                //If we arn't, its ar region we are serving, so give it everything we know
                if (m_registry.RequestModuleInterface<InterWorldCommunications> ().IsGettingUrlsForIWCConnection)
                    return m_localService;
                else
                    return this;
            }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("UserAccountHandler", "") != Name)
                return;

            m_localService = new UserAccountService();
            m_localService.Configure(config, registry);
            m_remoteService = new UserAccountServicesConnector();
            m_remoteService.Initialize(config, registry);
            registry.RegisterModuleInterface<IUserAccountService> (this);
            m_registry = registry;
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            if (m_localService != null)
                m_localService.Start(config, registry);
        }

        public void FinishedStartup()
        {
            if (m_localService != null)
                m_localService.FinishedStartup();
        }

        #endregion

        #region IUserAccountService Members

        public UserAccount GetUserAccount(UUID scopeID, UUID userID)
        {
            UserAccount account = m_localService.GetUserAccount(scopeID, userID);
            if (account == null)
                account = FixRemoteAccount(m_remoteService.GetUserAccount(scopeID, userID));
            return account;
        }

        public UserAccount GetUserAccount(UUID scopeID, string FirstName, string LastName)
        {
            UserAccount account = m_localService.GetUserAccount(scopeID, FirstName, LastName);
            if (account == null)
                account = FixRemoteAccount(m_remoteService.GetUserAccount(scopeID, FirstName, LastName));
            return account;
        }

        public UserAccount GetUserAccount(UUID scopeID, string Name)
        {
            UserAccount account = m_localService.GetUserAccount(scopeID, Name);
            if (account == null)
                account = FixRemoteAccount(m_remoteService.GetUserAccount(scopeID, Name));
            return account;
        }

        public List<UserAccount> GetUserAccounts(UUID scopeID, string query)
        {
            List<UserAccount> accounts = m_localService.GetUserAccounts(scopeID, query);
            accounts.AddRange(FixRemoteAccounts(m_remoteService.GetUserAccounts(scopeID, query)));
            return accounts;
        }

        private IEnumerable<UserAccount> FixRemoteAccounts (List<UserAccount> list)
        {
            List<UserAccount> accounts = new List<UserAccount> ();
            foreach (UserAccount account in list)
            {
                accounts.Add (FixRemoteAccount (account));
            }
            return accounts;
        }

        private UserAccount FixRemoteAccount (UserAccount userAccount)
        {
            if (userAccount == null)
                return userAccount;
            userAccount.Name = userAccount.FirstName + " " + userAccount.LastName + "@" + userAccount.GenericData["GridURL"];
            return userAccount;
        }

        public bool StoreUserAccount(UserAccount data)
        {
            return m_localService.StoreUserAccount(data);
        }

        public void CreateUser (string name, string md5password, string email)
        {
            m_localService.CreateUser (name, md5password, email);
        }

        public void CreateUser (UUID userID, string name, string md5password, string email)
        {
            m_localService.CreateUser (userID, name, md5password, email);
        }

        #endregion
    }
}
