namespace ProductManagementApi.Validators

{
    using FluentValidation;
    using ProductManagementApi.DTO;

    public class SearchProductsRequestValidator : AbstractValidator<SearchProductsRequest>
    {
        public SearchProductsRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(256).WithMessage("Name cannot exceed 256 characters.");
        }
    }
}