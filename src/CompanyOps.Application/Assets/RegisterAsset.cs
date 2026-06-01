using CompanyOps.Application.Abstractions;
using CompanyOps.Application.Common;
using CompanyOps.Domain.Assets;
using CompanyOps.Domain.Auditing;
using FluentValidation;

namespace CompanyOps.Application.Assets;

/// <summary>Register a new asset into inventory. ActorId is the authenticated IT Admin (JWT sub).</summary>
/// <remarks><c>Type</c> is nullable so an omitted asset type is rejected by the validator rather
/// than silently defaulting to <c>Laptop</c>.</remarks>
public sealed record RegisterAssetCommand(string Tag, string Name, AssetType? Type, Guid ActorId);

/// <summary>
/// Input validation for <see cref="RegisterAssetCommand"/> at the Application boundary
/// (non-negotiable #2). The Domain re-enforces these; this turns bad input into a field-level 400.
/// </summary>
public sealed class RegisterAssetValidator : AbstractValidator<RegisterAssetCommand>
{
    public RegisterAssetValidator()
    {
        RuleFor(x => x.Tag).NotEmpty().MaximumLength(Asset.TagMaxLength);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Asset.NameMaxLength);
        RuleFor(x => x.Type).NotNull().IsInEnum();
    }
}

public sealed class RegisterAssetHandler(
    IValidator<RegisterAssetCommand> validator,
    IAssetRepository assets,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AssetDto> HandleAsync(RegisterAssetCommand command, CancellationToken cancellationToken = default)
    {
        // Validate at the Application boundary (non-negotiable #2) before the aggregate is built.
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var now = timeProvider.GetUtcNow();

        // Order matters: Register validates + normalizes the tag (trim) — so we check uniqueness
        // against the normalized value — but the conflict check runs before Add, so a rejected
        // duplicate is never enlisted/persisted. A clean 409 instead of a unique-index 500.
        var asset = Asset.Register(command.Tag, command.Name, command.Type!.Value, now);
        if (await assets.TagExistsAsync(asset.Tag, cancellationToken))
        {
            throw new ConflictException($"An asset with tag '{asset.Tag}' already exists.");
        }

        assets.Add(asset);
        auditLogger.Add(AuditLog.ForAsset(AuditAction.AssetRegistered, asset.Id, command.ActorId, null, asset.Status, now));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AssetDto.FromDomain(asset);
    }
}
