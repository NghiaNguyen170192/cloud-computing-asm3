using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Contact.SetPreferences;

public class SetContactPreferencesCommandHandler(
    IUnitOfWork unitOfWork,
    IContactRepository contactRepository)
    : IRequestHandler<SetContactPreferencesCommand, bool>
{
    public async Task<bool> Handle(SetContactPreferencesCommand request, CancellationToken cancellationToken)
    {
        var contact = await contactRepository.FindByIdAsync(request.Id, cancellationToken);
        if (contact is null)
        {
            return false;
        }

        contact.SetCommunicationPreferences(request.DoNotEmail, request.DoNotSms);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
