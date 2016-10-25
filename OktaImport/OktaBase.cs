using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Okta.Core;
using Okta.Core.Clients;
using Okta.Core.Models;

namespace OktaImport
{
    public static class OktaBase
    {
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
    }
}
