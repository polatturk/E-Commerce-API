using Core.DTOs;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Commerce_API.Core.Interfaces
{
    public interface IPaymentProvider
    {
        // Bankanın ödeme sayfasına gitmek için gereken URL'i ve verileri hazırlar
        Task<string> CreatePaymentLinkAsync(PaymentCreateDto dto, Order order);

        // Bankadan gelen ödeme sonucunun (Hash) doğruluğunu kontrol eder
        bool ValidateHash(Dictionary<string, string> callbackData);
    }
}
