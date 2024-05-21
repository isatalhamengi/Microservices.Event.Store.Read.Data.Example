using MongoDB.Driver;
using Shared.Events;
using Shared.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Product.Event.Handler.Service.Services
{
    public class EventStoreBackgorundService : BackgroundService
    {
        IEventStoreService _eventStoreService;
        IMongoDBService _mongoDBService;

        public EventStoreBackgorundService(IEventStoreService eventStoreService, IMongoDBService mongoDBService)
        {
            _eventStoreService = eventStoreService;
            _mongoDBService = mongoDBService;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _eventStoreService.SubscribeToStreamAsync("products-stream", async (streamSubscription, resolvedEvent, cancellationToken) =>
            {
                string eventType = resolvedEvent.Event.EventType;
                object @event = JsonSerializer.Deserialize(resolvedEvent.Event.Data.ToArray(), Assembly.Load("Shared").GetTypes().FirstOrDefault(x => x.Name == eventType));

                var productCollection = _mongoDBService.GetCollection<Shared.Models.Product>("Products");
                Shared.Models.Product product = null;
                switch (@event)
                {
                    case NewProductAddedEvent e:
                        var hasProduct = await (await productCollection.FindAsync(x => x.Id.ToString() == e.ProductId)).AnyAsync();
                        if (!hasProduct)
                            await productCollection.InsertOneAsync(new()
                            {
                                Id = e.ProductId,
                                Count = e.InitialCount,
                                IsAvailable = true,
                                Price = e.InitialPrice,
                                ProductName = e.ProductName
                            });
                        break;
                    case CountDecreasedEvent e:
                        product =  await (await productCollection.FindAsync(p=> p.Id == e.ProductId)).FirstOrDefaultAsync();
                        if (product != null)
                        {
                            product.Count -= e.DecrementAmount;
                            await productCollection.FindOneAndReplaceAsync(p => p.Id == e.ProductId, product);
                        }
                        break;
                    case CountIncreasedEvent e:
                        product = await (await productCollection.FindAsync(p => p.Id == e.ProductId)).FirstOrDefaultAsync();
                        if (product != null)
                        {
                            product.Count += e.IncrementAmount;
                            await productCollection.FindOneAndReplaceAsync(p => p.Id == e.ProductId, product);
                        }
                        break;
                    case PriceDecreasedEvent e:
                        product = await (await productCollection.FindAsync(p => p.Id == e.ProductId)).FirstOrDefaultAsync();
                        if (product != null)
                        {
                            product.Price -= e.DecreamentAmount;
                            await productCollection.FindOneAndReplaceAsync(p => p.Id == e.ProductId, product);
                        }
                        break;
                    case PriceIncreasedEvent e:
                        product = await (await productCollection.FindAsync(p => p.Id == e.ProductId)).FirstOrDefaultAsync();
                        if (product != null)
                        {
                            product.Price += e.IncrementAmount;
                            await productCollection.FindOneAndReplaceAsync(p => p.Id == e.ProductId, product);
                        }
                        break;
                    case AvailabilityChangedEvent e:
                        product = await (await productCollection.FindAsync(p => p.Id == e.ProductId)).FirstOrDefaultAsync();
                        if (product != null)
                        {
                            product.IsAvailable = e.IsAvailable;
                            await productCollection.FindOneAndReplaceAsync(p => p.Id == e.ProductId, product);
                        }
                        break;
                }
            });
        }
    }
}
