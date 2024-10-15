using EventBus.Base.Events;

namespace EventBus.Base.Abstraction;

// Dışardan gelen Subscriptionları memory de tutucaz Dictionary ile List yaratıcaz ve tutucaz fakat bellekte değil de sonrasında Db de tutmak isteyebiliriz(ya da başka bir yerde) diye manager a yazdık.
public interface IEventBusSubscriptionManager
{
    bool IsEmpty { get; } // bir event dinliyor muyuz?

    event EventHandler<string> OnEventRemoved;
    void AddSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
    void RemoveSubscription<T, TH>() where TH : IIntegrationEventHandler<T> where T : IntegrationEvent;
    bool HasSubscriptionsForEvent<T>() where T : IntegrationEvent; // Dışardan gelen event i biz dinliyor muyuz? (dinamik olarak)
    bool HasSubscriptionsForEvent(string eventName); // Dışardan gelen event i biz dinliyor muyuz? (isimle)
    Type GetEventTypeByName(string eventName); //Eventin isminden ona ulaşmak için
    void Clear(); // Subscription ları silmek için
    IEnumerable<SubscriptionInfo> GetHandlerForEvent<T>() where T : IntegrationEvent;
    IEnumerable<SubscriptionInfo> GetHandlerForEvent(string eventName);
    string GetEventKey<T>(); // Eventler için kullanılan key.


}
