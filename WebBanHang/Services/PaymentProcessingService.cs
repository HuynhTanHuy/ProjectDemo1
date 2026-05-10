using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Services.PaymentGateway;

namespace WebBanHang.Services
{
    public class PaymentProcessingService : IPaymentProcessingService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPaymentSignatureService _signature;
        private readonly ILogger<PaymentProcessingService> _logger;

        public PaymentProcessingService(
            ApplicationDbContext db,
            IPaymentSignatureService signature,
            ILogger<PaymentProcessingService> logger)
        {
            _db = db;
            _signature = signature;
            _logger = logger;
        }

        public async Task<PaymentCallbackResult> ProcessSimulatedReturnAsync(
            SimulatedPaymentReturnDto dto,
            CancellationToken cancellationToken = default)
        {
            var normalizedResult = dto.Result.Trim().ToLowerInvariant();

            var payment = await _db.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o.OrderDetails)
                .FirstOrDefaultAsync(p => p.Id == dto.PaymentId, cancellationToken);

            if (payment == null || payment.Order == null)
            {
                return new PaymentCallbackResult { Code = "not_found", Message = "Không tìm thấy giao dịch." };
            }

            if (payment.OrderId != dto.OrderId)
            {
                _logger.LogWarning("Callback orderId không khớp payment {PaymentId}.", dto.PaymentId);
                return new PaymentCallbackResult
                {
                    Code = "order_mismatch",
                    OrderId = payment.OrderId,
                    PaymentId = payment.Id,
                    Message = "Dữ liệu đơn hàng không hợp lệ."
                };
            }

            if (payment.Amount != dto.Amount)
            {
                await LogTxAsync(
                    payment.Id,
                    "AmountMismatch",
                    dto.RawQueryForAudit,
                    cancellationToken);
                return new PaymentCallbackResult
                {
                    Code = "amount_mismatch",
                    OrderId = payment.OrderId,
                    PaymentId = payment.Id
                };
            }

            await LogTxAsync(
                payment.Id,
                "CallbackReceived",
                dto.RawQueryForAudit,
                cancellationToken);

            if (!_signature.Verify(dto.PaymentId, dto.OrderId, dto.Amount, normalizedResult, dto.Signature))
            {
                await LogTxAsync(
                    payment.Id,
                    "InvalidSignature",
                    dto.RawQueryForAudit,
                    cancellationToken);
                _logger.LogWarning("Chữ ký callback không hợp lệ. Payment {PaymentId}", dto.PaymentId);
                return new PaymentCallbackResult
                {
                    Code = "invalid_signature",
                    OrderId = payment.OrderId,
                    PaymentId = payment.Id
                };
            }

            if (payment.Status == PaymentStatus.Success)
            {
                await LogTxAsync(
                    payment.Id,
                    "IdempotentReplay",
                    "success replay",
                    cancellationToken);
                return new PaymentCallbackResult
                {
                    Code = "already_success",
                    OrderId = payment.OrderId,
                    PaymentId = payment.Id
                };
            }

            if (normalizedResult == "success")
            {
                if (payment.Status is PaymentStatus.Cancelled or PaymentStatus.Failed)
                {
                    await LogTxAsync(
                        payment.Id,
                        "LateSuccessIgnored",
                        dto.RawQueryForAudit,
                        cancellationToken);
                    return new PaymentCallbackResult
                    {
                        Code = "terminal_conflict",
                        OrderId = payment.OrderId,
                        PaymentId = payment.Id,
                        Message = "Giao dịch đã kết thúc, không thể xác nhận thanh toán."
                    };
                }

                return await CompleteSuccessAsync(payment, cancellationToken);
            }

            if (normalizedResult == "failed")
            {
                return await FailPaymentAsync(payment, PaymentStatus.Failed, "Thanh toán thất bại.", cancellationToken);
            }

            if (normalizedResult == "cancelled")
            {
                return await FailPaymentAsync(payment, PaymentStatus.Cancelled, "Người dùng hủy thanh toán.", cancellationToken);
            }

