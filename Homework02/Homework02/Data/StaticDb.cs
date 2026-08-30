using Homework02.Models;

namespace Homework02.Data;

public static class StaticDb
{
    public static List<Book> Books { get; } = new()
    {
        new Book { Id = 1, Author = "Robert Martin",  Title = "Clean Code" },
        new Book { Id = 2, Author = "Robert Martin",  Title = "The Clean Coder" },
        new Book { Id = 3, Author = "Martin Fowler",  Title = "Refactoring" },
        new Book { Id = 4, Author = "Andrew Hunt",    Title = "The Pragmatic Programmer" },
        new Book { Id = 5, Author = "Donald Knuth",   Title = "The Art of Computer Programming" },
    };
}
