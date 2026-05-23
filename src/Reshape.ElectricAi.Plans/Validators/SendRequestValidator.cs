using FluentValidation;
using Reshape.ElectricAi.Core.Dtos.Notifications;

namespace Reshape.ElectricAi.Plans.Validators;

public sealed class SendRequestValidator : AbstractValidator<SendRequest>
{
    public SendRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(120);

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required.")
            .MaximumLength(500);

        RuleFor(x => x.Icon)
            .Must(BeAbsoluteOrRelativeUri).When(x => !string.IsNullOrWhiteSpace(x.Icon))
            .WithMessage("Icon must be a valid URL or absolute path.");

        RuleFor(x => x.Badge)
            .Must(BeAbsoluteOrRelativeUri).When(x => !string.IsNullOrWhiteSpace(x.Badge))
            .WithMessage("Badge must be a valid URL or absolute path.");

        RuleFor(x => x.Url)
            .Must(BeAbsoluteOrRelativeUri).When(x => !string.IsNullOrWhiteSpace(x.Url))
            .WithMessage("Url must be a valid URL or absolute path.");
    }

    private static bool BeAbsoluteOrRelativeUri(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return true;
        }

        if (candidate.StartsWith('/'))
        {
            return true;
        }

        return Uri.TryCreate(candidate, UriKind.Absolute, out _);
    }
}
