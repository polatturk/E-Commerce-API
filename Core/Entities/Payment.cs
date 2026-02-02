using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities
{
    public class Payment : BaseEntity
    {
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CreditCard;
        public PaymentStatus Status { get; set; }

        public string? TransactionId { get; set; } // Bankadan/Aracı kurumdan dönen eşsiz işlem kodu
        public string? CardLastFour { get; set; }  // Kullanıcıya bilgi amaçlı (Örn: 4242)

        public Guid OrderId { get; set; } 
        public Order Order { get; set; }
    }
    
}
