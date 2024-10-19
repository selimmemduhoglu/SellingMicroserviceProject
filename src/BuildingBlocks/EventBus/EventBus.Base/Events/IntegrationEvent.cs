using Newtonsoft.Json;
using System;

namespace EventBus.Base.Events;

//Servisler arası iletişim kuran objemiz
public class IntegrationEvent
{
    //Bu 2 prop yeni bir event oluşunca Id si ve ne zmaan oluştuğunu gösteriyor.

    [JsonProperty]
    public Guid Id { get; private set; }

    [JsonProperty]
    public DateTime CreatedDate { get; private set; }

   
    public IntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreatedDate = DateTime.Now;
    }

    [JsonConstructor]
    public IntegrationEvent(Guid id, DateTime createdDate)
    {
        Id = id;
        CreatedDate = createdDate;
    }
}
