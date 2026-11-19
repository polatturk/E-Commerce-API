using Core.Entities;
using Core.Interfaces;
using Core.Response;
using Core.DTOs;
using Core.Mappings;
using DataAccess.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class CartService(IUnitOfWork _unitOfWork) : ICartService
{
    public async Task<Response<List<CartResponseDto>>> GetAllAsync()
    {
        var carts = await _unitOfWork.GetRepository<Cart>()
            .GetAllAsync(include: q => q.Include(c => c.Items));

        var dtos = CartMapper.ToResponseDtoList(carts.ToList());
        return Response<List<CartResponseDto>>.Success(dtos, 200);
    }

    public async Task<Response<CartResponseDto>> GetByIdAsync(Guid id, Guid userId)
    {
        var cart = await _unitOfWork.GetRepository<Cart>()
            .GetSingleAsync(
                expression: x => x.Id == id && x.UserId == userId,
                include: q => q.Include(c => c.Items).ThenInclude(i => i.Product)
            );

        if (cart == null)
        {
            return Response<CartResponseDto>.Fail("Sepet bulunamadı veya bu sepete erişim yetkiniz yok.", 404);
        }

        var dto = CartMapper.ToResponseDto(cart);
        return Response<CartResponseDto>.Success(dto, 200);
    }

    public async Task<Response<bool>> RemoveAsync(Guid id, Guid userId)
    {
        var cart = await _unitOfWork.GetRepository<Cart>()
            .GetSingleAsync(
                expression: x => x.Id == id && x.UserId == userId,
                include: q => q.Include(c => c.Items));

        if (cart == null)
            return Response<bool>.Fail("Sepet bulunamadı veya yetkiniz yok.", 404);

        var itemRepo = _unitOfWork.GetRepository<CartItem>();
        foreach (var item in cart.Items.ToList())
            itemRepo.Delete(item);

        cart.TotalAmount = 0;
        _unitOfWork.GetRepository<Cart>().Update(cart);
            
        await _unitOfWork.SaveChangesAsync();
        return Response<bool>.Success(true, 200);
    }
}