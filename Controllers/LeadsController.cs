using CrmApi.Services;
using CrmApi.Data;
using CrmApi.Dtos;
using CrmApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Controllers;

[Route("api/leads")]
public class LeadsController : BaseCrmController
{
    public LeadsController(CrmDbContext db) : base(db) { }

    private IQueryable<Lead> Visiveis() =>
        VeTudo ? LeadsDaEmpresa : LeadsDaEmpresa.Where(l => l.ResponsavelId == UsuarioId);

    [HttpGet]
    public async Task<PaginaDto<Lead>> Listar(
        string? busca, StatusLead? status, string? origem, int pagina = 1, int tamanho = 20)
    {
        (pagina, tamanho) = NormalizaPagina(pagina, tamanho);
        var q = Visiveis();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var p = Utilitarios.PadraoLike(busca);
            q = q.Where(l => EF.Functions.ILike(l.Nome, p)
                          || (l.Email != null && EF.Functions.ILike(l.Email, p))
                          || (l.Empresa != null && EF.Functions.ILike(l.Empresa, p)));
        }
        if (status.HasValue) q = q.Where(l => l.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(origem)) q = q.Where(l => l.Origem == origem);

        var total = await q.CountAsync();
        var itens = await q.OrderByDescending(l => l.CriadoEm)
            .Skip((pagina - 1) * tamanho).Take(tamanho).ToListAsync();
        return new PaginaDto<Lead>(total, pagina, tamanho, itens);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Lead>> Obter(int id)
    {
        var l = await Visiveis().FirstOrDefaultAsync(x => x.Id == id);
        return l is null ? NotFound() : l;
    }

    [HttpPost]
    public async Task<ActionResult<Lead>> Criar(LeadDto dto)
    {
        var l = new Lead { ResponsavelId = UsuarioId };
        Copiar(dto, l);
        l.Status = StatusLead.Novo; // todo lead entra como "Novo"; a conversão tem endpoint próprio
        Db.Leads.Add(l);
        await Db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obter), new { id = l.Id }, l);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Lead>> Atualizar(int id, LeadDto dto)
    {
        var l = await Visiveis().FirstOrDefaultAsync(x => x.Id == id);
        if (l is null) return NotFound();
        if (l.Status == StatusLead.Convertido)
            return BadRequest(new { erro = "Lead já convertido não pode ser alterado." });
        if (dto.Status == StatusLead.Convertido)
            return BadRequest(new { erro = "Use o endpoint de conversão para converter o lead." });

        Copiar(dto, l);
        await Db.SaveChangesAsync();
        return l;
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        var l = await Visiveis().FirstOrDefaultAsync(x => x.Id == id);
        if (l is null) return NotFound();
        Db.Leads.Remove(l);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    // Converte o lead em cliente (e, opcionalmente, já abre uma oportunidade).
    [HttpPost("{id:int}/converter")]
    public async Task<ActionResult<Cliente>> Converter(int id, [FromQuery] bool criarOportunidade = true)
    {
        var l = await Visiveis().FirstOrDefaultAsync(x => x.Id == id);
        if (l is null) return NotFound();
        if (l.Status == StatusLead.Convertido)
            return Conflict(new { erro = "Lead já foi convertido." });

        var cliente = new Cliente
        {
            Nome = l.Nome, Email = l.Email, Telefone = l.Telefone, Empresa = l.Empresa,
            ResponsavelId = l.ResponsavelId
        };
        Db.Clientes.Add(cliente);
        await Db.SaveChangesAsync();

        if (criarOportunidade)
        {
            Db.Oportunidades.Add(new Oportunidade
            {
                Titulo = $"Oportunidade - {l.Nome}",
                ClienteId = cliente.Id,
                ResponsavelId = l.ResponsavelId,
                Estagio = EstagioFunil.Prospeccao,
                Probabilidade = 10
            });
        }

        l.Status = StatusLead.Convertido;
        l.ClienteConvertidoId = cliente.Id;
        await Db.SaveChangesAsync();
        return cliente;
    }

    private static void Copiar(LeadDto d, Lead l)
    {
        l.Nome = d.Nome.Trim(); l.Email = d.Email; l.Telefone = d.Telefone;
        l.Empresa = d.Empresa; l.Origem = d.Origem; l.Status = d.Status;
    }
}
