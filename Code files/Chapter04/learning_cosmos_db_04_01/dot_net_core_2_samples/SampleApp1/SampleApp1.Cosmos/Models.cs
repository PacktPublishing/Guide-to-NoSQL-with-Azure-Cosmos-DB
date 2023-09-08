using System;

namespace SampleApp1.Cosmos
{
    #nullable enable annotations
    record Competition
    {
        public string id;
        public string title;
        public Location location;
        public string[] platforms;
        public string[] games;
        public int numberOfRegisteredCompetitors;
        public int? numberOfCompetitors;
        public int? numberOfViewers;
        public string status;
        public DateTime dateTime;
        public Winner[]? winners;
    }
    #nullable restore
    record Location
    {
        public string zipCode;
        public string state;
    }
    record Winner
    {
        public Player player;
        public int position;
        public int score;
        public int prize;
    }
    record Player
    {
        public string nickName;
        public string country;
        public string city;
    }
}