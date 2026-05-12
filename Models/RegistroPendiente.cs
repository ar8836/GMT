/// <summary>
/// Representa un registro en espera que aún no ha sido verificado.
/// </summary>
public class RegistroPendiente
{
    /// <summary>
    /// Identificador único utilizado como token de verificación.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Correo electrónico asociado al registro.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// JSON con datos adicionales (ej. nombre de la empresa, datos de alumno, etc.).
    /// </summary>
    public required string DatosJson { get; set; }

    /// <summary>
    /// Tipo de registro: "Alumno" o "Empresa".
    /// </summary>
    public required string TipoRegistro { get; set; }

    /// <summary>
    /// Marca de tiempo de expiración del registro pendiente.
    /// </summary>
    public required DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Clave S3 donde se almacenará archivos auxiliares (ej. foto de perfil).
    /// </summary>
    public string? S3Key { get; set; }
}