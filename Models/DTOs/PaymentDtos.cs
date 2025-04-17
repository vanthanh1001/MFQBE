using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Models.DTOs
{
    public class CreatePaymentRequest
    {
        [Required]
        [Range(1000, long.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 1000")]
        public long Amount { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [Url(ErrorMessage = "ReturnUrl phải là một URL hợp lệ")]
        public string ReturnUrl { get; set; }

        [Required]
        [Url(ErrorMessage = "CancelUrl phải là một URL hợp lệ")]
        public string CancelUrl { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Tên người mua không được vượt quá 100 ký tự")]
        public string BuyerName { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string BuyerEmail { get; set; }

        [Required]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [RegularExpression(@"^(0|\+84)[3|5|7|8|9][0-9]{8}$", ErrorMessage = "Số điện thoại không đúng định dạng")]
        public string BuyerPhone { get; set; }

        [Required]
        [Range(60, 86400, ErrorMessage = "Thời gian hết hạn phải từ 60 giây đến 24 giờ")]
        public int ExpiredAt { get; set; }
    }

    public class PaymentResponseDto
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("desc")]
        public string Desc { get; set; }

        [JsonPropertyName("data")]
        public PaymentDataDto Data { get; set; }

        [JsonPropertyName("signature")]
        public string Signature { get; set; }
    }

    public class PaymentDataDto
    {
        [JsonPropertyName("bin")]
        public string Bin { get; set; }

        [JsonPropertyName("accountNumber")]
        public string AccountNumber { get; set; }

        [JsonPropertyName("accountName")]
        public string AccountName { get; set; }

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("orderCode")]
        public long OrderCode { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("paymentLinkId")]
        public string PaymentLinkId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("checkoutUrl")]
        public string CheckoutUrl { get; set; }

        [JsonPropertyName("qrCode")]
        public string QrCode { get; set; }

        [JsonPropertyName("expiredAt")]
        public long ExpiredAt { get; set; }
    }

    public class PaymentStatusRequest
    {
        [Required]
        public string OrderCode { get; set; }
    }
} 