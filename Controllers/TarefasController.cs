using CrmApi.Services;
using CrmApi.Data;
using CrmApi.Dtos;
using CrmApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Controllers;

// Tarefas e agendamento de contatos (ligação, reunião, e-mail com data marcada).
[Route("api/tarefas")]
public class TarefasController : BaseCrmController
{
    public TarefasController(CrmDbContext db) : base(db) { }

    private IQueryable<Tarefa> Visiveis() =>
        VeTudo ? TarefasDaEmpresa : TarefasDaEmpresa.Where(t => t.ResponsavelId == UsuarioId);

    // Filtros: ?status=&tipo=&de=&ate=&atrasadas=true&clienteId=
    [HttpGet]
    public async Task<PaginaDto<Tarefa>> Listar(
        StatusTarefa? status, TipoTarefa? tipo, DateTime? de, DateTime? ate,
        bool? atrasadas, int? clienteId, int pagina = 1, int tamanho = 20)
    {
        (pagina, tamanho) = NormalizaPagina(pagina, tamanho);
        var q = Visiveis();
        if (status.HasValue) q = q.Where(t => t.Status == status.Value);
        if (tipo.HasValue) q = q.Where(t => t.Tipo == tipo.Value);
        if (de.HasValue) { var deUtc = Utilitarios.Utc(de.Value); q = q.Where(t => t.DataVencimento >= deUtc); }
        if (ate.HasValue) { var ateUtc = Utilitarios.Utc(ate.Value); q = q.Where(t => t.DataVencimento <= ateUtc); }
        if (clienteId.HasValue) q = q.Where(t => t.ClienteId == clienteId.Value);
        if (atrasadas == true)
        {
            var agora = DateTime.UtcNow;
            q = q.Where(t => t.Status != StatusTarefa.Concluida && t.DataVencimento < agora);
        }

        var total = await q.CountAsync();
        var itens = await q.OrderBy(t => t.DataVencimento)
            .Skip((pagina - 1) * tamanho).Take(tamanho).ToListAsync();
        return new PaginaDto<Tarefa>(total, pagina, tamanho, itens);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Tarefa>> Obter(int id)
    {
        var t = await Visiveis().FirstOrDefaultAsync(x => x.Id == id);
        return t is null ? NotFound() : t;
    }

    [HttpPost]
    public async Task<ActionResult<Tarefa>> Criar(TarefaDto dto)
    {
        if (!await VinculosValidos(dto)) return BadRequest(new { erro = "Cliente ou oportunidade inválidos." });
        var t = new Tarefa { ResponsavelId = UsuarioId };
        Copiar(dto, t);
        Db.Tarefas.Add(t);
        await Db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obter), new { id = t.Id }, t);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Tarefa>> Atualizar(int id, TarefaDto dto)
    {
        var t = await Visiveis().FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        if (!await VinculosValidos(dto)) return BadRequest(new { erro = "Cliente ou oportunidade inválidos." });
        Copiar(dto, t);
        await Db.SaveChangesAsync();
        return t;
    }

    [HttpPatch("{id:int}/concluir")]
    public async Task<ActionResult<Tarefa>> Concluir(int id)
    {
        var t = await Visiveis().FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        t.Status = StatusTarefa.Concluida;
        await Db.SaveChangesAsync();
        return t;
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        var t = await Visiveis().FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        Db.Tarefas.Remove(t);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    // Garante que o usuário só vincule tarefas a registros que ele pode ver.
    private async Task<bool> VinculosValidos(TarefaDto d)
    {
        if (d.ClienteId.HasValue)
        {
            var clientesVisiveis = VeTudo ? ClientesDaEmpresa : ClientesDaEmpresa.Where(c => c.ResponsavelId == UsuarioId);
            var ok = await clientesVisiveis.AnyAsync(c => c.Id == d.ClienteId);
            if (!ok) return false;
        }
        if (d.OportunidadeId.HasValue)
        {
            var oportunidadesVisiveis = VeTudo ? OportunidadesDaEmpresa : OportunidadesDaEmpresa.Where(o => o.ResponsavelId == UsuarioId);
            var ok = await oportunidadesVisiveis.AnyAsync(o => o.Id == d.OportunidadeId);
            if (!ok) return false;
        }
        return true;
    }

    private static void Copiar(TarefaDto d, Tarefa t)
    {
        t.Titulo = d.Titulo.Trim(); t.Descricao = d.Descricao; t.Tipo = d.Tipo;
        t.DataVencimento = Utilitarios.Utc(d.DataVencimento); t.Status = d.Status;
        t.ClienteId = d.ClienteId; t.OportunidadeId = d.OportunidadeId;
    }
}
