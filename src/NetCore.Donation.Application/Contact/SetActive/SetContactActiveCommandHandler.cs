using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Contact.SetActive;

public class SetContactActiveCommandHandler(
    IUnitOfWork unitOfWork,
    IContactRepository contactRepository)
    : IRequestHandler<SetContactActiveCommand, bool>
{
    public async Task<bool> Handle(SetContactActiveCommand request, CancellationToken cancellationToken)
    {
        var contact = await contactRepository.FindByIdAsync(request.Id, cancellationToken);
        if (contact is null)
        {
            return false;
        }

        if (request.IsActive)
        {
            contact.Activate();
        }
        else
        {
            contact.Deactivate();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}