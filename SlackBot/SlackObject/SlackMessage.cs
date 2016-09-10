using Newtonsoft.Json;

namespace SlackBot
{
    public class SlackMessage
    {
        [JsonProperty("message")]
        public string Text { get; set; }

        [JsonProperty("sender")]
        public SlackUser Sender { get; set; }

        [JsonProperty("channel")]
        public string Channel { get; set; }

        public SlackMessage(string text, SlackUser sender)
        {
            Text = text;
            Sender = sender;
        }
    }
}