using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using WebSocket = WebSocketSharp.WebSocket;

namespace SlackBot
{
    public abstract class SlackBotClient
    {
        private string _token;
        protected string Name { get; private set; }
        private WebSocket _socket;
        private bool _running = true;
        public string BotId { get; private set; }

        public List<SlackUser> Users => _getUsers();
        public List<SlackPublicChannel> PublicChannels => _getPublicChannels();
        public List<SlackPrivateChannel> PrivateChannels => _getPrivateChannels();

        public delegate void MessageHandler(object sender, SlackMessage message, SlackChannel channel);
        public event MessageHandler OnMessage;

        public delegate void ChannelJoinedHandler(object sender, SlackChannel channel);
        public event ChannelJoinedHandler OnChannelJoined;

        /// <summary>Initializes a new Slack client using a token generated on Slack and gives it a name</summary>
        /// <param name="token">Generated token from slack which allows the client to connect to a team</param>
        /// <param name="name">name given on slack so that the bot can identify itself</param>
        public SlackBotClient(string token)
        {
            _token = token;
            _start();
        }

        /// <summary>
        ///     Make a call directly to the Slack api.
        /// </summary>
        /// <param name="api">Slack api method such as "chat.postMessage"</param>
        /// <param name="paramaters">JSON object containing parameters for the Slack method</param>
        /// <returns>JSON response from the server</returns>
        public JObject ApiCall(string api, JObject paramaters)
        {
            WebClient wc = new WebClient();

            string urlParams = "?token=" + _token;
            if (paramaters != null)
            {
                foreach (KeyValuePair<string, JToken> paramater in paramaters)
                {
                    urlParams += $"&{paramater.Key}={paramater.Value}";
                }
            }

            string response = wc.DownloadString($"https://slack.com/api/" + api + urlParams);
            Console.WriteLine($"https://slack.com/api/"+api+urlParams);
            Console.WriteLine(JObject.Parse(response));
            return JObject.Parse(response);
        }

        private void _start()
        {
            WebClient wc = new WebClient();
            JObject response = ApiCall("rtm.start", null);

            _socket = new WebSocket(response["url"].ToString());
            _socket.OnMessage += _parseMessage;

            _socket.Connect();

            _setNameAndId();


            Task task = Task.Run((Action) _pingPongGame);
        }

        private void _setNameAndId()
        {
            JObject response = ApiCall("auth.test", null);
            if ((bool) response["ok"] != true)
            {
                throw new Exception("Couldn't retrieve name and id from Slack");
            }
            Name = (string) response["user"];
            BotId = (string) response["user_id"];
        }

        private bool _isFromSelf(JObject message)
        {
            if (message["subtype"] != null && (string)message["subtype"] == "bot_message")
            {
                return (string) message["username"] == Name;
            }
            return (string) message["user"] == BotId;
        }

        private void _parseMessage(object sender, MessageEventArgs e)
        {
            string jsonString = e.Data;
            JObject message = JObject.Parse(jsonString);
            Console.WriteLine(message);
            if (_isMessage(message) && !_isFromSelf(message))
            {
                OnMessage?.Invoke(this, new SlackMessage((string)message["text"],
                    _getUserFromId((string)message["user"])),
                    _getChannelFromId((string)message["channel"]));
            }
            else if (_isChannelJoined(message))
            {
                SlackChannel channel = _getChannelFromId((string) message["channel"]["id"]);
                OnChannelJoined?.Invoke(this, channel);
            }
        }

        protected List<SlackUser> _getUsers()
        {
            JObject response = ApiCall("users.list", new JObject());

            if (!((bool)response["ok"]))
            {
                throw new Exception(response["error"].ToString());
            }

            JArray arr = (JArray)response["members"];
            List<SlackUser> users = arr.ToObject<List<SlackUser>>();

            return users;
        }

        protected List<SlackPublicChannel> _getPublicChannels()
        {
            JObject response = ApiCall("channels.list", new JObject());

            if (!((bool) response["ok"]))
            {
                throw new Exception(response["error"].ToString());
            }

            JArray arr = (JArray) response["channels"];
            List<SlackPublicChannel> channels = arr.ToObject<List<SlackPublicChannel>>();

            return channels;
        }

        protected List<SlackPrivateChannel> _getPrivateChannels()
        {
            JObject response = ApiCall("im.list", new JObject());

            if (!((bool) response["ok"]))
            {
                throw new Exception(response["error"].ToString());
            }

            JArray arr = (JArray) response["ims"];
            List<SlackPrivateChannel> channels = arr.ToObject<List<SlackPrivateChannel>>();

            return channels;
        }

        protected SlackChannel _getChannelFromId(string id)
        {
            SlackChannel channel = null;
            if (id.StartsWith("C")) // public channel
            {

                JObject parameters = new JObject {{"channel", id}};
                JObject response = ApiCall("channels.info", parameters);

                if (!((bool) response["ok"]))
                {
                    throw new Exception(response["error"].ToString());
                }

                JObject Jchannel = (JObject) response["channel"];
                channel = Jchannel.ToObject<SlackPublicChannel>();
            }
            else if (id.StartsWith("D")) // directmessage
            {
                channel = PrivateChannels.FirstOrDefault(privateChannel => privateChannel.Id == id);
            }


            return channel;
        }

        protected SlackUser _getUserFromId(string id)
        {
            JObject parameters = new JObject {{"user", id}};
            JObject response = ApiCall("users.info", parameters);

            if (!((bool) response["ok"]))
            {
                throw new Exception(response["error"].ToString());
            }

            JObject Juser = (JObject) response["user"];
            SlackUser user = Juser.ToObject<SlackUser>();

            return user;
        }

        private static bool _isMessage(JObject response)
        {
            return ((string) response["type"] == "message");
        }



        private static bool _isChannelJoined(JObject response)
        {
            return ((string) response["type"] == "channel_joined");
        }

        private void _pingPongGame()
        {
            while (_running)
            {
                Thread.Sleep(2000);
                _socket.Send("{\"id\":199999,\"type\":\"ping\",}");
            }
        }

        /// <summary>Sends a message to a given slack channel</summary>
        /// <param name="channel">Slack channel to send the message to</param>
        /// <param name="message">Message to send to the channel, this supports slack message formatting</param>
        public void SendMessage(SlackChannel channel, string message)
        {
            JObject parameters = new JObject
            {
                {"channel", channel.Id},
                {"text", message},
                {"username", Name}
            };
            ApiCall("chat.postMessage", parameters);
        }

        /// <summary>Uploads a file to a slack channel</summary><
        /// <param name="channel">Channel to upload the file in</param>
        /// <param name="path">Path to the file to upload</param>
        /// <param name="filename">Filename for the uploaded file on slack</param>
        public void SlackSendFile(SlackChannel channel, string path, string filename)
        {
            WebClient wc = new WebClient();
            wc.UploadFile("https://slack.com/api/files.upload?token=" + _token + "&filename=" + filename + "&channels=" + channel.Id, path);
        }

        public List<SlackUser> GetUsersFromChannel(SlackPublicChannel channel)
        {
            return channel.Members.Select(_getUserFromId).ToList();
        }
    }
}
