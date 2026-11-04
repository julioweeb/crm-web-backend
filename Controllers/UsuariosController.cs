using CrmApi.Data;
using CrmApi.Dtos;
using CrmApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Controllers;

// Admin e SuperAdmin gerenciam usuários apenas dentro da empresa do próprio token.
[Route("api/usuarios")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class UsuariosController : BaseCrmController
{
    public UsuariosController(CrmDbContext db) : base(db) { }

    [HttpGet]
    public async Task<List<UsuarioDto>> Listar() =>
        await Db.Usuarios.Where(u => u.EmpresaId == EmpresaId).OrderBy(u => u.Nome)
            .Select(u => new UsuarioDto(u.Id, u.Nome, u.Email, u.Perfil, u.Ativo)).ToListAsync();

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Criar(UsuarioAdminDto dto)
    {
        if (dto.Perfil == Perfil.SuperAdmin)
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Senha) || !AuthController.SenhaForte(dto.Senha))
            return BadRequest(new { erro = "Senha obrigatória: 8+ caracteres, com letras e números." });

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await Db.Usuarios.AnyAsync(u => u.Email == email))
            return Conflict(new { erro = "E-mail já cadastrado." });

        var u = new Usuario
        {
            Nome = dto.Nome.Trim(),
            Email = email,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha),
            Perfil = dto.Perfil,
            Ativo = dto.Ativo,
            EmpresaId = EmpresaId
        };
        Db.Usuarios.Add(u);
        await Db.SaveChangesAsync();
        return CreatedAtAction(nameof(Listar), new UsuarioDto(u.Id, u.Nome, u.Email, u.Perfil, u.Ativo));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> Atualizar(int id, UsuarioAdminDto dto)
    {
        var u = await Db.Usuarios.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == EmpresaId);
        if (u is null) return NotFound();

        // O perfil SuperAdmin só é concedido pela configuração de bootstrap.
        if (dto.Perfil == Perfil.SuperAdmin && !(id == UsuarioId && u.Perfil == Perfil.SuperAdmin))
            return Forbid();

        // Ninguém pode remover o próprio acesso atual nem desativar a própria conta.
        if (id == UsuarioId && (dto.Perfil != u.Perfil || !dto.Ativo))
            return BadRequest(new { erro = "Você não pode alterar seu próprio perfil ou desativar sua conta." });

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await Db.Usuarios.AnyAsync(x => x.Email == email && x.Id != id))
            return Conflict(new { erro = "E-mail já cadastrado." });

        u.Nome = dto.Nome.Trim();
        u.Email = email;
        u.Perfil = dto.Perfil;
        u.Ativo = dto.Ativo;

        if (!string.IsNullOrWhiteSpace(dto.Senha))
        {
            if (!AuthController.SenhaForte(dto.Senha))
                return BadRequest(new { erro = "Senha fraca: 8+ caracteres, com letras e números." });
            u.SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha);
        }

        await Db.SaveChangesAsync();
        return new UsuarioDto(u.Id, u.Nome, u.Email, u.Perfil, u.Ativo);
    }
}