using GMT.Models;
using System;
using System.Threading.Tasks;

namespace GMT.Services
{
    public interface IVerificationService
    {
        Task<RegistroPendiente?> ObtenerRegistroPendienteActivoAsync(Guid token);
        Task<bool> ConfirmarRegistroAsync(Guid token);
    }
}
