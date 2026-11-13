using Core.DTOs;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace E_Commerce_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController(IUserService _userService) : ControllerBase
    {
        [Authorize(Roles = "Admin")]
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var response = await _userService.GetAllAsync();
            return StatusCode(response.StatusCode, response);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var response = await _userService.GetByIdAsync(id);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _userService.GetByIdAsync(userId);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("Register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(UserRegisterDto dto)
        {
            var response = await _userService.RegisterAsync(dto);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("Login")]
        [AllowAnonymous] 
        public async Task<IActionResult> Login(UserLoginDto dto)
        {
            var response = await _userService.LoginAsync(dto);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpPut("Update")]
        public async Task<IActionResult> Update(UserUpdateDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _userService.UpdateAsync(userId, dto);
            return StatusCode(response.StatusCode, response);
        }

        // Müşteri: sadece kendi hesabını siler (ID token'dan gelir)
        [Authorize]
        [HttpDelete("Delete")]
        public async Task<IActionResult> DeleteMyAccount()
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var response = await _userService.RemoveAsync(userId);
            return StatusCode(response.StatusCode, response);
        }

        // Admin: herhangi bir kullanıcıyı siler
        [Authorize(Roles = "Admin")]
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var response = await _userService.RemoveAsync(id);
            return StatusCode(response.StatusCode, response);
        }
    }
}