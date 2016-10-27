using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Okta.Core;
using Okta.Core.Clients;
using Okta.Core.Models;
using RestSharp;
using System.Threading;
using log4net;
using System.Collections.Specialized;

namespace OktaImport
{
    public  class OktaBase
    {

        //config logging
        ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //get Org info from web.config
        NameValueCollection appSettings = ConfigurationManager.AppSettings;

        public static string oktaOrg = ConfigurationManager.AppSettings["okta.OrgUrl"];
        public static string apiToken = ConfigurationManager.AppSettings["okta.OrgToken"];
        public static string AddoktaGroupID = ConfigurationManager.AppSettings["okta.AddGroupID"];
        //public static string DeleteoktaGroupID = ConfigurationManager.AppSettings["okta.DeleteGroupID"];


        //declare convenience client
        public static OktaClient oktaBaseClient;
        public static AuthClient authClient;
        public static UsersClient usersClient;
        public static GroupsClient groupsClient;
        public static GroupUsersClient groupUsersClient;



        public bool RateLimitCheck(IRestResponse response)
        {
            string limitMod = null;
            string remainingMod = null;
            string resetMod = null;
            var headerList = response.Headers.ToList();
            string limit = headerList.Find(x => x.Name == "X-Rate-Limit-Limit").Value.ToString();
            string remaining = headerList.Find(x => x.Name == "X-Rate-Limit-Remaining").Value.ToString();
            string reset = headerList.Find(x => x.Name == "X-Rate-Limit-Reset").Value.ToString();

            int limitIndex  = limit.IndexOf(",");
            if (limitIndex >0)
            {
                limitMod = limit.Substring(0, limitIndex);
            }
            else
            {
                limitMod = limit;
            }
 
            int remainingIndex = remaining.IndexOf(",");
            if (remainingIndex > 0)
            {
                remainingMod = remaining.Substring(0, remainingIndex);
            }
            else
            {
                remainingMod = remaining;
            }

            int resetIndex = reset.IndexOf(",");
            if (resetIndex > 0)
            {
                resetMod = reset.Substring(0, resetIndex);
            }
            else
            {
                resetMod = reset;
            }
  

            int myLimit = Convert.ToInt32(limitMod);
            int myRemaining = Convert.ToInt32(remainingMod);
            int myReset = Convert.ToInt32(resetMod);

            // Parse the string header to an int

            int waitUntilUnixTime;
            if (!int.TryParse(resetMod, out waitUntilUnixTime))
            {
                logger.Error("unable to calculate wait time");
            }
            // See how long until we hit that time
            var unixTime = (Int64)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
            //var millisToWait = unixTime - ((Int64)waitUntilUnixTime * 1000);
            var millisToWait =  ((Int64)waitUntilUnixTime * 1000) - unixTime;


            logger.Debug(" Limit Config:" + limitMod + " Remaining:" + remainingMod + " Epoch sec " +  unixTime/1000 + " ResetTime_sec:" + resetMod + " millisToWait:" + millisToWait);

            //if(millisToWait >= 100 )
            //{
                //logger.Debug(" wait for " + myReset.ToString());
                // wait the reset time then return true to recylce the command
                WaitTimer(millisToWait);
            //}
            return true;

        }

        public  void WaitTimer( Int64 milliseconds)
        {
            //logger.Debug("wait " + milliseconds);
            //delay before checking
            //provide time for user to respond
            //int milliseconds = 3000;
            //int milliseconds = Convert.ToInt32("3000");
            if (milliseconds > 0)
            {
                // Cross platform sleep
                using (var mre = new ManualResetEvent(false))
                {
                    mre.WaitOne((int)milliseconds);
                }
            }
            return;
        }



    }
}
