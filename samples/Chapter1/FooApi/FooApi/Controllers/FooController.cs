using FooApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace FooApi.Controllers
{
    [Route("api/[controller]")]
    public class FooController : Controller
    {
        [HttpGet("")]
        public IActionResult Get(int id)
        {
            var bar = new Bar() { Id = id };

            return Ok(bar);
        }
    }

}
