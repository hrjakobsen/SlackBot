# SlackBot
This is a Slack bot written in C#. It uses the slack RTM api to listen to changes in a Slack team, and the Slack API to interact with the Slack channels.

To use the bot, generate an API key on https://[YOUR_TEAM].slack.com/apps/new/A0F7YS25R-bots

Then create a class inheriting from `SlackBotClient` and you are good to go. You can use the `OnMessage` or `OnChannelJoined` events to interact with the Slack team. 
