using System.Collections.Generic;
using Newtonsoft.Json;

namespace SlackBot
{
    public class SlackPrivateChannel : SlackChannel
    {
        [JsonProperty("is_im")]
        public bool IsDirectMessage { get; set; }

        [JsonProperty("user")]
        public string UserId { get; set; }

        [JsonProperty("created")]
        public int CreatedAt { get; set; }

        [JsonProperty("is_user_deleted")]
        public bool UserIsDeleted { get; set; }
    }
}