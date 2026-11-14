using Microsoft.EntityFrameworkCore;
using SaaS.Cilios.Models;

namespace SaaS.Cilios.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<ConfiguracaoConta> ConfiguracoesConta => Set<ConfiguracaoConta>();
    public DbSet<ConfiguracaoAgenda> ConfiguracoesAgenda => Set<ConfiguracaoAgenda>();
    public DbSet<HorarioFuncionamento> HorariosFuncionamento => Set<HorarioFuncionamento>();
    public DbSet<BloqueioAgenda> Bloqueios => Set<BloqueioAgenda>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Agendamento>()
            .HasOne(a => a.Servico)
            .WithMany()
            .HasForeignKey(a => a.ServicoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Servico>().Property(s => s.Preco).HasPrecision(10, 2);
        modelBuilder.Entity<Agendamento>().Property(a => a.Valor).HasPrecision(10, 2);    

        // O código do link de cancelamento não pode se repetir (vários agendamentos sem código são permitidos)
        modelBuilder.Entity<Agendamento>()
            .HasIndex(a => a.CodigoPublico)
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}
