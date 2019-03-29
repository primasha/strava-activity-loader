using Strava.Streams;
using System;
using System.IO;

namespace StravaActivitiesLoader
{
    class StravaCache
    {
        public StravaCache(string root, StreamResolution streamResolution)
        {
            Root = root;
            StreamResolution = streamResolution;
            try
            {
                var info = File.ReadAllText(Path.Combine(Root, "info.txt"));
                LastPollDate = DateTime.SpecifyKind(DateTime.Parse(info), DateTimeKind.Utc);
            }
            catch (Exception)
            {
                //Console.WriteLine("Can't detect last poll date");
                //Console.WriteLine(ex.Message);
            }
        }

        public void UpdateLastPollDate(DateTime newDate)
        {
            File.WriteAllText(Path.Combine(Root, "info.txt"), newDate.ToString());
            LastPollDate = newDate;
        }

        public DateTime LastPollDate { get; private set; }
        public string Root { get; private set; }
        public StreamResolution StreamResolution { get; private set; }
    }
}
