using CrmApi.Services;
using CrmApi.Data;
using CrmApi.Dtos;
using CrmApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Controllers;

[Route("api/clientes")]
public class ClientesController : BaseCrmController
{
    public ClientesController(CrmDbContext db) : base(db) { }

    // Restringe a consulta aos clientes que o usuário pode ver.
    private IQueryable<Cliente> Visiveis() =>
        VeTudo ? Db.Clientes : Db.Clientes.Where(c => c.ResponsavelId == UsuarioId);

    // Busca e filtros: ?busca=texto&cidade=&estado=&ativo=true&pagina=1&tamanho=20
    [HttpGet]
    public async Task<PaginaDto<Cliente>> Listar(
        string? busca, string? cidade, string? estado, bool? ativo, int pagina = 1, int tamanho = 20)
    {
        (pagina, tamanho) = NormalizaPagina(pagina, tamanho);
        var q = Visiveis();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var p = Utilitarios.PadraoLike(busca);
            q = q.Where(c => EF.Functions.ILike(c.Nome, p)
                          || (c.Email != null && EF.Functions.ILike(c.Email, p))
                          || (c.Empresa != null && EF.Functions.ILike(c.Empresa, p))
                          || (c.Documento != null && EF.Functions.ILike(c.Documento, p)));
        }
        if (!string.IsNullOrWhiteSpace(cidade)) q = q.Where(c => c.Cidade == cidade);
        if (!string.IsNullOrWhiteSpace(estado)) q = q.Where(c => c.Estado == estado);
        if (ativo.HasValue) q = q.Where(c => c.Ativo == ativo.Value);

        var total = await q.CountAsync();
        var itens = await q.OrderBy(c => c.Nome).Skip((pagina - 1) * tamanho).Take(tamanho).ToListAsync();
        return new PaginaDto<Cliente>(total, pagina, tamanho, itens);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Cliente>> Obter(int id)
    {
        var c = await Visiveis().FirstOrDefaultAsync(x => x.Id == id);
        return c is null ? NotFound() : c;
    }

    [HttpPost]
    public async Task<ActionResult<Cliente>> Criar(ClienteDto dto)
    {
        var c = new Cliente { ResponsavelId = UsuarioId };
        Copiar(dto, c);
        Db.Clientes.Add(c);
        await Db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obter), new { id = c.Id }, c);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Cliente>> Atualizar(int id, ClienteDto dto)
    {
        var c = await Visiveis().FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        Copiar(dto, c);
        await Db.SaveChangesAsync();
        return c;
    }

    // Excluir é restrito a Admin e Gerente.
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Gerente")]
    public async Task<IActionResult> Excluir(int id)
    {
        var c = await Db.Clientes.FindAsync(id);
        if (c is null) return NotFound();
        Db.Clientes.Remove(c);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    // ===== Contatos do cliente =====
    [HttpGet("{id:int}/contatos")]
    public async Task<ActionResult<List<Contato>>> ListarContatos(int id)
    {
        if (!await Visiveis().AnyAsync(c => c.Id == id)) return NotFound();
        return await Db.Contatos.Where(c => c.ClienteId == id).OrderBy(c => c.Nome).ToListAsync();
    }

    [HttpPost("{id:int}/contatos")]
    public async Task<ActionResult<Contato>> CriarContato(int id, ContatoDto dto)
    {
        if (!await Visiveis().AnyAsync(c => c.Id == id)) return NotFound();
        var ct = new Contato
        {
            ClienteId = id, Nome = dto.Nome.Trim(), Cargo = dto.Cargo,
            Email = dto.Email, Telefone = dto.Telefone
        };
        Db.Contatos.Add(ct);
        await Db.SaveChangesAsync();
        return ct;
    }

    [HttpPut("{id:int}/contatos/{contatoId:int}")]
    public async Task<ActionResult<Contato>> AtualizarContato(int id, int contatoId, ContatoDto dto)
    {
        if (!await Visiveis().AnyAsync(c => c.Id == id)) return NotFound();
        var ct = await Db.Contatos.FirstOrDefaultAsync(c => c.Id == contatoId && c.ClienteId == id);
        if (ct is null) return NotFound();
        ct.Nome = dto.Nome.Trim(); ct.Cargo = dto.Cargo; ct.Email = dto.Email; ct.Telefone = dto.Telefone;
        await Db.SaveChangesAsync();
        return ct;
    }

    [HttpDelete("{id:int}/contatos/{contatoId:int}")]
    public async Task<IActionResult> ExcluirContato(int id, int contatoId)
    {
        if (!await Visiveis().AnyAsync(c => c.Id == id)) return NotFound();
        var ct = await Db.Contatos.FirstOrDefaultAsync(c => c.Id == contatoId && c.ClienteId == id);
        if (ct is null) return NotFound();
        Db.Contatos.Remove(ct);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    // ===== Histórico de interações =====
    [HttpGet("{id:int}/interacoes")]
    public async Task<ActionResult<List<Interacao>>> ListarInteracoes(int id)
    {
        if (!await Visiveis().AnyAsync(c => c.Id == id)) return NotFound();
        return await Db.Interacoes.Where(i => i.ClienteId == id)
            .OrderByDescending(i => i.Data).ToListAsync();
    }

    [HttpPost("{id:int}/interacoes")]
    public async Task<ActionResult<Interacao>> RegistrarInteracao(int id, InteracaoDto dto)
    {
        if (!await Visiveis().AnyAsync(c => c.Id == id)) return NotFound();
        if (dto.ContatoId.HasValue &&
            !await Db.Contatos.AnyAsync(c => c.Id == dto.ContatoId && c.ClienteId == id))
            return BadRequest(new { erro = "Contato não pertence a este cliente." });

        var i = new Interacao
        {
            ClienteId = id, ContatoId = dto.ContatoId, Tipo = dto.Tipo,
            Descricao = dto.Descricao.Trim(), Data = Utilitarios.Utc(dto.Data) ?? DateTime.UtcNow, UsuarioId = UsuarioId
        };
        Db.Interacoes.Add(i);
        await Db.SaveChangesAsync();
        return i;
    }

    private static void Copiar(ClienteDto d, Cliente c)
    {
        c.Nome = d.Nome.Trim(); c.Documento = d.Documento; c.Email = d.Email; c.Telefone = d.Telefone;
        c.Empresa = d.Empresa; c.Endereco = d.Endereco; c.Cidade = d.Cidade;
        c.Estado = d.Estado?.ToUpperInvariant(); c.Ativo = d.Ativo;
    }
}
