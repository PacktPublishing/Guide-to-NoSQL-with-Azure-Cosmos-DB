//namespace SampleApp1
//{
//    using Microsoft.Azure.Documents;
//    using Microsoft.Azure.Documents.Client;
//    using Microsoft.Azure.Documents.Linq;
//    using Microsoft.Extensions.Configuration;
//    using System;
//    using System.Linq;
//    using System.Threading.Tasks;

//    public class ProgramVersion1
//    {
//        private static string databaseId;
//        private static string collectionId;
//        private static DocumentClient client;

//        public static void Main(string[] args)
//        {
//            var configurationBuilder = new ConfigurationBuilder();
//            configurationBuilder.AddJsonFile("configuration.json", optional: false, reloadOnChange: false);
//            var configuration = configurationBuilder.Build();
//            string endpointUrl = configuration["CosmosDB:endpointUrl"];
//            string authorizationKey = configuration["CosmosDB:authorizationKey"];
//            databaseId = configuration["CosmosDB:databaseId"];
//            collectionId = configuration["CosmosDB:collectionId"];
//            try
//            {
//                using (client = new DocumentClient(new Uri(endpointUrl), authorizationKey))
//                {
//                    CreateAndQueryDynamicDocumentsAsync().Wait();
//                }
//            }
//            catch (DocumentClientException dce)
//            {
//                var baseException = dce.GetBaseException();
//                Console.WriteLine(
//                    $"DocumentClientException occurred. Status code: {dce.StatusCode}; Message: {dce.Message}; Base exception message: {baseException.Message}");
//            }
//            catch (Exception e)
//            {
//                var baseException = e.GetBaseException();
//                Console.WriteLine(
//                    $"Exception occurred. Message: {e.Message}; Base exception message: {baseException.Message}");
//            }
//            finally
//            {
//                Console.WriteLine("Press any key to exit the console application.");
//                Console.ReadKey();
//            }
//        }

//        private static async Task<Database> RetrieveOrCreateDatabaseAsync()
//        {
//            // Create a new document database if it doesn’t exist
//            var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(
//                new Database
//                {
//                    Id = databaseId,
//                });
//            switch (databaseResponse.StatusCode)
//            {
//                case System.Net.HttpStatusCode.Created:
//                    Console.WriteLine($"The database {databaseId} has been created.");
//                    break;
//                case System.Net.HttpStatusCode.OK:
//                    Console.WriteLine($"The database {databaseId} has been retrieved.");
//                    break;
//            }
//            return databaseResponse.Resource;
//        }

//        private static async Task<DocumentCollection> CreateCollectionIfNotExistsAsync()
//        {
//            var databaseUri = UriFactory.CreateDatabaseUri(databaseId);
//            DocumentCollection documentCollectionResource;
//            var isCollectionCreated = await client.CreateDocumentCollectionQuery(databaseUri)
//                .Where(c => c.Id == collectionId)
//                .CountAsync() == 1;
//            if (isCollectionCreated)
//            {
//                Console.WriteLine($"The collection {collectionId} already exists.");
//                var documentCollectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
//                var documentCollectionResponse = await client.ReadDocumentCollectionAsync(documentCollectionUri);
//                documentCollectionResource = documentCollectionResponse.Resource;
//            }
//            else
//            {
//                var documentCollection = new DocumentCollection
//                {
//                    Id = collectionId,
//                };
//                documentCollection.PartitionKey.Paths.Add("/location/zipCode");
//                var uniqueKey = new UniqueKey();
//                uniqueKey.Paths.Add("/title");
//                documentCollection.UniqueKeyPolicy.UniqueKeys.Add(uniqueKey);
//                var requestOptions = new RequestOptions
//                {
//                    OfferThroughput = 1000,
//                };
//                var collectionResponse = await client.CreateDocumentCollectionAsync(
//                    databaseUri,
//                    documentCollection,
//                    requestOptions);
//                if (collectionResponse.StatusCode == System.Net.HttpStatusCode.Created)
//                {
//                    Console.WriteLine($"The collection {collectionId} has been created.");
//                }
//                documentCollectionResource = collectionResponse.Resource;
//            }

//            return documentCollectionResource;
//        }

