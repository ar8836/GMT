namespace GMT.Services
{
    using System;
    using System.Threading.Tasks;

    public interface IVerificationService
    {
        Task<RegistroPendiente?> ObtenerRegistroPendienteActivoAsync(Guid token);
        Task<bool> ConfirmarRegistroAsync(Guid token);
    }
}