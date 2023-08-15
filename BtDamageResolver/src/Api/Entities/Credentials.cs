using System;
using System.ComponentModel.DataAnnotations;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// Credentials.
/// </summary>
[Serializable]
public class Credentials
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Credentials"/> class.
    /// </summary>
    public Credentials()
    {
        Password = string.Empty;
    }

    /// <summary>
    /// User authentication token, if any.
    /// </summary>
    /// <remarks>
    /// This can be used in place of user password, if you know it.
    /// It should be stored in browser local storage to avoid storing passwords.
    /// Transient.
    /// </remarks>
    public Guid? AuthenticationToken { get; set; }

    /// <summary>
    /// User or game name.
    /// </summary>
    [Required]
    [RegularExpression(@"^[A-Za-z0-9_-]*$", ErrorMessage = "Username must be composed of alphanumeric characters, dashes and underscores.")]
    [StringLength(32, ErrorMessage = "{0} length must be between {2} and {1} characters.", MinimumLength = 1)]
    public string Name { get; set; }

    /// <summary>
    /// User or game password.
    /// </summary>
    [RegularExpression(@"^\S*$", ErrorMessage = "No white space allowed in password.")]
    [StringLength(32, ErrorMessage = "{0} length must be less than {1} characters.")]
    public string Password { get; set; }
}