//        private static async Task<dynamic> GetCompetitionByTitle(string title)
//        {
//            // Build a query to retrieve a document with a specific title
//            var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
//            var documentQuery = client.CreateDocumentQuery(collectionUri,
//                $"SELECT * FROM Competitions c WHERE c.title = '{title}'",
//                new FeedOptions()
//                {
//                    EnableCrossPartitionQuery = true,
//                    MaxItemCount = 1,
//                })
//                .AsDocumentQuery();
//            while (documentQuery.HasMoreResults)
//            {
//                foreach (var competition in await documentQuery.ExecuteNextAsync())
//                {
//                    Console.WriteLine(
//                        $"The document with the following title exists: {title}");
//                    Console.WriteLine(competition);
//                    return competition;
//                }
//            }

//            // No matching document found
//            return null;
//        }

//        private static async Task<Document> InsertCompetition1(string competitionId,
//            string competitionTitle,
//            string competitionLocationZipCode)
//        {
//            // Insert a document related to a competition that has finished and has winners
//            var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
//            var documentResponse = await client.CreateDocumentAsync(collectionUri, new
//            {
//                id = competitionId,
//                title = competitionTitle,
//                location = new
//                { 
//                    zipCode = competitionLocationZipCode,
//                    state = "CA",
//                },
//                platforms = new[]
//                {
//                        "PS4", "XBox", "Switch"
//                },
//                games = new[]
//                {
//                        "Fortnite", "NBA Live 19"
//                },
//                numberOfRegisteredCompetitors = 80,
//                numberOfCompetitors = 60,
//                numberOfViewers = 300,
//                status = "Finished",
//                dateTime = DateTime.UtcNow.AddDays(-50),
//                winners = new[]
//                {
//                        new
//                        {
//                            player = new
//                            {
//                                nickName = "EnzoTheGreatest",
//                                country = "Italy",
//                                city = "Rome"
//                            },
//                            position = 1,
//                            score = 7500,
//                            prize = 1500,
//                        },
//                        new
//                        {
//                            player = new
//                            {
//                                nickName = "NicoInGamerLand",
//                                country = "Argentina",
//                                city = "Buenos Aires"
//                            },
//                            position = 2,
//                            score = 6500,
//                            prize = 750,
//                        },
//                        new
//                        {
//                            player = new
//                            {
//                                nickName = "KiwiBoy",
//                                country = "New Zealand",
//                                city = "Auckland"
//                            },
//                            position = 3,
//                            score = 3500,
//                            prize = 250,
//                        }
//                },
//            });

//            if (documentResponse.StatusCode == System.Net.HttpStatusCode.Created)
//            {
//                Console.WriteLine($"The competition with the title {competitionTitle} has been created.");
//            }

//            return documentResponse.Resource;
//        }

//        private static async Task<Document> UpdateScheduledCompetition(string competitionId,
//            string competitionLocationZipCode,
//            DateTime newDateTime,
//            int newNumberOfRegisteredCompetitors)
//        {
//            // Retrieve a document related to a competition that is scheduled 
//            // and update its date and its number of registered competitors
//            // The read operation requires the partition key
//            var documentToUpdateUri = UriFactory.CreateDocumentUri(databaseId, collectionId, competitionId);
//            var readDocumentResponse = await client.ReadDocumentAsync(documentToUpdateUri, new RequestOptions()
//            {
//                PartitionKey = new PartitionKey(competitionLocationZipCode)
//            });
//            ((dynamic)readDocumentResponse.Resource).dateTime = newDateTime;
//            ((dynamic)readDocumentResponse.Resource).numberOfRegisteredCompetitors = newNumberOfRegisteredCompetitors;
//            ResourceResponse<Document> updatedDocumentResponse = await client.ReplaceDocumentAsync(
//                documentToUpdateUri,
//                readDocumentResponse.Resource);

//            if (updatedDocumentResponse.StatusCode == System.Net.HttpStatusCode.OK)
//            {
//                Console.WriteLine($"The competition with id {competitionId} has been updated.");
//            }

//            return updatedDocumentResponse.Resource;
//        }

