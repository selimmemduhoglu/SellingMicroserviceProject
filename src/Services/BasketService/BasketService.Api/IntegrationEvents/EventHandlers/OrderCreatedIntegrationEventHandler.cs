using BasketService.Api.Core.Application.Repository;
using BasketService.Api.IntegrationEvents.Events;
using EventBus.Base.Abstraction;

namespace BasketService.Api.IntegrationEvents.EventHanders
{
    public class OrderCreatedIntegrationEventHandler : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
    {
        private readonly IBasketRepository _repository;
        private readonly ILogger<OrderCreatedIntegrationEvent> _logger;

        public OrderCreatedIntegrationEventHandler(IBasketRepository repository, ILogger<OrderCreatedIntegrationEvent> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task Handle(OrderCreatedIntegrationEvent @event)
        {
            _logger.LogInformation("----- Handling integration event: {IntegrationEventId} at BasketService.Api - ({@IntegrationEvent})", @event.Id, @event);

            await _repository.DeleteBasketAsync(@event.UserId);

            // Buranın amacı basket tekilerin order a gitmesi için eventBus a göndermemeiz gerekiğyor.
            //Burada da rabbitmq ya gönderildiğini basket servisine de haber veriyor ki gidip baskettekileri silsin.

        }
    }
}
