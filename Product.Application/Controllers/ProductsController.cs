using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Product.Application.Models.ViewModels;
using Shared.Events;
using Shared.Services.Abstractions;

namespace Product.Application.Controllers
{
    public class ProductsController : Controller
    {
        IEventStoreService _eventStoreService;
        IMongoDBService _mongoDBService;

        public ProductsController(IEventStoreService eventStoreService, IMongoDBService mongoDBService)
        {
            _eventStoreService = eventStoreService;
            _mongoDBService = mongoDBService;
        }

        public async Task<IActionResult> Index()
        {
            var productCollection = _mongoDBService.GetCollection<Shared.Models.Product>("Products");
            var products = await (await productCollection.FindAsync(_ => true)).ToListAsync(); ;
            return View(products);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateProductVM createProductVM)
        {
            NewProductAddedEvent newProductAdded = new()
            {
                ProductId = Guid.NewGuid().ToString(),
                InitialCount = createProductVM.Count,
                InitialPrice = createProductVM.Price,
                IsAvailable = createProductVM.IsAvailable,
                ProductName = createProductVM.ProductName
            };

            await _eventStoreService.AppendToStreamAsync("products-stream", new[] { _eventStoreService.GenerateEventData(newProductAdded) });
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(string productId)
        {
            var productCollection = _mongoDBService.GetCollection<Shared.Models.Product>("Products");
            var product = await (await productCollection.FindAsync(p => p.Id == productId)).FirstOrDefaultAsync();
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> CountUpdate(Shared.Models.Product model)
        {
            var productCollection = _mongoDBService.GetCollection<Shared.Models.Product>("Products");
            var product = await (await productCollection.FindAsync(p => p.Id == model.Id)).FirstOrDefaultAsync();
            if (product.Count > model.Count)
            {
                CountDecreasedEvent countDecreasedEvent = new()
                {
                    DecrementAmount = model.Count,
                    ProductId = model.Id
                };
                await _eventStoreService.AppendToStreamAsync("products-stream", new[]
                {
                    _eventStoreService.GenerateEventData(countDecreasedEvent)
                });
            }
            else if (product.Count < model.Count)
            {
                CountIncreasedEvent countIncreasedEvent = new()
                {
                    IncrementAmount = model.Count,
                    ProductId = model.Id
                };
                await _eventStoreService.AppendToStreamAsync("products-stream", new[]
                {
                    _eventStoreService.GenerateEventData(countIncreasedEvent)
                });
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> PriceUpdate(Shared.Models.Product model)
        {
            var productCollection = _mongoDBService.GetCollection<Shared.Models.Product>("Products");
            var product = await (await productCollection.FindAsync(p => p.Id == model.Id)).FirstOrDefaultAsync();
            if (product.Price > model.Price)
            {
                PriceDecreasedEvent priceDecreasedEvent = new()
                {
                    DecreamentAmount = model.Price,
                    ProductId = model.Id
                };

                await _eventStoreService.AppendToStreamAsync("products-stream", new[]
                {
                    _eventStoreService.GenerateEventData(priceDecreasedEvent)
                });
            }
            else if (product.Price < model.Price)
            {
                PriceIncreasedEvent priceIncreasedEvent = new()
                {
                    IncrementAmount = model.Price,
                    ProductId = model.Id
                };

                await _eventStoreService.AppendToStreamAsync("products-stream", new[]
                {
                    _eventStoreService.GenerateEventData(priceIncreasedEvent)
                });

            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> AvailableUpdate(Shared.Models.Product model)
        {
            var productCollection = _mongoDBService.GetCollection<Shared.Models.Product>("Products");
            var product = await (await productCollection.FindAsync(p => p.Id == model.Id)).FirstOrDefaultAsync();
            if (product.IsAvailable != model.IsAvailable)
            {
                AvailabilityChangedEvent availabilityChangedEvent = new()
                {
                    ProductId = model.Id,
                    IsAvailable = model.IsAvailable
                };

                await _eventStoreService.AppendToStreamAsync("products-stream", new[]
                {
                    _eventStoreService.GenerateEventData(availabilityChangedEvent)
                });
            }
            return RedirectToAction("Index");
        }
    }
}
