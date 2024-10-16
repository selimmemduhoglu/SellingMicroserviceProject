using EventBus.Base;
using EventBus.Base.Abstraction;
using EventBus.Factory;
using EventBus.UnitTest.Events.EventHandlers;
using EventBus.UnitTest.Events.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EventBus.UnitTest;

[TestClass]
public class EventBusTests
{
    private ServiceCollection services;

    public EventBusTests()
    {
        services = new ServiceCollection(); //bunun kulalnılma amacı ServiceProvider elde etmek için
        //services.AddLogging(configure => configure.AddConsole());
    }

    [TestMethod]
    public void subscribe_event_on_rabbitmq_test()
    {
        //Burası şey demek senden ne zaman ki   IEventBus Interface ile ile birşey istenirse git return EventBusFactory.Create(config, sp); bu işlemi yap demek. (yani Create ettiği için Service Bus ı ayağa kaldırmış oldu.)
        services.AddSingleton<IEventBus>(sp =>
        {
            EventBusConfig config = new()
            {
                ConnectionRetryCount = 5,
                SubscriptionClientAppName = "EventBus.UnitTest",
                DefaultTopicName = "SellingBuddyTopicName",
                EventBusType = EventBusType.RabbitMQ, //Burada hangi EventBus ı istediğimizi söyledik
                EventNameSuffix = "IntegrationEvent",
            };

            return EventBusFactory.Create(config, sp);

        });

        var sp = services.BuildServiceProvider();

        var eventBus = sp.GetRequiredService<IEventBus>();

        eventBus.Subscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();
        // Bir istek geldiği zaman, bir mesaj geldiği zaman OrderCreatedIntegrationEvent ' e git OrderCreatedIntegrationEventHandler ' ı kullan demiş olduk.

        eventBus.UnSubscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();

    }
}
