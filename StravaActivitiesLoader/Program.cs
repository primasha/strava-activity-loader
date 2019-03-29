using Strava.Activities;
using Strava.Clients;
using System;
using Strava.Streams;
using System.Collections.Generic;
using CommandLine;
using System.Linq;

namespace StravaActivitiesLoader
{
    public class Program
    {
        public class Options
        {
            [Option(Default = new string[] {"All", "High", "Medium", "Low" }, Separator = ' ')]
            public IEnumerable<string> Streams { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).
                WithParsed(o =>
                {
                    Console.OutputEncoding = System.Text.Encoding.UTF8;

                    var auth = new StravaAuthentication().PerformAuthentication();
                    Console.WriteLine($"Token: {auth.AccessToken}");
                    var client = new StravaClient(auth);
                    var athlete = client.Athletes.GetAthlete();
                    Console.WriteLine($"{athlete.FirstName} {athlete.LastName}");

                    foreach (var r in o.Streams)
                    {
                        if (Enum.TryParse(r, true, out StreamResolution streamResolution))
                        {
                            Console.WriteLine($"Syncing [{streamResolution}]");
                            var cache = new StravaCache($"{athlete.LastName}_{athlete.Id}/{streamResolution}", streamResolution);

                            SyncStravaCache(client, cache);
                        }
                        else
                        {
                            Console.WriteLine($"Unknown stream resolution [{r}]");
                        }
                    }
                });
        }

        private static void SyncStravaCache(StravaClient client, StravaCache cache)
        {
            var allActivities = new List<ActivitySummary>();
            var pollDate = DateTime.UtcNow;
            int n = 0;
            EventHandler<ActivityReceivedEventArgs> OnActivityRecieved = delegate(object sender, ActivityReceivedEventArgs ea)
            {
                ++n;
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.WriteLine($"Found {n,5} activities...");
            };

            client.Activities.ActivityReceived += OnActivityRecieved;

            if (cache.LastPollDate == default(DateTime))
            {
                allActivities = client.Activities.GetAllActivities();
            }
            else
            {
                allActivities = client.Activities.GetActivitiesAfter(cache.LastPollDate);
            }
            client.Activities.ActivityReceived -= OnActivityRecieved;

            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine($"There are {allActivities.Count,5} activities");

            var publisher = new ActivityPublisher(client, cache.Root, cache.StreamResolution);

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

            cache.UpdateLastPollDate(pollDate);
        }
    }
}
