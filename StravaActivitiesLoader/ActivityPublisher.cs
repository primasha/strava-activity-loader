using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using Strava.Activities;
using Strava.Clients;
using Strava.Streams;

using Geo;
using Geo.Gps;
using Geo.Gps.Serialization.Xml.Gpx.Gpx11;
using System.Xml.Serialization;

namespace StravaActivitiesLoader
{
    class ActivityPublisher
    {
        StravaClient _client;
        string _rootFolder;
        StreamResolution _streamResolution;
        XmlSerializer _ser = new XmlSerializer(typeof(GpxFile));

        public ActivityPublisher(StravaClient client,
                                 string rootFolder,
                                 StreamResolution streamResolution = StreamResolution.Low)
        {
            _client = client;
            _rootFolder = rootFolder;
            _streamResolution = streamResolution;
        }

        private string GetFileName(string name)
        {
            var newName = new StringBuilder();
            var invalid = new HashSet<char>(Path.GetInvalidFileNameChars());
            foreach (char c in name)
            {
                if (invalid.Contains(c))
                {
                    newName.Append(Uri.HexEscape(c));
                }
                else
                {
                    newName.Append(c);
                }
            }

            return newName.ToString();
        }

        public void ProcessActivity(ActivitySummary actSummary)
        {
            var outputDir = Path.Combine(_rootFolder, actSummary.DateTimeStart.Year.ToString());
            Directory.CreateDirectory(outputDir);

            var fileName = Path.Combine(outputDir, $"{GetFileName(actSummary.Name)}_{actSummary.Id}.gpx");
            if (File.Exists(fileName))
                return;

            var streams = _client.Streams.GetActivityStream(actSummary.Id.ToString(),
                StreamType.LatLng | StreamType.Altitude | StreamType.Time,
                _streamResolution);

            var latlngStream = streams.First(s => s.StreamType == StreamType.LatLng);
            var timeStream = streams.First(s => s.StreamType == StreamType.Time);
            var altitudeStream = streams.First(s => s.StreamType == StreamType.Altitude);

            var track = GenerateTrack(actSummary, latlngStream, timeStream, altitudeStream);

            var gpxFile = new GpxFile
            {
                metadata = new GpxMetadata
                {
                    name = actSummary.Name,
                    time = DateTime.Parse(actSummary.StartDate),
                    timeSpecified = true
                },
                
                trk = new GpxTrack[]
                {
                    track
                }
            };
                        
            using (var st = new StreamWriter(fileName))
            {
                _ser.Serialize(st, gpxFile);
            }
        }

        private GpxTrack GenerateTrack(ActivitySummary actSummary,
            ActivityStream latlngStream,
            ActivityStream timeStream,
            ActivityStream altitudeStream)
        {
            var coordinates = latlngStream.Data.Select(d =>
            {
                var ja = (JArray)d;
                return new Geo.Coordinate(ja[0].ToObject<double>(), ja[1].ToObject<double>());
            }).ToList();

            var startDate = DateTime.Parse(actSummary.StartDate);
            var times = timeStream.Data.Cast<long>().Select(d => startDate.AddSeconds(d)).ToList();
            var altitude = altitudeStream.Data.Cast<double>().ToList();

            var fixes = new GpxWaypoint[coordinates.Count];
            for (int i = 0; i < coordinates.Count; ++i)
            {
                fixes[i] = new GpxWaypoint
                {
                    lat = (decimal)(coordinates[i].Latitude),
                    lon = (decimal)(coordinates[i].Longitude),
                    ele = (decimal)altitude[i],
                    eleSpecified = true,
                    time = times[i],
                    timeSpecified = true
                };
            }

            return new GpxTrack
            {
                type = actSummary.Type.ToString(),
                trkseg = new GpxTrackSegment[]
                {
                    new GpxTrackSegment
                    {
                        trkpt = fixes
                    }
                }
            };
        }
    }
}
