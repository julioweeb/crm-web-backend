using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CrmApi.Models;

// ===== Enumerações =====
public enum Perfil { Vendedor = 0, Gerente = 1, Admin = 2 }
public enum StatusLead { Novo = 0, Contatado = 1, Qualificado = 2, Desqualificado = 3, Convertido = 4 }
public enum EstagioFunil { Prospeccao = 0, Qualificacao = 1, Proposta = 2, Negociacao = 3, Ganha = 4, Perdida = 5 }
public enum TipoInteracao { Ligacao = 0, Email = 1, Reuniao = 2, Mensagem = 3, Outro = 4 }
public enum TipoTarefa { Tarefa = 0, Ligacao = 1, Reuniao = 2, Email = 3 }
public enum StatusTarefa { Pendente = 0, EmAndamento = 1, Concluida = 2 }

// ===== Entidades =====
public class Usuario
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string Nome { get; set; } = "";
    [Required, MaxLength(160)] public string Email { get; set; } = "";
    // Nunca armazenamos a senha em texto puro: apenas o hash BCrypt.
    [JsonIgnore] public string SenhaHash { get; set; } = "";
    public Perfil Perfil { get; set; } = Perfil.Vendedor;
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public int? EmpresaId { get; set; }
    [JsonIgnore] public Empresa? Empresa { get; set; }
}

public class Empresa
{
    public int Id { get; set; }
    [Required, MaxLength(160)] public string Nome { get; set; } = "";
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    [JsonIgnore] public List<Usuario> Usuarios { get; set; } = new();
}

public class Cliente
{
    public int Id { get; set; }
    [Required, MaxLength(160)] public string Nome { get; set; } = "";
    [MaxLength(20)] public string? Documento { get; set; }      // CPF ou CNPJ
    [MaxLength(160)] public string? Email { get; set; }
    [MaxLength(30)] public string? Telefone { get; set; }
    [MaxLength(160)] public string? Empresa { get; set; }
    [MaxLength(200)] public string? Endereco { get; set; }
    [MaxLength(80)] public string? Cidade { get; set; }
    [MaxLength(2)] public string? Estado { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public int ResponsavelId { get; set; }
    [JsonIgnore] public Usuario? Responsavel { get; set; }
    [JsonIgnore] public List<Contato> Contatos { get; set; } = new();
    [JsonIgnore] public List<Oportunidade> Oportunidades { get; set; } = new();
    [JsonIgnore] public List<Interacao> Interacoes { get; set; } = new();
}

public class Contato
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    [JsonIgnore] public Cliente? Cliente { get; set; }
    [Required, MaxLength(120)] public string Nome { get; set; } = "";
    [MaxLength(80)] public string? Cargo { get; set; }
    [MaxLength(160)] public string? Email { get; set; }
    [MaxLength(30)] public string? Telefone { get; set; }
}

public class Interacao
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    [JsonIgnore] public Cliente? Cliente { get; set; }
    public int? ContatoId { get; set; }
    public TipoInteracao Tipo { get; set; }
    [Required, MaxLength(1000)] public string Descricao { get; set; } = "";
    public DateTime Data { get; set; } = DateTime.UtcNow;
    public int UsuarioId { get; set; }
    [JsonIgnore] public Usuario? Usuario { get; set; }
}

public class Lead
{
    public int Id { get; set; }
    [Required, MaxLength(160)] public string Nome { get; set; } = "";
    [MaxLength(160)] public string? Email { get; set; }
    [MaxLength(30)] public string? Telefone { get; set; }
    [MaxLength(160)] public string? Empresa { get; set; }
    [MaxLength(80)] public string? Origem { get; set; }          // site, indicação, evento...
    public StatusLead Status { get; set; } = StatusLead.Novo;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public int ResponsavelId { get; set; }
    [JsonIgnore] public Usuario? Responsavel { get; set; }
    public int? ClienteConvertidoId { get; set; }
}

public class Oportunidade
{
    public int Id { get; set; }
    [Required, MaxLength(160)] public string Titulo { get; set; } = "";
    public decimal Valor { get; set; }
    public EstagioFunil Estagio { get; set; } = EstagioFunil.Prospeccao;
    public int Probabilidade { get; set; }                        // 0 a 100
    public DateTime? PrevisaoFechamento { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? FechadaEm { get; set; }

    public int ClienteId { get; set; }
    [JsonIgnore] public Cliente? Cliente { get; set; }
    public int ResponsavelId { get; set; }
    [JsonIgnore] public Usuario? Responsavel { get; set; }
}

// Tarefas também servem como "agendamento de contatos" (Tipo = Ligacao/Reuniao/Email + data).
public class Tarefa
{
    public int Id { get; set; }
    [Required, MaxLength(160)] public string Titulo { get; set; } = "";
    [MaxLength(1000)] public string? Descricao { get; set; }
    public TipoTarefa Tipo { get; set; } = TipoTarefa.Tarefa;
    public DateTime DataVencimento { get; set; }
    public StatusTarefa Status { get; set; } = StatusTarefa.Pendente;
    public int? ClienteId { get; set; }
    public int? OportunidadeId { get; set; }
    public int ResponsavelId { get; set; }
    [JsonIgnore] public Usuario? Responsavel { get; set; }
}

public class Anotacao
{
    public int Id { get; set; }
    [Required, MaxLength(2000)] public string Texto { get; set; } = "";
    public int? ClienteId { get; set; }
    public int? OportunidadeId { get; set; }
    public int? LeadId { get; set; }
    public int UsuarioId { get; set; }
    [JsonIgnore] public Usuario? Usuario { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
