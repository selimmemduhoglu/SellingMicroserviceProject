using System;

namespace EventBus.Base;

// Bu objenin amacı dışardan bize gödnerilen verilerin içerde tutulmasını sağlıcaz.

public class SubscriptionInfo
{
    public Type? HandlerType { get; }

    public SubscriptionInfo(Type? handlerType)
    {
        HandlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));
    }

    public static SubscriptionInfo Typed(Type handlerType)
    {
        return new SubscriptionInfo(handlerType);
    }
}
