﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace SlackBot
{
    public class SlackPublicChannel : SlackChannel
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("is_channel")]
        public string IsChannel { get; set; }

        [JsonProperty("creator")]
        public string Creator { get; set; }

        [JsonProperty("is_archived")]
        public bool IsArchived { get; set; }

        [JsonProperty("is_general")]
        public bool IsGeneral { get; set; }

        [JsonProperty("members")]
        public List<string> Members { get; set; }

        [JsonProperty("topic")]
        public SlackTopic Topic { get; set; }

        [JsonProperty("purpose")]
        public SlackPurpose Purpose { get; set; }

        [JsonProperty("is_member")]
        public bool IsMember { get; set; }


        public class SlackTopic
        {
            [JsonProperty("value")]
            public string Value { get; set; }

            [JsonProperty("creator")]
            public string Creator { get; set; }
        }

        public class SlackPurpose
        {
            [JsonProperty("value")]
            public string Value { get; set; }

            [JsonProperty("creator")]
            public string Creator { get; set; }
        }
    }
}