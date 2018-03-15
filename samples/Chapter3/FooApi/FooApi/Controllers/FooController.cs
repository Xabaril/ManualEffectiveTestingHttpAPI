using FooApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FooApi.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    public class FooController : Controller
    {
        [HttpGet()]
        [Authorize("GetPolicy")]
        public IActionResult Get(int id)
        {
            var bar = new Bar() { Id = id };

            return Ok(bar);
        }

        [HttpPost()]
        [Authorize("PostPolicy")]
        public IActionResult Post([FromBody]Bar bar)
        {
            return CreatedAtAction(nameof(Get), new { id = bar.Id });
        }
    }

}
