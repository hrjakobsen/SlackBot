using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OxyPlot;
using System.Text;
using OxyPlot.WindowsForms;
using LineSeries = OxyPlot.Series.LineSeries;

namespace SlackBot
{
    public class AttendanceSlackBot : SlackBotClient
    {
        public AttendanceSlackBot(string token) : base(token)
        {


            OnChannelJoined += (sender, channel) =>
            {
                SendMessage(channel,
                    "Hi, I'm `" + Name + "`." +
                    "I can keep attendance in a Slack channel for the members of that channel.\n" +
                    "To get a list of things I can do, simply type `<@" + BotId + "|" + Name + "> help`");
                if (!_isTracking(channel))
                {
                    File.WriteAllText($"Data/{channel.Id}_data.txt", "0");
                }
            };
            OnMessage += _parseMessage;

        }


        private void _parseMessage(object sender, SlackMessage message, SlackChannel channel)
        {
            if (!message.Text.StartsWith($"<@{BotId}>")) return;

            Dictionary<string, Delegate> commands = new Dictionary<string, Delegate>()
            {
                {"help", new Action<SlackMessage, SlackChannel>(helpCommand)},
                {"keep", new Action<SlackMessage, SlackChannel>(_keepAttendance)},
                {"image", new Action<SlackMessage, SlackChannel>(_countAttendance)},
                {"reset", new Action<SlackMessage, SlackChannel>(_resetChannel)},
                {"revert", new Action<SlackMessage, SlackChannel>(_revertAttendance)},
                {"redo", new Action<SlackMessage, SlackChannel>(_notYetImplemented)}

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

        private void _countAttendance(SlackMessage message, SlackChannel channel)
        {
            string[] currentData = File.ReadAllText($"Data/{channel.Id}_data.txt").Split('\n');
            int numberOfMeetings;
            if (!int.TryParse(currentData[0], out numberOfMeetings))
            {
                SendMessage(channel, "This didn't work, sorry :(");
                return;
            }

            List<string> userIds = GetUsersFromChannel(channel).Select(x => x.Id).ToList();

            int[][] userData = new int[userIds.Count][];
            for (int i = 0; i < userIds.Count; i++)
            {
                userData[i] = new int[numberOfMeetings];
                for (int j = 0; j < numberOfMeetings; j++)
                {
                    userData[i][j] = 1;
                }
            }

            for (int i = 1; i < currentData.Length; i++)
            {
                string[] usersNotThere = currentData[i].Split(',');
                for (int j = 1; j < usersNotThere.Length; j++)
                {
                    userData[userIds.IndexOf(usersNotThere[j])][int.Parse(usersNotThere[0]) - 1] = 0;
                }
            }

            var c =_createChart(userData, GetUsersFromChannel(channel).Select(x => x.Name).ToArray());
            string fileName = Path.GetTempPath() + Guid.NewGuid() + ".png";
            string fileNamePng = Path.GetTempPath() + Guid.NewGuid() + ".png";
            using (var stream = File.Create(fileName))
            {
                var svgExporter = new PngExporter {Width = 600, Height = 400};
                svgExporter.Export(c, stream);
            }


            SlackSendFile(channel, fileName, "attendance");


        }


        private static PlotModel _createChart(int[][] data, string[] names)
        {
            Console.WriteLine($"The data array is {data.Length}x{data[0].Length}");

            PlotModel chart = new PlotModel();
            for (int i = 0; i < names.Length; i++)
            {
                LineSeries series = new LineSeries {Title = names[i], MarkerType = MarkerType.Circle};
                int numberOfTimesThere = 0;
                for (int j = 0; j < data[i].Length; j++)
                {
                    numberOfTimesThere += data[i][j];
                    DataPoint nextPoint = new DataPoint(j + 1, (float) numberOfTimesThere * 100 / (j + 1));
                    series.Points.Add(nextPoint);
                }
                chart.Series.Add(series);
            }

            return chart;


        }

        private void _resetChannel(SlackMessage message, SlackChannel channel)
        {
            File.WriteAllText($"Data/{channel.Id}_data.txt", "0");
        }

        private void _keepAttendance(SlackMessage message, SlackChannel channel)
        {
            List<string> userIDs = null;
            try
            {
                userIDs = _getUserIdsFromMessage(message, 2);
            }
            catch (ArgumentException e)
            {
                SendMessage(channel, e.Message);
                return;
            }
            if (!_isTracking(channel))
            {
                File.WriteAllText($"Data/{channel.Id}_data.txt", "0");
            }

            string[] currentData = File.ReadAllText($"Data/{channel.Id}_data.txt").Split('\n');
            int numberOfMeetings = 0;
            if (!int.TryParse(currentData[0], out numberOfMeetings))
            {
                SendMessage(_getChannelFromId(message.Channel), "Attendance data was corrupted. " +
                                                                "I've reset it for you. Sorry about this incident.");
                _resetChannel(message, channel);
            }

            string newFileContent = (++numberOfMeetings).ToString();
            for (int i = 1; i < currentData.Length; i++)
            {
                newFileContent += "\n" + currentData[i];
            }

            if (userIDs.Count != 0)
            {
                string ids = userIDs.Aggregate("\n" + numberOfMeetings + ",", (current, userID) => current + (userID + ","));
                newFileContent += ids.Substring(0, ids.Length - 1);
            }

            File.WriteAllText($"Data/{channel.Id}_data.txt", newFileContent);

            _countAttendance(message, channel);
        }

        private List<string> _getUserIdsFromMessage(SlackMessage message, int startIndex)
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

            return userIDs;
        }

        private void _revertAttendance(SlackMessage message, SlackChannel channel)
        {
            String[] data = File.ReadAllLines($"Data/{channel.Id}_data.txt");

            int numberOfMeetings = int.Parse(data[0]);

            if (numberOfMeetings == 0) return;

            data[0] = (numberOfMeetings - 1).ToString();

            int itemsToWriteToFile = data.Length;

            if (data.Last().StartsWith(numberOfMeetings + ","))
            {
                itemsToWriteToFile--;
            }

            StringBuilder fileData = new StringBuilder();

            for (int i = 0; i < itemsToWriteToFile; i++)
            {
                fileData.Append(data[i] + "\n");
            }

            //Remove last character (a newline)
            fileData.Remove(fileData.Length - 1, 1);

            File.WriteAllText($"Data/{channel.Id}_data.txt", fileData.ToString());
        }

        private void _notYetImplemented(SlackMessage message, SlackChannel channel)
        {
            SendMessage(channel, "This has not been implemented yet");
        }

        private bool _isTracking(SlackChannel channel)
        {
            if (!Directory.Exists("Data"))
            {
                Directory.CreateDirectory("Data");
            }
            return File.Exists($"Data/{channel.Id}_data.txt");
        }

        private void helpCommand(SlackMessage message, SlackChannel channel)
        {
            string text = String.Format("The commands you need to know are:" +
                       "\n        •`<@{0}|{1}> help` to show this message" +
                       "\n        •`<@{0}|{1}> keep [users *not* attending]` to register attendance at a meeting" +
                       "\n        •`<@{0}|{1}> reset` to reset all information about attendance in this channel. Be careful. This *can not* be undone" +
                       "\n        •`<@{0}|{1}> revert` to remove the last registered meeting" +
                       "\n        •`<@{0}|{1}> redo [meeting] [users *not* attending]`" +
                       "\n        •`<@{0}|{1}> image` to get a chart showing the latest attendance information", BotId, Name);
            SendMessage(channel, text);
        }

    }
}