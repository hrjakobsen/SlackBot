using System;
using System.Runtime.Remoting.Channels;
using System.Threading;

namespace SlackBot
{
    public class AttendanceSlackBot : SlackBotClient
    {
        public AttendanceSlackBot(string token) : base(token, "attendancebot")
        {

            OnChannelJoined += (sender, channel) =>
            {
                SendMessage(channel,
                    "Hi, I'm `attendancebot`." +
                    "\nI can keep attendance in a Slack channel for the members of that channel.\n" +
                    "To get a list of things I can do, simply type `@attendancebot help`");
            };
            OnMessage += (sender, message, channel) =>
            {
                if (message.message.StartsWith($"<@{BotId}>"))
                {
                    SendMessage(channel, "Hello " + message.sender.profile.real_name);
                }
            };
        }
    }
}