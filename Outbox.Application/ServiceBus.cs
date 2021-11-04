using Azure.Messaging.ServiceBus;
using CAP.Application;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Outbox.Application
{
    public interface IServiceBus
    {
        Task SendMessageAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken);
        Task SendMessage(object payload, string topic, CancellationToken cancellationToken);
    }

    public class ServiceBus : IServiceBus
    {
        private readonly ILogger<ServiceBusClient> _logger;
        private readonly ServiceBusClientSingleton _serviceBusClientSingleton;

        public ServiceBus(ILogger<ServiceBusClient> logger, ServiceBusClientSingleton serviceBusClientSingleton)
        {
            _logger = logger;
            _serviceBusClientSingleton = serviceBusClientSingleton;
        }

        public async Task SendMessageAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken)
        {
            try
            {
                var serviceBusMessage = GenerateServiceBusMessage(JsonConvert.DeserializeObject(outboxMessage.Payload),
                                                                  outboxMessage.MessageId);

                var serviceBusClient = _serviceBusClientSingleton.Client;
                var sender = serviceBusClient.CreateSender(outboxMessage.ChannelName);
                await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
                await sender.CloseAsync(cancellationToken);

                _logger.LogInformation("Message sent to {ChannelType} {ChannelName}", outboxMessage.ChannelType,
                                       outboxMessage.ChannelName);
            }
            catch (Exception ex)
            {
                throw new Exception(
                                                      $"Message failed to send to {outboxMessage.ChannelType} {outboxMessage.ChannelName}",
                                                      ex);
            }
        }

        private ServiceBusMessage GenerateServiceBusMessage(object payload, Guid? messageId = null)
        {
            var serviceBusMessage = new ServiceBusMessage()
            {
                Body = new BinaryData(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload))),
                MessageId = messageId.HasValue
                    ? messageId.ToString()
                    : Guid.NewGuid().ToString(),
                PartitionKey = Guid.NewGuid().ToString(),
                CorrelationId = Guid.NewGuid().ToString()
            };

            return serviceBusMessage;
        }

        public async Task SendMessage(object payload, string topic, CancellationToken cancellationToken)
        {
            try
            {
                var serviceBusMessage = new ServiceBusMessage()
                {
                    Body = new BinaryData(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload))),
                    MessageId = Guid.NewGuid().ToString(),
                    PartitionKey = Guid.NewGuid().ToString(),
                    CorrelationId = Guid.NewGuid().ToString()
                };

                var serviceBusClient = _serviceBusClientSingleton.Client;
                var sender = serviceBusClient.CreateSender(topic);
                await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
                await sender.CloseAsync(cancellationToken);

                _logger.LogInformation($"Message sent to topic {topic}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Message failed to send to topic {topic}", ex);
            }
        }
    }
    public class ServiceBusClientSingleton
    {
        public readonly ServiceBusClient Client;

        public ServiceBusClientSingleton(string connectionString)
        {
            Client = new ServiceBusClient(connectionString);
        }
    }
}
