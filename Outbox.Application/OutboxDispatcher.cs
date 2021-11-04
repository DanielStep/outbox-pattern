using CAP.Application;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Outbox.Application
{
    public interface IOutboxMessageDispatcher
    {
        Task DispatchOutBoxMessagesAsync(CancellationToken cancellationToken);
        void QueueInOutbox(Guid messageId);
        Task DispatchAsync(Guid messageId, CancellationToken cancellationToken);
    }

    public class OutboxMessageDispatcher : IOutboxMessageDispatcher
    {
        private List<Guid> OutboxMessageIds { get; } = new();

        private readonly ILogger<OutboxMessageDispatcher> _logger;
        private readonly IMessageRepository _messageRepository;
        private readonly IServiceBus _serviceBusClient;

        public OutboxMessageDispatcher(ILogger<OutboxMessageDispatcher> logger, IMessageRepository messageRepository, IServiceBus serviceBusClient)
        {
            _logger = logger;
            _messageRepository = messageRepository;
            _serviceBusClient = serviceBusClient;
        }

        public void QueueInOutbox(Guid messageId)
        {
            OutboxMessageIds.Add(messageId);
        }

        public List<Guid> ViewOutboxQueue()
        {
            return OutboxMessageIds.ToList();
        }

        public async Task DispatchOutBoxMessagesAsync(CancellationToken cancellationToken)
        {
            foreach (var outboxMessageId in OutboxMessageIds)
            {
                await DispatchAsync(outboxMessageId, cancellationToken);
            }

            OutboxMessageIds.Clear();
        }

        public async Task DispatchAsync(Guid messageId, CancellationToken cancellationToken)
        {
            var isDispatched = false;

            var outboxMessage = await _messageRepository.GetOutboxMessageAsync(messageId, cancellationToken);

            if (outboxMessage == null)
            {
                _logger.LogInformation("{MessageId} is not found in outbox", messageId);
                return;
            }

            try
            {
                await _serviceBusClient.SendMessageAsync(outboxMessage, cancellationToken);

                isDispatched = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox message service failed to process {MessageId}", messageId);
            }
            finally
            {
                var outboxMessageStatus =
                    isDispatched ? OutboxMessageStatus.Dispatched : OutboxMessageStatus.Pending;

                await _messageRepository.UpdateOutMessageStatusAsync(messageId,
                                                                     outboxMessageStatus,
                                                                     cancellationToken);

                _logger.LogInformation("Outbox message {MessageId} has {Status}", messageId, outboxMessage.Status);
            }
        }
    }
}
