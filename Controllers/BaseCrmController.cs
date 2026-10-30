using System.Security.Claims;
using CrmApi.Data;
using CrmApi.Models;
using CrmApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmApi.Controllers;

[ApiController]
[Authorize]
public abstract class BaseCrmController : ControllerBase
{
    protected readonly CrmDbContext Db;
    protected BaseCrmController(CrmDbContext db) => Db = db;

    // Id do usuário logado, lido do token JWT.
    protected int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected int EmpresaId => int.Parse(User.FindFirstValue(TokenService.EmpresaIdClaim)!);

    // Admin e Gerente enxergam todos os registros; Vendedor só os seus.
    protected bool VeTudo => User.IsInRole("Admin") || User.IsInRole("Gerente");

    protected IQueryable<Cliente> ClientesDaEmpresa => Db.Clientes
        .Where(c => c.Responsavel != null && c.Responsavel.EmpresaId == EmpresaId);
    protected IQueryable<Lead> LeadsDaEmpresa => Db.Leads
        .Where(l => l.Responsavel != null && l.Responsavel.EmpresaId == EmpresaId);
    protected IQueryable<Oportunidade> OportunidadesDaEmpresa => Db.Oportunidades
        .Where(o => o.Responsavel != null && o.Responsavel.EmpresaId == EmpresaId
            && o.Cliente != null && o.Cliente.Responsavel != null
            && o.Cliente.Responsavel.EmpresaId == EmpresaId);
    protected IQueryable<Tarefa> TarefasDaEmpresa => Db.Tarefas
        .Where(t => t.Responsavel != null && t.Responsavel.EmpresaId == EmpresaId
            && (!t.ClienteId.HasValue || Db.Clientes.Any(c => c.Id == t.ClienteId
                && c.Responsavel != null && c.Responsavel.EmpresaId == EmpresaId))
            && (!t.OportunidadeId.HasValue || Db.Oportunidades.Any(o => o.Id == t.OportunidadeId
                && o.Responsavel != null && o.Responsavel.EmpresaId == EmpresaId)));

    protected static (int pagina, int tamanho) NormalizaPagina(int pagina, int tamanho)
        => (Math.Max(1, pagina), Math.Clamp(tamanho, 1, 100));
}
