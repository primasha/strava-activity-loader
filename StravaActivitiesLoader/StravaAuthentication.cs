using System;
using System.IO;
using System.Threading;
using Strava.Authentication;

namespace StravaActivitiesLoader
{
    class StravaAuthentication : WebAuthentication
    {
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

                var lines = File.ReadAllLines($"strava.client.secret.token");
                // ClientId, ClientSecret
                GetTokenAsync(lines[0], lines[1], Scope.Full);

                WaitHandle.WaitAll(new[] { _accesTokenReceivedEvent.WaitHandle, _authCodeReceivedEvent.WaitHandle });

                File.WriteAllText(fileName, AccessToken);
                accessToken = AccessToken;
            }

            return new StaticAuthentication(accessToken);
        }
    }
}
