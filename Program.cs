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
    public static void Main()
        {
            var myChannel = Channel.CreateUnbounded<string>();
            ConnectionFactory factory = CreateRabbitMQFactory();
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                FetchMessageAndWriteToChannel(myChannel, channel);
                InvokeExtermalService(myChannel);
                while (true) { }
            }

            static ConnectionFactory CreateRabbitMQFactory()
            {
                var factory = new ConnectionFactory() { HostName = "localhost" };
                factory.UserName = "admin";
                factory.Password = "Admin@123";
                return factory;
            }

            static void InvokeExtermalService(Channel<string> myChannel)
            {
                _ = Task.Factory.StartNew(() =>
                {
                    var item = myChannel.Reader.ReadAsync();
                    Console.WriteLine(item);
                });
            }

            static void FetchMessageAndWriteToChannel(Channel<string> myChannel, IModel channel)
            {
                channel.QueueDeclare("two.port", true, false, false, null);

                var consumer = new EventingBasicConsumer(channel);

                _ = Task.Factory.StartNew(() =>
                {
                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        myChannel.Writer.WriteAsync(message);
                    };
                });

                channel.BasicConsume("two.port", true, consumer);
            }
        }
    }
}
