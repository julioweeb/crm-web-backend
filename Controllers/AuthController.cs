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

    // O cadastro público foi desativado. Novos usuários são criados por UsuariosController,
    // protegido com [Authorize(Roles = "Admin")].

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<AuthRespostaDto>> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var usuario = await _db.Usuarios.Include(u => u.Empresa)
            .FirstOrDefaultAsync(u => u.Email == email);

        // Mensagem genérica: não revela se o e-mail existe ou se a senha está errada.
        if (usuario is null || !usuario.Ativo || !BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.SenhaHash))
            return Unauthorized(new { erro = "E-mail ou senha inválidos." });

        if (usuario.EmpresaId is null || usuario.Empresa is null || !usuario.Empresa.Ativo)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { erro = "Usuário sem empresa ativa vinculada. Solicite ao administrador que verifique o cadastro." });

        return Ok(MontaResposta(usuario));
    }

    [HttpGet("eu")]
    [Authorize]
    public async Task<ActionResult<UsuarioDto>> Eu()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var empresaId = int.Parse(User.FindFirstValue(TokenService.EmpresaIdClaim)!);
        var u = await _db.Usuarios.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == empresaId);
        if (u is null || !u.Ativo) return Unauthorized();
        return new UsuarioDto(u.Id, u.Nome, u.Email, u.Perfil, u.Ativo);
    }

    [HttpPut("eu")]
    [Authorize]
    public async Task<ActionResult<UsuarioDto>> AtualizarMeuPerfil(MeuPerfilAtualizarDto dto)
    {
        var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var empresaId = int.Parse(User.FindFirstValue(TokenService.EmpresaIdClaim)!);
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u =>
            u.Id == usuarioId && u.EmpresaId == empresaId && u.Ativo);
        if (usuario is null) return Unauthorized();

        var nome = dto.Nome.Trim();
        var email = dto.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email))
            return BadRequest(new { erro = "Nome e e-mail são obrigatórios." });

        if (await _db.Usuarios.AnyAsync(u => u.Email == email && u.Id != usuarioId))
            return Conflict(new { erro = "E-mail já cadastrado." });

        var trocarSenha = !string.IsNullOrWhiteSpace(dto.SenhaAtual)
            || !string.IsNullOrWhiteSpace(dto.NovaSenha);
        if (trocarSenha)
        {
            if (string.IsNullOrWhiteSpace(dto.SenhaAtual) || string.IsNullOrWhiteSpace(dto.NovaSenha))
                return BadRequest(new { erro = "Informe a senha atual e a nova senha." });
            if (!BCrypt.Net.BCrypt.Verify(dto.SenhaAtual, usuario.SenhaHash))
                return BadRequest(new { erro = "A senha atual está incorreta." });
            if (!SenhaForte(dto.NovaSenha))
                return BadRequest(new { erro = "A nova senha deve ter 8+ caracteres, com letras e números." });

            usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.NovaSenha);
        }

        usuario.Nome = nome;
        usuario.Email = email;
        await _db.SaveChangesAsync();
        return new UsuarioDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Perfil, usuario.Ativo);
    }

    private AuthRespostaDto MontaResposta(Usuario u)
    {
        var (token, expira) = _tokens.Gerar(u);
        return new AuthRespostaDto(token, expira, new UsuarioDto(u.Id, u.Nome, u.Email, u.Perfil, u.Ativo));
    }

    internal static bool SenhaForte(string senha) =>
        senha.Length >= 8 && Regex.IsMatch(senha, "[A-Za-z]") && Regex.IsMatch(senha, "[0-9]");
}
