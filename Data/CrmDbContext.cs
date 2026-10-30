using CrmApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Data;

public class CrmDbContext : DbContext
{
    public CrmDbContext(DbContextOptions<CrmDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Contato> Contatos => Set<Contato>();
    public DbSet<Interacao> Interacoes => Set<Interacao>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Oportunidade> Oportunidades => Set<Oportunidade>();
    public DbSet<Tarefa> Tarefas => Set<Tarefa>();
    public DbSet<Anotacao> Anotacoes => Set<Anotacao>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();

        mb.Entity<Empresa>().Property(e => e.Nome).IsRequired().HasMaxLength(160);
        mb.Entity<Usuario>().HasOne(u => u.Empresa).WithMany(e => e.Usuarios)
            .HasForeignKey(u => u.EmpresaId).OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Oportunidade>().Property(o => o.Valor).HasPrecision(18, 2);

        // Evita exclusão em cascata por múltiplos caminhos (limitação do SQL Server).
        mb.Entity<Cliente>().HasOne(c => c.Responsavel).WithMany()
            .HasForeignKey(c => c.ResponsavelId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Oportunidade>().HasOne(o => o.Responsavel).WithMany()
            .HasForeignKey(o => o.ResponsavelId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Oportunidade>().HasOne(o => o.Cliente).WithMany(c => c.Oportunidades)
            .HasForeignKey(o => o.ClienteId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<Lead>().HasOne(l => l.Responsavel).WithMany()
            .HasForeignKey(l => l.ResponsavelId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Tarefa>().HasOne(t => t.Responsavel).WithMany()
            .HasForeignKey(t => t.ResponsavelId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Anotacao>().HasOne(a => a.Usuario).WithMany()
            .HasForeignKey(a => a.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Interacao>().HasOne(i => i.Usuario).WithMany()
            .HasForeignKey(i => i.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Interacao>().HasOne(i => i.Cliente).WithMany(c => c.Interacoes)
            .HasForeignKey(i => i.ClienteId).OnDelete(DeleteBehavior.Cascade);
    }
}
