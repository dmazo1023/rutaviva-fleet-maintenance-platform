using RutaViva.BuildingBlocks.Domain.Guards;
using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Workshops.Domain.Common;
using RutaViva.Workshops.Domain.Events;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Domain;

public sealed class Workshop : AggregateRoot<WorkshopId>
{
    public const int MaxNameLength = 200;

    private Workshop()
    {
        Name = null!;
        TaxId = null!;
        Email = null!;
        Address = null!;
    }

    public string Name { get; private set; }

    public TaxId TaxId { get; private set; }

    public EmailAddress Email { get; private set; }

    public PhoneNumber? Phone { get; private set; }

    public PostalAddress Address { get; private set; }

    public WorkshopStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<Workshop> Register(
        string? name,
        TaxId taxId,
        EmailAddress email,
        PhoneNumber? phone,
        PostalAddress address)
    {
        Guard.NotNull(taxId);
        Guard.NotNull(email);
        Guard.NotNull(address);

        Result<string> validName = NormalizeName(name);
        if (validName.IsFailure)
        {
            return validName.Error;
        }

        DateTimeOffset now = TimeProvider.System.GetUtcNow();
        Workshop workshop = new()
        {
            Id = WorkshopId.New(),
            Name = validName.Value,
            TaxId = taxId,
            Email = email,
            Phone = phone,
            Address = address,
            Status = WorkshopStatus.Active,
            CreatedAt = now
        };

        workshop.Raise(new WorkshopRegistered(workshop.Id, taxId, workshop.Name, address.City, workshop.Status, now));
        return workshop;
    }

    public Result Activate() => ChangeStatus(WorkshopStatus.Active);

    public Result Deactivate() => ChangeStatus(WorkshopStatus.Inactive);

    private static Result<string> NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return WorkshopErrors.NameRequired;
        }

        string normalized = name.Trim();
        if (normalized.Length > MaxNameLength)
        {
            return WorkshopErrors.NameTooLong;
        }

        return normalized;
    }

    private Result ChangeStatus(WorkshopStatus target)
    {
        if (Status == target)
        {
            return Result.Success();
        }

        Status = target;
        Raise(new WorkshopStatusChanged(Id, Status, TimeProvider.System.GetUtcNow()));
        return Result.Success();
    }
}
