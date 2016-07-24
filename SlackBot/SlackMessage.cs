namespace SlackBot
{
    public class SlackMessage
    {
        public string message { get; set; }
        public SlackUser sender { get; set; }
        public string channel { get; set; }

        public SlackMessage(string message, SlackUser sender)
        {
            this.message = message;
            this.sender = sender;
        }
    }
}