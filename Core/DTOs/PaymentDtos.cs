using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs
{
    public record PaymentCreateDto(decimal Amount, PaymentMethod PaymentMethod, Guid OrderId, string CardNumber, string CVV, string ExpiryDate);

    public record PaymentResponseDto(Guid Id, decimal Amount, DateTime PaymentDate, PaymentMethod PaymentMethod, PaymentStatus Status, Guid OrderId);
}
