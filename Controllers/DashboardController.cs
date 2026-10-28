using CrmApi.Data;
using CrmApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Controllers;

[Route("api/dashboard")]
public class DashboardController : BaseCrmController
{
    public DashboardController(CrmDbContext db) : base(db) { }

    public record PorStatusLead(StatusLead Status, int Quantidade);
    public record PorEstagio(EstagioFunil Estagio, int Quantidade, decimal Valor);
    public record ResumoDashboard(
        int TotalClientes, int TotalLeads, int LeadsNovos,
        int OportunidadesAbertas, decimal ValorEmAberto,
        decimal ValorGanhoNoMes, decimal TaxaConversao,
        int TarefasPendentes, int TarefasAtrasadas,
        List<PorStatusLead> LeadsPorStatus, List<PorEstagio> FunilResumo);

    // Os indicadores respeitam a permissão: vendedor vê só os seus números.
    [HttpGet]
    public async Task<ResumoDashboard> Obter()
    {
        var clientes = VeTudo ? Db.Clientes : Db.Clientes.Where(c => c.ResponsavelId == UsuarioId);
        var leads = VeTudo ? Db.Leads : Db.Leads.Where(l => l.ResponsavelId == UsuarioId);
        var ops = VeTudo ? Db.Oportunidades : Db.Oportunidades.Where(o => o.ResponsavelId == UsuarioId);
        var tarefas = VeTudo ? Db.Tarefas : Db.Tarefas.Where(t => t.ResponsavelId == UsuarioId);

        var agora = DateTime.UtcNow;
        var inicioMes = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var abertas = ops.Where(o => o.Estagio != EstagioFunil.Ganha && o.Estagio != EstagioFunil.Perdida);
        var ganhas = await ops.CountAsync(o => o.Estagio == EstagioFunil.Ganha);
        var perdidas = await ops.CountAsync(o => o.Estagio == EstagioFunil.Perdida);

        var leadsPorStatus = await leads.GroupBy(l => l.Status)
            .Select(g => new PorStatusLead(g.Key, g.Count())).ToListAsync();

        var funil = await ops.GroupBy(o => o.Estagio)
            .Select(g => new PorEstagio(g.Key, g.Count(), g.Sum(x => x.Valor))).ToListAsync();

        return new ResumoDashboard(
            TotalClientes: await clientes.CountAsync(),
            TotalLeads: await leads.CountAsync(),
            LeadsNovos: await leads.CountAsync(l => l.Status == StatusLead.Novo),
            OportunidadesAbertas: await abertas.CountAsync(),
            ValorEmAberto: await abertas.SumAsync(o => (decimal?)o.Valor) ?? 0,
            ValorGanhoNoMes: await ops.Where(o => o.Estagio == EstagioFunil.Ganha && o.FechadaEm >= inicioMes)
                .SumAsync(o => (decimal?)o.Valor) ?? 0,
            TaxaConversao: ganhas + perdidas == 0 ? 0 : Math.Round(100m * ganhas / (ganhas + perdidas), 1),
            TarefasPendentes: await tarefas.CountAsync(t => t.Status != StatusTarefa.Concluida),
            TarefasAtrasadas: await tarefas.CountAsync(t => t.Status != StatusTarefa.Concluida && t.DataVencimento < agora),
            LeadsPorStatus: leadsPorStatus,
            FunilResumo: Enum.GetValues<EstagioFunil>()
                .Select(e => funil.FirstOrDefault(f => f.Estagio == e) ?? new PorEstagio(e, 0, 0)).ToList());
    }
}
