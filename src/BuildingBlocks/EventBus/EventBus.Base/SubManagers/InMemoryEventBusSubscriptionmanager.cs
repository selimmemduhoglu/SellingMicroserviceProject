using EventBus.Base.Abstraction;
using EventBus.Base.Events;

namespace EventBus.Base.SubManagers;


public class InMemoryEventBusSubscriptionManager : IEventBusSubscriptionManager
{
    private readonly Dictionary<string, List<SubscriptionInfo>> _handlers; // Eventleri tutmak için
    private readonly List<Type> _eventTypes;

    public event EventHandler<string> OnEventRemoved;
    public Func<string, string> eventNameGetter;

    public InMemoryEventBusSubscriptionManager(Func<string, string> eventNameGetter)
    {
        _handlers = new Dictionary<string, List<SubscriptionInfo>>();
        _eventTypes = new List<Type>();
        this.eventNameGetter = eventNameGetter; // keywordleri kırpmak için
    }
    public bool IsEmpty => !_handlers.Keys.Any();
    public void Clear() => _handlers.Clear();

    //public event EventHandler<string> OnEventRemoved;

    private void AddSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        var eventName = GetEventKey<T>(); // Eventin ismini almak için

        AddSubscription(typeof(TH), eventName);
        if (!_eventTypes.Contains(typeof(T)))
        {
            _eventTypes.Add(typeof(T));
        }
    }
    private void AddSubscription(Type handlerType, string eventName)  // Listede varsa hata vcer yoksa ekle.
    {
        if (!HasSubscriptionsForEvent(eventName))
        {
            _handlers.Add(eventName, new List<SubscriptionInfo>());
        }
        if (_handlers[eventName].Any(s => s.HandlerType == handlerType))
        {
            throw new ArgumentException($"handler Type {handlerType.Name} already regidtered for '{eventName}'", nameof(handlerType));
        }
        _handlers[eventName].Add(SubscriptionInfo.Typed(handlerType));
    }
    public IEnumerable<SubscriptionInfo> GetHandlerForEvent<T>() where T : IntegrationEvent
    {
        var key = GetEventKey<T>();
        return GetHandlerForEvent(key);
    }
    public void RemoveSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        var handlerRemove = FindSubscriptionToRemove<T, TH>();
        var eventName = GetEventKey<T>();
        RemoveHandler(eventName, handlerRemove);
    }
    private void RemoveHandler(string eventName, SubscriptionInfo subscriptionInfo)
    {
        if (subscriptionInfo != null)
        {
            _handlers[eventName].Remove(subscriptionInfo);
            if (!_handlers[eventName].Any())
            {
                _handlers.Remove(eventName);
                var eventType = _eventTypes.SingleOrDefault(e => e.Name == eventName);
                if (eventName != null)
                {
                    _eventTypes.Remove(eventType);
                }
                RaiseOnEventRemoved(eventName);

            }
        }
    }
    public IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent
    {
        var key = GetEventKey<T>();
        return GetHandlerForEvent(key);
    }
    public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName) => _handlers[eventName];
    private void RaiseOnEventRemoved(string eventName) // Eğer ki eventHandler dan bir event silindiyse eventi kullananalara haber vermek için.
    {
        var handler = OnEventRemoved;
        handler?.Invoke(this, eventName);
    }
    public IEnumerable<SubscriptionInfo> GetHandlerForEvent(string eventName) => _handlers[eventName];
    private SubscriptionInfo FindSubscriptionToRemove<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        var eventName = GetEventKey<T>();
        return FindSubscriptionToRemove(eventName, typeof(TH));
    }
    private SubscriptionInfo FindSubscriptionToRemove(string eventName, Type handlerType)
    {

        if (!HasSubscriptionsForEvent(eventName))
        {
            return null;
        }

        return _handlers[eventName].SingleOrDefault(s => s.HandlerType == handlerType);
    }
    public bool HasSubscriptionsForEvent<T>() where T : IntegrationEvent
    {
        var key = GetEventKey<T>();
        return HasSubscriptionsForEvent(key);
    }
    public bool HasSubscriptionsForEvent(string eventName) => _handlers.ContainsKey(eventName);
    public Type GetEventTypeByName(string eventName) => _eventTypes.SingleOrDefault(t => t.Name == eventName);
    public string GetEventKey<T>()
    {
        string eventName = typeof(T).Name;
        return eventNameGetter(eventName);
    }
    void IEventBusSubscriptionManager.AddSubscription<T, TH>()
    {
        throw new NotImplementedException();
    }
}
