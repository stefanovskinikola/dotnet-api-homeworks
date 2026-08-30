using System.ComponentModel.DataAnnotations;

namespace Homework02.Models;

public class Book : BaseEntity
{
    [Required(ErrorMessage = "Author is required.")]
    public string Author { get; set; } = string.Empty;

    [Required(ErrorMessage = "Title is required.")]
    public string Title { get; set; } = string.Empty;
}
