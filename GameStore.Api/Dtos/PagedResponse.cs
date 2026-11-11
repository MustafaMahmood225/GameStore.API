namespace GameStore.Api.Dtos
{
    // WHY: A generic record (the <T>) can wrap any kind of item, so you can reuse it later
    // for genres or anything else. It carries the page of items plus what a client needs
    // to build "next / previous" buttons.
    public record PagedResponse<T>(
        IReadOnlyList<T> Items, // the slice of games
        int Page,
        int PageSize,
        int TotalCount) // the total number of games in the database, not just this page
    {
        // WHY: Computed from the other values, so it can never disagree with them.
        // Math.Ceiling rounds up (21 items at 10 per page = 3 pages), and the cast to
        // double avoids integer division, which would drop the remainder
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / (double)PageSize);

    }



}
