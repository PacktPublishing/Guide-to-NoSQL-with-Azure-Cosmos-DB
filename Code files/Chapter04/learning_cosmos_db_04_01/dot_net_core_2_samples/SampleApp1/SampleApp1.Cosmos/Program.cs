namespace SampleApp1.Cosmos
{
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.Fluent;
    using Microsoft.Extensions.Configuration;
    using System;
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
                    CreateAndQueryDynamicDocumentsAsync().Wait();
                }
            }
            catch (CosmosException ce)
            {
                var baseException = ce.GetBaseException();
                Console.WriteLine(
                    $"CosmosException occurred. Status code: {ce.StatusCode}; Message: {ce.Message}; Base exception message: {baseException.Message}");
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
        private static async Task<dynamic> GetCompetitionByTitle(string title)
        {
            // Retrieve a document with a specific title
            var container = _client.GetDatabase(_databaseId).GetContainer(_collectionId);
            using var feedIterator = 
                container.GetItemQueryIterator<Competition>(
                    $"SELECT * FROM Competitions c WHERE c.title = '{title}'"
                    , requestOptions: new QueryRequestOptions { MaxItemCount = 1 });
            while (feedIterator.HasMoreResults)
            {
                foreach (var competition in await feedIterator.ReadNextAsync())
                {
                    Console.WriteLine(
                        $"The document with the following title exists: {title}");
                    Console.WriteLine(competition);
                    return competition;
                }
            }

            // No matching document found
            return null;
        }

        private static async Task<Competition> InsertCompetition1(string competitionId, 
            string competitionTitle, 
            string competitionLocationZipCode)
        {
            // Insert a document related to a competition that has finished and has winners
            var container = _client.GetDatabase(_databaseId).GetContainer(_collectionId);            
            var itemResponse =
                await container.CreateItemAsync<Competition>(
                    new Competition
                    {
                        id = competitionId,
                        title = competitionTitle,
                        location = new Location { zipCode = competitionLocationZipCode, state = "CA", },
                        platforms = new[] { "PS4", "XBox", "Switch" },
                        games = new[] { "Fortnite", "NBA Live 19" },
                        numberOfRegisteredCompetitors = 80,
                        numberOfCompetitors = 60,
                        numberOfViewers = 300,
                        status = "Finished",
                        dateTime = DateTime.UtcNow.AddDays(-50),
                        winners = new Winner[]
                        {
                            new Winner
                            {
                                player = new Player { nickName = "EnzoTheGreatest", country = "Italy", city = "Rome" },
                                position = 1,
                                score = 7500,
                                prize = 1500,
                            },
                            new Winner
                            {
                                player = new Player { nickName = "NicoInGamerLand", country = "Argentina", city = "Buenos Aires" },
                                position = 2,
                                score = 6500,
                                prize = 750,
                            },
                            new Winner
                            {
                                player = new Player { nickName = "KiwiBoy", country = "New Zealand", city = "Auckland" },
                                position = 3,
                                score = 3500,
                                prize = 250,
                            }
                        },
                    });
            
            if (itemResponse.StatusCode == HttpStatusCode.Created)
            {
                Console.WriteLine($"The competition with the title {competitionTitle} has been created.");
            }

            return itemResponse.Resource;
        }

        private static async Task<Competition> InsertCompetition2(string competitionId, 
            string competitionTitle, 
            string competitionLocationZipCode)
        {
            // Insert a document related to a competition that is scheduled 
            // and doesn’t have winners yet
            var container = _client.GetDatabase(_databaseId).GetContainer(_collectionId);
            var itemResponse =
                await container.CreateItemAsync<Competition>(
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
                            "PC", "PS4", "XBox" 
                        },
                        games = new[] 
                        { 
                            "Madden NFL 19", "Fortnite" 
                        },
                        numberOfRegisteredCompetitors = 160,
                        status = "Scheduled",
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

        private static async Task<Competition> UpdateScheduledCompetition(
            string competitionId, string competitionLocationZipCode, DateTime newDateTime, int newNumberOfRegisteredCompetitors)
        {
            // Retrieve a document related to a competition that is scheduled 
            // and update its date and its number of registered competitors
            // The read operation requires the partition key
            var container = _client.GetDatabase(_databaseId).GetContainer(_collectionId);
            var readResponse = await container.ReadItemAsync<Competition>(competitionId, new PartitionKey(competitionLocationZipCode));
            var competition = readResponse.Resource;
            competition.dateTime = newDateTime;
            competition.numberOfRegisteredCompetitors = newNumberOfRegisteredCompetitors;
            var upsertResponse = await container.UpsertItemAsync<Competition>(competition);
            if (upsertResponse.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine($"The competition with id {competitionId} has been updated.");
            }
            return upsertResponse.Resource;
        }
        private static async Task ListScheduledCompetitions()
        {
            // Retrieve the titles for all the scheduled competitions that have more than 200 registered competitors
            var container = _client.GetDatabase(_databaseId).GetContainer(_collectionId);
            using var feedIterator =
                container.GetItemQueryIterator<string>(
                    $"SELECT VALUE c.title FROM Competitions c WHERE c.numberOfRegisteredCompetitors > 200 AND c.status = 'Scheduled'"
                    , requestOptions: new QueryRequestOptions { MaxItemCount = 100 });
            while (feedIterator.HasMoreResults)
            {
                var feedResponse = await feedIterator.ReadNextAsync();
                foreach (var title in feedResponse)
                {
                    Console.WriteLine(title);
                }
            }
        }

        private static async Task CreateAndQueryDynamicDocumentsAsync()
        {
            var database = await RetrieveOrCreateDatabaseAsync();
            Console.WriteLine(
                $"The database {_databaseId} is available for operations");
            var collection = await CreateCollectionIfNotExistsAsync();
            Console.WriteLine(
                $"The collection {_collectionId} is available for operations");
            string competition1Id = "1";
            string competition1Title = "Crowns for Gamers - Portland 2018";
            string competition1ZipCode = "90210";
            var competition1 = await GetCompetitionByTitle(competition1Title);
            if (competition1 == null)
            {
                competition1 = await InsertCompetition1(competition1Id, competition1Title, competition1ZipCode);
            }

            string competition2Title = "Defenders of the crown - San Diego 2018";
            bool isCompetition2Inserted = await DoesCompetitionWithTitleExist(competition2Title);
            string competition2Id = "2";
            string competition2LocationZipCode = "92075";
            if (isCompetition2Inserted)
            {
                Console.WriteLine(
                    $"The document with the following title exists: {competition2Title}");
            }
            else
            {
                var competition2 = await InsertCompetition2(competition2Id, competition2Title, competition2LocationZipCode);
            }

            var updatedCompetition2 = 
                await UpdateScheduledCompetition(competition2Id, 
                    competition2LocationZipCode, 
                    DateTime.UtcNow.AddDays(60), 
                    250);

            await ListScheduledCompetitions();
        }
    }
}
