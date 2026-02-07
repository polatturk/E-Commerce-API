using Core.DTOs;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace E_Commerce_API.Controllers
{
    [Authorize] // Tüm ödeme işlemleri yetki gerektirsin
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController(IPaymentService _paymentService) : ControllerBase
    {
        [HttpGet("GetAll")]
        [Authorize(Roles = "Admin")] // Sadece admin tüm ödemeleri görebilsin
        public async Task<IActionResult> GetAll()
        {
            var response = await _paymentService.GetAllAsync();
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var response = await _paymentService.GetByIdAsync(id);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpPost("Create")]
        public async Task<IActionResult> Create(PaymentCreateDto dto)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Ödeme yapmak için giriş yapmalısınız.");

            var response = await _paymentService.CreateAsync(dto, Guid.Parse(userIdClaim));
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("Initialize")]
        public async Task<IActionResult> Initialize([FromBody] PaymentCreateDto dto)
        {
            // Kullanıcı ID'sini JWT Token'dan alıyoruz
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Servisimiz bize iyzico'nun HTML formunu (string) dönecek
            var response = await _paymentService.InitializePaymentFormAsync(dto, userId);

            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("Callback")]
        [AllowAnonymous] // iyzico sunucusu bizimle konuşacağı için bu endpoint herkese açık olmalı
        [Consumes("application/x-www-form-urlencoded")] // iyzico veriyi 'form' olarak gönderir
        public async Task<IActionResult> Callback([FromForm] IFormCollection collection)
        {
            // iyzico'dan dönen token'ı alıyoruz
            var token = collection["token"];

            // Ödemeyi doğrula ve siparişi onayla
            var response = await _paymentService.CompletePaymentAsync(token);

            // Ödeme bittiğinde kullanıcıyı web sitendeki "Başarı" veya "Hata" sayfasına yönlendiriyoruz
            if (response.IsSuccess)
            {
                return Redirect("http://localhost:4200/checkout/success"); // Frontend adresin
            }

            return Redirect("http://localhost:4200/checkout/fail");
        }
    }
}
