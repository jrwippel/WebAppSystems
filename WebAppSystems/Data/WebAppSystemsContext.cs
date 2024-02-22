using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAppSystems.Models;

namespace WebAppSystems.Data
{
    public class WebAppSystemsContext : DbContext
    {
        public WebAppSystemsContext (DbContextOptions<WebAppSystemsContext> options)
            : base(options)
        {
        }

        public DbSet<WebAppSystems.Models.Department> Department { get; set; } = default!;
        public DbSet<WebAppSystems.Models.Attorney> Attorney { get; set; } = default!;
        public DbSet<WebAppSystems.Models.ProcessRecord> ProcessRecord { get; set; } = default!;
        public DbSet<WebAppSystems.Models.Client>? Client { get; set; }

        public DbSet<WebAppSystems.Models.Mensalista> Mensalista { get; set; } = default!;
     

        public DbSet<WebAppSystems.Models.PercentualArea> PercentualAreas { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);          



            // Outras configurações de modelo podem ir aqui, se necessário
        }               

        public DbSet<WebAppSystems.Models.PercentualArea>? PercentualArea { get; set; }

        public DbSet<WebAppSystems.Models.ValorCliente>? ValorCliente { get; set; }
    }
}


