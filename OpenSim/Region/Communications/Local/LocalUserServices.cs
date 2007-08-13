using System;
using libsecondlife;
using OpenSim.Framework.Communications;
using OpenSim.Framework.Data;
using OpenSim.Framework.Types;
using OpenSim.Framework.UserManagement;
using OpenSim.Framework.Utilities;

namespace OpenSim.Region.Communications.Local
{
    public class LocalUserServices : UserManagerBase, IUserServices
    {
        private CommunicationsLocal m_Parent;

        private NetworkServersInfo serversInfo;
        private uint defaultHomeX ;
        private uint defaultHomeY;
        private bool authUsers = false;
        private string welcomeMessage = "Welcome to OpenSim";

        public LocalUserServices(CommunicationsLocal parent, NetworkServersInfo serversInfo, bool authenticate, string welcomeMess)
        {
            m_Parent = parent;
            this.serversInfo = serversInfo;
            defaultHomeX = this.serversInfo.DefaultHomeLocX;
            defaultHomeY = this.serversInfo.DefaultHomeLocY;
            this.authUsers = authenticate;
            if (welcomeMess != "")
            {
                this.welcomeMessage = welcomeMess;
            }
        }

        public UserProfileData GetUserProfile(string firstName, string lastName)
        {
            return GetUserProfile(firstName + " " + lastName);
        }

        public UserProfileData GetUserProfile(string name)
        {
            return this.getUserProfile(name);
        }

        public UserProfileData GetUserProfile(LLUUID avatarID)
        {
            return this.getUserProfile(avatarID);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string GetMessage()
        {
            return welcomeMessage;
        }

        public override UserProfileData GetTheUser(string firstname, string lastname)
        {
            UserProfileData profile = getUserProfile(firstname, lastname);
            if (profile != null)
            {
               
                return profile;
            }

            if (!authUsers)
            {
                //no current user account so make one
                Console.WriteLine("No User account found so creating a new one ");
                this.AddUserProfile(firstname, lastname, "test", defaultHomeX, defaultHomeY);

                profile = getUserProfile(firstname, lastname);

                return profile;
            }
            return null;
        }

        public override bool AuthenticateUser(UserProfileData profile, string password)
        {
            if (!authUsers)
            {
                //for now we will accept any password in sandbox mode
                Console.WriteLine("authorising user");
                return true;
            }
            else
            {
                Console.WriteLine( "Authenticating " + profile.username + " " + profile.surname);

                password = password.Remove(0, 3); //remove $1$

                string s = Util.Md5Hash(password + ":" + profile.passwordSalt);

                return profile.passwordHash.Equals(s.ToString(), StringComparison.InvariantCultureIgnoreCase);
            }
        }

        public override void CustomiseResponse(LoginResponse response, UserProfileData theUser)
        {
            ulong currentRegion = theUser.currentAgent.currentHandle;
            RegionInfo reg = m_Parent.GridServer.RequestNeighbourInfo(currentRegion);

            if (reg != null)
            {
                response.Home = "{'region_handle':[r" + (reg.RegionLocX * 256).ToString() + ",r" + (reg.RegionLocY * 256).ToString() + "], " +
                 "'position':[r" + theUser.homeLocation.X.ToString() + ",r" + theUser.homeLocation.Y.ToString() + ",r" + theUser.homeLocation.Z.ToString() + "], " +
                 "'look_at':[r" + theUser.homeLocation.X.ToString() + ",r" + theUser.homeLocation.Y.ToString() + ",r" + theUser.homeLocation.Z.ToString() + "]}";
                string capsPath = Util.GetRandomCapsPath();
                response.SimAddress = reg.ExternalEndPoint.Address.ToString();
                response.SimPort = (Int32)reg.ExternalEndPoint.Port;
                response.RegionX = reg.RegionLocX ;
                response.RegionY = reg.RegionLocY ;

                //following port needs changing as we don't want a http listener for every region (or do we?)
                response.SeedCapability = "http://" + reg.ExternalHostName + ":" + this.serversInfo.HttpListenerPort.ToString() + "/CAPS/" + capsPath + "0000/";
                theUser.currentAgent.currentRegion = reg.SimUUID;
                theUser.currentAgent.currentHandle = reg.RegionHandle;

                Login _login = new Login();
                //copy data to login object
                _login.First = response.Firstname;
                _login.Last = response.Lastname;
                _login.Agent = response.AgentID;
                _login.Session = response.SessionID;
                _login.SecureSession = response.SecureSessionID;
                _login.CircuitCode = (uint)response.CircuitCode;
                _login.CapsPath = capsPath;

                m_Parent.InformRegionOfLogin(currentRegion, _login);
            }
            else
            {
                Console.WriteLine("not found region " + currentRegion);
            }

        }

        public UserProfileData SetupMasterUser(string firstName, string lastName)
        {
            return SetupMasterUser(firstName, lastName, "");
        }

        public UserProfileData SetupMasterUser(string firstName, string lastName, string password)
        {
            UserProfileData profile = getUserProfile(firstName, lastName);
            if (profile != null)
            {

                return profile;
            }

            Console.WriteLine("Unknown Master User. Sandbox Mode: Creating Account");
            this.AddUserProfile(firstName, lastName, password, defaultHomeX, defaultHomeY);

            profile = getUserProfile(firstName, lastName);

            if (profile == null)
            {
                Console.WriteLine("Unknown Master User after creation attempt. No clue what to do here.");
            }

            return profile;
        }
    }
}
