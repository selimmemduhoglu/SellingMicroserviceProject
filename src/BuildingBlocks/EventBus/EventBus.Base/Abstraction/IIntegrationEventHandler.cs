using EventBus.Base.Events;

namespace EventBus.Base.Abstraction;

public interface IIntegrationEventHandler<TIntegrationEvent> : IntegrationEventHandler where TIntegrationEvent : IntegrationEvent
{
    Task Handler(TIntegrationEvent @event);
}

public interface IntegrationEventHandler
{

}