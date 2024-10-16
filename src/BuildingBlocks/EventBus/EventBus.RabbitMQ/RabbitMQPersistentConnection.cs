using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Net.Sockets;

namespace EventBus.RabbitMQ;

public class RabbitMQPersistentConnection : IDisposable
{
    IConnectionFactory connectionFactory;
    private readonly int retryCount; // bağlanmak için kaç kere denesin diye
    private IConnection connection; // hangi conenction ın açık olup  olmadığı bilgisini tutmak için
    private object lock_object = new object();  // Bu her conenction create edildiğinde gelecek.
    private bool _dispose;

    public RabbitMQPersistentConnection(IConnectionFactory connectionFactory, int retryCount = 5)
    {
        this.connectionFactory = connectionFactory;
        this.retryCount = retryCount;
    }
    public bool isConnection => connection != null && connection.IsOpen; // Bu 2 şartı sağlıyorsa true dönebilir problem yok. (O an connection ın aktif olup olmadığı bilgisini dönüyor.) - (Connection un olup ve açık olması durumuna göre bakılıyor.)

    public IModel CreateModel()
    {
        return connection.CreateModel();
    }
    public void Dispose()
    {
        _dispose = true; // bunun amacı aşağıda yapılan Retry mekanizmaları çalıştığında dispose edilip edilmemsinin kontolünü yapıyoruz.
        connection.Dispose();
    }
    public bool TryConnect() // Bu yapı Retry mekanizması kurmaya yarıyor.
    {
        // Kaç kere deneyeceği parametre olarak verildi bu yüzden recursive bir şey olmayacak ve sonsuza kadar gitmeyecek. Sınırlı bir tekrar etme mekanizması oluşturuldu.
         
        lock (lock_object) // lock_object'sini kilitledik. Burada aynı method çağrıldığında önce ki methodun bitmeisni bekleyecek.
        {
            RetryPolicy policy = Policy.Handle<SocketException>()
                              .Or<BrokerUnreachableException>()
                              .WaitAndRetry(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                              {
                              }

                            );

            policy.Execute(() =>
            {
                connection = connectionFactory.CreateConnection();
            });


            if (isConnection)
            { // Burada ki eventler RabbitMQ nun eventleri
                connection.ConnectionShutdown += Connection_ConnectionShutdown;
                connection.CallbackException += Connection_CallbackException;
                connection.ConnectionBlocked += Connection_ConnectionBlocked;
                //log
                return true;
            }
            return false;
        }
    }

    private void Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
    {
        if (_dispose) return; // Eğer ki dispose edildiyse denemesin ve geri gitsin diye yazdık.
                              // log Connection_ConnectionShutdown
        TryConnect(); // Tekrar TryConnect yapısını çağırıp tekrar denemesi için
    }
    private void Connection_ConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
    {
        if (_dispose) return;
        TryConnect();
    }
    private void Connection_CallbackException(object sender, CallbackExceptionEventArgs e)
    {
        if (_dispose) return;
        TryConnect();
    }
}