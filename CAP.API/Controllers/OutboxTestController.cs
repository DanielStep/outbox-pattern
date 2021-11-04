using CAP.Application;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Outbox.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace CAP.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OutboxTestController : ControllerBase
    {
        private readonly OutboxDbContext _context;
        private readonly IOutboxMessageDispatcher _outboxDispatcher;
        private readonly IMessageRepository _messageRepo;

        public OutboxTestController(OutboxDbContext context, IOutboxMessageDispatcher outboxDispatcher, IMessageRepository messageRepo)
        {
            _context = context;
            _outboxDispatcher = outboxDispatcher;
            _messageRepo = messageRepo;
        }

        [HttpGet]
        public async Task Get()
        {
            var cancellationToken = new CancellationTokenSource().Token;

            using (var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var messageId = Guid.NewGuid();
                _context.BusinessRecords.Add(new BusinessRecord() { Name = "outbox-test" });

                var serviceBusEvent = new OutboxTestServiceBusEvent()
                {
                    CorrelationId = Guid.NewGuid(),
                    Jwt = "test",
                    MessageId = messageId,
                    MoulaApiKey = "test",
                    RequestSessionId = Guid.NewGuid()
                };
                await _messageRepo.SaveOutboxMessageAsync(serviceBusEvent,
                                                                ChannelType.Topic,
                                                                "test-topic",
                                                                cancellationToken);
                _outboxDispatcher.QueueInOutbox(messageId);


                _context.SaveChanges();
                transactionScope.Complete();
            }
            await _outboxDispatcher.DispatchOutBoxMessagesAsync(cancellationToken);
        }
    }
}
