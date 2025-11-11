using System.ComponentModel.DataAnnotations;

namespace MauseTalkBackend.Domain.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Uživatelské jméno je povinné")]
    [MinLength(3, ErrorMessage = "Uživatelské jméno musí mít alespoň 3 znaky")]
    public string Username { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Heslo je povinné")]
    [MinLength(6, ErrorMessage = "Heslo musí mít alespoň 6 znaků")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required(ErrorMessage = "Uživatelské jméno je povinné")]
    [MinLength(3, ErrorMessage = "Uživatelské jméno musí mít alespoň 3 znaky")]
    public string Username { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email je povinný")]
    [EmailAddress(ErrorMessage = "Neplatný email formát")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Heslo je povinné")]
    [MinLength(6, ErrorMessage = "Heslo musí mít alespoň 6 znaků")]
    public string Password { get; set; } = string.Empty;
    
    public string? DisplayName { get; set; }
}

public class AuthResponse
{
    public UserDto User { get; set; } = new();
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class UpdateUserRequest
{
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
}