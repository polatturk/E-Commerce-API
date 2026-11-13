using Core.DTOs;
using Core.Entities;
using Riok.Mapperly.Abstractions;

namespace Core.Mappings
{

    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class UserMapper
    {
        public static partial User ToEntity(UserRegisterDto dto);

        public static partial void UpdateEntityFromDto(UserUpdateDto dto, User entity);

        public static UserResponseDto ToResponseDto(User user)=> new(user.Id,$"{user.FirstName} {user.LastName}".Trim(),user.Email,user.Role,user.Gender);

        public static List<UserResponseDto> ToResponseDtoList(List<User> users)
            => users.Select(ToResponseDto).ToList();
    }
}