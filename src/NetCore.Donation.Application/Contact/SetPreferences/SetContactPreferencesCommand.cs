using MediatR;

namespace NetCore.Donation.Application.Contact.SetPreferences;

public sealed record SetContactPreferencesCommand(Guid Id, bool DoNotEmail, bool DoNotSms) : IRequest<bool>;
