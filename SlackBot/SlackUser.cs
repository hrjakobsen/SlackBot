namespace SlackBot
{
    public class SlackUser
    {
        public string id { get; set; }
        public string name { get; set; }
        public bool deleted { get; set; }
        public SlackProfile profile { get; set; }
        public bool is_admin { get; set; }
        public bool is_owner { get; set; }

        public class SlackProfile
        {
            public string first_name { get; set; }
            public string last_name { get; set; }
            public string real_name { get; set; }
            public string email { get; set; }
            public string skype { get; set; }
            public string phone { get; set; }
        }
    }
}