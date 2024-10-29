using BasketService.Api.Core.Application.Repository;
using BasketService.Api.Core.Domain.Models;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Net;

namespace BasketService.Api.Infrastructure.Repository
{
    public class RedisBasketRepository : IBasketRepository
    {
        private readonly ILogger<RedisBasketRepository> _logger;
        private readonly ConnectionMultiplexer _redis;  //Redis bağlantısını yöneten ConnectionMultiplexer nesnesi
        private readonly IDatabase _database; //Redis içindeki veritabanına erişim sağlayan IDatabase nesnesi.



        public RedisBasketRepository(ILoggerFactory loggerFactory, ConnectionMultiplexer redis)
        {
            _logger = loggerFactory.CreateLogger<RedisBasketRepository>();
            _redis = redis;
            _database = redis.GetDatabase();
        }

        public async Task<bool> DeleteBasketAsync(string id) // id ,username demek aslında
        {
            return await _database.KeyDeleteAsync(id);

            //Verilen id anahtarıyla (username) Redis'ten ilgili veriyi siler ve true veya false olarak başarılı olup olmadığını döner.
        }
        public IEnumerable<string> GetUsers()
        {
            IServer server = GetServer();
            IEnumerable<RedisKey> data = server.Keys();

            return data?.Select(k => k.ToString());

            //Redis sunucusundaki tüm anahtarları alır (yani tüm kullanıcıları). GetServer metodu ile Redis sunucusuna bağlanarak Keys fonksiyonuyla tüm anahtarları çeker.
        }
        public async Task<CustomerBasket> GetBasketAsync(string customerId)
        {
            RedisValue data = await _database.StringGetAsync(customerId);

            if (data.IsNullOrEmpty)
            {
                return null;  //Eğer veri yoksa null döner.
            } 

            return JsonConvert.DeserializeObject<CustomerBasket>(data);

            //Verilen customerId'ye ait veriyi Redis'ten alır. Bu veri JSON formatında saklandığından JsonConvert.DeserializeObject<CustomerBasket>(data) kullanarak CustomerBasket nesnesine çevirir.
        }
        public async Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket)
        {
            bool created = await _database.StringSetAsync(basket.BuyerId, JsonConvert.SerializeObject(basket));

            if (!created)
            {
                _logger.LogInformation("Problem occur persisting the item.");
                return null;
            }

            _logger.LogInformation("Basket item persisted succesfully.");

            return await GetBasketAsync(basket.BuyerId);

            //Sepet güncelleme işlemi yapar. Verilen CustomerBasket nesnesini JSON formatına çevirerek Redis'e kaydeder.
            //Eğer kayıt başarısız olursa null döner ve bir hata mesajı loglanır.
            //Kayıt başarılı olursa, log mesajı ile durum bildirilir ve güncellenmiş sepet geri döndürülür.
        }



        private IServer GetServer()
        {
            EndPoint[] endpoint = _redis.GetEndPoints();
            return _redis.GetServer(endpoint.First());
        }
    }
}
