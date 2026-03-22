using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Application.Common.Models;

public record PagedResult<T>
    ( IReadOnlyList<T> Items,
      int TotalCount,
      int PageNumber,
      int PageSize
    )
{
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => PageNumber < TotalPages;
}
