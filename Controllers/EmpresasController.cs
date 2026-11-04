using CrmApi.Data;
using CrmApi.Dtos;
using CrmApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Controllers;

[ApiController]
[Route("api/empresas")]
[Authorize(Roles = "SuperAdmin")]
public class EmpresasController : ControllerBase
{
    private readonly CrmDbContext _db;
    public EmpresasController(CrmDbContext db) => _db = db;

    [HttpGet]
    public async Task<List<EmpresaDto>> Listar() =>
        await _db.Empresas.OrderBy(e => e.Nome)
            .Select(e => new EmpresaDto(e.Id, e.Nome, e.Ativo, e.CriadoEm, e.Usuarios.Count))
            .ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmpresaDto>> Obter(int id)
    {
        var empresa = await _db.Empresas.Where(e => e.Id == id)
            .Select(e => new EmpresaDto(e.Id, e.Nome, e.Ativo, e.CriadoEm, e.Usuarios.Count))
            .FirstOrDefaultAsync();
        return empresa is null ? NotFound() : Ok(empresa);
    }

    // Cria empresa e primeiro Admin na mesma operação; o vínculo é automático.
    [HttpPost]
    public async Task<ActionResult<EmpresaDto>> Criar(EmpresaCriarDto dto)
    {
        if (!AuthController.SenhaForte(dto.AdminSenha))
            return BadRequest(new { erro = "A senha do Admin deve ter 8+ caracteres, com letras e números." });

        var email = dto.AdminEmail.Trim().ToLowerInvariant();
        if (await _db.Usuarios.AnyAsync(u => u.Email == email))
            return Conflict(new { erro = "Este e-mail já está cadastrado no CRM." });

        var empresa = new Empresa { Nome = dto.Nome.Trim() };
        empresa.Usuarios.Add(new Usuario
        {
            Nome = dto.AdminNome.Trim(),
            Email = email,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.AdminSenha),
            Perfil = Perfil.Admin,
            Ativo = true,
            Empresa = empresa
        });

        _db.Empresas.Add(empresa);
        await _db.SaveChangesAsync();

        var resposta = new EmpresaDto(empresa.Id, empresa.Nome, empresa.Ativo, empresa.CriadoEm, 1);
        return CreatedAtAction(nameof(Obter), new { id = empresa.Id }, resposta);
    }
}