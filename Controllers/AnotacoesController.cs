using CrmApi.Data;
using CrmApi.Dtos;
using CrmApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Controllers;

[Route("api/anotacoes")]
public class AnotacoesController : BaseCrmController
{
    public AnotacoesController(CrmDbContext db) : base(db) { }

    private IQueryable<Anotacao> Visiveis() =>
        VeTudo ? Db.Anotacoes : Db.Anotacoes.Where(a => a.UsuarioId == UsuarioId);

    [HttpGet]
    public async Task<List<Anotacao>> Listar(int? clienteId, int? oportunidadeId, int? leadId)
    {
        var q = Visiveis();
        if (clienteId.HasValue) q = q.Where(a => a.ClienteId == clienteId);
        if (oportunidadeId.HasValue) q = q.Where(a => a.OportunidadeId == oportunidadeId);
        if (leadId.HasValue) q = q.Where(a => a.LeadId == leadId);
        return await q.OrderByDescending(a => a.CriadoEm).Take(200).ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Anotacao>> Criar(AnotacaoDto dto)
    {
        if (dto.ClienteId is null && dto.OportunidadeId is null && dto.LeadId is null)
            return BadRequest(new { erro = "Vincule a anotação a um cliente, oportunidade ou lead." });

        // Só permite anotar em registros que o usuário enxerga.
        if (dto.ClienteId.HasValue && !await (VeTudo ? Db.Clientes : Db.Clientes.Where(c => c.ResponsavelId == UsuarioId))
                .AnyAsync(c => c.Id == dto.ClienteId))
            return BadRequest(new { erro = "Cliente inválido." });
        if (dto.OportunidadeId.HasValue && !await (VeTudo ? Db.Oportunidades : Db.Oportunidades.Where(o => o.ResponsavelId == UsuarioId))
                .AnyAsync(o => o.Id == dto.OportunidadeId))
            return BadRequest(new { erro = "Oportunidade inválida." });
        if (dto.LeadId.HasValue && !await (VeTudo ? Db.Leads : Db.Leads.Where(l => l.ResponsavelId == UsuarioId))
                .AnyAsync(l => l.Id == dto.LeadId))
            return BadRequest(new { erro = "Lead inválido." });

        var a = new Anotacao
        {
            Texto = dto.Texto.Trim(), ClienteId = dto.ClienteId,
            OportunidadeId = dto.OportunidadeId, LeadId = dto.LeadId, UsuarioId = UsuarioId
        };
        Db.Anotacoes.Add(a);
        await Db.SaveChangesAsync();
        return a;
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        // Só o autor apaga a própria anotação (Admin/Gerente também podem moderar).
        var a = await Visiveis().FirstOrDefaultAsync(x => x.Id == id);
        if (a is null) return NotFound();
        Db.Anotacoes.Remove(a);
        await Db.SaveChangesAsync();
        return NoContent();
    }
}
