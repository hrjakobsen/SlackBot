using System.Collections.Generic;

namespace SlackBot
{
    public class SlackChannel
    {
        public string id { get; set; }
        public string name { get; set; }
        public string is_channel { get; set; }
        public string creator { get; set; }
        public bool is_archived { get; set; }
        public bool is_general { get; set; }
        public List<string> members { get; set; }
        public SlackTopic topic { get; set; }
        public SlackPurpose purpose { get; set; }
        public bool is_member { get; set; }


        public class SlackTopic
        {
            public string value { get; set; }
            public string creator { get; set; }
        }

        public class SlackPurpose
        {
            public string value { get; set; }
            public string creator { get; set; }
        }

    }
}