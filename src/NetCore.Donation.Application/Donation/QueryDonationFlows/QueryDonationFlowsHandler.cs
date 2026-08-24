using MediatR;
using NetCore.Donation.Application.Donation.DTOs;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Application.Donation.QueryDonationFlows;

public class QueryDonationFlowsHandler(
    IOutboxMessageRepository outboxMessageRepository,
    IContactRepository contactRepository,
    ITransactionRepository transactionRepository,
    IJournalRepository journalRepository,
    IReceiptRepository receiptRepository,
    IPaymentScheduleRepository paymentScheduleRepository,
    IPaymentMethodRepository paymentMethodRepository)
    : IRequestHandler<QueryDonationFlows, IReadOnlyList<QueryDonationFlowDto>>
{
    public async Task<IReadOnlyList<QueryDonationFlowDto>> Handle(
        QueryDonationFlows request,
        CancellationToken cancellationToken)
    {
        var messages = await outboxMessageRepository.ListAsync(cancellationToken);
        var flows = DonationFlowAssembler.IncludePostedTransactions(
            DonationFlowAssembler.Assemble(messages),
            transactionRepository.GetAll().ToList(),
            journalRepository.GetAll().ToList(),
            receiptRepository.GetAll().ToList(),
            paymentScheduleRepository.GetAll()
                .Select(schedule => new { schedule.Id, schedule.Identifier })
                .ToList()
                .ToDictionary(schedule => schedule.Id, schedule => schedule.Identifier),
            paymentMethodRepository.GetAll()
                .Select(method => new { method.Id, method.DisplayName })
                .ToList()
                .ToDictionary(method => method.Id, method => method.DisplayName));
        ApplyContactNames(flows);
        return flows;
    }

    private void ApplyContactNames(IReadOnlyList<QueryDonationFlowDto> flows)
    {
        var contactIds = flows
            .Where(flow => flow.ContactId is not null)
            .Select(flow => flow.ContactId!.Value)
            .Distinct()
            .ToHashSet();
        if (contactIds.Count == 0)
        {
            return;
        }

        var names = contactRepository.GetAll()
            .Where(contact => contactIds.Contains(contact.Id))
            .Select(contact => new { contact.Id, contact.FirstName, contact.LastName })
            .ToList()
            .ToDictionary(contact => contact.Id, contact => $"{contact.FirstName} {contact.LastName}".Trim());

        foreach (var flow in flows)
        {
            if (flow.ContactId is { } contactId && names.TryGetValue(contactId, out var fullName))
            {
                flow.ContactFullName = fullName;
            }
        }
    }
}
