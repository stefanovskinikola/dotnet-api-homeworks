using Homework02.Data;
using Homework02.Models;
using Microsoft.AspNetCore.Mvc;

namespace Homework02.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    // GET /api/books
    // GET /api/books?index=2
    [HttpGet]
    public ActionResult GetAll([FromQuery] int? index)
    {
        if (index.HasValue)
        {
            if (index.Value < 0 || index.Value >= StaticDb.Books.Count)
                return NotFound($"No book found at index {index.Value}. Valid range: 0 – {StaticDb.Books.Count - 1}.");

            return Ok(StaticDb.Books[index.Value]);
        }

        return Ok(StaticDb.Books);
    }

    // GET /api/books/search?author=Robert Martin&title=Clean Code
    [HttpGet("search")]
    public ActionResult<List<Book>> Search(
        [FromQuery] string? author,
        [FromQuery] string? title)
    {
        List<Book> results = StaticDb.Books;

        if (!string.IsNullOrWhiteSpace(author))
            results = results.Where(b =>
                b.Author.Contains(author, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(title))
            results = results.Where(b =>
                b.Title.Contains(title, StringComparison.OrdinalIgnoreCase)).ToList();

        return Ok(results);
    }

    // POST /api/books
    [HttpPost]
    public ActionResult<Book> AddBook([FromBody] Book book)
    {
        book.Id = StaticDb.Books.Count > 0
            ? StaticDb.Books.Max(b => b.Id) + 1
            : 1;

        StaticDb.Books.Add(book);

        return CreatedAtAction(nameof(GetAll), null, book);
    }

    // POST /api/books/titles
    [HttpPost("titles")]
    public ActionResult<List<string>> GetTitles([FromBody] List<Book?>? books)
    {
        // Edge case 1: client sends literal null as the JSON body.
        if (books is null)
            return BadRequest("Request body must be a JSON array of books.");

        // Edge case 2: empty array — nothing to process.
        if (books.Count == 0)
            return BadRequest("Please provide at least one book.");

        // Edge case 3: array contains null elements e.g. [null, {...}].
        if (books.Any(b => b is null))
            return BadRequest("The array must not contain null elements.");

        // Reject if any book is missing an author or title.
        if (books.Any(b => string.IsNullOrWhiteSpace(b!.Author) || string.IsNullOrWhiteSpace(b.Title)))
            return BadRequest("Every book must have both 'author' and 'title'.");

        var titles = books
            .Select(b => b!.Title)
            .ToList();

        return Ok(titles);
    }
}