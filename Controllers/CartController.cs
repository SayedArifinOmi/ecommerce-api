using AuthApi.Contracts;
using AuthApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly CartService _cartService;
    public CartController(CartService cartService) => _cartService = cartService;

    private int GetUserId() => int.Parse(User.Claims.First(c => c.Type == "uid").Value);

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var items = await _cartService.GetCartAsync(GetUserId());
        return Ok(new CartResponseDto(items));
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] CartItemDto dto)
    {
        try
        {
            await _cartService.AddToCartAsync(GetUserId(), dto.ProductId, dto.Quantity);
            return Ok(new { message = "Added to cart" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] CartItemDto dto)
    {
        await _cartService.UpdateQuantityAsync(GetUserId(), dto.ProductId, dto.Quantity);
        return Ok();
    }

    [HttpDelete("remove/{productId}")]
    public async Task<IActionResult> Remove(int productId)
    {
        await _cartService.RemoveAsync(GetUserId(), productId);
        return NoContent();
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> Clear()
    {
        await _cartService.ClearAsync(GetUserId());
        return NoContent();
    }
}
