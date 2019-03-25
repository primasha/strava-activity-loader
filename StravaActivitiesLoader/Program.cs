using Strava.Activities;
using Strava.Clients;
using System;
using Strava.Streams;

namespace StravaActivitiesLoader
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var auth = new StravaAuthentication().PerformAuthentication();
            Console.WriteLine($"Token: {auth.AccessToken}");
            var client = new StravaClient(auth);
            var athlete = client.Athletes.GetAthlete();
            Console.WriteLine($"{athlete.FirstName} {athlete.LastName}");

            int n = 0;
            client.Activities.ActivityReceived += delegate (object sender, ActivityReceivedEventArgs ea)
            {
                ++n;
                Console.SetCursorPosition(0, 2);
                Console.WriteLine($"Found {n, 5} activities...");
            };

            var allActivities = client.Activities.GetAllActivities();
            //var allActivities = client.Activities.GetActivitiesAfter(new DateTime(2018, 10, 10));

            Console.SetCursorPosition(0, 2);
            Console.WriteLine($"There are {allActivities.Count, 5} activities");

            var publisher = new ActivityPublisher(client, "Cache/Medium", StreamResolution.Medium);

            for (int i = 0; i < allActivities.Count; ++i)
            {
                var a = allActivities[i];
                Console.Write($"{i} {a.Name}");
                if (a.IsTrainer || a.IsManual)
                {
                    Console.WriteLine(" skipped");
                    continue;
                }
                Console.WriteLine("");

                publisher.ProcessActivity(a);
            }
        }
    }
}