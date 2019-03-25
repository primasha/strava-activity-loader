using System;
using System.IO;
using System.Threading;
using Strava.Authentication;

namespace StravaActivitiesLoader
{
    class StravaAuthentication : WebAuthentication
    {
        static private string ClientId = "2774";
        static private string ClientSecret = "4693316dc7b6ad7e4095cf25b45d13fa2459bd77";

        private ManualResetEventSlim _authCodeReceivedEvent = new ManualResetEventSlim(false);
        private ManualResetEventSlim _accesTokenReceivedEvent = new ManualResetEventSlim(false);

        public StravaAuthentication()
        {
            AuthCodeReceived += OnAuthCodeReceived;
            AccessTokenReceived += OnAccessTokenReceived;
        }

        protected virtual void OnAuthCodeReceived(object sender, AuthCodeReceivedEventArgs args)
        {
            AuthCode = args.AuthCode;
            _authCodeReceivedEvent.Set();
        }

        protected virtual void OnAccessTokenReceived(object sender, TokenReceivedEventArgs args)
        {
            AccessToken = args.Token;
            _accesTokenReceivedEvent.Set();
        }

        public IAuthentication PerformAuthentication()
        {
            string fileName = $"{Environment.UserName}.strava.token";
            string accessToken;
            try
            {
                accessToken = File.ReadAllText(fileName);
            }
            catch (FileNotFoundException)
            {
                _authCodeReceivedEvent.Reset();
                _accesTokenReceivedEvent.Reset();

                GetTokenAsync(ClientId, ClientSecret, Scope.Full);

                WaitHandle.WaitAll(new[] { _accesTokenReceivedEvent.WaitHandle, _authCodeReceivedEvent.WaitHandle });

                File.WriteAllText(fileName, AccessToken);
                accessToken = AccessToken;
            }

            return new StaticAuthentication(accessToken);
        }
    }
}
