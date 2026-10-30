using CrmApi.Services;
using CrmApi.Data;
using CrmApi.Dtos;
using CrmApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Controllers;

[Route("api/oportunidades")]
public class OportunidadesController : BaseCrmController
{
    public OportunidadesController(CrmDbContext db) : base(db) { }

    private IQueryable<Oportunidade> Visiveis() =>
        VeTudo ? OportunidadesDaEmpresa : OportunidadesDaEmpresa.Where(o => o.ResponsavelId == UsuarioId);

    // Item da lista já traz o nome do cliente para exibir no funil.
    public record OportunidadeItem(
        int Id, string Titulo, decimal Valor, EstagioFunil Estagio, int Probabilidade,
        DateTime? PrevisaoFechamento, int ClienteId, string ClienteNome, DateTime CriadoEm);

    private static IQueryable<OportunidadeItem> Projetar(IQueryable<Oportunidade> q) =>
        q.Select(o => new OportunidadeItem(o.Id, o.Titulo, o.Valor, o.Estagio, o.Probabilidade,
            o.PrevisaoFechamento, o.ClienteId, o.Cliente!.Nome, o.CriadoEm));

    [HttpGet]
    public async Task<PaginaDto<OportunidadeItem>> Listar(
        string? busca, EstagioFunil? estagio, int? clienteId, int pagina = 1, int tamanho = 20)
    {
        (pagina, tamanho) = NormalizaPagina(pagina, tamanho);
        var q = Visiveis();
        if (!string.IsNullOrWhiteSpace(busca))
        {
            var p = Utilitarios.PadraoLike(busca);
            q = q.Where(o => EF.Functions.ILike(o.Titulo, p) || EF.Functions.ILike(o.Cliente!.Nome, p));
        }
        if (estagio.HasValue) q = q.Where(o => o.Estagio == estagio.Value);
        if (clienteId.HasValue) q = q.Where(o => o.ClienteId == clienteId.Value);

        var total = await q.CountAsync();
        var itens = await Projetar(q.OrderByDescending(o => o.CriadoEm))
            .Skip((pagina - 1) * tamanho).Take(tamanho).ToListAsync();
        return new PaginaDto<OportunidadeItem>(total, pagina, tamanho, itens);
    }

    // Funil de vendas: uma coluna por estágio, com quantidade e valor total.
    public record ColunaFunil(EstagioFunil Estagio, int Quantidade, decimal ValorTotal, List<OportunidadeItem> Itens);

    [HttpGet("funil")]
    public async Task<List<ColunaFunil>> Funil()
    {
        var todas = await Projetar(Visiveis().OrderByDescending(o => o.CriadoEm)).ToListAsync();
        return Enum.GetValues<EstagioFunil>()
            .Select(e =>
            {
                var itens = todas.Where(o => o.Estagio == e).ToList();
                return new ColunaFunil(e, itens.Count, itens.Sum(i => i.Valor), itens);
            }).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Oportunidade>> Obter(int id)
    {
        var o = await Visiveis().FirstOrDefaultAsync(x => x.Id == id);
        return o is null ? NotFound() : o;
    }

    [HttpPost]
    public async Task<ActionResult<Oportunidade>> Criar(OportunidadeDto dto)
    {
        // O cliente precisa existir e ser visível para quem está criando.
        var clientesVisiveis = VeTudo ? ClientesDaEmpresa : ClientesDaEmpresa.Where(c => c.ResponsavelId == UsuarioId);
        var clienteOk = await clientesVisiveis.AnyAsync(c => c.Id == dto.ClienteId);
        if (!clienteOk) return BadRequest(new { erro = "Cliente inválido." });

        var o = new Oportunidade { ResponsavelId = UsuarioId };
        Copiar(dto, o);
        Db.Oportunidades.Add(o);
        await Db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obter), new { id = o.Id }, o);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Oportunidade>> Atualizar(int id, OportunidadeDto dto)
    {
        var o = await Visiveis().FirstOrDefaultAsync(x => x.Id == id);
        if (o is null) return NotFound();
        if (dto.ClienteId != o.ClienteId)
            return BadRequest(new { erro = "Não é possível trocar o cliente da oportunidade." });
        Copiar(dto, o);
        await Db.SaveChangesAsync();
        return o;
    }

    // Move a oportunidade no funil (usado pelo arrastar-e-soltar do front-end).
    [HttpPatch("{id:int}/estagio")]
    public async Task<ActionResult<Oportunidade>> MudarEstagio(int id, MudarEstagioDto dto)
    {
        var o = await Visiveis().FirstOrDefaultAsync(x => x.Id == id);
        if (o is null) return NotFound();
        AplicarEstagio(o, dto.Estagio);
        await Db.SaveChangesAsync();
        return o;
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        var o = await Visiveis().FirstOrDefaultAsync(x => x.Id == id);
        if (o is null) return NotFound();
        Db.Oportunidades.Remove(o);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    private static void Copiar(OportunidadeDto d, Oportunidade o)
    {
        o.Titulo = d.Titulo.Trim(); o.Valor = d.Valor; o.ClienteId = d.ClienteId;
        o.Probabilidade = d.Probabilidade; o.PrevisaoFechamento = Utilitarios.Utc(d.PrevisaoFechamento);
        AplicarEstagio(o, d.Estagio);
    }

    // Regra de negócio: ganhar = 100%, perder = 0%, e registra a data de fechamento.
    private static void AplicarEstagio(Oportunidade o, EstagioFunil novo)
    {
        o.Estagio = novo;
        if (novo == EstagioFunil.Ganha) { o.Probabilidade = 100; o.FechadaEm ??= DateTime.UtcNow; }
        else if (novo == EstagioFunil.Perdida) { o.Probabilidade = 0; o.FechadaEm ??= DateTime.UtcNow; }
        else o.FechadaEm = null;
    }
}
