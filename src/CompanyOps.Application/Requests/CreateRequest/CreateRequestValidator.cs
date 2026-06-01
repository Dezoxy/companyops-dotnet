using CompanyOps.Domain.Requests;
using FluentValidation;

namespace CompanyOps.Application.Requests.CreateRequest;

/// <summary>
/// Input validation for <see cref="CreateRequestCommand"/> at the Application boundary
/// (non-negotiable #2). The Domain re-enforces these invariants — this layer turns a bad
/// request into a clean, field-level 400 before the aggregate is built, and closes the gaps
/// the Domain cannot see (a missing required <c>Type</c> that would otherwise default silently).
/// </summary>
public sealed class CreateRequestValidator : AbstractValidator<CreateRequestCommand>
{
    public CreateRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(Request.TitleMaxLength);

        // Optional, but bounded: an uncapped description is a write-path DoS vector.
        RuleFor(x => x.Description)
            .MaximumLength(Request.DescriptionMaxLength)
            .When(x => x.Description is not null);

        // Required: a missing type must not default to Procurement (the command keeps it nullable
        // so omission is distinguishable); a present value must be a defined enum member.
        RuleFor(x => x.Type)
            .NotNull()
            .IsInEnum();

        RuleFor(x => x.Priority)
            .IsInEnum()
            .When(x => x.Priority is not null);

        RuleFor(x => x.Category)
            .IsInEnum()
            .When(x => x.Category is not null);
    }
}
