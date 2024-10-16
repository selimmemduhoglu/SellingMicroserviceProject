using EventBus.Base;
using EventBus.Base.Events;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace EventBus.AzureServiceBus;

public class EventBusServiceBus : BaseEventBus
{
    private ITopicClient topicClient;
    private ManagementClient managementClient;
    private ILogger logger;

    public EventBusServiceBus(IServiceProvider serviceProvider, EventBusConfig config) : base(serviceProvider, config)
    {
        logger = serviceProvider.GetService(typeof(ILogger<EventBusServiceBus>)) as ILogger<EventBusServiceBus>;
        managementClient = new ManagementClient(config.EventBusConnectionString);
        topicClient = CreateTopicClient();
    }

    private ITopicClient CreateTopicClient()
    {
        if (topicClient is null || topicClient.IsClosedOrClosing)
            topicClient = new TopicClient(EventBusConfig.EventBusConnectionString, EventBusConfig.DefaultTopicName, RetryPolicy.Default);

        // Ensure that topic already exists
        if (!managementClient.TopicExistsAsync(EventBusConfig.DefaultTopicName).GetAwaiter().GetResult())
            managementClient.CreateTopicAsync(EventBusConfig.DefaultTopicName).GetAwaiter().GetResult();

        return topicClient;
    }

    #region Public Method

    public override void Publish(IntegrationEvent @event)
    {
        string eventStr = JsonConvert.SerializeObject(@event);
        byte[] eventArr = Encoding.UTF8.GetBytes(eventStr);

        string eventName = @event.GetType().Name;  // example : OrderCreatedIntegrationEvent
        eventName = ProcessEventName(eventName);   // example : OrderCreated


        Message message = new Message()
        {
            MessageId = Guid.NewGuid().ToString(),
            Body = eventArr,
            Label = eventName,
        };

        topicClient.SendAsync(message);

    }
    public override void Subscribe<T, TH>()
    {
        string eventName = typeof(T).Name;
        eventName = ProcessEventName(eventName);

        if (!SubsManager.HasSubscriptionsForEvent(eventName))
        {
            ISubscriptionClient subscriptionClient = CreateSucbscriptionClientIfNotExists(eventName);
            RegisterSubscriptionClientMessageHandler(subscriptionClient);

        }

        logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).Name);

        SubsManager.AddSubscription<T, TH>();
    }
    public override void UnSubscribe<T, TH>()
    {
        var eventName = typeof(T).Name;

        try
        {
            var subscriptionClient = CreateSubscriptionClient(eventName);

            subscriptionClient.RemoveRuleAsync(eventName).GetAwaiter().GetResult();
        }
        catch (MessagingEntityNotFoundException)
        {
            logger.LogWarning("The message entity {eventName} Could not be found", eventName);
        }

        logger.LogInformation("UnSubscribe from event {EventName}", eventName);

        SubsManager.RemoveSubscription<T, TH>();
    }

    public override void Dispose()
    {
        base.Dispose();

        topicClient.CloseAsync().GetAwaiter().GetResult();
        managementClient.CloseAsync().GetAwaiter().GetResult();
        topicClient = null;
        managementClient = null;
    }
    #endregion

    #region Private Method

    private void RegisterSubscriptionClientMessageHandler(ISubscriptionClient subscriptionClient)
    {
        subscriptionClient.RegisterMessageHandler(async (message, token) =>
        {
            var eventName = $"{message.Label}";
            var messageData = Encoding.UTF8.GetString(message.Body);

            // Complete the message so that it is not received again.
            if (await ProcessEvent(ProcessEventName(eventName), messageData))
            {
                await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
            }
        },
        new MessageHandlerOptions(ExceptionReceivedHandler) { MaxConcurrentCalls = 10, AutoComplete = false });

    }
    private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
    {
        Exception ex = exceptionReceivedEventArgs.Exception;
        var context = exceptionReceivedEventArgs.ExceptionReceivedContext;

        logger.LogError(ex, "ERROR handling message: {ExceptionMessage} - Context: {@ExceptionContext}", ex.Message, context);

        return Task.CompletedTask;
    }
    private ISubscriptionClient CreateSucbscriptionClientIfNotExists(String eventName) // Bir Subscription create ediyoruz.
    {
        SubscriptionClient subClient = CreateSubscriptionClient(eventName);

        bool exist = managementClient.SubscriptionExistsAsync(EventBusConfig.DefaultTopicName, GetSubName(eventName)).GetAwaiter().GetResult(); // Subscription zaten var mı?

        if (!exist)
        {
            managementClient.CreateSubscriptionAsync(EventBusConfig.DefaultTopicName, GetSubName(eventName)).GetAwaiter().GetResult(); // Eğer ki subs yoksa yarat.
            RemoveDefaultrule(subClient); // Eğer ki subs yoksa yarat ve rule unu sil.
        }
        CreateRuleIfNotExists(ProcessEventName(eventName), subClient); // Silinen rule yerine eğer ki rule yoksa bir rule create edilmesi gerekiyor burada create ediyoruz.
        return subClient;
    }
    private void CreateRuleIfNotExists(String eventname, ISubscriptionClient subscriptionClient)
    {
        bool ruleExists;

        try
        {
            RuleDescription rule = managementClient.GetRuleAsync(EventBusConfig.DefaultTopicName, eventname, eventname).GetAwaiter().GetResult();
            ruleExists = rule is not null;
        }
        catch (MessagingEntityNotFoundException)
        {
            // Azure management client does noıt have RuleExists method.
            ruleExists = false;
        }

        if (!ruleExists)
        {
            subscriptionClient.AddRuleAsync(new RuleDescription
            {
                Filter = new CorrelationFilter
                {
                    Label = eventname,
                },
                Name = eventname,
            }).GetAwaiter().GetResult();
        }
    }
    private void RemoveDefaultrule(SubscriptionClient subscriptionClient)
    {
        try
        {
            subscriptionClient.RemoveRuleAsync(RuleDescription.DefaultRuleName).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            logger.LogWarning("The messaging entity {DefaultRuleName} Could not be found.", RuleDescription.DefaultRuleName);
        }
    }
    private SubscriptionClient CreateSubscriptionClient(string eventName)
    {
        return new SubscriptionClient(EventBusConfig.EventBusConnectionString, EventBusConfig.DefaultTopicName, GetSubName(eventName));
    }

    #endregion
}
