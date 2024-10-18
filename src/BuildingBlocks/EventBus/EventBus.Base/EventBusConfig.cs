namespace EventBus.Base;

public class EventBusConfig
{
    public int ConnectionRetryCount { get; set; } = 5; // Bağlanırken en fazla 5 kere dene
    public string DefaultTopicName { get; set; } = "SellingEventBus"; // Default Topic Name (Bu topic name in altına kuyrukları oluşturucaz.)
    public string EventBusConnectionString { get; set; } = String.Empty;
    public string SubscriberClientAppName { get; set; } = String.Empty;
    public string EventNamePrefix { get; set; } = String.Empty;  // (Trimlemek için)
    public string EventNameSuffix { get; set; } = "IntegrationEvent"; // (Trimlemek için)
    public EventBusType EventBusType { get; set; } = EventBusType.RabbitMQ; // Defaultta EventBus olarak RabbitMQ kullanıcaz (parametre gönderilmezse)
    public object Connection { get; set; } = new(); // bunun object olmasının sebebi şu : kullanılacak olan Event in dll ini yükselemek zorunda bırakmamak için burada object şeklinde verdik ve içerde hangi Event kullanılacaksa orada cast ederek kullanmak için.


    public bool DeleteEventPrefix => !String.IsNullOrEmpty(EventNamePrefix);
    public bool DeleteEventSuffix => !String.IsNullOrEmpty(EventNameSuffix);

}
public enum EventBusType
{
    RabbitMQ = 0,
    AzureServiceBus = 1
}
