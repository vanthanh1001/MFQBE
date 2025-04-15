using System.Threading.Tasks;
using Models.DTOs;

namespace Services.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponseDto> CreatePaymentLinkAsync(CreatePaymentRequest request);
        Task<PaymentResponseDto> GetPaymentStatusAsync(string orderCode);
    }
}