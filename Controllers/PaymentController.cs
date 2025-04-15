using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.DTOs;
using Services.Interfaces;

namespace Controllers
{
    // [Authorize] // Tạm thời bỏ authorize để test
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("create")]
        public async Task<ActionResult<PaymentResponseDto>> CreatePayment([FromBody] CreatePaymentRequest request)
        {
            try
            {
                Console.WriteLine("Received payment request: " + System.Text.Json.JsonSerializer.Serialize(request));
                var response = await _paymentService.CreatePaymentLinkAsync(request);
                Console.WriteLine("Payment response: " + System.Text.Json.JsonSerializer.Serialize(response));
                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating payment: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("status/{orderCode}")]
        public async Task<ActionResult<PaymentResponseDto>> GetPaymentStatus([FromRoute] string orderCode)
        {
            try
            {
                var response = await _paymentService.GetPaymentStatusAsync(orderCode);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}