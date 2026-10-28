using System.Security.Claims;
using System.Text.RegularExpressions;
using CrmApi.Data;
using CrmApi.Dtos;
using CrmApi.Models;
using CrmApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly CrmDbContext _db;
    private readonly TokenService _tokens;

    public AuthController(CrmDbContext db, TokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    // Cadastro público: sempre cria um Vendedor. Perfis maiores só o Admin concede.
    [HttpPost("registro")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<AuthRespostaDto>> Registro(RegistroDto dto)
    {
        if (!SenhaForte(dto.Senha))
            return BadRequest(new { erro = "A senha deve ter 8+ caracteres, com letras e números." });

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Usuarios.AnyAsync(u => u.Email == email))
            return Conflict(new { erro = "E-mail já cadastrado." });

        var usuario = new Usuario
        {
            Nome = dto.Nome.Trim(),
            Email = email,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha),
            Perfil = Perfil.Vendedor
        };
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        return Ok(MontaResposta(usuario));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<AuthRespostaDto>> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

        // Mensagem genérica: não revela se o e-mail existe ou se a senha está errada.
        if (usuario is null || !usuario.Ativo || !BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.SenhaHash))
            return Unauthorized(new { erro = "E-mail ou senha inválidos." });

        return Ok(MontaResposta(usuario));
    }

    [HttpGet("eu")]
    [Authorize]
    public async Task<ActionResult<UsuarioDto>> Eu()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var u = await _db.Usuarios.FindAsync(id);
        if (u is null || !u.Ativo) return Unauthorized();
        return new UsuarioDto(u.Id, u.Nome, u.Email, u.Perfil, u.Ativo);
    }

    private AuthRespostaDto MontaResposta(Usuario u)
    {
        var (token, expira) = _tokens.Gerar(u);
        return new AuthRespostaDto(token, expira, new UsuarioDto(u.Id, u.Nome, u.Email, u.Perfil, u.Ativo));
    }

    internal static bool SenhaForte(string senha) =>
        senha.Length >= 8 && Regex.IsMatch(senha, "[A-Za-z]") && Regex.IsMatch(senha, "[0-9]");
}
