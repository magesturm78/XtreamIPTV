using System;
using System.Collections.Generic;
using System.IO;

namespace XtreamIPTV.Models
{
    public class M3u8Segment
    {
        public double Duration { get; set; }
        public string Title { get; set; }
        public string Uri { get; set; }
    }

    public class M3u8Playlist
    {
        public int TargetDuration { get; set; }
        public int PlaylistVersion { get; set; }
        public List<M3u8Segment> Segments { get; set; } = new List<M3u8Segment>();

        public static M3u8Playlist Parse(string filePath)
        {
            var playlist = new M3u8Playlist();
            var lines = File.ReadAllLines(filePath);

            M3u8Segment currentSegment = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("#EXT-X-TARGETDURATION:"))
                {
                    playlist.TargetDuration = int.Parse(line.Split(':')[1]);
                }
                else if (line.StartsWith("#EXT-X-VERSION:"))
                {
                    playlist.PlaylistVersion = int.Parse(line.Split(':')[1]);
                }
                else if (line.StartsWith("#EXTINF:"))
                {
                    // Parse duration and title: #EXTINF:10.0, Title
                    var parts = line.Replace("#EXTINF:", "").Split(',');
                    currentSegment = new M3u8Segment
                    {
                        Duration = double.Parse(parts[0].Trim()),
                        Title = parts.Length > 1 ? parts[1].Trim() : ""
                    };
                }
                else if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                {
                    if (currentSegment != null)
                    {
                        currentSegment.Uri = line.Trim();
                        playlist.Segments.Add(currentSegment);
                        currentSegment = null; // Reset for the next segment
                    }
                }
            }

            return playlist;
        }
    }
}
