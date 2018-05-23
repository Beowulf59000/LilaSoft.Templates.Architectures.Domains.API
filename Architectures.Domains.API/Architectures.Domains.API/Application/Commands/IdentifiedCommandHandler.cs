namespace LilaSoft.Architectures.Domains.API.Application.Commands
{
    using LilaSoft.Architectures.Domains.Infrastructure.Idempotency;
    using MediatR;
    using System.Threading;
    using System.Threading.Tasks;

    public class IdentifiedCommandHandler<T, R> : IRequestHandler<IdentifiedCommand<T, R>, R> where T : IRequest<R>
    {
        private readonly IMediator _mediator;
        private readonly IRequestManager _requestManager;

        public IdentifiedCommandHandler(IMediator mediator, IRequestManager requestManager)
        {
            _mediator = mediator;
            _requestManager = requestManager;
        }

        protected virtual R CreateResultForDuplicateRequest()
        {
            return default(R);
        }

        public async Task<R> Handle(IdentifiedCommand<T, R> message, CancellationToken cancellationToken)
        {
            var alreadyExists = await _requestManager.ExistAsync(message.Id);
            if (alreadyExists)
            {
                return CreateResultForDuplicateRequest();
            }
            else
            {
                await _requestManager.CreateRequestForCommandAsync<T>(message.Id);

                // Send the embeded business command to mediator so it runs its related CommandHandler 
                var result = await _mediator.Send(message.Command);

                return result;
            }
        }
    }
}
