using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Collections.Concurrent;
using System.Text;

namespace Guwahak.Network.Mq.Rpc
{
    public class RpcClient : IDisposable
    {
        private readonly string requestTo;
        private readonly string replyQueueName;

        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> callbackMapper;

        public RpcClient(ConnectionFactory connectionFactory, string requestTo, string replyQueue)
        {
            var factory = connectionFactory;
            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            this.requestTo = requestTo;
            replyQueueName = channel.QueueDeclare(queue: replyQueue).QueueName;

            callbackMapper = new ConcurrentDictionary<string, TaskCompletionSource<string>>();

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                if (!callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
                    return;
                var body = ea.Body.ToArray();
                var response = Encoding.UTF8.GetString(body);
                tcs.TrySetResult(response);
            };

            channel.BasicConsume(consumer: consumer,
                                 queue: replyQueueName,
                                 autoAck: true);
        }

        public Task<string> CallAsync(string message, CancellationToken cancellationToken = default)
        {
            IBasicProperties props = channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyQueueName;
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var tcs = new TaskCompletionSource<string>();
            callbackMapper.TryAdd(correlationId, tcs);

            channel.BasicPublish(exchange: string.Empty,
                                 routingKey: requestTo,
                                 basicProperties: props,
                                 body: messageBytes);

            cancellationToken.Register(() => callbackMapper.TryRemove(correlationId, out _));
            return tcs.Task;
        }

        public void Dispose()
        {
            connection.Close();
        }
    }
}
