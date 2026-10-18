namespace ProductManagementApi.Validators

{
    using FluentValidation;
    using ProductManagementApi.DTO;

    public class StockLevelRequestValidator : AbstractValidator<StockLevelRequest>
    {
        public StockLevelRequestValidator()
        {
            RuleFor(x => x.Min).NotNull().GreaterThanOrEqualTo(0);
            RuleFor(x => x.Max).NotNull().GreaterThanOrEqualTo(0);

            RuleFor(x => x)
                .Must(x => x.Min <= x.Max)
                .When(x => x.Min.HasValue && x.Max.HasValue)
                .WithMessage("Min must be less than or equal to Max.");
        }
    }
}