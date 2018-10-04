namespace SampleApp1
{
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using SampleApp1.Models;
    using SampleApp1.Types;
    using System.Collections.Generic;

    public class Program
    {
        private static string databaseId;
        private static string collectionId;
        private static DocumentClient client;
        private static Uri collectionUri;

        public static void Main(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("configuration.json", optional: false, reloadOnChange: false);
            var configuration = configurationBuilder.Build();
            string endpointUrl = configuration["CosmosDB:endpointUrl"];
            string authorizationKey = configuration["CosmosDB:authorizationKey"];
            databaseId = configuration["CosmosDB:databaseId"];
            collectionId = configuration["CosmosDB:collectionId"];
            try
            {
                using (client = new DocumentClient(new Uri(endpointUrl), authorizationKey))
                {
                    CreateAndQueryCompetitionsWithLinqAsync().Wait();
                }
            }
            catch (DocumentClientException dce)
            {
                var baseException = dce.GetBaseException();
                Console.WriteLine(
                    $"DocumentClientException occurred. Status code: {dce.StatusCode}; Message: {dce.Message}; Base exception message: {baseException.Message}");
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
            var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(
                new Database
                {
                    Id = databaseId,
                });
            switch (databaseResponse.StatusCode)
            {
                case System.Net.HttpStatusCode.Created:
                    Console.WriteLine($"The database {databaseId} has been created.");
                    break;
                case System.Net.HttpStatusCode.OK:
                    Console.WriteLine($"The database {databaseId} has been retrieved.");
                    break;
            }
            return databaseResponse.Resource;
        }

        private static async Task<Offer> UpdateOfferForCollectionAsync(string collectionSelfLink, int newOfferThroughput)
        {
            // Create an asynchronous query to retrieve the current offer for the document collection
            // Notice that the current version of the API only allows to use the SelfLink for the collection 
            // to retrieve its associated offer
            Offer existingOffer = null;
            var offerQuery = client.CreateOfferQuery()
                .Where(o => o.ResourceLink == collectionSelfLink)
                .AsDocumentQuery();
            while (offerQuery.HasMoreResults)
            {
                foreach (var offer in await offerQuery.ExecuteNextAsync<Offer>())
                {
                    existingOffer = offer;
                }
            }
            if (existingOffer == null)
            {
                throw new Exception("I couldn't retrieve the offer for the collection.");
            }
            // Set the desired throughput to newOfferThroughput RU/s for the new offer built based on the current offer
            var newOffer = new OfferV2(existingOffer, newOfferThroughput);
            var replaceOfferResponse = await client.ReplaceOfferAsync(newOffer);

            return replaceOfferResponse.Resource;
        }

        private static async Task<DocumentCollection> CreateCollectionIfNotExistsAsync()
        {
            var databaseUri = UriFactory.CreateDatabaseUri(databaseId);
            DocumentCollection documentCollectionResource;
            var isCollectionCreated = await client.CreateDocumentCollectionQuery(databaseUri)
                .Where(c => c.Id == collectionId)
                .CountAsync() == 1;
            if (isCollectionCreated)
            {
                Console.WriteLine($"The collection {collectionId} already exists.");
                var documentCollectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
                var documentCollectionResponse = await client.ReadDocumentCollectionAsync(documentCollectionUri);
                documentCollectionResource = documentCollectionResponse.Resource;
            }
            else
            {
                var documentCollection = new DocumentCollection
                {
                    Id = collectionId,
                };
                documentCollection.PartitionKey.Paths.Add("/location/zipCode");
                var uniqueKey = new UniqueKey();
                uniqueKey.Paths.Add("/title");
                documentCollection.UniqueKeyPolicy.UniqueKeys.Add(uniqueKey);
                var requestOptions = new RequestOptions
                {
                    OfferThroughput = 1000,
                };
                var collectionResponse = await client.CreateDocumentCollectionAsync(
                    databaseUri,
                    documentCollection,
                    requestOptions);
                if (collectionResponse.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    Console.WriteLine($"The collection {collectionId} has been created.");
                }
                documentCollectionResource = collectionResponse.Resource;
            }

            return documentCollectionResource;
        }

        private static async Task<Competition> GetCompetitionByTitleWithLinq(string title)
        {
            // Build a query to retrieve a Competition with a specific title
            var documentQuery = client.CreateDocumentQuery<Competition>(collectionUri,
                new FeedOptions()
                {
                    EnableCrossPartitionQuery = true,
                    MaxItemCount = 1
                })
                .Where(c => c.Title == title)
                .Select(c => c)
                .AsDocumentQuery();
            while (documentQuery.HasMoreResults)
            {
                var feedResponse = await documentQuery.ExecuteNextAsync<Competition>();
                foreach (var competition in feedResponse)
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
            var documentResponse = await client.CreateDocumentAsync(collectionUri, competition);

            if (documentResponse.StatusCode == System.Net.HttpStatusCode.Created)
            {
                Console.WriteLine($"The {competition.Status} competition with the title {competition.Title} has been created.");
            }

            Competition insertedCompetition = (dynamic) documentResponse.Resource;
            return insertedCompetition;
        }

        private static async Task<Competition> InsertCompetition3()
        {
            var competition = new Competition
            {
                Id = "3",
                Title = "League of legends - San Diego 2018",
                Location = new Location
                {
                    ZipCode = "92075",
                    State = "CA"
                },
                Platforms = new[]
                {
                    GamingPlatform.Switch
                },
                Games = new[]
                {
                    "Fortnite", "NBA Live 19", "PES 2019"
                },
                NumberOfRegisteredCompetitors = 80,
                NumberOfCompetitors = 30,
                NumberOfViewers = 390,
                Status = CompetitionStatus.Finished,
                DateTime = DateTime.UtcNow.AddDays(-20),
                Winners = new[]
                {
                    new Winner
                    {
                        Player = new Player
                        {
                            NickName = "BrandonMilazzo",
                            Country = "Italy",
                            City = "Milazzo"
                        },
                        Position = 1,
                        Score = 12850,
                        Prize = 800
                    },
                    new Winner
                    {
                        Player = new Player
                        {
                            NickName = "Kevin Maverick",
                            Country = "Ireland",
                            City = "Dublin"
                        },
                        Position = 2,
                        Score = 12500,
                        Prize = 400
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
                Id = "4",
                Title = "League of legends - San Diego 2019",
                Location = new Location
                {
                    ZipCode = "92075",
                    State = "CA"
                },
                Platforms = new[]
                {
                    GamingPlatform.Switch, GamingPlatform.PC, GamingPlatform.XBox
                },
                Games = new[]
                {
                    "Madden NFL 19", "Fortnite"
                },
                Status = CompetitionStatus.Scheduled,
                DateTime = DateTime.UtcNow.AddDays(300),
            };

            return await InsertCompetition(competition);
        }

        private static async Task<bool> DoesCompetitionWithTitleExistWithLinq(string competitionTitle)
        {
            var competitionsCount = await client.CreateDocumentQuery<Competition>(collectionUri,
                new FeedOptions()
                {
                    EnableCrossPartitionQuery = true,
                    MaxItemCount = 1,
                })
                .Where(c => c.Title == competitionTitle)
                .CountAsync();

            return (competitionsCount == 1);
        }

        private static async Task<Competition> UpdateScheduledCompetitionWithPlatforms(string competitionId, 
            string competitionLocationZipCode,
            DateTime newDateTime,
            int newNumberOfRegisteredCompetitors,
            IList<GamingPlatform> newGamingPlatforms)
        {
            // Retrieve a document related to a competition that is scheduled 
            // and update its date, number of registered competitors and platforms
            // The read operation requires the partition key
            var documentToUpdateUri = UriFactory.CreateDocumentUri(databaseId, collectionId, competitionId);
            var readCompetitionResponse = await client.ReadDocumentAsync<Competition>(documentToUpdateUri, new RequestOptions()
            {
                PartitionKey = new PartitionKey(competitionLocationZipCode)
            });
            readCompetitionResponse.Document.DateTime = newDateTime;
            readCompetitionResponse.Document.NumberOfRegisteredCompetitors = newNumberOfRegisteredCompetitors;
            readCompetitionResponse.Document.Platforms = newGamingPlatforms.ToArray();

            var updatedCompetitionResponse = await client.ReplaceDocumentAsync(
                documentToUpdateUri,
                readCompetitionResponse.Document);

            if (updatedCompetitionResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine($"The competition with id {competitionId} has been updated.");
            }

            Competition updatedCompetition = (dynamic)updatedCompetitionResponse.Resource;
            return updatedCompetition;
        }

        private static async Task<Document> InsertCompetition2(string competitionId, 
            string competitionTitle,
            string competitionLocationZipCode)
        {
            // Insert a document related to a competition that is scheduled 
            // and doesn’t have winners yet
            var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
            var documentResponse = await client.CreateDocumentAsync(collectionUri, new
            {
                id = competitionId,
                title = competitionTitle,
                location = new
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

            if (documentResponse.StatusCode == System.Net.HttpStatusCode.Created)
            {
                Console.WriteLine($"The competition with the title {competitionTitle} has been created.");
            }

            return documentResponse.Resource;
        }

        private static async Task<bool> DoesCompetitionWithTitleExist(string competitionTitle)
        {
            bool exists = false;
            // Retrieve the number of documents with a specific title
            // Very important: Cross partition queries only support 'VALUE <AggreateFunc>' for aggregates
            var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
            var documentCountQuery = client.CreateDocumentQuery(collectionUri,
                $"SELECT VALUE COUNT(1) FROM Competitions c WHERE c.title = '{competitionTitle}'",
                new FeedOptions()
                {
                    EnableCrossPartitionQuery = true,
                    MaxItemCount = 1,
                })
                .AsDocumentQuery();
            while (documentCountQuery.HasMoreResults)
            {
                var documentCountQueryResult = await documentCountQuery.ExecuteNextAsync();
                exists = (documentCountQueryResult.FirstOrDefault() == 1);
            }

            return exists;
        }

        private static async Task ListScheduledCompetitionsWithLinq()
        {
            // Retrieve the titles for all the scheduled competitions that have more than 5 registered competitors
            var selectTitleQuery = client.CreateDocumentQuery<Competition>(collectionUri,
                new FeedOptions()
                {
                    EnableCrossPartitionQuery = true,
                    MaxItemCount = 100,
                })
                .Where(c => (c.NumberOfRegisteredCompetitors > 5) 
                && (c.Status == CompetitionStatus.Scheduled))
                .Select(c => c.Title)
                .AsDocumentQuery();

            while (selectTitleQuery.HasMoreResults)
            {
                var selectTitleQueryResult = await selectTitleQuery.ExecuteNextAsync<string>();
                foreach (var title in selectTitleQueryResult)
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
            var winnersQuery = client.CreateDocumentQuery<Competition>(collectionUri,
                new FeedOptions()
                {
                    MaxItemCount = 100,
                })
                .Where(c => (c.Location.ZipCode == zipCode)
                && (c.Status == CompetitionStatus.Finished)
                && (c.Platforms.Contains(gamingPlatform)))
                .Select(c => c.Winners[0])
                .AsDocumentQuery();

            while (winnersQuery.HasMoreResults)
            {
                var winnersQueryResult = await winnersQuery.ExecuteNextAsync<Winner>();
                foreach (var winner in winnersQueryResult)
                {
                    Console.WriteLine($"Nickname: {winner.Player.NickName}, Score: {winner.Score}");
                }
            }
        }

        private static async Task CreateAndQueryCompetitionsWithLinqAsync()
        {
            var database = await RetrieveOrCreateDatabaseAsync();
            Console.WriteLine(
                $"The database {databaseId} is available for operations with the following AltLink: {database.AltLink}");
            var collection = await CreateCollectionIfNotExistsAsync();
            Console.WriteLine(
                $"The collection {collectionId} is available for operations with the following AltLink: {collection.AltLink}");
            // Increase the provisioned throughput for the collection to 2000 RU/s
            var offer1 = await UpdateOfferForCollectionAsync(collection.SelfLink, 2000);
            Console.WriteLine(
                $"The collection {collectionId} has been re-configured with a provision throuhgput of 2000 RU/s");
            //
            collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
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
                    $"The {competition4.Status} competition  with the following title exists: {competition4.Title}");
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
            
            // Decrease the provisioned throughput for the collection to 1000 RU/s
            var offer2 = await UpdateOfferForCollectionAsync(collection.SelfLink, 1000);
            Console.WriteLine(
                $"The collection {collectionId} has been re-configured with a provision throuhgput of 1000 RU/s");
        }
    }
}
