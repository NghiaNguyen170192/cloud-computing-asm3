using MediatR;

namespace NetCore.Donation.Application.Outbox.Process;

public class DrainOutboxMessagesCommandHandler(IMediator mediator)
    : IRequestHandler<DrainOutboxMessagesCommand, int>
{
    public async Task<int> Handle(DrainOutboxMessagesCommand request, CancellationToken cancellationToken)
    {
        var processed = 0;

        for (var cycle = 0; cycle < request.MaxCycles; cycle++)
        {
            var batch = await mediator.Send(
                new ProcessOutboxMessagesCommand(request.BatchSize),
                cancellationToken);
            if (batch == 0)
            {
                return processed;
            }

            processed += batch;
        }

        return processed;
    }
}
