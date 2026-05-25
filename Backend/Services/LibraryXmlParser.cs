using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Backend.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.Services;

public sealed class LibraryXmlParser
{
    private const string DateFormat = "yyyy-MM-dd";
    private readonly ILogger<LibraryXmlParser> _logger;

    public LibraryXmlParser()
        : this(NullLogger<LibraryXmlParser>.Instance)
    {
    }

    public LibraryXmlParser(ILogger<LibraryXmlParser> logger)
    {
        _logger = logger;
    }

    public LibraryParseResult ParseFile(string xmlPath)
    {
        _logger.LogInformation("Reading XML file {XmlPath}.", xmlPath);

        if (!File.Exists(xmlPath))
        {
            return CreateFailedResult("file_not_found", $"XML file was not found: {xmlPath}");
        }

        try
        {
            var xml = File.ReadAllText(xmlPath);
            return Parse(xml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not read XML file {XmlPath}.", xmlPath);
            return CreateFailedResult("file_read_error", $"Could not read XML file: {ex.Message}");
        }
    }

    public LibraryParseResult Parse(string xmlContent)
    {
        _logger.LogInformation("Parsing XML content.");

        var issues = new List<ValidationIssue>();
        var books = new List<Book>();
        var loans = new List<Loan>();

        XDocument document;
        try
        {
            // Load with line info to provide better error messages later on
            document = XDocument.Parse(xmlContent, LoadOptions.SetLineInfo);
        }
        catch (XmlException ex)
        {
            return CreateFailedResult("invalid_xml", $"Invalid XML format: {ex.Message}");
        }

        // load the root element
        var root = document.Element("library");
        if (root is null)
        {
            return CreateFailedResult("missing_root", "Root element <library> is missing.");
        }

        // load the books element
        var booksElement = root.Element("books");
        if (booksElement is null)
        {
            issues.Add(new ValidationIssue("missing_books", "Element <books> is missing.", "library/books"));
        }
        else
        {

            var seenBookIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // load each book element and parse it, continue loading the rest of the file even if some books have issues to find as many issues as possible in one run
            foreach (var bookElement in booksElement.Elements("book"))
            {
                // parse the book, if parsing fails skip it but continue with the rest of the file to find more issues
                var parsedBook = ParseBook(bookElement, issues);
                if (parsedBook is null)
                {
                    continue;
                }

                // check for duplicate book ids and report an issue if found
                if (!seenBookIds.Add(parsedBook.Id))
                {
                    issues.Add(new ValidationIssue(
                        "duplicate_book_id",
                        $"Book id '{parsedBook.Id}' is duplicated.",
                        ElementLocation(bookElement, $"book[{parsedBook.Id}]")));
                    continue;
                }

                books.Add(parsedBook);
            }
        }

        var knownBookIds = books.Select(b => b.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // load the loan elements
        var loansElement = root.Element("loans");
        if (loansElement is null)
        {
            issues.Add(new ValidationIssue("missing_loans", "Element <loans> is missing.", "library/loans"));
        }
        else
        {
            foreach (var loanElement in loansElement.Elements("loan"))
            {
                var parsedLoan = ParseLoan(loanElement, issues);
                if (parsedLoan is null)
                {
                    continue;
                }

                if (!knownBookIds.Contains(parsedLoan.BookRef))
                {
                    issues.Add(new ValidationIssue(
                        "unknown_book_reference",
                        $"Loan '{parsedLoan.Id}' references unknown book id '{parsedLoan.BookRef}'.",
                        ElementLocation(loanElement, $"loan[{parsedLoan.Id}]/bookRef")));
                }

                loans.Add(parsedLoan);
            }
        }

        _logger.LogInformation(
            "Finished parsing XML. Books: {BookCount}, Loans: {LoanCount}, Issues: {IssueCount}.",
            books.Count,
            loans.Count,
            issues.Count);

        return new LibraryParseResult(new LibraryData(books, loans), issues);
    }

    private Book? ParseBook(XElement bookElement, List<ValidationIssue> issues)
    {
        var id = RequiredAttribute(bookElement, "id", issues);
        var title = RequiredElementValue(bookElement, "title", issues);
        var author = RequiredElementValue(bookElement, "author", issues);
        var genre = RequiredElementValue(bookElement, "genre", issues);
        var publishedYearText = RequiredElementValue(bookElement, "publishedYear", issues);

        if (id is null || title is null || author is null || genre is null || publishedYearText is null)
        {
            return null;
        }

        if (!int.TryParse(publishedYearText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var publishedYear))
        {
            AddIssue(
                issues,
                "invalid_published_year",
                $"Book '{id}' has invalid publishedYear '{publishedYearText}'.",
                ElementLocation(bookElement, $"book[{id}]/publishedYear"));
            return null;
        }

        return new Book(id, title, author, genre, publishedYear);
    }

    private Loan? ParseLoan(XElement loanElement, List<ValidationIssue> issues)
    {
        var id = RequiredAttribute(loanElement, "id", issues);
        var bookRef = RequiredElementValue(loanElement, "bookRef", issues);
        var borrowerElement = loanElement.Element("borrower");
        var loanDateText = RequiredElementValue(loanElement, "loanDate", issues);
        var dueDateText = RequiredElementValue(loanElement, "dueDate", issues);

        var returnDateText = loanElement.Element("returnDate")?.Value?.Trim();

        string? borrowerEmployeeId = null;
        string? borrowerName = null;
        if (borrowerElement is null)
        {
            AddIssue(
                issues,
                "missing_borrower",
                "Loan borrower element is missing.",
                ElementLocation(loanElement, "loan/borrower"));
        }
        else
        {
            borrowerEmployeeId = RequiredElementValue(borrowerElement, "employeeId", issues);
            borrowerName = RequiredElementValue(borrowerElement, "name", issues);
        }

        if (id is null || bookRef is null || borrowerEmployeeId is null || borrowerName is null || loanDateText is null || dueDateText is null)
        {
            return null;
        }

        if (!TryParseDate(loanDateText, out var loanDate))
        {
            AddIssue(
                issues,
                "invalid_date",
                $"Loan '{id}' has invalid loanDate '{loanDateText}'. Expected format: {DateFormat}.",
                ElementLocation(loanElement, $"loan[{id}]/loanDate"));
            return null;
        }

        if (!TryParseDate(dueDateText, out var dueDate))
        {
            AddIssue(
                issues,
                "invalid_date",
                $"Loan '{id}' has invalid dueDate '{dueDateText}'. Expected format: {DateFormat}.",
                ElementLocation(loanElement, $"loan[{id}]/dueDate"));
            return null;
        }

        DateOnly? returnDate = null;
        if (!string.IsNullOrWhiteSpace(returnDateText))
        {
            if (!TryParseDate(returnDateText, out var parsedReturnDate))
            {
                AddIssue(
                    issues,
                    "invalid_date",
                    $"Loan '{id}' has invalid returnDate '{returnDateText}'. Expected format: {DateFormat}.",
                    ElementLocation(loanElement, $"loan[{id}]/returnDate"));
                return null;
            }

            returnDate = parsedReturnDate;
        }

        if (dueDate < loanDate)
        {
            AddIssue(
                issues,
                "invalid_date_range",
                $"Loan '{id}' has dueDate before loanDate.",
                ElementLocation(loanElement, $"loan[{id}]"));
        }

        if (returnDate is not null && returnDate.Value < loanDate)
        {
            AddIssue(
                issues,
                "invalid_date_range",
                $"Loan '{id}' has returnDate before loanDate.",
                ElementLocation(loanElement, $"loan[{id}]"));
        }

        return new Loan(
            id,
            bookRef,
            new Borrower(borrowerEmployeeId, borrowerName),
            loanDate,
            dueDate,
            returnDate);
    }

    private static bool TryParseDate(string raw, out DateOnly date)
    {
        return DateOnly.TryParseExact(
            raw,
            DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }

    private string? RequiredAttribute(XElement element, string attributeName, List<ValidationIssue> issues)
    {
        if (element.Attribute(attributeName) is null)
        {
            AddIssue(
                issues,
                "missing_attribute",
                $"Required attribute '{attributeName}' is missing.",
                ElementLocation(element, $"{element.Name.LocalName}/@{attributeName}"));
            return null;
        }

        var value = element.Attribute(attributeName)?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        AddIssue(
            issues,
            "missing_value",
            $"Required attribute '{attributeName}' is missing or empty.",
            ElementLocation(element, $"{element.Name.LocalName}/@{attributeName}"));
        return null;
    }

    private string? RequiredElementValue(XElement element, string elementName, List<ValidationIssue> issues)
    {
        var child = element.Element(elementName);
        var value = child?.Value?.Trim();

        if (child is null)
        {
            AddIssue(
                issues,
                "missing_element",
                $"Required element '{elementName}' is missing.",
                ElementLocation(element, $"{element.Name.LocalName}/{elementName}"));
            return null;
        }

        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        AddIssue(
            issues,
            "missing_element_value",
            $"Required element '{elementName}' is missing or empty.",
            ElementLocation(element, $"{element.Name.LocalName}/{elementName}"));
        return null;
    }

    private LibraryParseResult CreateFailedResult(string code, string message)
    {
        return new LibraryParseResult(new LibraryData([], []), [CreateIssue(code, message)]);
    }

    private void AddIssue(List<ValidationIssue> issues, string code, string message, string? location = null)
    {
        issues.Add(CreateIssue(code, message, location));
    }

    private ValidationIssue CreateIssue(string code, string message, string? location = null)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            _logger.LogWarning("Parse issue {Code}: {Message}", code, message);
        }
        else
        {
            _logger.LogWarning("Parse issue {Code}: {Message} ({Location})", code, message, location);
        }

        return new ValidationIssue(code, message, location);
    }

    private static string ElementLocation(XElement element, string fallback)
    {
        if (element is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
        {
            return $"line {lineInfo.LineNumber}, position {lineInfo.LinePosition}";
        }

        return fallback;
    }
}
