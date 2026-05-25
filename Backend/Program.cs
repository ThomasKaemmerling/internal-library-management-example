using Backend.Logging;
using Backend.Services;

var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = "AllowedOrigins";

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddProvider(new DailyRollingFileLoggerProvider(
    Path.Combine(builder.Environment.ContentRootPath, "Logs"),
    "Backend.Services.LibraryXmlParser"));

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<LibraryXmlParser>();
builder.Services.AddSingleton<LibraryAnalyticsService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(allowedOrigins, policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseCors(allowedOrigins);
app.UseHttpsRedirection();

app.MapGet("/api/status", () =>
{
    return Results.Ok(new
    {
        message = "Backend is reachable from Angular",
        timestampUtc = DateTime.UtcNow
    });
})
.WithName("GetApiStatus");

app.MapGet("/api/library/analysis", (LibraryXmlParser parser, LibraryAnalyticsService analytics, IWebHostEnvironment environment) =>
{
    var xmlPath = Path.Combine(environment.ContentRootPath, "library.xml");
    var parseResult = parser.ParseFile(xmlPath);
    var today = DateOnly.FromDateTime(DateTime.UtcNow);

    return Results.Ok(new
    {
        parseIssues = parseResult.Issues,
        overdueLoans = analytics.GetOverdueLoans(parseResult.Data, today),
        topBorrowedBooksPerGenre = analytics.GetTopBorrowedBooksPerGenre(parseResult.Data),
        averageLoanDurationDays = analytics.GetAverageLoanDurationDays(parseResult.Data),
        mostActiveBorrowers = analytics.GetMostActiveBorrowers(parseResult.Data)
    });
})
.WithName("GetLibraryAnalysis");

app.MapGet("/api/library/issues", (LibraryXmlParser parser, IWebHostEnvironment environment) =>
{
    var xmlPath = Path.Combine(environment.ContentRootPath, "library.xml");
    var parseResult = parser.ParseFile(xmlPath);
    return Results.Ok(parseResult.Issues);
})
.WithName("GetLibraryIssues");

app.MapGet("/api/library/overdue_loans", (LibraryXmlParser parser, LibraryAnalyticsService analytics, IWebHostEnvironment environment) =>
{
    var xmlPath = Path.Combine(environment.ContentRootPath, "library.xml");
    var parseResult = parser.ParseFile(xmlPath);
    var today = DateOnly.FromDateTime(DateTime.UtcNow);

    return Results.Ok(new
    {
        validationIssues = parseResult.Issues,
        data = analytics.GetOverdueLoans(parseResult.Data, today)
    });
})
.WithName("GetOverdueLoans")
.Produces<object>(StatusCodes.Status200OK);

app.MapGet("/api/library/most_loaned_books", (LibraryXmlParser parser, LibraryAnalyticsService analytics, IWebHostEnvironment environment) =>
{
    var xmlPath = Path.Combine(environment.ContentRootPath, "library.xml");
    var parseResult = parser.ParseFile(xmlPath);

    return Results.Ok(new
    {
        validationIssues = parseResult.Issues,
        data = analytics.GetTopBorrowedBooksPerGenre(parseResult.Data, topPerGenre: 3)
    });
})
.WithName("GetMostLoanedBooks")
.Produces<object>(StatusCodes.Status200OK);

app.MapGet("/api/library/average_loan_period", (LibraryXmlParser parser, LibraryAnalyticsService analytics, IWebHostEnvironment environment) =>
{
    var xmlPath = Path.Combine(environment.ContentRootPath, "library.xml");
    var parseResult = parser.ParseFile(xmlPath);

    var averageDays = analytics.GetAverageLoanDurationDays(parseResult.Data);

    return Results.Ok(new
    {
        validationIssues = parseResult.Issues,
        averageLoanDurationDays = averageDays,
        completedLoansCount = parseResult.Data.Loans
            .Count(l => l.ReturnDate is not null && l.ReturnDate.Value >= l.LoanDate)
    });
})
.WithName("GetAverageLoanPeriod")
.Produces<object>(StatusCodes.Status200OK);

app.Run();
