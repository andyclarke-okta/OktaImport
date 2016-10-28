using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using log4net;
using Newtonsoft.Json.Linq;
using Okta.Core;
using Okta.Core.Clients;
using Okta.Core.Models;
using RestSharp;
using FileHelpers;
using System.IO;

namespace OktaImport
{
    public partial class ImportUser : ServiceBase
    {

        //config logging
        ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //get Org info from web.config
        NameValueCollection appSettings = ConfigurationManager.AppSettings;

        OktaBase oktaBase;
//constructor
        public ImportUser()
        {
            oktaBase = new OktaBase();
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {

            //Console.WriteLine("Start import of: " + _path);
            string _path = appSettings["sourcefile"];
            DateTime _startdate = DateTime.Now;
            logger.Info("Start import of: " + _path + " time " + _startdate);

            var _engine = new FileHelperEngine<GAUsers>();
            var _result = _engine.ReadFile(_path);
            string _pw_url = null;
            string myUrl = null;
            RestClient client = null;
            RestRequest request = null;
            //string oktaId = null;

            int _processed = 0;
            OktaBase.oktaBaseClient = new OktaClient(OktaBase.apiToken, new Uri(OktaBase.oktaOrg));
            OktaBase.usersClient = OktaBase.oktaBaseClient.GetUsersClient();
            User _oktauser = new User();

            //string _errorpath = _path + "-error";
            //using (var w = new StreamWriter(_errorpath))
            //{
            //w.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}", "Login", "First", "Middle", "Last", "Password", "Aicpauid", "MemberType", "MemberStatus", "Roles"));
            //w.Flush();

            foreach (GAUsers _user in _result)
                {

                    _processed++;
                    if (ValidateUser(_user))
                    {

                    #region OktaSDK
                    _oktauser = null;

                    // make rest api call to see if user exists

//using restsharp here only to test rate limit code
                    logger.Debug("start check user " + _user.Login);
                    bool rspRateLimitCheck = false;
                    do {
                        //logger.Debug("process user " + _user.Login);
                        myUrl = OktaBase.oktaOrg + "/api/v1/users/" + _user.Login;
                        client = new RestClient(myUrl);
                        request = new RestRequest(Method.GET);
                        request.AddHeader("cache-control", "no-cache");
                        request.AddHeader("Accept", "application/json");
                        request.AddHeader("Content-type", "application/json");
                        request.AddHeader("Authorization", "SSWS " + OktaBase.apiToken + "");
                        IRestResponse<User> response = client.Execute<User>(request);
                        if (response.StatusDescription == "OK")
                        {
                            _oktauser = response.Data;
                            rspRateLimitCheck = false;
                        }
                        else if(response.StatusCode.ToString() == "429")
                        {
                           // logger.Debug("http status " + response.StatusCode + " check for rate limit on user " + _user.Login);
                            rspRateLimitCheck = oktaBase.RateLimitCheck(response);
                        }
                    } while(rspRateLimitCheck);

                    logger.Debug("complete check user " + _user.Login);



//this can be used, only commented out so i could test rate limit code
                    //logger.Debug("start check user " + _user.Login);
                    //try
                    //{
                    //    _oktauser = OktaBase.usersClient.Get(_user.Login);
                    //}
                    //catch { }
                    //logger.Debug("complete check user " + _user.Login);


                    if (_oktauser != null && !string.IsNullOrEmpty(_oktauser.Id) &&   _oktauser.Credentials.Provider.Name != "OKTA")
                    {
                        _pw_url = "";
                        // make rest api call to change federation
                        rspRateLimitCheck = false;
                        do
                        {
                        myUrl = OktaBase.oktaOrg + "/api/v1/users/" + _oktauser.Id + "/lifecycle/reset_password?sendEmail=false";
                        client = new RestClient(myUrl);
                        request = new RestRequest(Method.POST);

                        request.AddHeader("cache-control", "no-cache");
                        request.AddHeader("Accept", "application/json");
                        request.AddHeader("Content-type", "application/json");
                        request.AddHeader("Authorization", "SSWS " + OktaBase.apiToken + "");                     
                        IRestResponse<resetPasswordResponse> rspResetPassword = client.Execute<resetPasswordResponse>(request);
                        if (rspResetPassword.StatusDescription == "OK")
                        {
                            _pw_url = rspResetPassword.Data.resetPasswordUrl;
                            rspRateLimitCheck = false;
                        }
                        else
                        {
                            rspRateLimitCheck = oktaBase.RateLimitCheck(rspResetPassword);
                        }
                    } while (rspRateLimitCheck) ;





                    

                    }


                    if (_oktauser != null)
                        {                             
                            // update
                            Password _pw = new Password();
                            _pw.Value = _user.Password;
                            _oktauser.Profile.FirstName = _user.FirstName;
                            _oktauser.Profile.LastName = _user.LastName;
                            _oktauser.Profile.Email = _user.Login.ToLower();
                            _oktauser.Profile.Login = _user.Login.ToLower();
                            _oktauser.Credentials.Password = _pw;

                            if (!String.IsNullOrWhiteSpace(_user.AICPAUID))
                                _oktauser.Profile.SetProperty("AICPAUID", _user.AICPAUID);
                            if (!String.IsNullOrWhiteSpace(_user.MemberStatus))
                                _oktauser.Profile.SetProperty("MemberStatus", _user.MemberStatus);
                            if (!String.IsNullOrWhiteSpace(_user.MemberType))
                                _oktauser.Profile.SetProperty("MemberType", _user.MemberType);
                            if (!String.IsNullOrWhiteSpace(_user.Roles))
                            {
                                string[] _roles = _user.Roles.Split(';');
                                _oktauser.Profile.SetProperty("Roles", _roles);
                            }
                            try
                            {
                                _oktauser = OktaBase.usersClient.Update(_oktauser);
                                logger.Debug("Updated User " + _user.Login.ToLower() + " " + _user.FirstName + " " + _user.MiddleName + " " + _user.LastName + " " + _user.Password + " " + _user.AICPAUID + " " + _user.MemberType + " " + _user.MemberStatus + " " + _user.Roles);
                            }
                            catch
                            {                            
                                // some sort of issue, write to log
                                //var line = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}", _user.Login.ToLower(), _user.FirstName, _user.MiddleName, _user.LastName, _user.Password, _user.AICPAUID, _user.MemberType, _user.MemberStatus, _user.Roles);
                                //w.WriteLine(line);
                                //w.Flush();
                                logger.Error("Error Updating " + _user.Login.ToLower() + " " + _user.FirstName + " " + _user.MiddleName + " " + _user.LastName + " " + _user.Password + " " + _user.AICPAUID + " " + _user.MemberType + " " + _user.MemberStatus + " " + _user.Roles);                           
                            }


                            if (!string.IsNullOrEmpty(_oktauser.Id))
                            {
                                // add user to group
                                try
                                {
                                    OktaBase.groupsClient = OktaBase.oktaBaseClient.GetGroupsClient();
                                    Group _grp = OktaBase.groupsClient.Get(OktaBase.AddoktaGroupID);
                                    OktaBase.groupUsersClient = OktaBase.oktaBaseClient.GetGroupUsersClient(_grp);
                                    _oktauser = OktaBase.groupUsersClient.Add(_oktauser);
                                }
                                catch 
                                {
                                    logger.Error("Error Adding User to Group " + _user.Login.ToLower() + " " + _user.FirstName + " " + _user.MiddleName + " " + _user.LastName + " " + _user.Password + " " + _user.AICPAUID + " " + _user.MemberType + " " + _user.MemberStatus + " " + _user.Roles);                              
                                }
                            }

                        }
                        else
                        {
                            // add
                            _oktauser = new User();
                            Password _pw = new Password();
                            _pw.Value = _user.Password;
                            _oktauser.Profile.FirstName = _user.FirstName;
                            _oktauser.Profile.LastName = _user.LastName;
                            _oktauser.Profile.Email = _user.Login.ToLower();
                            _oktauser.Profile.Login = _user.Login.ToLower();
                            _oktauser.Credentials.Password = _pw;
                            if (!String.IsNullOrWhiteSpace(_user.AICPAUID))
                                _oktauser.Profile.SetProperty("AICPAUID", _user.AICPAUID);
                            if (!String.IsNullOrWhiteSpace(_user.MemberStatus))
                                _oktauser.Profile.SetProperty("MemberStatus", _user.MemberStatus);
                            if (!String.IsNullOrWhiteSpace(_user.MemberType))
                                _oktauser.Profile.SetProperty("MemberType", _user.MemberType);
                            if (!String.IsNullOrWhiteSpace(_user.Roles))
                            {
                                string[] _roles = _user.Roles.Split(';');
                                _oktauser.Profile.SetProperty("Roles", _roles);
                            }

                            try
                            {
                                _oktauser = OktaBase.usersClient.Add(_oktauser, true);

                                logger.Debug("Added User " + _user.Login.ToLower() + " " + _user.FirstName + " " + _user.MiddleName + " " + _user.LastName + " " + _user.Password + " " + _user.AICPAUID + " " + _user.MemberType + " " + _user.MemberStatus + " " + _user.Roles);
                                
                            }
                            catch 
                            {
                                logger.Error("Error Adding User " + _user.Login.ToLower() + " " + _user.FirstName + " " + _user.MiddleName + " " + _user.LastName + " " + _user.Password + " " + _user.AICPAUID + " " + _user.MemberType + " " + _user.MemberStatus + " " + _user.Roles);
                                
                            }

                            if (!string.IsNullOrEmpty(_oktauser.Id))
                            {
                                // add user to group
                                try
                                {
                                    OktaBase.groupsClient = OktaBase.oktaBaseClient.GetGroupsClient();
                                    Group _grp = OktaBase.groupsClient.Get(OktaBase.AddoktaGroupID);
                                    OktaBase.groupUsersClient = OktaBase.oktaBaseClient.GetGroupUsersClient(_grp);
                                    _oktauser = OktaBase.groupUsersClient.Add(_oktauser);
                                }
                                catch 
                                {
                                    logger.Error("Error Adding User to Group " + _user.Login.ToLower() + " " + _user.FirstName + " " + _user.MiddleName + " " + _user.LastName + " " + _user.Password + " " + _user.AICPAUID + " " + _user.MemberType + " " + _user.MemberStatus + " " + _user.Roles);
                                
                                }
                            }
                        }

                        #endregion
                    }

                    if (_processed % 1000 == 0)
                    {
                        logger.Info(_processed.ToString() + " - " + DateTime.Now.ToLongTimeString());
                    }
                //else
                //    Console.WriteLine("." + DateTime.Now.ToShortTimeString());
                //reset parameter
                _oktauser = null;
                }
            //   }

            DateTime _enddate = DateTime.Now;
            logger.Debug("End import of: " + _path + " time " + _enddate);
            TimeSpan ts = _enddate - _startdate;
            logger.Info("Time to process count: " + _processed +  " (hours): " + ts.TotalHours.ToString());


        }


        static bool ValidateUser(GAUsers user)
        {
            bool bRet = true;

            if (String.IsNullOrWhiteSpace(user.Login))
                bRet = false;
            if (String.IsNullOrWhiteSpace(user.FirstName))
                bRet = false;
            if (String.IsNullOrWhiteSpace(user.LastName))
                bRet = false;


            return bRet;
        }

        protected override void OnStop()
        {
            logger.Debug("enter OnStop ");
        }

        //allow for interactive session
        internal void TestStartupAndStop(string[] args)
        {
            this.OnStart(args);
            Console.ReadLine();
            this.OnStop();
        }



    }

    [DelimitedRecord("\t")]
    [IgnoreFirst]
    public class GAUsers
    { 
        public string Login;
        public string FirstName;
        public string MiddleName;
        public string LastName;
        public string Password;
        public string AICPAUID;
        public string MemberType;
        public string MemberStatus;
        public string Roles;

    }

    public class resetPasswordResponse
    {
        public string resetPasswordUrl { get; set; }

    }


}


