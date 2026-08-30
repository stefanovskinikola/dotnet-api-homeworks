using Homework01.Data;
using Homework01.Models;
using Microsoft.AspNetCore.Mvc;

namespace Homework01.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    // GET api/users
    [HttpGet]
    public ActionResult<List<User>> GetAll()
    {
        return Ok(StaticDb.Users);
    }

    // GET api/users/{id}
    [HttpGet("{id:int}")]
    public ActionResult<User> GetById(int id)
    {
        if (id < 1)
            return BadRequest(new { message = "ID must be 1 or greater." });

        var user = StaticDb.Users.FirstOrDefault(u => u.Id == id);
        if (user is null)
            return NotFound(new { message = $"No user found with id {id}." });

        return Ok(user);
    }
}