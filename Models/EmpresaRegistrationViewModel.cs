using System.ComponentModel.DataAnnotations;

namespace GMT.Models
{
    public class EmpresaRegistrationViewModel
    {
        [Required]
        [Display(Name = "Nombre de la empresa")]
        public string? NombreEmpresa { get; set; }

        [Required]
        [RfcLength(ErrorMessage = "Actualmente solo se aceptan registros de Personas Morales.")]
        [Display(Name = "RFC")]
        public string? RFC { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Correo electrónico")]
        public string? CorreoElectronico { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string? Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string? ConfirmPassword { get; set; }
    }

    public class RfcLengthAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || !(value is string rfc))
            {
                return new ValidationResult("El RFC es requerido.");
            }

            // Remove any whitespace
            rfc = rfc.Trim();

            if (rfc.Length != 12)
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}
