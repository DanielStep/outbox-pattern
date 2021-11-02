using CAP.Application;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CAP.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CapTestController : ControllerBase
    {
        private readonly OutboxDbContext _context;
        private readonly ICapPublisher _capBus;

        public CapTestController(OutboxDbContext context, ICapPublisher capPublisher)
        {
            _context = context;
            _capBus = capPublisher;
        }

        [HttpGet]
        public void Get()
        {
            using (_context.Database.BeginTransaction(_capBus, autoCommit: true))
            {
                _context.BusinessRecords.Add(new BusinessRecord() { Name = "ef.transaction" });

                _capBus.Publish("test-topic", "test-message");
            }
        }

        [NonAction]
        [CapSubscribe("test-topic")]
        public void Subscriber(string message)
        {
            Console.WriteLine($@"{DateTime.Now} Subscriber invoked, Info: {message}");
        }

    }
}
