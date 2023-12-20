using Guwahak.Network.Packet;
using Guwahak.Network.Utility;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Guwahak.Network.Mq.Rpc
{
    public delegate Task<string> OnReqArrived(object packet);
    public class MqRpcServer
    {
        readonly IConnection connection;
        readonly IModel channel;
        readonly OnReqArrived onRequestArrived;

        public MqRpcServer(ConnectionFactory connectionFactory, string queueName, OnReqArrived onRequest)
        {
            var factory = connectionFactory;
            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            channel.QueueDeclare(queue: queueName,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
            var consumer = new EventingBasicConsumer(channel);

            channel.BasicConsume(queue: queueName,
                             autoAck: false,
                             consumer: consumer);

            consumer.Received += OnReceive;
            onRequestArrived = onRequest;
        }

        public async void OnReceive(object o, BasicDeliverEventArgs ea)
        {
            var response = string.Empty;

            var channel = (o as EventingBasicConsumer)?.Model;

            var body = ea.Body.ToArray();
            var props = ea.BasicProperties;
            var replyProps = channel?.CreateBasicProperties();

            if (replyProps != null)
            {
                replyProps.CorrelationId = props.CorrelationId;
            }

            try
            {
                var message = Encoding.UTF8.GetString(body);
                Packet.Packet packet_raw = JsonUtil.JsonToObject<Packet.Packet>(message);
                Type t = Converter.GetType(packet_raw.Order);
                var packet = JsonUtil.JsonToObject(message, t);

                response = await onRequestArrived(packet);
            }
            catch (Exception e)
            {
                var error = new PacketException(e);
                response = JsonUtil.ObjectToJson(error);
            }
            finally
            {
                var responseBytes = Encoding.UTF8.GetBytes(response);
                channel.BasicPublish(exchange: string.Empty,
                                     routingKey: props.ReplyTo,
                                     basicProperties: replyProps,
                                     body: responseBytes);
                channel?.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
        }

        public void Dispose()
        {
            connection.Close();
        }
    }
}
