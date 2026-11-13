using Core.Entities;
using Core.Interfaces;
using Core.Response;
using Core.DTOs;
using Core.Mappings;
using DataAccess.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class UserService(IUnitOfWork _unitOfWork, ITokenService _tokenService) : IUserService
{

    public async Task<Response<List<UserResponseDto>>> GetAllAsync()
    {
        var users = await _unitOfWork.GetRepository<User>().GetAllAsync();

        if (users == null)
            return Response<List<UserResponseDto>>.Fail("Kullanıcılar bulunamadı", 404);

        var dtos = UserMapper.ToResponseDtoList(users.ToList());

        return Response<List<UserResponseDto>>.Success(dtos, 200);
    }

    public async Task<Response<UserResponseDto>> GetByIdAsync(Guid id)
    {
        var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(id);

        if (user == null)
            return Response<UserResponseDto>.Fail("Kullanıcı bulunamadı", 404);

        var dto = UserMapper.ToResponseDto(user);

        return Response<UserResponseDto>.Success(dto, 200);
    }

    public async Task<Response<UserResponseDto>> RegisterAsync(UserRegisterDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var isEmailTaken = await _unitOfWork.GetRepository<User>().AnyAsync(x => x.Email == email);
        if (isEmailTaken)
            return Response<UserResponseDto>.Fail("Bu email adresi zaten kullanımda.", 409);

        try
        {
            var entity = UserMapper.ToEntity(dto);

            entity.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            entity.Role = Core.Enums.UserRole.Customer;

            await _unitOfWork.GetRepository<User>().AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            var responseDto = UserMapper.ToResponseDto(entity);
            return Response<UserResponseDto>.Success(responseDto, 201);
        }
        catch (Exception ex)
        {
            // Loglama buraya gelecek (NLog/Serilog vb.)
            return Response<UserResponseDto>.Fail($"Kayıt işlemi başarısız: {ex.Message}", 500);
        }
    }

    public async Task<Response<TokenResponseDto>> LoginAsync(UserLoginDto dto)
    {
        var userList = await _unitOfWork.GetRepository<User>().GetAllAsync(x => x.Email == dto.Email);
        var user = userList.FirstOrDefault();

        if (user == null)
        {
            return Response<TokenResponseDto>.Fail("Email veya şifre hatalı.", 404);
        }

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);

        if (!isPasswordValid)
        {
            return Response<TokenResponseDto>.Fail("Email veya şifre hatalı.", 400);
        }

        var tokenResponse = _tokenService.CreateToken(user);

        return Response<TokenResponseDto>.Success(tokenResponse, 200);
    }

    public async Task<Response<bool>> UpdateAsync(Guid userId, UserUpdateDto dto)
    {
        var repo = _unitOfWork.GetRepository<User>();

        var existingUser = await repo.GetByIdAsync(userId);
        if (existingUser == null)
            return Response<bool>.Fail("Kullanıcı bulunamadı", 404);

        UserMapper.UpdateEntityFromDto(dto, existingUser);

        repo.Update(existingUser);
        await _unitOfWork.SaveChangesAsync();

        return Response<bool>.Success(true, 200);
    }

    public async Task<Response<bool>> RemoveAsync(Guid id)
    {
        var repo = _unitOfWork.GetRepository<User>();

        var user = await repo.GetByIdAsync(id);

        if (user == null)
            return Response<bool>.Fail("Kullanıcı bulunamadı", 404);

        repo.Delete(user);
        await _unitOfWork.SaveChangesAsync();

        return Response<bool>.Success(true, 200);
    }
}