using SampleApp1.Cosmos.Types;
using System;

namespace SampleApp1.Cosmos.Models
{
    #nullable enable annotations
    record Competition
    {
        public string id;
        public string title;
        public Location location;
        public GamingPlatform[] platforms;
        public string[] games;
        public int numberOfRegisteredCompetitors;
        public int? numberOfCompetitors;
        public int? numberOfViewers;
        public CompetitionStatus status;
        public DateTime dateTime;
        public Winner[]? winners;
    }
    #nullable restore
}