            if (normalizedResult == "expired")
            {
                return await FailPaymentAsync(payment, PaymentStatus.Expired, "Giao dịch hết hạn.", cancellationToken);
            }

            return new PaymentCallbackResult
            {
                Code = "unknown_result",
                OrderId = payment.OrderId,
                PaymentId = payment.Id
            };
        }

        private async Task<PaymentCallbackResult> CompleteSuccessAsync(
            Payment payment,
            CancellationToken cancellationToken)
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var order = await _db.Orders
                        .Include(o => o.OrderDetails)
                        .FirstAsync(o => o.Id == payment.OrderId, cancellationToken);

                    foreach (var line in order.OrderDetails)
                    {
                        var product = await _db.Products
                            .FirstOrDefaultAsync(p => p.Id == line.ProductId, cancellationToken);
                        if (product == null || product.Stock < line.Quantity)
                        {
                            payment.Status = PaymentStatus.Failed;
                            payment.FailureReason = "Tồn kho không đủ khi hoàn tất thanh toán.";
                            payment.CompletedAtUtc = DateTime.UtcNow;
                            order.OrderStatus = OrderStatus.Pending;
                            await _db.SaveChangesAsync(cancellationToken);
                            await tx.CommitAsync(cancellationToken);
                            await LogTxAsync(
                                payment.Id,
                                "StockInsufficientOnPay",
                                null,
                                cancellationToken);
                            _logger.LogError(
                                "Thanh toán thành công nhưng thiếu tồn kho. Order {OrderId}",
                                order.Id);
                            return new PaymentCallbackResult
                            {
                                Code = "stock_error",
                                OrderId = order.Id,
                                PaymentId = payment.Id,
                                Message = "Không đủ hàng để hoàn tất đơn. Đơn vẫn ở trạng thái chờ xử lý."
                            };
                        }

                        product.Stock -= line.Quantity;
                    }

                    payment.Status = PaymentStatus.Success;
                    payment.CompletedAtUtc = DateTime.UtcNow;
                    payment.FailureReason = null;
                    order.OrderStatus = OrderStatus.Paid;

                    await _db.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);

                    await LogTxAsync(payment.Id, "PaymentSuccessCommitted", null, cancellationToken);
                    _logger.LogInformation("Thanh toán thành công Order {OrderId} Payment {PaymentId}", order.Id, payment.Id);

                    return new PaymentCallbackResult
                    {
                        Code = "success",
                        OrderId = order.Id,
                        PaymentId = payment.Id
                    };
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Lỗi khi commit thanh toán.");
                    return new PaymentCallbackResult
                    {
                        Code = "server_error",
                        OrderId = payment.OrderId,
                        PaymentId = payment.Id
                    };
                }
            });
        }

        private async Task<PaymentCallbackResult> FailPaymentAsync(
            Payment payment,
            PaymentStatus status,
            string reason,
            CancellationToken cancellationToken)
        {
            if (payment.Status == PaymentStatus.Success)
            {
                await LogTxAsync(payment.Id, "FailureIgnoredAfterSuccess", reason, cancellationToken);
                return new PaymentCallbackResult
                {
                    Code = "already_success",
                    OrderId = payment.OrderId,
                    PaymentId = payment.Id
                };
            }

            payment.Status = status;
            payment.CompletedAtUtc = DateTime.UtcNow;
            payment.FailureReason = reason;
            await _db.SaveChangesAsync(cancellationToken);
            await LogTxAsync(payment.Id, "PaymentFailedOrCancelled", reason, cancellationToken);

            return new PaymentCallbackResult
            {
                Code = status == PaymentStatus.Cancelled ? "cancelled" : status == PaymentStatus.Expired ? "expired" : "failed",
                OrderId = payment.OrderId,
                PaymentId = payment.Id
            };
        }

        private async Task LogTxAsync(
            int paymentId,
            string eventType,
            string? payload,
            CancellationToken cancellationToken)
        {
            _db.PaymentTransactions.Add(new PaymentTransaction
            {
                PaymentId = paymentId,
                EventType = eventType,
                PayloadSnapshot = payload != null && payload.Length > 3800 ? payload[..3800] : payload,
                CreatedAtUtc = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
