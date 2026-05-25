using Backend.Services;

namespace Backend.Tests;

public sealed class LibraryXmlParserTests
{
    private readonly LibraryXmlParser _parser = new();

  // Baseline test: valid XML should parse without any validation issues.
    [Fact]
    public void Parse_WithValidXml_ParsesBooksAndLoans()
    {
        var xml = """
                  <library>
                    <books>
                      <book id="B1">
                        <title>Book A</title>
                        <author>Author A</author>
                        <genre>Fiction</genre>
                        <publishedYear>2020</publishedYear>
                      </book>
                    </books>
                    <loans>
                      <loan id="L1">
                        <bookRef>B1</bookRef>
                        <borrower>
                          <employeeId>E1</employeeId>
                          <name>Jane Doe</name>
                        </borrower>
                        <loanDate>2026-01-01</loanDate>
                        <dueDate>2026-01-20</dueDate>
                        <returnDate>2026-01-18</returnDate>
                      </loan>
                    </loans>
                  </library>
                  """;

        var result = _parser.Parse(xml);

        Assert.Single(result.Data.Books);
        Assert.Single(result.Data.Loans);
        Assert.Empty(result.Issues);
    }

    // Structural validation tests.

    [Fact]
    public void Parse_WithInvalidXml_ReturnsInvalidXmlIssue()
    {
        var xml = "<library><books><book id=\"B1\"></books></library>";

        var result = _parser.Parse(xml);

        Assert.Empty(result.Data.Books);
        Assert.Empty(result.Data.Loans);
        Assert.Contains(result.Issues, i => i.Code == "invalid_xml");
    }

    [Fact]
    public void Parse_WithMissingLibraryRoot_ReturnsMissingRootIssue()
    {
        var xml = """
                  <catalog>
                    <books></books>
                    <loans></loans>
                  </catalog>
                  """;

        var result = _parser.Parse(xml);

        Assert.Empty(result.Data.Books);
        Assert.Empty(result.Data.Loans);
        Assert.Contains(result.Issues, i => i.Code == "missing_root");
    }

    [Fact]
    public void Parse_WithMissingBooksElement_ReportsMissingBooksIssue()
    {
        var xml = """
                  <library>
                    <loans></loans>
                  </library>
                  """;

        var result = _parser.Parse(xml);

        Assert.Contains(result.Issues, i => i.Code == "missing_books");
    }

    [Fact]
    public void Parse_WithMissingLoansElement_ReportsMissingLoansIssue()
    {
        var xml = """
                  <library>
                    <books></books>
                  </library>
                  """;

        var result = _parser.Parse(xml);

        Assert.Contains(result.Issues, i => i.Code == "missing_loans");
    }

    // Book validation tests.

