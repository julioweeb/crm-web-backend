using System.ComponentModel.DataAnnotations;
using CrmApi.Models;

namespace CrmApi.Dtos;

// DTOs de entrada: só expõem os campos que o cliente da API pode alterar.
// Isso evita "mass assignment" (ex.: alguém enviar ResponsavelId ou Perfil no JSON).

public record RegistroDto(
    [Required, MaxLength(120)] string Nome,
    [Required, EmailAddress, MaxLength(160)] string Email,
    [Required, MinLength(8), MaxLength(100)] string Senha);

public record LoginDto(
    [Required, EmailAddress] string Email,
    [Required] string Senha);

public record AuthRespostaDto(string Token, DateTime ExpiraEm, UsuarioDto Usuario);

public record UsuarioDto(int Id, string Nome, string Email, Perfil Perfil, bool Ativo);

public record UsuarioAdminDto(
    [Required, MaxLength(120)] string Nome,
    [Required, EmailAddress, MaxLength(160)] string Email,
    string? Senha,
    Perfil Perfil,
    bool Ativo);

public record ClienteDto(
    [Required, MaxLength(160)] string Nome,
    [MaxLength(20)] string? Documento,
    [EmailAddress, MaxLength(160)] string? Email,
    [MaxLength(30)] string? Telefone,
    [MaxLength(160)] string? Empresa,
    [MaxLength(200)] string? Endereco,
    [MaxLength(80)] string? Cidade,
    [MaxLength(2)] string? Estado,
    bool Ativo = true);

public record ContatoDto(
    [Required, MaxLength(120)] string Nome,
    [MaxLength(80)] string? Cargo,
    [EmailAddress, MaxLength(160)] string? Email,
    [MaxLength(30)] string? Telefone);

public record InteracaoDto(
    int? ContatoId,
    TipoInteracao Tipo,
    [Required, MaxLength(1000)] string Descricao,
    DateTime? Data);

public record LeadDto(
    [Required, MaxLength(160)] string Nome,
    [EmailAddress, MaxLength(160)] string? Email,
    [MaxLength(30)] string? Telefone,
    [MaxLength(160)] string? Empresa,
    [MaxLength(80)] string? Origem,
    StatusLead Status = StatusLead.Novo);

public record OportunidadeDto(
    [Required, MaxLength(160)] string Titulo,
    [Range(0, 999999999)] decimal Valor,
    int ClienteId,
    EstagioFunil Estagio,
    [Range(0, 100)] int Probabilidade,
    DateTime? PrevisaoFechamento);

public record MudarEstagioDto(EstagioFunil Estagio);

public record TarefaDto(
    [Required, MaxLength(160)] string Titulo,
    [MaxLength(1000)] string? Descricao,
    TipoTarefa Tipo,
    DateTime DataVencimento,
    StatusTarefa Status,
    int? ClienteId,
    int? OportunidadeId);

public record AnotacaoDto(
    [Required, MaxLength(2000)] string Texto,
    int? ClienteId,
    int? OportunidadeId,
    int? LeadId);

public record PaginaDto<T>(int Total, int Pagina, int Tamanho, List<T> Itens);
