using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Response;
using Core.DTOs;
using Core.Mappings;
using DataAccess.UnitOfWork;
using Microsoft.EntityFrameworkCore;
    
namespace Business.Services;

public class PaymentService(IUnitOfWork _unitOfWork) : IPaymentService
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
}