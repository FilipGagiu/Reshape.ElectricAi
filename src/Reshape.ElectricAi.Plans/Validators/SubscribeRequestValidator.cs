using FluentValidation;
using Reshape.ElectricAi.Core.Dtos.Notifications;

namespace Reshape.ElectricAi.Plans.Validators;

public sealed class SubscribeRequestValidator : AbstractValidator<SubscribeRequest>
{
    public SubscribeRequestValidator()
    {
        RuleFor(x => x.Endpoint)
            .NotEmpty().WithMessage("Endpoint is required.")
            .Must(BeAbsoluteHttpsUri).WithMessage("Endpoint must be an absolute https URL.");

        RuleFor(x => x.P256dh)
            .NotEmpty().WithMessage("P256dh key is required.")
            .MaximumLength(200);

        RuleFor(x => x.Auth)
            .NotEmpty().WithMessage("Auth key is required.")
            .MaximumLength(64);

        RuleFor(x => x.UserAgent)
            .MaximumLength(512);
    }

    private static bool BeAbsoluteHttpsUri(string candidate) =>
        Uri.TryCreate(candidate, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}
