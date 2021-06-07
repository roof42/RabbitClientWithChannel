using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Receive
{
    class Program
    {
    public static async Task Main()
        {
            var myChannel = Channel.CreateUnbounded<string>();
            ConnectionFactory factory = CreateRabbitMQFactory();
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                await FetchMessageAndWriteToChannel(myChannel, channel);
                await InvokeExtermalService(myChannel);
                while (true) { }
            }

            static ConnectionFactory CreateRabbitMQFactory()
            {
                var factory = new ConnectionFactory() { HostName = "localhost" };
                factory.UserName = "admin";
                factory.Password = "Admin@123";
                return factory;
            }

            static async Task InvokeExtermalService(Channel<string> myChannel)
            {
                _ = await Task.Factory.StartNew(async () =>
                {
                    await foreach(var item in myChannel.Reader.ReadAllAsync())
                    {
                        Console.WriteLine("From channel t0 somewhere->"+item);
                        await Task.Delay(1000);
                    }
                });
            }

            static async Task FetchMessageAndWriteToChannel(Channel<string> myChannel, IModel channel)
            {
                channel.QueueDeclare("two.port", true, false, false, null);

                var consumer = new EventingBasicConsumer(channel);

                await Task.Factory.StartNew(() =>
                {
                    consumer.Received += async (model, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        Console.WriteLine("From rabbit to channel->"+message);
                        await myChannel.Writer.WriteAsync(message);
                    };
                    
                });
                channel.BasicConsume("two.port", true, consumer);
            }
        }
    }
}
