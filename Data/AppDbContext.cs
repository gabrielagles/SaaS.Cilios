using Microsoft.EntityFrameworkCore;
using SaaS.Cilios.Models;

namespace SaaS.Cilios.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<ConfiguracaoConta> ConfiguracoesConta => Set<ConfiguracaoConta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Agendamento>()
            .HasOne(a => a.Servico)
            .WithMany()
            .HasForeignKey(a => a.ServicoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Servico>().Property(s => s.Preco).HasPrecision(10, 2);
        modelBuilder.Entity<Agendamento>().Property(a => a.Valor).HasPrecision(10, 2);    

        base.OnModelCreating(modelBuilder);
    }
}
