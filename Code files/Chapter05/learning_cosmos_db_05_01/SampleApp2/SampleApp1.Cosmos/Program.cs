namespace SampleApp1.Cosmos
{
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.Linq;
    using Microsoft.Extensions.Configuration;
    using SampleApp1.Cosmos.Models;
    using SampleApp1.Cosmos.Types;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    public class Program
    {
        private static string _databaseId;
        private static string _collectionId;
        private static CosmosClient _client;

        static void Main(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("configuration.json", optional: false, reloadOnChange: false);
            var configuration = configurationBuilder.Build();
            string endpointUrl = configuration["CosmosDB:endpointUrl"];
            string authorizationKey = configuration["CosmosDB:authorizationKey"];
            _databaseId = configuration["CosmosDB:databaseId"];
            _collectionId = configuration["CosmosDB:collectionId"];
            try
            {
                using (_client = new CosmosClient(endpointUrl, authorizationKey))
                {
                    CreateAndQueryCompetitionsWithLinqAsync().Wait();
                }
            }
            catch (CosmosException ce)
            {
                var baseException = ce.GetBaseException();
                Console.WriteLine(
                    $"DocumentClientException occurred. Status code: {ce.StatusCode}; Message: {ce.Message}; Base exception message: {baseException.Message}");
            }
            catch (Exception e)
            {
                var baseException = e.GetBaseException();
                Console.WriteLine(
                    $"Exception occurred. Message: {e.Message}; Base exception message: {baseException.Message}");
            }
            finally
            {
                Console.WriteLine("Press any key to exit the console application.");
                Console.ReadKey();
            }
        }

        private static async Task<Database> RetrieveOrCreateDatabaseAsync()
        {
            // Create a new document database if it doesn’t exist
            var databaseResponse = await _client.CreateDatabaseIfNotExistsAsync(_databaseId);
            switch (databaseResponse.StatusCode)
            {
                case HttpStatusCode.Created:
                    Console.WriteLine($"The database {_databaseId} has been created.");
                    break;
                case HttpStatusCode.OK:
                    Console.WriteLine($"The database {_databaseId} has been retrieved.");
                    break;
            }
            return databaseResponse.Database;
        }

        private static async Task<Container> CreateCollectionIfNotExistsAsync()
        {
            var database = _client.GetDatabase(_databaseId);
            var containerResponse =
                await database
                    .DefineContainer(_collectionId, "/location/zipCode")
                    .WithUniqueKey().Path("/title")
                    .Attach()
                    .CreateIfNotExistsAsync(throughput: 1000);
            switch (containerResponse.StatusCode)
            {
                case HttpStatusCode.Created:
                    Console.WriteLine($"The collection {_collectionId} has been created.");
                    break;
                case HttpStatusCode.OK:
                    Console.WriteLine($"The collection {_collectionId} has been retrieved.");
                    break;
            }

            return containerResponse.Container;
        }
        private static async Task<Competition> GetCompetitionByTitleWithLinq(string title)
        {
            // Build a query to retrieve a Competition with a specific title
            var container = _client.GetDatabase(_databaseId).GetContainer(_collectionId);
            var feedIterator =
                container
                    .GetItemLinqQueryable<Competition>(requestOptions: new QueryRequestOptions { MaxItemCount = 1 })
                    .Where(c => c.title == title)
                    .Select(c => c)
                    .ToFeedIterator();
            while (feedIterator.HasMoreResults)
            {
                foreach (var competition in await feedIterator.ReadNextAsync())
                {
                    Console.WriteLine(
                        $"The Competition with the following title exists: {title}");
                    Console.WriteLine(competition);
                    return competition;
                }
            }

            // No matching document found
            return null;
        }

        private static async Task<Competition> InsertCompetition(Competition competition)
        {
            var container = _client.GetDatabase(_databaseId).GetContainer(_collectionId);
            var itemResponse = await container.CreateItemAsync<Competition>(competition);

            if (itemResponse.StatusCode == HttpStatusCode.Created)
            {
                Console.WriteLine($"The {competition.status} competition with the title {competition.title} has been created.");
            }

            Competition insertedCompetition = itemResponse.Resource; 
            return insertedCompetition;
        }
        private static async Task<Competition> InsertCompetition3()
        {
            var competition = new Competition
            {
                id = "3",
                title = "League of legends - San Diego 2018",
                location = new Location
                {
                    zipCode = "92075",
                    state = "CA"
                },
                platforms = new[]
                {
                    GamingPlatform.Switch
                },
                games = new[]
                {
                    "Fortnite", "NBA Live 19", "PES 2019"
                },
                numberOfRegisteredCompetitors = 80,
                numberOfCompetitors = 30,
                numberOfViewers = 390,
                status = CompetitionStatus.Finished,
                dateTime = DateTime.UtcNow.AddDays(-20),
                winners = new[]
                {
                    new Winner
                    {
                        player = new Player
                        {
                            nickName = "BrandonMilazzo",
                            country = "Italy",
                            city = "Milazzo"
                        },
                        position = 1,
                        score = 12850,
                        prize = 800
                    },
                    new Winner
                    {
                        player = new Player
                        {
                            nickName = "Kevin Maverick",
                            country = "Ireland",
                            city = "Dublin"
                        },
                        position = 2,
                        score = 12500,
                        prize = 400
                    },
                },
            };

            return await InsertCompetition(competition);
        }

        private static async Task<Competition> InsertCompetition4()
        {
            // Insert a document related to a competition that is scheduled 
            // and doesn’t have winners yet
            var competition = new Competition
            {
                id = "4",
                title = "League of legends - San Diego 2019",
                location = new Location
                {
                    zipCode = "92075",
                    state = "CA"
                },
                platforms = new[]
                {
                    GamingPlatform.Switch, GamingPlatform.PC, GamingPlatform.XBox
                },
                games = new[]
                {
                    "Madden NFL 19", "Fortnite"
                },
                status = CompetitionStatus.Scheduled,
                dateTime = DateTime.UtcNow.AddDays(300),
            };

            return await InsertCompetition(competition);
        }
        private static async Task<bool> DoesCompetitionWithTitleExistWithLinq(string competitionTitle)
        {
            var container = _client.GetDatabase(_databaseId).GetContainer(_collectionId);
            var competitionsCount =
                await container
                    .GetItemLinqQueryable<Competition>(requestOptions: new QueryRequestOptions { MaxItemCount = 1 })
                    .Where(c => c.title == competitionTitle)
                    .CountAsync();

            return competitionsCount == 1;
        }
        private static async Task<Competition> UpdateScheduledCompetitionWithPlatforms(string competitionId, 
            string competitionLocationZipCode, 
            DateTime newDateTime, 
            int newNumberOfRegisteredCompetitors, 
            IList<GamingPlatform> newGamingPlatforms)
        {
            // Retrieve a document related to a competition that is scheduled 
            // and update its date, number of registered competitors and platforms
            // The read operation requires the key
            var container = _client.GetDatabase(_databaseId).GetContainer(_collectionId);
            var readResponse = await container.ReadItemAsync<Competition>(competitionId, new PartitionKey(competitionLocationZipCode));
            var competition = readResponse.Resource;
            competition.dateTime = newDateTime;
            competition.numberOfRegisteredCompetitors = newNumberOfRegisteredCompetitors;
            competition.platforms = newGamingPlatforms.ToArray();

            var upsertResponse = await container.UpsertItemAsync<Competition>(competition);

            if (upsertResponse.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine($"The competition with id {competitionId} has been updated.");
            }

            Competition updatedCompetition = upsertResponse.Resource; 
            return updatedCompetition;
        }

        private static async Task<Competition> InsertCompetition2(string competitionId, 
            string competitionTitle, 
            string competitionLocationZipCode)
        {
            // Insert a document related to a competition that is scheduled 
            // and doesn’t have winners yet
            var container = _client.GetDatabase(_databaseId).GetContainer(_collectionId);
            var itemResponse =
                await container
                    .CreateItemAsync<Competition>(
                        new Competition
                        {
                            id = competitionId,
                            title = competitionTitle,
                            location = new Location 
                            { 
                                zipCode = competitionLocationZipCode, 
                                state = "CA", 
                            },
                            platforms = new[] 
                            { 
                                GamingPlatform.PC, GamingPlatform.PS4, GamingPlatform.XBox 
                            },
                            games = new[] 
                            { 
                                "Madden NFL 19", "Fortnite" 
                            },
                            numberOfRegisteredCompetitors = 160,
                            status = CompetitionStatus.Scheduled,
                            dateTime = DateTime.UtcNow.AddDays(50),
                        });
            
            if (itemResponse.StatusCode == HttpStatusCode.Created)
            {
                Console.WriteLine($"The competition with the title {competitionTitle} has been created.");
            }
            
            return itemResponse.Resource;
        }

        private static async Task<bool> DoesCompetitionWithTitleExist(string competitionTitle)
        {
            bool exists = false;
            // Retrieve the number of documents with a specific title
            var container = _client.GetDatabase(_databaseId).GetContainer(_collectionId);
            using var feedIterator =
                container.GetItemQueryIterator<int>(
                    $"SELECT VALUE COUNT(1) FROM Competitions c WHERE c.title = '{competitionTitle}'"
                    , requestOptions: new QueryRequestOptions { MaxItemCount = 1 });
            while (feedIterator.HasMoreResults)
            {
                var feedResponse = await feedIterator.ReadNextAsync();
                exists = feedResponse.FirstOrDefault() == 1;
            }
            
            return exists;
        }

        private static async Task ListScheduledCompetitionsWithLinq()
        {
            // Retrieve the titles for all the scheduled competitions that have more than 5 registered competitors
            var container = _client.GetDatabase(_databaseId).GetContainer(_collectionId);
            using var feedIterator =
                container
                    .GetItemLinqQueryable<Competition>(requestOptions: new QueryRequestOptions { MaxItemCount = 100 })
                    .Where(c => c.numberOfRegisteredCompetitors > 5 && c.status == CompetitionStatus.Scheduled)
                    .Select(c => c.title)
                    .ToFeedIterator();

            while (feedIterator.HasMoreResults)
            {
                var feedResponse = await feedIterator.ReadNextAsync();
                foreach (var title in feedResponse)
                {
                    Console.WriteLine(title);
                }
            }
        }

        private static async Task ListFinishedCompetitionsFirstWinner(GamingPlatform gamingPlatform, string zipCode)
        {
            // Retrieve the winner with the first position for all the finished competitions 
            // that allowed the platform received as an argument
            // and located in the zipCode received as an argument.
            var container = _client.GetDatabase(_databaseId).GetContainer(_collectionId);
            using var feedIterator =
                container
                    .GetItemLinqQueryable<Competition>(requestOptions: new QueryRequestOptions { MaxItemCount = 100 })
                    .Where(c => c.location.zipCode == zipCode && c.status == CompetitionStatus.Finished && c.platforms.Contains(gamingPlatform))
                    .Select(c => c.winners[0])
                    .ToFeedIterator();

            while (feedIterator.HasMoreResults)
            {
                var winnersQueryResult = await feedIterator.ReadNextAsync();
                foreach (var winner in winnersQueryResult)
                {
                    Console.WriteLine($"Nickname: {winner.player.nickName}, Score: {winner.score}");
                }
            }
        }

        private static async Task CreateAndQueryCompetitionsWithLinqAsync()
        {
            var database = await RetrieveOrCreateDatabaseAsync();
            Console.WriteLine(
                $"The database {_databaseId} is available for operations");
            var collection = await CreateCollectionIfNotExistsAsync();
            Console.WriteLine(
                $"The collection {_collectionId} is available for operations");
            var competition3 = await GetCompetitionByTitleWithLinq("League of legends - San Diego 2018");
            if (competition3 == null)
            {
                competition3 = await InsertCompetition3();
            }

            bool isCompetition4Inserted = await DoesCompetitionWithTitleExistWithLinq("League of legends - San Diego 2019");
            Competition competition4;
            if (isCompetition4Inserted)
            {
                competition4 = await GetCompetitionByTitleWithLinq("League of legends - San Diego 2019");
                Console.WriteLine(
                    $"The {competition4.status} competition  with the following title exists: {competition4.title}");
            }
            else
            {
                competition4 = await InsertCompetition4();
            }

            var updatedCompetition4 = await UpdateScheduledCompetitionWithPlatforms("4",
                "92075",
                DateTime.UtcNow.AddDays(300),
                10,
                new List<GamingPlatform>
                {
                    GamingPlatform.PC, GamingPlatform.XBox
                });

            await ListScheduledCompetitionsWithLinq();
            await ListFinishedCompetitionsFirstWinner(GamingPlatform.PS4, "90210");
            await ListFinishedCompetitionsFirstWinner(GamingPlatform.Switch, "92075");
        }
    }
}