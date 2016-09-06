using System;
using Newtonsoft.Json;

namespace SlackBot
{
    public class SlackUser
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("deleted")]
        public bool Deleted { get; set; }

        [JsonProperty("profile")]
        public SlackProfile Profile { get; set; }

        [JsonProperty("is_admin")]
        public bool IsAdmin { get; set; }

        [JsonProperty("is_owner")]
        public bool IsOwner { get; set; }

        public class SlackProfile
        {
            [JsonProperty("first_name")]
            public string FirstName { get; set; }

            [JsonProperty("last_name")]
            public string LastName { get; set; }

            [JsonProperty("real_name")]
            public string RealName { get; set; }

            [JsonProperty("email")]
            public string Email { get; set; }

            [JsonProperty("skype")]
            public string Skype { get; set; }

            [JsonProperty("phone")]
            public string Phone { get; set; }
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            SlackUser user = obj as SlackUser;
            return user != null && user.Name == Name;
        }
    }
}