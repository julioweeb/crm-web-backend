using System.Text;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using CrmApi.Data;
using CrmApi.Models;
using CrmApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

var builder = WebApplication.CreateBuilder(args);

// ===== Padrões de DESENVOLVIMENTO =====
// Em Development, se a chave JWT e o admin inicial não estiverem configurados, usamos valores de teste.
// Em produção isso NÃO acontece: as variáveis são obrigatórias.
if (builder.Environment.IsDevelopment())
{
    if (string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Key"]))
        builder.Configuration["Jwt:Key"] = "chave-somente-para-desenvolvimento-troque-em-producao-123456";
    if (string.IsNullOrWhiteSpace(builder.Configuration["Seed:AdminEmail"]))
        builder.Configuration["Seed:AdminEmail"] = "admin@crm.local";
    if (string.IsNullOrWhiteSpace(builder.Configuration["Seed:AdminSenha"]))
        builder.Configuration["Seed:AdminSenha"] = "Admin@12345";
    if (string.IsNullOrWhiteSpace(builder.Configuration["Seed:EmpresaNome"]))
        builder.Configuration["Seed:EmpresaNome"] = "Empresa inicial";
}

// ===== Banco de dados (PostgreSQL / Supabase) =====
var conexao = builder.Configuration.GetConnectionString("Padrao");
if (string.IsNullOrWhiteSpace(conexao))
    throw new InvalidOperationException(
        "Configure ConnectionStrings:Padrao (user-secrets em desenvolvimento ou variável de ambiente ConnectionStrings__Padrao). Veja docs/SUPABASE.md.");

builder.Services.AddDbContext<CrmDbContext>(opt => opt.UseNpgsql(conexao));

// ===== Autenticação JWT =====
var chaveJwt = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(chaveJwt) || chaveJwt.Length < 32)
    throw new InvalidOperationException(
        $"Configure Jwt:Key com pelo menos 32 caracteres (ambiente atual: {builder.Environment.EnvironmentName}). Em Development há um valor padrão; em produção use variável de ambiente.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chaveJwt)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        opt.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                var usuarioClaim = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var empresaClaim = principal?.FindFirst(TokenService.EmpresaIdClaim)?.Value;
                var perfilClaim = principal?.FindFirst(ClaimTypes.Role)?.Value;

                if (!int.TryParse(usuarioClaim, out var usuarioId)
                    || !int.TryParse(empresaClaim, out var empresaId))
                {
                    context.Fail("Token sem usuário ou empresa válida.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<CrmDbContext>();
                var perfilAtual = await db.Usuarios
                    .AsNoTracking()
                    .Where(u => u.Id == usuarioId
                        && u.EmpresaId == empresaId
                        && u.Ativo
                        && u.Empresa != null
                        && u.Empresa.Ativo)
                    .Select(u => (Perfil?)u.Perfil)
                    .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

                if (perfilAtual is null
                    || !Enum.TryParse<Perfil>(perfilClaim, out var perfilDoToken)
                    || perfilAtual.Value != perfilDoToken)
                {
                    context.Fail("Usuário, empresa ou perfil do token não está mais ativo.");
                }
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<TokenService>();

// ===== Limite de tentativas no login (proteção contra força bruta) =====
builder.Services.AddRateLimiter(opt =>
{
    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opt.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

// ===== CORS: só o front-end configurado pode chamar a API =====
var origens = builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.WithOrigins(origens).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CRM API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Informe: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ===== Cria o banco e o usuário administrador inicial =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
    // Para aprendizado: EnsureCreated. Em produção, troque por migrations (dotnet ef migrations add).
    var jaExiste = db.Database
    .SqlQueryRaw<bool>("SELECT to_regclass('public.\"Usuarios\"') IS NOT NULL AS \"Value\"")
    .AsEnumerable().First();
    if (!jaExiste)
        db.GetService<IRelationalDatabaseCreator>().CreateTables();

    // Atualiza bancos existentes sem afetar os dados: Empresa e o vínculo opcional do usuário.
    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS public."Empresas" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "Nome" character varying(160) NOT NULL,
            "Ativo" boolean NOT NULL,
            "CriadoEm" timestamp with time zone NOT NULL
        );
        ALTER TABLE public."Usuarios" ADD COLUMN IF NOT EXISTS "EmpresaId" integer NULL;
        CREATE INDEX IF NOT EXISTS "IX_Usuarios_EmpresaId" ON public."Usuarios" ("EmpresaId");
        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM pg_constraint
                WHERE conname = 'FK_Usuarios_Empresas_EmpresaId'
                  AND conrelid = 'public."Usuarios"'::regclass
            ) THEN
                ALTER TABLE public."Usuarios"
                    ADD CONSTRAINT "FK_Usuarios_Empresas_EmpresaId"
                    FOREIGN KEY ("EmpresaId") REFERENCES public."Empresas" ("Id")
                    ON DELETE RESTRICT;
            END IF;
        END $$;
        """);

    // SEGURANÇA (Supabase): o schema "public" é exposto por uma API REST pública (PostgREST).
    // Ativar o Row Level Security, sem nenhuma policy, bloqueia o acesso pela chave pública (anon).
    // A nossa API conecta como dono do banco (role "postgres") e continua acessando normalmente.
    // O comando é idempotente: pode rodar a cada inicialização.
    string[] comandosRls =
    {
        "ALTER TABLE public.\"Empresas\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE public.\"Usuarios\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE public.\"Clientes\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE public.\"Contatos\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE public.\"Interacoes\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE public.\"Leads\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE public.\"Oportunidades\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE public.\"Tarefas\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE public.\"Anotacoes\" ENABLE ROW LEVEL SECURITY;"
    };
    foreach (var sql in comandosRls)
        db.Database.ExecuteSqlRaw(sql);

    var adminEmail = app.Configuration["Seed:AdminEmail"];
    var adminSenha = app.Configuration["Seed:AdminSenha"];
    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminSenha)
        && !db.Usuarios.Any())
    {
        var empresaNome = app.Configuration["Seed:EmpresaNome"]?.Trim();
        if (string.IsNullOrWhiteSpace(empresaNome))
            throw new InvalidOperationException("Configure Seed:EmpresaNome para criar a empresa do administrador inicial.");

        var empresa = db.Empresas.FirstOrDefault(e => e.Ativo && e.Nome == empresaNome);
        if (empresa is null)
        {
            empresa = new Empresa { Nome = empresaNome };
            db.Empresas.Add(empresa);
        }

        db.Usuarios.Add(new Usuario
        {
            Nome = "Administrador",
            Email = adminEmail.Trim().ToLowerInvariant(),
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(adminSenha),
            Perfil = Perfil.Admin,
            Empresa = empresa
        });
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
