using Homework01.Models;

namespace Homework01.Data;

public static class StaticDb
{
    public static readonly List<User> Users = new()
    {
        new User { Id = 1, Name = "Alice Johnson" },
        new User { Id = 2, Name = "Bob Smith" },
        new User { Id = 3, Name = "Charlie Brown" },
        new User { Id = 4, Name = "Diana Prince" },
        new User { Id = 5, Name = "Edward Norton" }
    };
}
