using Newtonsoft.Json;

namespace SlackBot
{
    public class SlackChannel
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        public override bool Equals(object obj)
        {
            SlackChannel channel = obj as SlackChannel;
            return channel != null && Id.Equals(channel.Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}