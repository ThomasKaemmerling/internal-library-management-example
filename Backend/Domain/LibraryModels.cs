namespace Backend.Domain;

public sealed record Book(
    string Id,
    string Title,
    string Author,
    string Genre,
    int PublishedYear);

public sealed record Borrower(
    string EmployeeId,
    string Name);

public sealed record Loan(
    string Id,
    string BookRef,
    Borrower Borrower,
    DateOnly LoanDate,
    DateOnly DueDate,
    DateOnly? ReturnDate);

public sealed record LibraryData(
    IReadOnlyList<Book> Books,
    IReadOnlyList<Loan> Loans);

public sealed record ValidationIssue(
    string Code,
    string Message,
    string? Location = null);

public sealed record LibraryParseResult(
    LibraryData Data,
    IReadOnlyList<ValidationIssue> Issues);

public sealed record OverdueLoanResult(
    string LoanId,
    string BookId,
    string BookTitle,
    string EmployeeId,
    string BorrowerName,
    DateOnly DueDate,
    int DaysOverdue);

public sealed record TopBorrowedBookResult(
    string BookId,
    string Title,
    int BorrowCount);

public sealed record GenreTopBooksResult(
    string Genre,
    IReadOnlyList<TopBorrowedBookResult> Books);

public sealed record BorrowerActivityResult(
    string EmployeeId,
    string Name,
    int LoanCount);