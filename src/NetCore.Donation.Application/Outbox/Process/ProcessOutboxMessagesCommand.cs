using MediatR;

namespace NetCore.Donation.Application.Outbox.Process;

public sealed record ProcessOutboxMessagesCommand(int BatchSize = 20) : IRequest<int>;
