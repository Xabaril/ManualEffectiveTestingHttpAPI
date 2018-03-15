using FooApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FooApi.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    public class FooController : Controller
    {
        private readonly FooDbContext context;

        public FooController(FooDbContext context)
        {
            this.context = context;
        }

        [HttpGet()]
        [Authorize("GetPolicy")]
        public async Task<IActionResult> Get(int id)
        {
            var bar = await context.Bars.FindAsync(id);

            return Ok(bar);
        }

        [HttpPost()]
        [Authorize("PostPolicy")]
        public async Task<IActionResult> Post([FromBody]Bar bar)
        {
            await context.AddAsync(bar);
            return CreatedAtAction(nameof(Get), new { id = bar.Id });
        }
    }
}
