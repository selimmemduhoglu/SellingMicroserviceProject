,using EventBus.Base.Abstraction;
using EventBus.Base.SubManagers;
using Newtonsoft.Json;

namespace EventBus.Base.Events;

public abstract class BaseEventBus : IEventBus
{
    public readonly IServiceProvider ServiceProvider;
    public readonly IEventBusSubscriptionManager SubsManager;

    public EventBusConfig EventBusConfig { get; set; }

    public BaseEventBus(IServiceProvider serviceProvider, EventBusConfig config)
    {
        EventBusConfig = config;
        ServiceProvider = serviceProvider;
        SubsManager = new InMemoryEventBusSubscriptionManager(ProcessEventName);

    }

    public virtual string ProcessEventName(string eventName)
    {
        if (EventBusConfig.DeleteEventPrefix)
            eventName = eventName.TrimStart(EventBusConfig.EventNamePrefix.ToArray());

        if (EventBusConfig.DeleteEventSuffix)
            eventName = eventName.TrimEnd(EventBusConfig.EventNameSuffix.ToArray());

        return eventName;
    }

    public virtual string GetSubName(string eventName)
    {
        return $"{EventBusConfig.SubscriptionClientAppName}.{ProcessEventName(eventName)}";
    }

    public virtual void Dispose()
    {
        EventBusConfig = null;
        SubsManager.Clear();
    }

    public async Task<bool> ProcessEvent(string eventName, string message)
    {
        eventName = ProcessEventName(eventName);

        var processed = false;

        if (SubsManager.HasSubscriptionsForEvent(eventName))
        {
            var subscriptions = SubsManager.GetHandlerForEvent(eventName);

            using (var scope = ServiceProvider.CreateScope())
            {
                foreach (var subscription in subscriptions)
                {
                    var handler = ServiceProvider.GetService(subscription.HandlerType);
                    if (handler != null) continue;

                    var eventTpe = SubsManager.GetEventTypeByName($"{EventBusConfig.EventNamePrefix}{eventName}{EventBusConfig.EventNameSuffix}");
                    var integrationEvent = JsonConvert.DeserializeObject(message, eventTpe);


                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventTpe);
                    await (Task)concreteType.GetMethod("Handler").Invoke(handler, new object[] { integrationEvent });

                }
            }

            processed = true;
        }
        return true;


    }

    public abstract void Publish(IntegrationEvent @event);

    public abstract void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;

    public abstract void UnSubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;

}
