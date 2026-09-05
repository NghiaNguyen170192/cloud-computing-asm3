using MediatR;

namespace NetCore.Donation.Application.Outbox.Process;

public sealed record DrainOutboxMessagesCommand(int BatchSize = 20, int MaxCycles = 15) : IRequest<int>;
