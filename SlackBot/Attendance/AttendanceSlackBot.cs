using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OxyPlot;
using System.Text;
using OxyPlot.Axes;
using OxyPlot.WindowsForms;
using LineSeries = OxyPlot.Series.LineSeries;
using Newtonsoft.Json;

namespace SlackBot
{
    public class AttendanceSlackBot : SlackBotClient
    {
        private Dictionary<SlackChannel, AttendanceData> _data;

        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new DictionaryAsArrayResolver()
        };

        public AttendanceSlackBot(string token) : base(token)
        {
            OnChannelJoined += (sender, channel) =>
            {
                SendMessage(channel,
                    "Hi, I'm `" + Name + "`." +
                    "I can keep attendance in a Slack channel for the members of that channel.\n" +
                    "To get a list of things I can do, simply type `<@" + BotId + "|" + Name + "> help`");

            };
            OnMessage += _parseMessage;

            if (File.Exists("attendanceData.txt"))
            {
                _data = JsonConvert.DeserializeObject<Dictionary<SlackChannel, AttendanceData>>(File.ReadAllText(
                    "attendanceData.txt"), settings);
            }
            else
            {
                _data = new Dictionary<SlackChannel, AttendanceData>();
            }

        }

        private void saveData()
        {
            File.WriteAllText("attendanceData.txt", JsonConvert.SerializeObject(_data, settings));
        }

        private void _parseMessage(object sender, SlackMessage message, SlackChannel channel)
        {
            if (!message.Text.StartsWith($"<@{BotId}>")) return;
            if (!_isTracking(channel))
            {
                _data.Add(channel, new AttendanceData());
            }
            Dictionary<string, Delegate> commands = new Dictionary<string, Delegate>()
            {
                {"help", new Action<SlackMessage, SlackChannel>(helpCommand)},
                {"keep", new Action<SlackMessage, SlackChannel>(_keepAttendance)},
                {"track", new Action<SlackMessage, SlackChannel>(_trackUsers)},
                {"image", new Action<SlackMessage, SlackChannel>(_countAttendance)},
                {"reset", new Action<SlackMessage, SlackChannel>(_resetChannel)},
                {"revert", new Action<SlackMessage, SlackChannel>(_revertAttendance)},
                {"redo", new Action<SlackMessage, SlackChannel>(_redoAttendance)}

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

        private void _redoAttendance(SlackMessage message, SlackChannel channel)
        {
            List<SlackUser> usersInMessage = _getUsersFromMessage(message, 3);
            try
            {
                int meeting = int.Parse(message.Text.Split(' ')[2]) + 1;
                foreach (KeyValuePair<SlackUser, List<bool>> pair in _data[channel])
                {
                    pair.Value[meeting] = !usersInMessage.Contains(pair.Key);
                }
                _countAttendance(message, channel);
            }
            catch (InvalidOperationException)
            {
                SendMessage(channel, "That is not a meeting I know of");
            }
            catch (IndexOutOfRangeException)
            {
                SendMessage(channel, "You haven't had that meeting yet");
            }
        }

        private void _trackUsers(SlackMessage message, SlackChannel channel)
        {
            if (!_isTracking(channel))
            {
                _data.Add(channel, new AttendanceData());
            }

            List<SlackUser> usersFromMessage = _getUsersFromMessage(message, 2);
            int numberOfMeetings = _data[channel].Any() ? _data[channel].First().Value.Count : 0;
            _data[channel].Add(numberOfMeetings, usersFromMessage.ToArray());
            saveData();
        }

        private void _countAttendance(SlackMessage message, SlackChannel channel)
        {
            var c =_createChart(channel);
            string fileName = Path.GetTempPath() + Guid.NewGuid() + ".png";
            using (var stream = File.Create(fileName))
            {
                var svgExporter = new PngExporter {Width = 600, Height = 400};
                svgExporter.Export(c, stream);
            }


            SlackSendFile(channel, fileName, "attendance");
        }

        private PlotModel _createChart(SlackChannel channel)
        {
            PlotModel chart = new PlotModel {LegendPosition = LegendPosition.RightTop, LegendPlacement = LegendPlacement.Outside};
            chart.Axes.Add(new LinearAxis {Position = AxisPosition.Left, Minimum = 40, Maximum = 110});
            foreach (KeyValuePair<SlackUser, List<bool>> pair in _data[channel])
            {
                LineSeries series = new LineSeries {MarkerType = MarkerType.Circle};
                int numberOfTimesThere = 0;
                for (int j = 0; j < pair.Value.Count; j++)
                {
                    numberOfTimesThere += pair.Value[j] ? 1 : 0;
                    DataPoint nextPoint = new DataPoint(j + 1, (float) numberOfTimesThere * 100 / (j + 1));
                    series.Points.Add(nextPoint);
                }
                series.Title = pair.Key.Name + " (" + Math.Round(series.Points.Last().Y, 2) + "%)";
                chart.Series.Add(series);
            }

            return chart;


        }

        private void _resetChannel(SlackMessage message, SlackChannel channel)
        {
            _data[channel] = new AttendanceData();
            saveData();
        }

        private void _keepAttendance(SlackMessage message, SlackChannel channel)
        {
            List<SlackUser> usersNotThere = null;
            try
            {
                usersNotThere = _getUsersFromMessage(message, 2);
            }
            catch (ArgumentException e)
            {
                SendMessage(channel, e.Message);
                return;
            }
            _data[channel].Update(usersNotThere);
            _countAttendance(message, channel);
            saveData();
        }

        private List<SlackUser> _getUsersFromMessage(SlackMessage message, int startIndex)
        {
            List<string> userIDs = new List<string>();
            string[] usersPart = message.Text.Split(' ');

            for (int i = startIndex; i < usersPart.Length; i++)
            {
                string user = usersPart[i];
                if (!(user.StartsWith("<@") && user.EndsWith(">")))
                {
                    throw new ArgumentException("User " + user + " is unknown, sorry.");
                }
                string userId = user.Trim('<', '@', '>');
                if (userId.Contains("|"))
                {
                    userId = userId.Split('|')[0];
                }
                userIDs.Add(userId);
            }

            return userIDs.Select(_getUserFromId).ToList();
        }

        private void _revertAttendance(SlackMessage message, SlackChannel channel)
        {
            _data[channel].RemoveMeeting(_data[channel].Count - 1);
        }

        private bool _isTracking(SlackChannel channel)
        {
            return _data.ContainsKey(channel);
        }

        private void helpCommand(SlackMessage message, SlackChannel channel)
        {
            string text = String.Format("The commands you need to know are:" +
                       "\n        •`<@{0}|{1}> help` to show this message" +
                       "\n        •`<@{0}|{1}> keep [users *not* attending]` to register attendance at a meeting" +
                       "\n        •`<@{0}|{1}> reset` to reset all information about attendance in this channel. Be careful. This *can not* be undone" +
                       "\n        •`<@{0}|{1}> revert` to remove the last registered meeting" +
                       "\n        •`<@{0}|{1}> redo [meeting number]` to re-register a meeting (starts at 1!)" +
                       "\n        •`<@{0}|{1}> image` to get a chart showing the latest attendance information", BotId, Name);
            SendMessage(channel, text);
        }

    }
}