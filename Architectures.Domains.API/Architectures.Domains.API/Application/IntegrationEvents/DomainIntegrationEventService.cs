using LilaSoft.Patterns.EventBus.EntityFrameworkIntegrationEventLog.Services;
using LilaSoft.Patterns.EventBus.EntityFrameworkIntegrationEventLog.Utilities;
using LilaSoft.Patterns.EventBus.EventBus.Abstractions;
using LilaSoft.Patterns.EventBus.EventBus.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace LilaSoft.Architectures.Domains.API.Application.IntegrationEvents
{
    public class DomainIntegrationEventService : IDomainIntegrationEventService
    {
        private readonly Func<DbConnection, IIntegrationEventLogService> _integrationEventLogServiceFactory;
        private readonly IEventBus _eventBus;
        private readonly DbContext _dbContext;
        private readonly IIntegrationEventLogService _eventLogService;

        public DomainIntegrationEventService(IEventBus eventBus, DbContext dbContext, Func<DbConnection, IIntegrationEventLogService> integrationEventLogServiceFactory)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _integrationEventLogServiceFactory = integrationEventLogServiceFactory ?? throw new ArgumentNullException(nameof(integrationEventLogServiceFactory));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _eventLogService = _integrationEventLogServiceFactory(_dbContext.Database.GetDbConnection());
        }

        public async Task PublishThroughEventBusAsync(IntegrationEvent evt)
        {
            await SaveEventAndOrderingContextChangesAsync(evt);
            _eventBus.Publish(evt);
            await _eventLogService.MarkEventAsPublishedAsync(evt);
        }

        private async Task SaveEventAndOrderingContextChangesAsync(IntegrationEvent evt)
        {          
            await ResilientTransaction.New(_dbContext)
                .ExecuteAsync(async () => {
                    await _dbContext.SaveChangesAsync();
                    await _eventLogService.SaveEventAsync(evt, _dbContext.Database.CurrentTransaction.GetDbTransaction());
                });
        }
    }
}
