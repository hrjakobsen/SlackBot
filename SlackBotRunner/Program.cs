using System;
using SlackBot;

namespace SlackBotRunner
{
    class Program
    {
        static void Main(string[] args)
        {

            AttendanceSlackBot bot = new AttendanceSlackBot(Environment.GetEnvironmentVariable("SLACK_API_KEY"));
            Console.ReadKey();
        }
    }
}
