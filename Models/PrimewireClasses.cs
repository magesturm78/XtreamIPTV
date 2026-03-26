using System;
using System.Collections.Generic;
using System.Text;

namespace XtreamIPTV.Models
{
    public class KeyData
    {
        public string link { get; set; }
        public int host_id { get; set; }
        public string host { get; set; }
    }

    // PrimewireRoot myDeserializedClass = JsonConvert.DeserializeObject<PrimewireRoot>(myJsonResponse);
    public class Info
    {
        public string type { get; set; }
        public object tvmaze_id { get; set; }
        public string tmdb_image { get; set; }
        public string tmdb_id { get; set; }
        public string tmdb_backdrop { get; set; }
        public string title { get; set; }
        public string status { get; set; }
        public string release_date { get; set; }
        public string imdb_id { get; set; }
        public string description { get; set; }
    }

    public class PrimewireRoot
    {
        public List<Server> servers { get; set; }
        public Info info { get; set; }
    }

    public class Server
    {
        public object quality { get; set; }
        public string name { get; set; }
        public string key { get; set; }
        public string file_size { get; set; }
        public string file_name { get; set; }
        public string audio_type { get; set; }
        public string audio_language { get; set; }
    }
}
