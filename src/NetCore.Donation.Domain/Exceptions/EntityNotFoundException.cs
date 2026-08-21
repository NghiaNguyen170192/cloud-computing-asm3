namespace NetCore.Donation.Domain.Exceptions;

public class EntityNotFoundException(string request, Guid id) : Exception($"[{request}]-[{id}] is not found");