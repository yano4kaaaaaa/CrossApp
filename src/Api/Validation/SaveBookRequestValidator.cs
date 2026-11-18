using Api.Contracts;
using FluentValidation;

namespace Api.Validation;

// Додаткове завдання 2: валідація через FluentValidation.
public sealed class SaveBookRequestValidator : AbstractValidator<SaveBookRequest>
{
    public SaveBookRequestValidator()
    {
        RuleFor(x => x.Isbn)
            .NotEmpty().WithMessage("ISBN обов'язковий.")
            .MaximumLength(20).WithMessage("ISBN: до 20 символів.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Назва обов'язкова.")
            .MaximumLength(200).WithMessage("Назва: до 200 символів.");

        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("Автор обов'язковий.")
            .MaximumLength(200).WithMessage("Автор: до 200 символів.");

        RuleFor(x => x.Year)
            .InclusiveBetween(1450, DateTime.Today.Year + 1)
            .WithMessage($"Рік має бути від 1450 до {DateTime.Today.Year + 1}.");

        RuleFor(x => x.Copies)
            .GreaterThanOrEqualTo(0).WithMessage("Кількість примірників не може бути від'ємною.");
    }
}