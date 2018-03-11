using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FooApi.Models
{
    public class FooDbContext : DbContext
    {
        public DbSet<Bar> Bars { get; set; }

        public FooDbContext(DbContextOptions<FooDbContext> options) : base(options)
        {

        }
    }
}