//        private static async Task<Document> InsertCompetition2(string competitionId,
//            string competitionTitle,
//            string competitionLocationZipCode)
//        {
//            // Insert a document related to a competition that is scheduled 
//            // and doesn’t have winners yet
//            var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
//            var documentResponse = await client.CreateDocumentAsync(collectionUri, new
//            {
//                id = competitionId,
//                title = competitionTitle,
//                location = new
//                {
//                    zipCode = competitionLocationZipCode,
//                    state = "CA",
//                },
//                platforms = new[]
//                {
//                        "PC", "PS4", "XBox"
//                },
//                games = new[]
//                {
//                        "Madden NFL 19", "Fortnite"
//                },
//                numberOfRegisteredCompetitors = 160,
//                status = "Scheduled",
//                dateTime = DateTime.UtcNow.AddDays(50),
//            });

//            if (documentResponse.StatusCode == System.Net.HttpStatusCode.Created)
//            {
//                Console.WriteLine($"The competition with the title {competitionTitle} has been created.");
//            }

//            return documentResponse.Resource;
//        }

//        private static async Task<bool> DoesCompetitionWithTitleExist(string competitionTitle)
//        {
//            bool exists = false;
//            // Retrieve the number of documents with a specific title
//            // Very important: Cross partition queries only support 'VALUE <AggreateFunc>' for aggregates
//            var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
//            var documentCountQuery = client.CreateDocumentQuery(collectionUri,
//                $"SELECT VALUE COUNT(1) FROM Competitions c WHERE c.title = '{competitionTitle}'",
//                new FeedOptions()
//                {
//                    EnableCrossPartitionQuery = true,
//                    MaxItemCount = 1,
//                })
//                .AsDocumentQuery();
//            while (documentCountQuery.HasMoreResults)
//            {
//                var documentCountQueryResult = await documentCountQuery.ExecuteNextAsync();
//                exists = (documentCountQueryResult.FirstOrDefault() == 1);
//            }

//            return exists;
//        }

//        private static async Task ListScheduledCompetitions()
//        {
//            // Retrieve the titles for all the scheduled competitions that have more than 200 registered competitors
//            var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
//            var selectTitleQuery = client.CreateDocumentQuery(collectionUri,
//                $"SELECT VALUE c.title FROM Competitions c WHERE c.numberOfRegisteredCompetitors > 200 AND c.status = 'Scheduled'",
//                new FeedOptions()
//                {
//                    EnableCrossPartitionQuery = true,
//                    MaxItemCount = 100,
//                })
//                .AsDocumentQuery();
//            while (selectTitleQuery.HasMoreResults)
//            {
//                var selectTitleQueryResult = await selectTitleQuery.ExecuteNextAsync();
//                foreach (var title in selectTitleQueryResult)
//                {
//                    Console.WriteLine(title);
//                }
//            }
//        }

//        private static async Task CreateAndQueryDynamicDocumentsAsync()
//        {
//            var database = await RetrieveOrCreateDatabaseAsync();
//            Console.WriteLine(
//                $"The database {databaseId} is available for operations with the following AltLink: {database.AltLink}");
//            var collection = await CreateCollectionIfNotExistsAsync();
//            Console.WriteLine(
//                $"The collection {collectionId} is available for operations with the following AltLink: {collection.AltLink}");
//            string competition1Id = "1";
//            string competition1Title = "Crowns for Gamers - Portland 2018";
//            string competition1ZipCode = "90210";
//            var competition1 = await GetCompetitionByTitle(competition1Title);
//            if (competition1 == null)
//            {
//                competition1 = await InsertCompetition1(competition1Id, competition1Title, competition1ZipCode);
//            }

//            string competition2Title = "Defenders of the crown - San Diego 2018";
//            bool isCompetition2Inserted = await DoesCompetitionWithTitleExist(competition2Title);
//            string competition2Id = "2";
//            string competition2LocationZipCode = "92075";
//            if (isCompetition2Inserted)
//            {
//                Console.WriteLine(
//                    $"The document with the following title exists: {competition2Title}");
//            }
//            else
//            {
//                var competition2 = await InsertCompetition2(competition2Id, competition2Title, competition2LocationZipCode);
//            }

//            var updatedCompetition2 = await UpdateScheduledCompetition(competition2Id,
//                competition2LocationZipCode,
//                DateTime.UtcNow.AddDays(60),
//                250);

//            await ListScheduledCompetitions();
//        }
//    }
//}
