using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs
{
    using System.ComponentModel.DataAnnotations;

    public record UserRegisterDto(
        [Required, MaxLength(50)] string FirstName,
        [Required, MaxLength(50)] string LastName,
        [Required, EmailAddress, MaxLength(256)] string Email,
        [Required, MinLength(8), MaxLength(72)] string Password,
        [EnumDataType(typeof(Gender))] Gender Gender);

    public record UserLoginDto(
        [Required] string Email,
        [Required] string Password);

    public record UserUpdateDto(
        [Required, MaxLength(50)] string FirstName,
        [Required, MaxLength(50)] string LastName,
        [EnumDataType(typeof(Gender))] Gender Gender);

    public record UserResponseDto(Guid Id, string FullName, string Email, UserRole Role, Gender Gender);
    public record TokenResponseDto(string AccessToken, DateTime AccessTokenExpiration);
    public record GoogleLoginDto([Required] string IdToken);
}
