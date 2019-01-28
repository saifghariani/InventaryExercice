using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Text;

namespace Inventary
{
    class Program
    {
        static void Main(string[] args)
        {
            UserCredentials adminCredentials = new UserCredentials("admin", "changeit");
            Sale sale = new Sale(SaleId.New, "HeadSet", 30, (float)20.50);

            var conn = Connect();
            var streamName = "achievedsales";
            var projectionName = "salesCounter";
            PersistentSubscriptionSettings settings = PersistentSubscriptionSettings.Create()
                .DoNotResolveLinkTos()
                .StartFromCurrent();
            var salestring = JsonConvert.SerializeObject(sale);
            EventData eventData = new EventData(sale.SaleId.GetGuid(), "ProductSold", false, Encoding.UTF8.GetBytes(salestring), null);

            #region addNewEvent
            //conn.AppendToStreamAsync(streamName, ExpectedVersion.Any, adminCredentials, eventData).Wait();
            #endregion
            #region CreateSubscription
            //conn.CreatePersistentSubscriptionAsync(streamName, "inventaryManager", settings, adminCredentials).Wait();
            #endregion

#if true
            #region InventaryManager
            Console.WriteLine("Inventary Manager : \n___________\n");
            InventaryManager(conn, streamName, adminCredentials);
            #endregion
#else
            #region Director
            Console.WriteLine("Director : \n___________\n");
            Director(conn, projectionName, adminCredentials);
            #endregion
#endif
            //conn.DeleteStreamAsync(streamName, ExpectedVersion.Any, adminCredentials);
            Console.ReadKey();
            conn.Close();
        }
        static IEventStoreConnection Connect()
        {
            var conn = EventStoreConnection.Create(new Uri("tcp://admin:changeit@localhost:1113"));
            conn.ConnectAsync().Wait();
            return conn;
        }
        static void InventaryManager(IEventStoreConnection conn, string streamName, UserCredentials userCredentials)
        {
            //Existing events
            var streamEvents = conn.ReadStreamEventsForwardAsync(streamName, 0, 10, true, userCredentials).Result;
            foreach (var evt in streamEvents.Events) {
                var json = Encoding.UTF8.GetString(evt.Event.Data);
                dynamic sales = JsonConvert.DeserializeObject(json);
                Console.WriteLine("Product : {0}, Quantity : {1}", sales.ProductName,
                    sales.Quantity);
            }
            //Listening for new events
            Console.WriteLine("\nListening for new sales...\n");
            conn.ConnectToPersistentSubscription(streamName, "inventaryManager", (_, x) => {
                var data = Encoding.ASCII.GetString(x.Event.Data);
                dynamic sales = JsonConvert.DeserializeObject(data);
                Console.WriteLine("New\tProduct : {0}, Quantity : {1}", sales.ProductName,
                    sales.Quantity);
            }, (sub, reason, ex) => { }, userCredentials);

        }
        static async void Director(IEventStoreConnection conn, string projectionName, UserCredentials userCredentials)
        {
            var countItemsProjection = @"options({
            resultStreamName: '"+projectionName+@"'
            })
            fromAll()
            .when({
                $init: function(){
                                return {
                                count: 0
                                }
                            },
                ProductSold: function(s, e){
                                    s.count += 1;
                                    
                            }
                        }).outputState()";
            ProjectionsManager projectionsManager = new ProjectionsManager(new ConsoleLogger(), new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2113), TimeSpan.FromMilliseconds(5000));

#region Create/Update Projection
            //projectionsManager.CreateContinuousAsync(projectionName, countItemsProjection, true, userCredentials);
            //projectionsManager.EnableAsync(projectionName, userCredentials);
            //projectionsManager.UpdateQueryAsync(projectionName, countItemsProjection, true, userCredentials);
            //projectionsManager.ResetAsynsc(projectionName, countItemsProjection, userCredentials).Wait();
#endregion

            var readEvents = conn.ReadStreamEventsForwardAsync(projectionName, 0, 10, true)
            .Result;
            var projectionState = await projectionsManager.GetStateAsync(projectionName, userCredentials);
            //var json = Encoding.UTF8.GetString(projectionState);
            dynamic salesNumber = JsonConvert.DeserializeObject(projectionState);
            Console.WriteLine("There has been {0} sales. ", salesNumber.count);
            
        }
    }
}
