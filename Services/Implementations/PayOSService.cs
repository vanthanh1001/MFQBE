using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Models.DTOs;
using Services.Interfaces;
using System.Security.Cryptography;

namespace Services.Implementations
{
    public class PayOSService : IPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;
        private readonly string _clientId;
        private readonly string _apiKey;
        private readonly string _checksum;
        private readonly JsonSerializerOptions _jsonOptions;

        public PayOSService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _baseUrl = configuration["PayOS:BaseUrl"];
            _clientId = configuration["PayOS:ClientId"];
            _apiKey = configuration["PayOS:ApiKey"];
            _checksum = configuration["PayOS:Checksum"];

            _httpClient.DefaultRequestHeaders.Add("x-client-id", _clientId);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        private string GenerateSignature(long orderCode, long amount, string description, string cancelUrl, string returnUrl)
        {
            // Sort theo alphabet: amount, cancelUrl, description, orderCode, returnUrl
            var data = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
            
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_checksum));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        // Helper method để sinh mã đơn hàng ngẫu nhiên
        private static long GenerateOrderCode()
        {
            // Giới hạn của PayOS là 9007199254740991 (2^53 - 1)
            // Để an toàn, chúng ta sẽ chỉ dùng 12 chữ số
            Random random = new Random();
            
            // Lấy 6 chữ số cuối của timestamp
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 1000000;
            // Sinh 6 số random
            int randomNum = random.Next(100000, 999999);
            
            // Kết hợp thành số 12 chữ số: 6 chữ số timestamp + 6 chữ số random
            return long.Parse($"{timestamp}{randomNum}");
        }

        public async Task<PaymentResponseDto> CreatePaymentLinkAsync(CreatePaymentRequest request)
        {
            try 
            {
                var endpoint = $"{_baseUrl}/v2/payment-requests";
                var orderCode = GenerateOrderCode();

                // Tính timestamp hết hạn từ số giây
                var expiredAt = DateTimeOffset.UtcNow.AddSeconds(request.ExpiredAt).ToUnixTimeSeconds();

                // Tạo signature
                var signature = GenerateSignature(
                    orderCode,
                    request.Amount,
                    request.Description,
                    request.CancelUrl,
                    request.ReturnUrl
                );

                // Tạo object mới với orderCode là số nguyên
                var paymentRequest = new
                {
                    orderCode = orderCode,
                    amount = request.Amount,
                    description = request.Description,
                    returnUrl = request.ReturnUrl,
                    cancelUrl = request.CancelUrl,
                    buyerName = request.BuyerName,
                    buyerEmail = request.BuyerEmail,
                    buyerPhone = request.BuyerPhone,
                    expiredAt = expiredAt,
                    signature = signature
                };

                Console.WriteLine($"Sending request to PayOS: {JsonSerializer.Serialize(paymentRequest)}");
                
                var content = new StringContent(
                    JsonSerializer.Serialize(paymentRequest, new JsonSerializerOptions 
                    { 
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    }), 
                    Encoding.UTF8, 
                    "application/json"
                );
                
                var response = await _httpClient.PostAsync(endpoint, content);
                var responseString = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"PayOS response status: {response.StatusCode}");
                Console.WriteLine($"PayOS response: {responseString}");
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"PayOS API error: {responseString}");
                }

                var paymentResponse = JsonSerializer.Deserialize<PaymentResponseDto>(responseString, _jsonOptions);
                if (paymentResponse == null)
                {
                    throw new Exception("Failed to deserialize PayOS response");
                }

                return paymentResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PayOSService: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<PaymentResponseDto> GetPaymentStatusAsync(string orderCode)
        {
            var endpoint = $"{_baseUrl}/v2/payment-requests/{orderCode}";
            var response = await _httpClient.GetAsync(endpoint);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"PayOS API error: {responseString}");
            }

            var paymentResponse = JsonSerializer.Deserialize<PaymentResponseDto>(responseString, _jsonOptions);
            if (paymentResponse == null)
            {
                throw new Exception("Failed to deserialize PayOS response");
            }

            return paymentResponse;
        }
    }
}