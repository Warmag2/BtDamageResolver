using System.ComponentModel.DataAnnotations;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Entities
{
    public class Credentials
    {
        public Credentials()
        {
            Password = string.Empty;
        }

        [Required]
        [RegularExpression(@"^[A-Za-z0-9_-]*$", ErrorMessage = "Username must be composed of alphanumeric characters, dashes and underscores.")]
        [StringLength(32, ErrorMessage = "{0} length must be between {2} and {1} characters.", MinimumLength = 1)]
        public string Name { get; set; }

        [RegularExpression(@"^\S*$", ErrorMessage = "No white space allowed in password.")]
        [StringLength(32, ErrorMessage = "{0} length must be less than {1} characters.")]
        public string Password { get; set; }
    }
}