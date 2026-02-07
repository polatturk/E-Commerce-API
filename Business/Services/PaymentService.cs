using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Response;
using Core.DTOs;
using Core.Mappings;
using DataAccess.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; 
using Iyzipay; 
using Iyzipay.Model; 
using Iyzipay.Request;
using Payment = Core.Entities.Payment;


namespace Business.Services;

public class PaymentService(IUnitOfWork _unitOfWork, IConfiguration _configuration) : IPaymentService
{
    public async Task<Response<List<PaymentResponseDto>>> GetAllAsync()
    {
        var payments = await _unitOfWork.GetRepository<Payment>().GetAllAsync();

        var dtos = PaymentMapper.ToResponseDtoList(payments.ToList());
        return Response<List<PaymentResponseDto>>.Success(dtos, 200);
    }

    public async Task<Response<PaymentResponseDto>> GetByIdAsync(Guid id)
    {
        var payment = await _unitOfWork.GetRepository<Payment>().GetByIdAsync(id);

        if (payment == null)
            return Response<PaymentResponseDto>.Fail("Ödeme kaydı bulunamadı", 404);

        var dto = PaymentMapper.ToResponseDto(payment);
        return Response<PaymentResponseDto>.Success(dto, 200);
    }

    public async Task<Response<PaymentResponseDto>> CreateAsync(PaymentCreateDto dto, Guid userId)
    {
        var order = await _unitOfWork.GetRepository<Order>()
            .GetSingleAsync(x => x.Id == dto.OrderId && x.UserId == userId);

        if (order == null)
        {
            return Response<PaymentResponseDto>.Fail("Sipariş bulunamadı veya size ait değil.", 404);
        }

        if (dto.Amount < order.TotalAmount)
        {
            return Response<PaymentResponseDto>.Fail($"Yetersiz ödeme. Gereken: {order.TotalAmount}", 400);
        }

        if (order.Status != OrderStatus.Pending)
        {
            return Response<PaymentResponseDto>.Fail("Bu siparişin ödeme süreci zaten tamamlanmış veya iptal edilmiş.", 400);
        }   

        var entity = PaymentMapper.ToEntity(dto);
        entity.PaymentDate = DateTime.Now;
        entity.Status = PaymentStatus.Success; 

        entity.TransactionId = "TRX-" + Guid.NewGuid().ToString().ToUpper().Substring(0, 10);

        if (!string.IsNullOrEmpty(dto.CardNumber) && dto.CardNumber.Length >= 4)
        {
            entity.CardLastFour = dto.CardNumber.Substring(dto.CardNumber.Length - 4);
        }

        order.Status = OrderStatus.Processing;

        await _unitOfWork.GetRepository<Payment>().AddAsync(entity);
        _unitOfWork.GetRepository<Order>().Update(order);

        await ClearCartAsync(userId);

        await _unitOfWork.SaveChangesAsync();

        var responseDto = PaymentMapper.ToResponseDto(entity);
        return Response<PaymentResponseDto>.Success(
            responseDto,
            201,
            "Ödemeniz başarıyla alındı. Siparişiniz hazırlanıyor!"
        );
    }

    private async Task ClearCartAsync(Guid userId)
    {
        var cart = await _unitOfWork.GetRepository<Cart>()
            .GetSingleAsync(x => x.UserId == userId, include: q => q.Include(c => c.Items));

        if (cart != null && cart.Items.Any())
        {
            _unitOfWork.GetRepository<CartItem>().DeleteRange(cart.Items);
        }
    }

    private Options GetIyzicoOptions()
    {
        return new Options
        {
            ApiKey = _configuration["IyzicoSettings:ApiKey"],
            SecretKey = _configuration["IyzicoSettings:SecretKey"],
            BaseUrl = _configuration["IyzicoSettings:BaseUrl"]
        };
    }

    public async Task<Response<string>> InitializePaymentFormAsync(PaymentCreateDto dto, Guid userId)
    {
        var order = await _unitOfWork.GetRepository<Order>().GetSingleAsync(x => x.Id == dto.OrderId && x.UserId == userId);
        if (order == null) return Response<string>.Fail("Sipariş bulunamadı.", 404);

        var options = GetIyzicoOptions();
        string price = order.TotalAmount.ToString("F2").Replace(",", ".");

        var request = new CreateCheckoutFormInitializeRequest
        {
            Locale = Locale.TR.ToString(),
            ConversationId = order.Id.ToString(),
            Price = price,
            PaidPrice = price,
            Currency = Currency.TRY.ToString(),
            BasketId = order.Id.ToString(),
            PaymentGroup = PaymentGroup.PRODUCT.ToString(),
            CallbackUrl = "https://localhost:7123/api/Payment/Callback"
        };

        request.Buyer = new Buyer { Id = userId.ToString(), Name = "Müşteri", Surname = "Soyadı", Email = "test@email.com", IdentityNumber = "11111111111", RegistrationAddress = "Adres", City = "Istanbul", Country = "Turkey" };
        var address = new Iyzipay.Model.Address {ContactName = "Müşteri Adı", City = "Istanbul", Country = "Turkey", Description = "Adres detayı"};
        request.ShippingAddress = address; request.BillingAddress = address;
        request.BasketItems = new List<BasketItem> { new BasketItem { Id = "B1", Name = "Sipariş", Category1 = "Genel", ItemType = BasketItemType.PHYSICAL.ToString(), Price = price } };

        var checkoutFormInitialize = await CheckoutFormInitialize.Create(request, options);

        if (checkoutFormInitialize.Status == Iyzipay.Model.Status.SUCCESS.ToString())
        {
            return Response<string>.Success(checkoutFormInitialize.CheckoutFormContent, 200);
        }

        return Response<string>.Fail(checkoutFormInitialize.ErrorMessage, 400);
    }

    public async Task<Response<bool>> CompletePaymentAsync(string token)
    {
        var options = GetIyzicoOptions();
        var request = new RetrieveCheckoutFormRequest { Token = token };
        var checkoutForm = await CheckoutForm.Retrieve(request, options);

        if (checkoutForm.PaymentStatus == "SUCCESS")
        {
            var orderId = Guid.Parse(checkoutForm.BasketId);
            var order = await _unitOfWork.GetRepository<Order>().GetByIdAsync(orderId);

            if (order != null)
            {
                order.Status = OrderStatus.Processing;
                _unitOfWork.GetRepository<Order>().Update(order);
                var payment = new Payment { Amount = order.TotalAmount, PaymentDate = DateTime.Now, Status = PaymentStatus.Success, OrderId = orderId, TransactionId = checkoutForm.PaymentId, CardLastFour = checkoutForm.LastFourDigits };
                await _unitOfWork.GetRepository<Payment>().AddAsync(payment);
                await ClearCartAsync(order.UserId);
                await _unitOfWork.SaveChangesAsync();
                return Response<bool>.Success(true, 200);
            }
        }
        return Response<bool>.Fail("Ödeme başarısız.", 400);
    }
}