    [Fact]
    public void Parse_WithMissingBookId_ReportsMissingAttributeIssue()
    {
        var xml = """
                  <library>
                    <books>
                      <book>
                        <title>Book A</title>
                        <author>Author A</author>
                        <genre>Fiction</genre>
                        <publishedYear>2020</publishedYear>
                      </book>
                    </books>
                    <loans></loans>
                  </library>
                  """;

        var result = _parser.Parse(xml);

        Assert.Empty(result.Data.Books);
        Assert.Contains(result.Issues, i => i.Code == "missing_attribute" && i.Message.Contains("id", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_WithEmptyBookId_ReportsMissingValueIssue()
    {
        var xml = """
                  <library>
                    <books>
                      <book id="">
                        <title>Book A</title>
                        <author>Author A</author>
                        <genre>Fiction</genre>
                        <publishedYear>2020</publishedYear>
                      </book>
                    </books>
                    <loans></loans>
                  </library>
                  """;

        var result = _parser.Parse(xml);

        Assert.Empty(result.Data.Books);
        Assert.Contains(result.Issues, i => i.Code == "missing_value" && i.Message.Contains("id", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_WithEmptyTitle_ReportsMissingElementValueIssue()
    {
        var xml = """
                  <library>
                    <books>
                      <book id="B1">
                        <title></title>
                        <author>Author A</author>
                        <genre>Fiction</genre>
                        <publishedYear>2020</publishedYear>
                      </book>
                    </books>
                    <loans></loans>
                  </library>
                  """;

        var result = _parser.Parse(xml);

        Assert.Empty(result.Data.Books);
        Assert.Contains(result.Issues, i => i.Code == "missing_element_value" && i.Message.Contains("title", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_WithMissingAuthor_ReportsMissingElementIssue()
    {
        var xml = """
                  <library>
                    <books>
                      <book id="B1">
                        <title>Book A</title>
                        <genre>Fiction</genre>
                        <publishedYear>2020</publishedYear>
                      </book>
                    </books>
                    <loans></loans>
                  </library>
                  """;

        var result = _parser.Parse(xml);

        Assert.Empty(result.Data.Books);
        Assert.Contains(result.Issues, i => i.Code == "missing_element" && i.Message.Contains("author", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_WithMissingPublishedYearElement_ReportsMissingElementIssue()
    {
        var xml = """
                  <library>
                    <books>
                      <book id="B1">
                        <title>Book A</title>
                        <author>Author A</author>
                        <genre>Fiction</genre>
                      </book>
                    </books>
                    <loans></loans>
                  </library>
                  """;

        var result = _parser.Parse(xml);

        Assert.Empty(result.Data.Books);
        Assert.Contains(result.Issues, i => i.Code == "missing_element" && i.Message.Contains("publishedYear", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_WithEmptyPublishedYearElement_ReportsMissingElementValueIssue()
    {
        var xml = """
                  <library>
                    <books>
                      <book id="B1">
                        <title>Book A</title>
                        <author>Author A</author>
                        <genre>Fiction</genre>
                        <publishedYear></publishedYear>
                      </book>
                    </books>
                    <loans></loans>
                  </library>
                  """;

        var result = _parser.Parse(xml);

        Assert.Empty(result.Data.Books);
        Assert.Contains(result.Issues, i => i.Code == "missing_element_value" && i.Message.Contains("publishedYear", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_WithInvalidPublishedYear_ReportsInvalidPublishedYearIssue()
    {
        var xml = """
                  <library>
                    <books>
                      <book id="B1">
                        <title>Book A</title>
                        <author>Author A</author>
                        <genre>Fiction</genre>
                        <publishedYear>NAN</publishedYear>
                      </book>
                    </books>
                    <loans></loans>
                  </library>
                  """;

        var result = _parser.Parse(xml);

        Assert.Empty(result.Data.Books);
        Assert.Contains(result.Issues, i => i.Code == "invalid_published_year" && i.Message.Contains("publishedYear", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_WithDuplicateBookIds_ReportsDuplicateIssueAndKeepsFirstBook()
    {
        var xml = """
                  <library>
                    <books>
                      <book id="B1">
                        <title>Book A</title>
                        <author>Author A</author>
                        <genre>Fiction</genre>
                        <publishedYear>2020</publishedYear>
                      </book>
                      <book id="B1">
                        <title>Book B</title>
                        <author>Author B</author>
                        <genre>Fiction</genre>
                        <publishedYear>2021</publishedYear>
                      </book>
                    </books>
                    <loans></loans>
                  </library>
                  """;

        var result = _parser.Parse(xml);

        Assert.Single(result.Data.Books);
        Assert.Contains(result.Issues, i => i.Code == "duplicate_book_id");
    }

    // Loan validation tests.

    [Fact]
    public void Parse_WithUnknownBookReference_ReportsIssueWithoutCrashing()
    {
        var xml = """
                  <library>
                    <books>
                      <book id="B1">
                        <title>Book A</title>
                        <author>Author A</author>
                        <genre>Fiction</genre>
                        <publishedYear>2020</publishedYear>
                      </book>
                    </books>
                    <loans>
                      <loan id="L1">
                        <bookRef>B999</bookRef>
                        <borrower>
                          <employeeId>E1</employeeId>
                          <name>Jane Doe</name>
                        </borrower>
                        <loanDate>2026-01-01</loanDate>
                        <dueDate>2026-01-20</dueDate>
                        <returnDate></returnDate>
                      </loan>
                    </loans>
                  </library>
                  """;

        var result = _parser.Parse(xml);

        Assert.Single(result.Data.Loans);
        Assert.Contains(result.Issues, i => i.Code == "unknown_book_reference");
    }

    [Fact]
    public void Parse_WithMissingBorrowerElement_ReportsMissingBorrowerIssue()
    {
        var xml = """
                  <library>
                    <books>
                      <book id="B1">
                        <title>Book A</title>
                        <author>Author A</author>
                        <genre>Fiction</genre>
                        <publishedYear>2020</publishedYear>
                      </book>
                    </books>
                    <loans>
                      <loan id="L1">
                        <bookRef>B1</bookRef>
                        <loanDate>2026-01-01</loanDate>
                        <dueDate>2026-01-20</dueDate>
                        <returnDate></returnDate>
                      </loan>
                    </loans>
                  </library>
                  """;

        var result = _parser.Parse(xml);

        Assert.Empty(result.Data.Loans);
        Assert.Contains(result.Issues, i => i.Code == "missing_borrower");
    }

    [Fact]
    public void Parse_WithInvalidDateFormat_ReportsReadableIssue()
    {
        var xml = """
                  <library>
                    <books>
                      <book id="B1">
                        <title>Book A</title>
                        <author>Author A</author>
                        <genre>Fiction</genre>
                        <publishedYear>2020</publishedYear>
                      </book>
                    </books>
                    <loans>
                      <loan id="L1">
                        <bookRef>B1</bookRef>
                        <borrower>
                          <employeeId>E1</employeeId>
                          <name>Jane Doe</name>
                        </borrower>
                        <loanDate>01-01-2026</loanDate>
                        <dueDate>2026-01-20</dueDate>
                        <returnDate></returnDate>
                      </loan>
                    </loans>
                  </library>
                  """;

        var result = _parser.Parse(xml);

        Assert.Empty(result.Data.Loans);
        Assert.Contains(result.Issues, i => i.Code == "invalid_date" && i.Message.Contains("loanDate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_WithInvalidDueDateFormat_ReportsInvalidDateIssue()
    {
        var xml = """
                  <library>
                    <books>
                      <book id="B1">
                        <title>Book A</title>
                        <author>Author A</author>
                        <genre>Fiction</genre>
                        <publishedYear>2020</publishedYear>
                      </book>
                    </books>
                    <loans>
                      <loan id="L1">
                        <bookRef>B1</bookRef>
                        <borrower>
                          <employeeId>E1</employeeId>
                          <name>Jane Doe</name>
                        </borrower>
                        <loanDate>2026-01-01</loanDate>
                        <dueDate>20-01-2026</dueDate>
                        <returnDate></returnDate>
                      </loan>
                    </loans>
                  </library>
                  """;

        var result = _parser.Parse(xml);

        Assert.Empty(result.Data.Loans);
        Assert.Contains(result.Issues, i => i.Code == "invalid_date" && i.Message.Contains("dueDate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_WithInvalidReturnDateFormat_ReportsInvalidDateIssue()
    {
        var xml = """
                  <library>
                    <books>
                      <book id="B1">
                        <title>Book A</title>
                        <author>Author A</author>
                        <genre>Fiction</genre>
                        <publishedYear>2020</publishedYear>
                      </book>
                    </books>
                    <loans>
                      <loan id="L1">
                        <bookRef>B1</bookRef>
                        <borrower>
                          <employeeId>E1</employeeId>
                          <name>Jane Doe</name>
                        </borrower>
                        <loanDate>2026-01-01</loanDate>
                        <dueDate>2026-01-20</dueDate>
                        <returnDate>20-01-2026</returnDate>
                      </loan>
                    </loans>
                  </library>
                  """;

        var result = _parser.Parse(xml);

        Assert.Empty(result.Data.Loans);
        Assert.Contains(result.Issues, i => i.Code == "invalid_date" && i.Message.Contains("returnDate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_WithDueDateBeforeLoanDate_ReportsInvalidDateRangeIssue()
    {
        var xml = """
                  <library>
                    <books>
                      <book id="B1">
                        <title>Book A</title>
                        <author>Author A</author>
                        <genre>Fiction</genre>
                        <publishedYear>2020</publishedYear>
                      </book>
                    </books>
                    <loans>
                      <loan id="L1">
                        <bookRef>B1</bookRef>
                        <borrower>
                          <employeeId>E1</employeeId>
                          <name>Jane Doe</name>
                        </borrower>
                        <loanDate>2026-01-20</loanDate>
                        <dueDate>2026-01-01</dueDate>
                        <returnDate></returnDate>
                      </loan>
                    </loans>
                  </library>
                  """;

        var result = _parser.Parse(xml);

        Assert.Single(result.Data.Loans);
        Assert.Contains(result.Issues, i => i.Code == "invalid_date_range" && i.Message.Contains("dueDate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_WithReturnDateBeforeLoanDate_ReportsInvalidDateRangeIssue()
    {
        var xml = """
                  <library>
                    <books>
                      <book id="B1">
                        <title>Book A</title>
                        <author>Author A</author>
                        <genre>Fiction</genre>
                        <publishedYear>2020</publishedYear>
                      </book>
                    </books>
                    <loans>
                      <loan id="L1">
                        <bookRef>B1</bookRef>
                        <borrower>
                          <employeeId>E1</employeeId>
                          <name>Jane Doe</name>
                        </borrower>
                        <loanDate>2026-01-20</loanDate>
                        <dueDate>2026-01-25</dueDate>
                        <returnDate>2026-01-01</returnDate>
                      </loan>
                    </loans>
                  </library>
                  """;

        var result = _parser.Parse(xml);

        Assert.Single(result.Data.Loans);
        Assert.Contains(result.Issues, i => i.Code == "invalid_date_range" && i.Message.Contains("returnDate", StringComparison.OrdinalIgnoreCase));
    }

    // Robustness test: parser should keep valid entities while collecting issues from invalid ones.
    [Fact]
    public void Parse_WithMixedValidAndInvalidEntries_CollectsIssuesAndKeepsValidData()
    {
        var xml = """
                  <library>
                    <books>
                      <book id="B1">
                        <title>Clean Code</title>
                        <author>Robert C. Martin</author>
                        <genre>IT</genre>
                        <publishedYear>2008</publishedYear>
                      </book>
                      <book id="B2">
                        <title></title>
                        <author>Author B</author>
                        <genre>IT</genre>
                        <publishedYear>2020</publishedYear>
                      </book>
                    </books>
                    <loans>
                      <loan id="L1">
                        <bookRef>B1</bookRef>
                        <borrower>
                          <employeeId>E1</employeeId>
                          <name>Jane Doe</name>
                        </borrower>
                        <loanDate>2026-01-01</loanDate>
                        <dueDate>2026-01-20</dueDate>
                        <returnDate></returnDate>
                      </loan>
                      <loan id="L2">
                        <bookRef>B999</bookRef>
                        <borrower>
                          <employeeId>E2</employeeId>
                          <name>John Doe</name>
                        </borrower>
                        <loanDate>2026-02-01</loanDate>
                        <dueDate>2026-02-20</dueDate>
                        <returnDate></returnDate>
                      </loan>
                    </loans>
                  </library>
                  """;

        var result = _parser.Parse(xml);

        Assert.Single(result.Data.Books);
        Assert.Equal(2, result.Data.Loans.Count);
        Assert.Contains(result.Issues, i => i.Code == "missing_element_value" && i.Message.Contains("title", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Issues, i => i.Code == "unknown_book_reference");
    }
}
