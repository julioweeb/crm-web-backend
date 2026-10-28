namespace CrmApi.Services;

public static class Utilitarios
{
    // O PostgreSQL (Npgsql) só aceita DateTime com Kind=Utc em colunas "timestamptz".
    // Datas vindas do JSON sem fuso (ex.: "2026-10-30") chegam como Unspecified; aqui tratamos como UTC.
    public static DateTime Utc(DateTime d) => d.Kind switch
    {
        DateTimeKind.Utc => d,
        DateTimeKind.Local => d.ToUniversalTime(),
        _ => DateTime.SpecifyKind(d, DateTimeKind.Utc)
    };

    public static DateTime? Utc(DateTime? d) => d.HasValue ? Utc(d.Value) : null;

    // Monta o padrão do ILike escapando % e _ digitados pelo usuário.
    public static string PadraoLike(string texto) =>
        "%" + texto.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
}
