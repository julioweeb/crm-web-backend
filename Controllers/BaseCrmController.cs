using System.Security.Claims;
using CrmApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmApi.Controllers;

[ApiController]
[Authorize]
public abstract class BaseCrmController : ControllerBase
{
    protected readonly CrmDbContext Db;
    protected BaseCrmController(CrmDbContext db) => Db = db;

    // Id do usuário logado, lido do token JWT.
    protected int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Admin e Gerente enxergam todos os registros; Vendedor só os seus.
    protected bool VeTudo => User.IsInRole("Admin") || User.IsInRole("Gerente");

    protected static (int pagina, int tamanho) NormalizaPagina(int pagina, int tamanho)
        => (Math.Max(1, pagina), Math.Clamp(tamanho, 1, 100));
}
