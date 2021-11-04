using CAP.Application;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Outbox.Application
{
    public interface IMessageRepository
    {
        Task<OutboxMessage> GetOutboxMessageAsync(Guid messageId, CancellationToken cancellationToken = default);
        Task UpdateOutMessageStatusAsync(Guid messageId, OutboxMessageStatus status, CancellationToken cancellationToken = default);
        Task<IEnumerable<OutboxMessage>> GetPendingOutboxMessagesAsync(int batchCount, CancellationToken cancellationToken = default);

        Task<OutboxMessage> SaveOutboxMessageAsync(ServiceBusSendable request,
                                                   ChannelType channelType,
                                                   string channelName,
                                                   CancellationToken cancellationToken = default);
    }

    public class MessageRepository : IMessageRepository
    {
        private readonly OutboxDbContext _messageContext;
        private readonly ILogger<MessageRepository> _logger;

        public MessageRepository(OutboxDbContext messageContext, ILogger<MessageRepository> logger)
        {
            _messageContext = messageContext;
            _logger = logger;
        }

        public async Task<OutboxMessage> GetOutboxMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            return (await _messageContext.OutboxMessages.FirstOrDefaultAsync(x => x.MessageId == messageId,
                                                                             cancellationToken));
        }

        public async Task<OutboxMessage> SaveOutboxMessageAsync(ServiceBusSendable request,
                                                                ChannelType channelType,
                                                                string channelName,
                                                                CancellationToken cancellationToken = default)
        {
            try
            {
                var outboxMessage = new OutboxMessage
                {
                    MessageId = request.MessageId,
                    Name = request.GetType().Name,
                    ChannelType = channelType,
                    ChannelName = channelName,
                    Payload = JsonConvert.SerializeObject(request),
                    Status = OutboxMessageStatus.Pending,
                    OccurredOn = DateTime.UtcNow,
                };

                await _messageContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
                await _messageContext.SaveChangesAsync(cancellationToken);

                return outboxMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to log outbox message {request.MessageId}: {ex.Message}");
                return null;
            }
        }

        public async Task UpdateOutMessageStatusAsync(Guid messageId, OutboxMessageStatus status, CancellationToken cancellationToken = default)
        {
            try
            {
                var outboxMessageToUpdate = await _messageContext.OutboxMessages.FirstOrDefaultAsync(
                                                                                                     x => x.MessageId == messageId,
                                                                                                     cancellationToken);
                outboxMessageToUpdate.Status = status;
                outboxMessageToUpdate.ProcessedAt = DateTime.UtcNow;

                _messageContext.OutboxMessages.Update(outboxMessageToUpdate);
                await _messageContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                                 "Failed to update status for message {MessageId}: {ErrorMessage}",
                                 messageId,
                                 ex.Message);
            }
        }

        public async Task<IEnumerable<OutboxMessage>> GetPendingOutboxMessagesAsync(int batchCount, CancellationToken cancellationToken = default)
        {
            var batchCountParameter = new SqlParameter("@batchCount", batchCount);
            var statusParameter = new SqlParameter("@status", OutboxMessageStatus.Processing.ToString());

            var connection = await _messageContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            try
            {
                var outboxMessages = await _messageContext.OutboxMessages.FromSqlRaw(
                                                                                        @"update top (@batchCount) OutboxMessages 
                                                                                        set Status = @status
                                                                                        output inserted.Id,
                                                                                        inserted.MessageId,
                                                                                        inserted.Name,
                                                                                        inserted.ChannelType,
                                                                                        inserted.ChannelName,
                                                                                        inserted.Payload,
                                                                                        inserted.Status,
                                                                                        inserted.OccurredOn,
                                                                                        inserted.ProcessedAt
                                                                                            from OutboxMessages
                                                                                            with (ROWLOCK, READPAST, UPDLOCK)
                                                                                            where Status = 'Pending'",
                                                                                        batchCountParameter,
                                                                                        statusParameter)
                    .ToListAsync(cancellationToken);

                await connection.CommitAsync(cancellationToken);

                return outboxMessages;
            }

            catch (Exception ex)
            {
                await connection.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
