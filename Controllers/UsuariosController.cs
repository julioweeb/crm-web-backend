using CrmApi.Data;
using CrmApi.Dtos;
using CrmApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Controllers;

// Controle de usuários e permissões: somente Admin.
[Route("api/usuarios")]
[Authorize(Roles = "Admin")]
public class UsuariosController : BaseCrmController
{
    public UsuariosController(CrmDbContext db) : base(db) { }

    [HttpGet]
    public async Task<List<UsuarioDto>> Listar() =>
        await Db.Usuarios.OrderBy(u => u.Nome)
            .Select(u => new UsuarioDto(u.Id, u.Nome, u.Email, u.Perfil, u.Ativo)).ToListAsync();

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Criar(UsuarioAdminDto dto)
    {
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
            Ativo = dto.Ativo
        };
        Db.Usuarios.Add(u);
        await Db.SaveChangesAsync();
        return CreatedAtAction(nameof(Listar), new UsuarioDto(u.Id, u.Nome, u.Email, u.Perfil, u.Ativo));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> Atualizar(int id, UsuarioAdminDto dto)
    {
        var u = await Db.Usuarios.FindAsync(id);
        if (u is null) return NotFound();

        // O admin não pode se rebaixar nem se desativar (evita ficar sem administrador).
        if (id == UsuarioId && (dto.Perfil != Perfil.Admin || !dto.Ativo))
            return BadRequest(new { erro = "Você não pode remover seu próprio acesso de administrador." });

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
