using EventBus.Base;
using EventBus.Base.Events;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using System.Text;

namespace EventBus.RabbitMQ;

public class EventBusRabbitMQ : BaseEventBus
{
    RabbitMQPersistentConnection persistentConnection;
    private readonly IConnectionFactory connectionFactory;
    private readonly IModel consumerChannel;
    public EventBusRabbitMQ(IServiceProvider serviceProvider, EventBusConfig config) : base(serviceProvider, config)
    {
        if (config.Connection != null)
        {
            var connJson = JsonConvert.SerializeObject(EventBusConfig.Connection, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            connectionFactory = JsonConvert.DeserializeObject<ConnectionFactory>(connJson);
        }
        else
            connectionFactory = new ConnectionFactory();

        persistentConnection = new RabbitMQPersistentConnection(connectionFactory, config.ConnectionRetryCount);

        consumerChannel = CreateConsumerChannel();

        SubsManager.OnEventRemoved += SubsManager_OnEventRemoved;
    }


    #region Public Method

    public override void Publish(IntegrationEvent @event)
    {
        if (!persistentConnection.isConnection)
        {
            persistentConnection.TryConnect();
        }

        RetryPolicy policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(EventBusConfig.ConnectionRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                // log

            });


        string eventName = @event.GetType().Name;
        eventName = ProcessEventName(eventName);

        consumerChannel.ExchangeDeclare(exchange: EventBusConfig.DefaultTopicName, type: "direct"); // BUrada exchange in olmama ihtimaline karşılık create ed,l,yor.

        string message = JsonConvert.SerializeObject(@event);

        byte[] body = Encoding.UTF8.GetBytes(message);

        policy.Execute(() =>
        {
            IBasicProperties properties = consumerChannel.CreateBasicProperties();

            properties.DeliveryMode = 2; // persistent

            consumerChannel.QueueDeclare(queue: GetSubName(eventName), // Queue'nun create edilip edilmediği bilgisi için. Create edilmediyse create edecek.
                     durable: true,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);

            consumerChannel.BasicPublish(
                     exchange: EventBusConfig.DefaultTopicName,
                     routingKey: eventName,
                     mandatory: true,
                     basicProperties: properties,
                     body: body);

        });

    }
    public override void Subscribe<T, TH>()
    {
        string eventName = typeof(T).Name;
        eventName = ProcessEventName(eventName);

        if (!SubsManager.HasSubscriptionsForEvent(eventName))
        {
            if (!persistentConnection.isConnection)  // Connection bağlı değilse bağlanıyor.
            {
                persistentConnection.TryConnect();
            }

            consumerChannel.QueueDeclare(queue: GetSubName(eventName), // Burada queue create edilir. (burada consume edeceğimiz queue nin daha önceden oluşturulup oluşturulmadığı bilgisini almak.)
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            consumerChannel.QueueBind(queue: GetSubName(eventName), // burada queue baplanır. (burada exchange ve queue bind ediliyor.)
                exchange: EventBusConfig.DefaultTopicName,
                routingKey: eventName);
        }

        SubsManager.AddSubscription<T, TH>();
        StartBasicConsume(eventName);
    }
    public override void UnSubscribe<T, TH>()
    {
        SubsManager.RemoveSubscription<T, TH>();
    }

    #endregion


    #region Private Method

    private void SubsManager_OnEventRemoved(object sender, string eventName)
    {
        eventName = ProcessEventName(eventName);

        if (!persistentConnection.isConnection)
        {
            persistentConnection.TryConnect();
        }
        // Daha önceden bind ettiğimiz wueue yu UnBind et.
        consumerChannel.QueueUnbind(queue: eventName,
            exchange: EventBusConfig.DefaultTopicName,
            routingKey: eventName);


        if (SubsManager.IsEmpty)
        {
            consumerChannel.Close();
        }
    }
    private IModel CreateConsumerChannel()
    {
        if (!persistentConnection.isConnection) // Connection ımızın connectred olup olmadığına bakıp oan göre bağlanıyoruz.
        {
            persistentConnection.TryConnect();
        }

        IModel channel = persistentConnection.CreateModel(); // Bağlanma durumuna göre bir channel oluşturulur ve Model return eder.
        channel.ExchangeDeclare(exchange: EventBusConfig.DefaultTopicName, type: "direct");

        return channel;
    }
    private void StartBasicConsume(string eventName)
    {
        if (consumerChannel != null)
        {
            EventingBasicConsumer consumer = new EventingBasicConsumer(consumerChannel);

            consumer.Received += Consumer_Received;

            consumerChannel.BasicConsume(
                queue: GetSubName(eventName),
                autoAck: false,
                consumer: consumer);
        }
    }
    private async void Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
    {
        string eventName = eventArgs.RoutingKey;
        eventName = ProcessEventName(eventName);
        string message = Encoding.UTF8.GetString(eventArgs.Body.Span);

        try
        {
            await ProcessEvent(eventName, message);
        }
        catch (Exception ex)
        {
            // logging
        }

        consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
    }

    #endregion

}
