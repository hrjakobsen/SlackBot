# SlackBot
This is a C# Slack bot SDK. It uses the slack RTM API to listen to changes in a Slack team, and the Slack API to interact with the Slack channels.

To use the SDK, generate an API key on https://my.slack.com/apps/new/A0F7YS25R-bots

Then create a class inheriting from `SlackBotClient` and you are good to go. You can use the `OnMessage` or `OnChannelJoined` events to interact with the Slack team. 
