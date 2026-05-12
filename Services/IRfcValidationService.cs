using System;

namespace GMT.Services
{
    public interface IRfcValidationService
    {
        Task<bool> ValidarRfcSatAsync(string rfc);
    }
}
