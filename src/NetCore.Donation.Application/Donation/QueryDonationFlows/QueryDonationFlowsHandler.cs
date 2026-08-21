using MediatR;
using NetCore.Donation.Application.Donation.DTOs;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Application.Donation.QueryDonationFlows;

public class QueryDonationFlowsHandler(IOutboxMessageRepository outboxMessageRepository)
    : IRequestHandler<QueryDonationFlows, IReadOnlyList<QueryDonationFlowDto>>
{
    public async Task<IReadOnlyList<QueryDonationFlowDto>> Handle(
        QueryDonationFlows request,
        CancellationToken cancellationToken)
    {
        var messages = await outboxMessageRepository.ListAsync(cancellationToken);
        return DonationFlowAssembler.Assemble(messages);
    }
}
