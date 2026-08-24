using MediatR;
using NetCore.Donation.Domain.Enums;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Donation.UserMakesDonation;

public class UserMakesDonationCommandHandler(
    IUnitOfWork unitOfWork,
    ICountryRepository countryRepository,
    IContactRepository contactRepository,
    IPaymentMethodRepository paymentMethodRepository,
    IPaymentScheduleRepository paymentScheduleRepository,
    ITransactionRepository transactionRepository)
    : IRequestHandler<UserMakesDonationCommand, UserMakesDonationResult>
{
    public async Task<UserMakesDonationResult> Handle(
        UserMakesDonationCommand request,
        CancellationToken cancellationToken)
    {
        var country = await countryRepository.FindByIdAsync(request.CountryId);
        if (country is null)
        {
            throw new ArgumentException($"Country '{request.CountryId}' was not found.", nameof(request));
        }

        var contact = await contactRepository.FindByEmailAsync(request.Email, cancellationToken);
        if (contact is null)
        {
            contact = Domain.Entities.Contact.Create(
                request.FirstName,
                request.LastName,
                request.DateOfBirth,
                request.AddressLine,
                request.Email,
                request.PhoneNumber,
                request.CountryId,
                request.Gender,
                request.DoNotEmail,
                request.DoNotSms);
            await contactRepository.AddAsync(contact, cancellationToken);
        }
        else
        {
            contact.SetCommunicationPreferences(request.DoNotEmail, request.DoNotSms);
        }

        var bookDate = request.BookDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var paymentMethod = Domain.Entities.PaymentMethod.Create(
            contact.Id,
            request.PaymentMethodName,
            request.PaymentType);
        await paymentMethodRepository.AddAsync(paymentMethod, cancellationToken);

        if (request.IsRecurring)
        {
            var interval = ResolveRecurringInterval(request);
            var schedule = Domain.Entities.PaymentSchedule.Create(
                contact.Id,
                paymentMethod.Id,
                request.Amount,
                bookDate,
                interval,
                request.PaymentType);
            await paymentScheduleRepository.AddAsync(schedule, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new UserMakesDonationResult(
                contact.Id,
                paymentMethod.Id,
                schedule.Id,
                null,
                true);
        }

        var transaction = Domain.Entities.Transaction.CreatePending(
            request.Amount,
            paymentScheduleId: null,
            contact.Id,
            paymentMethod.Id,
            request.PaymentType,
            bookDate,
            isRecurring: false);
        await transactionRepository.AddAsync(transaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserMakesDonationResult(
            contact.Id,
            paymentMethod.Id,
            null,
            transaction.Id,
            false);
    }

    private static RecurringInterval ResolveRecurringInterval(UserMakesDonationCommand request)
    {
        if (request.RecurringInterval == RecurringInterval.OneOff)
        {
            throw new ArgumentException(
                "A recurring donation requires a recurring interval.",
                nameof(request));
        }

        return request.RecurringInterval;
    }
}
