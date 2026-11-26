using Core.DTOs;
using Core.Entities;
using Core.Interfaces;
using Core.Mappings;
using Core.Response;
using DataAccess.UnitOfWork;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Business.Services;

public class UserService(IUnitOfWork _unitOfWork, ITokenService _tokenService, IConfiguration _config) : IUserService
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

            entity.Email = email; 

            entity.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            entity.Role = Core.Enums.UserRole.Customer;

            await _unitOfWork.GetRepository<User>().AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            var responseDto = UserMapper.ToResponseDto(entity);
            return Response<UserResponseDto>.Success(responseDto, 201);
        }
        catch (DbUpdateException)
        {
            // Aynı anda gelen iki istek unique index'e takılırsa buraya düşer
            return Response<UserResponseDto>.Fail("Bu email adresi zaten kullanımda.", 409);
        }
        catch (Exception)
        {
            // TODO: logla (ILogger / Serilog)
            return Response<UserResponseDto>.Fail("Kayıt işlemi başarısız oldu.", 500);
        }
    }

    public async Task<Response<TokenResponseDto>> LoginAsync(UserLoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var userList = await _unitOfWork.GetRepository<User>().GetAllAsync(x => x.Email == email);
        var user = userList.FirstOrDefault();

        if (user == null)
        {
            return Response<TokenResponseDto>.Fail("Email veya şifre hatalı.", 401);
        }

        if (user.Password == null)
        {
            return Response<TokenResponseDto>.Fail("Bu hesap Google ile oluşturulmuş, Google ile giriş yapın.", 400);
        }

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);

        if (!isPasswordValid)
        {
            return Response<TokenResponseDto>.Fail("Email veya şifre hatalı.", 401);
        }

        var tokenResponse = _tokenService.CreateToken(user);

        return Response<TokenResponseDto>.Success(tokenResponse, 200);
    }

    public async Task<Response<TokenResponseDto>> GoogleLoginAsync(string idToken)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _config["Google:ClientId"] }
                });
        }
        catch (InvalidJwtException)
        {
            return Response<TokenResponseDto>.Fail("Geçersiz Google token.", 401);
        }

        if (!payload.EmailVerified)
            return Response<TokenResponseDto>.Fail("Google e-postası doğrulanmamış.", 400);

        var email = payload.Email.Trim().ToLowerInvariant();
        var repo = _unitOfWork.GetRepository<User>();

        var userList = await repo.GetAllAsync(x => x.Email == email);
        var user = userList.FirstOrDefault();

        if (user == null)
        {
            user = new User
            {
                Email = email,
                FirstName = Truncate(payload.GivenName ?? "Kullanıcı", 50),
                LastName = Truncate(payload.FamilyName ?? "-", 50),
                GoogleId = payload.Subject,
                Password = null,
                Role = Core.Enums.UserRole.Customer
            };
            await repo.AddAsync(user);
        }
        else if (user.GoogleId == null)
        {
            user.GoogleId = payload.Subject;
            repo.Update(user);
        }
        else if (user.GoogleId != payload.Subject)
        {
            return Response<TokenResponseDto>.Fail("Bu hesap başka bir Google hesabına bağlı.", 400);
        }

        await _unitOfWork.SaveChangesAsync();

        var tokenResponse = _tokenService.CreateToken(user);
        return Response<TokenResponseDto>.Success(tokenResponse, 200);
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value.Trim() : value[..max].Trim();

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