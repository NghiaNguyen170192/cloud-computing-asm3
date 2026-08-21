using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.PaymentMethod.Create;

public class CreatePaymentMethodCommandHandler(
    IUnitOfWork unitOfWork,
    IPaymentMethodRepository paymentMethodRepository,
    IContactRepository contactRepository)
    : IRequestHandler<CreatePaymentMethodCommand, Guid>
{
    public async Task<Guid> Handle(CreatePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        if (!await contactRepository.IsExistAsync(request.ContactId, cancellationToken))
        {
            throw new ArgumentException($"Contact '{request.ContactId}' was not found.", nameof(request));
        }

        var paymentMethod = request.ToDbEntity();

        await paymentMethodRepository.AddAsync(paymentMethod, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return paymentMethod.Id;
    }
}