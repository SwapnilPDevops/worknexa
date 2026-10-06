using FluentValidation;
using Microsoft.Extensions.Options;
using Worknexa.Application.Common;
using Worknexa.Application.Configuration;

namespace Worknexa.Application.Validation;

public sealed class PageRequestValidator : AbstractValidator<PageRequest>
{
    public PageRequestValidator(IOptions<PaginationSettings> pagination)
    {
        var maxPageSize = pagination.Value.MaxPageSize;

        RuleFor(request => request.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, maxPageSize);
    }
}
