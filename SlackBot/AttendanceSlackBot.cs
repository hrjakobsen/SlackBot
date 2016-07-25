using System;
using System.Collections.Generic;

namespace SlackBot
{
    public class AttendanceSlackBot : SlackBotClient
    {
        public AttendanceSlackBot(string token) : base(token, "attendancebot")
        {

            OnChannelJoined += (sender, channel) =>
            {
                SendMessage(channel,
                    "Hi, I'm `" + _name + "`." +
                    "I can keep attendance in a Slack channel for the members of that channel.\n" +
                    "To get a list of things I can do, simply type `<@" + BotId + "|" + _name + "> help`");
            };
            OnMessage += _parseMessage;

        }

        private void _parseMessage(object sender, SlackMessage message, SlackChannel channel)
        {
            if (!message.Text.StartsWith($"<@{BotId}>")) return;

            Dictionary<string, Delegate> commands = new Dictionary<string, Delegate>()
            {
                {"keep", new Action<SlackMessage, SlackChannel>(keepAttendance)},
                {"help", new Action<SlackMessage, SlackChannel>(helpCommand)}
            };

            string[] msgParts = message.Text.Split(' ');

            try
            {
                commands[msgParts[1]].Method.Invoke(this, new object[] {message, channel});
            }
            catch (KeyNotFoundException)
            {
                SendMessage(channel, "I do not understand that command.");
            }
            catch (IndexOutOfRangeException)
            {
                SendMessage(channel, "I do not understand that command.");
            }
        }

        private void keepAttendance(SlackMessage message, SlackChannel channel)
        {
            SendMessage(channel, message.Text);
        }

        private void helpCommand(SlackMessage message, SlackChannel channel)
        {
            string text = "The commands you need to know are:" +
                       "\n        •`<@" + BotId + "|" + _name + "> help` to show this message" +
                       "\n        •`<@" + BotId + "|" + _name + "> init` to start tracking attendance in this channel" +
                       "\n        •`<@" + BotId + "|" + _name + "> keep [users *not* attending]` to register attendance at a meeting" +
                       "\n        •`<@" + BotId + "|" + _name + "> reset` to reset all information about attendance in this channel. Be careful. This *can not* be undone" +
                       "\n        •`<@" + BotId + "|" + _name + "> revert` to remove the last registered meeting" +
                       "\n        •`<@" + BotId + "|" + _name + "> redo [meeting] [users *not* attending]`" +
                       "\n        •`<@" + BotId + "|" + _name + "> image` to get a chart showing the latest attendance information";
            SendMessage(channel, text);
        }

    }
}