using Backend.Domain;

namespace Backend.Services;

public sealed class LibraryAnalyticsService
{
    public IReadOnlyList<OverdueLoanResult> GetOverdueLoans(LibraryData data, DateOnly today)
    {
        var booksById = data.Books.ToDictionary(b => b.Id, StringComparer.OrdinalIgnoreCase);

        return data.Loans
            .Where(l => l.ReturnDate is null && l.DueDate < today)
            .Select(loan =>
            {
                var hasBook = booksById.TryGetValue(loan.BookRef, out var book);
                return new OverdueLoanResult(
                    loan.Id,
                    loan.BookRef,
                    hasBook ? book!.Title : "Unknown book",
                    loan.Borrower.EmployeeId,
                    loan.Borrower.Name,
                    loan.DueDate,
                    today.DayNumber - loan.DueDate.DayNumber);
            })
            .OrderByDescending(r => r.DaysOverdue)
            .ToList();
    }

    public IReadOnlyList<GenreTopBooksResult> GetTopBorrowedBooksPerGenre(LibraryData data, int topPerGenre = 3)
    {
        var booksById = data.Books.ToDictionary(b => b.Id, StringComparer.OrdinalIgnoreCase);

        var borrowedBooks = data.Loans
            .Where(l => booksById.ContainsKey(l.BookRef))
            .GroupBy(l => l.BookRef, StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                Book = booksById[g.Key],
                BorrowCount = g.Count()
            });

        return borrowedBooks
            .GroupBy(x => x.Book.Genre)
            .OrderBy(g => g.Key)
            .Select(genreGroup =>
            {
                var topBooks = genreGroup
                    .OrderByDescending(x => x.BorrowCount)
                    .ThenBy(x => x.Book.Title)
                    .Take(topPerGenre)
                    .Select(x => new TopBorrowedBookResult(
                        x.Book.Id,
                        x.Book.Title,
                        x.BorrowCount))
                    .ToList();

                return new GenreTopBooksResult(genreGroup.Key, topBooks);
            })
            .ToList();
    }

    public double? GetAverageLoanDurationDays(LibraryData data)
    {
        var completed = data.Loans
            .Where(l => l.ReturnDate is not null && l.ReturnDate.Value >= l.LoanDate)
            .Select(l => l.ReturnDate!.Value.DayNumber - l.LoanDate.DayNumber)
            .ToList();

        if (completed.Count == 0)
        {
            return null;
        }

        return completed.Average();
    }

    public IReadOnlyList<BorrowerActivityResult> GetMostActiveBorrowers(LibraryData data, int top = 5)
    {
        return data.Loans
            .GroupBy(l => new { l.Borrower.EmployeeId, l.Borrower.Name })
            .Select(g => new BorrowerActivityResult(
                g.Key.EmployeeId,
                g.Key.Name,
                g.Count()))
            .OrderByDescending(x => x.LoanCount)
            .ThenBy(x => x.Name)
            .Take(top)
            .ToList();
    }
}


