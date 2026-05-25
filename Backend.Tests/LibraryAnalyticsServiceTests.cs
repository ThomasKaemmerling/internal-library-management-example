using Backend.Domain;
using Backend.Services;

namespace Backend.Tests;

public sealed class LibraryAnalyticsServiceTests
{
    private readonly LibraryAnalyticsService _service = new();

    [Fact]
    public void GetOverdueLoans_ReturnsOnlyUnreturnedAndPastDueLoans()
    {
        var data = BuildData();

        var result = _service.GetOverdueLoans(data, new DateOnly(2026, 05, 24));

        Assert.Equal(2, result.Count);
        Assert.Equal("L2", result[0].LoanId);
        Assert.Equal("L1", result[1].LoanId);
        Assert.All(result, r => Assert.True(r.DaysOverdue > 0));
    }

    [Fact]
    public void GetTopBorrowedBooksPerGenre_ReturnsTopThreeForEachGenre()
    {
        var data = BuildData();

        var result = _service.GetTopBorrowedBooksPerGenre(data, topPerGenre: 3);

        var itGenre = Assert.Single(result, r => r.Genre == "IT");
        Assert.Equal(3, itGenre.Books.Count);
        Assert.Equal("B1", itGenre.Books[0].BookId);
        Assert.Equal(3, itGenre.Books[0].BorrowCount);

        var novelGenre = Assert.Single(result, r => r.Genre == "Roman");
        Assert.Single(novelGenre.Books);
        Assert.Equal("B4", novelGenre.Books[0].BookId);
    }

    [Fact]
    public void GetAverageLoanDurationDays_UsesCompletedLoansOnly()
    {
        var data = BuildData();

        var result = _service.GetAverageLoanDurationDays(data);

        Assert.NotNull(result);
        Assert.Equal(19, result!.Value, 2);
    }

    private static LibraryData BuildData()
    {
        var books = new List<Book>
        {
            new("B1", "Clean Code", "A", "IT", 2008),
            new("B2", "DDD", "B", "IT", 2003),
            new("B3", "Refactoring", "C", "IT", 1999),
            new("B4", "Der Schwarm", "D", "Roman", 2004)
        };

        var loans = new List<Loan>
        {
            new("L1", "B1", new Borrower("E1", "Anna"), new DateOnly(2026, 4, 1), new DateOnly(2026, 5, 1), null),
            new("L2", "B2", new Borrower("E2", "Markus"), new DateOnly(2026, 3, 1), new DateOnly(2026, 4, 1), null),
            new("L3", "B1", new Borrower("E3", "Julia"), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 11)),
            new("L4", "B1", new Borrower("E4", "Nina"), new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 15), new DateOnly(2026, 2, 20)),
            new("L5", "B2", new Borrower("E1", "Anna"), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 20), new DateOnly(2026, 1, 20)),
            new("L6", "B3", new Borrower("E2", "Markus"), new DateOnly(2026, 2, 10), new DateOnly(2026, 2, 18), new DateOnly(2026, 2, 21)),
            new("L7", "B4", new Borrower("E5", "Tobias"), new DateOnly(2026, 3, 10), new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 15))
        };

        return new LibraryData(books, loans);
    }
}
