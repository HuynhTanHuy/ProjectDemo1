using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebBanHang.Models;
using WebBanHang.Options;
using WebBanHang.Services.Results;

namespace WebBanHang.Services
{
    public class OrderCheckoutService : IOrderCheckoutService
    {
        private readonly ApplicationDbContext _db;
        private readonly OrderCheckoutOptions _checkoutOptions;
        private readonly ILogger<OrderCheckoutService> _logger;

        public OrderCheckoutService(
            ApplicationDbContext db,
            IOptions<OrderCheckoutOptions> checkoutOptions,
            ILogger<OrderCheckoutService> logger)
        {
            _db = db;
            _checkoutOptions = checkoutOptions.Value;
            _logger = logger;
        }

        public async Task<ServiceResult<(int OrderId, int PaymentId)>> CreatePendingOrderWithPaymentAsync(
            string userId,
            ShoppingCart cart,
            string shippingAddress,
            string notes,
            CancellationToken cancellationToken = default)
        {
            if (cart?.Items == null || cart.Items.Count == 0)
            {
                return ServiceResult<(int OrderId, int PaymentId)>.Fail("empty_cart", "Giỏ hàng trống.");
            }

            if (string.IsNullOrWhiteSpace(shippingAddress))
            {
                return ServiceResult<(int OrderId, int PaymentId)>.Fail(
                    "invalid_address",
                    "Địa chỉ giao hàng không hợp lệ.");
            }

            var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _db.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            foreach (var item in cart.Items)
            {
                if (!products.TryGetValue(item.ProductId, out var p))
                {
                    return ServiceResult<(int OrderId, int PaymentId)>.Fail(
                        "product_missing",
                        $"Sản phẩm {item.ProductId} không còn tồn tại.");
                }

                if (p.Stock < item.Quantity)
                {
                    return ServiceResult<(int OrderId, int PaymentId)>.Fail(
                        "insufficient_stock",
                        $"Sản phẩm \"{p.Name}\" không đủ tồn kho.");
                }
            }

            var total = cart.Items.Sum(i => i.Price * i.Quantity) + _checkoutOptions.ShippingCost;

            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var order = new Order
                    {
                        UserId = userId,
                        OrderDate = DateTime.UtcNow,
                        TotalPrice = total,
                        ShippingAddress = shippingAddress.Trim(),
                        Notes = notes?.Trim() ?? string.Empty,
                        OrderStatus = OrderStatus.Pending,
                        OrderDetails = cart.Items.Select(i => new OrderDetail
                        {
                            ProductId = i.ProductId,
                            Quantity = i.Quantity,
                            Price = i.Price
                        }).ToList()
                    };

                    _db.Orders.Add(order);
                    await _db.SaveChangesAsync(cancellationToken);

                    var txCode = BuildTransactionCode(order.Id);
                    var payment = new Payment
                    {
                        OrderId = order.Id,
                        Amount = total,
                        Status = PaymentStatus.Pending,
                        PaymentMethod = PaymentMethod.SimulatedGateway,
                        TransactionCode = txCode,
                        GatewayProvider = "Simulated",
                        IdempotencyKey = Guid.NewGuid().ToString("N"),
                        CreatedAtUtc = DateTime.UtcNow
                    };

                    _db.Payments.Add(payment);
                    await _db.SaveChangesAsync(cancellationToken);

                    await tx.CommitAsync(cancellationToken);
                    _logger.LogInformation("Tạo đơn {OrderId} và thanh toán {PaymentId} trạng thái Pending.", order.Id, payment.Id);
                    return ServiceResult<(int OrderId, int PaymentId)>.Ok((order.Id, payment.Id));
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Lỗi tạo đơn chờ thanh toán.");
                    return ServiceResult<(int OrderId, int PaymentId)>.Fail("server_error", "Không thể tạo đơn hàng.");
                }
            });
        }

        public async Task<ServiceResult<int>> EnsurePendingPaymentAsync(
            string userId,
            int orderId,
            CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, cancellationToken);

            if (order == null)
            {
                return ServiceResult<int>.Fail("not_found", "Không tìm thấy đơn hàng.");
            }

            if (order.OrderStatus != OrderStatus.Pending)
            {
                return ServiceResult<int>.Fail("invalid_state", "Đơn không ở trạng thái chờ thanh toán.");
            }

            var existing = await _db.Payments
                .Where(p => p.OrderId == orderId && p.Status == PaymentStatus.Pending)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing != null)
            {
                return ServiceResult<int>.Ok(existing.Id);
            }

            var payment = new Payment
            {
                OrderId = order.Id,
                Amount = order.TotalPrice,
                Status = PaymentStatus.Pending,
                PaymentMethod = PaymentMethod.SimulatedGateway,
                TransactionCode = BuildTransactionCode(order.Id),
                GatewayProvider = "Simulated",
                IdempotencyKey = Guid.NewGuid().ToString("N"),
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Tạo thanh toán mới {PaymentId} cho đơn {OrderId}.", payment.Id, order.Id);
            return ServiceResult<int>.Ok(payment.Id);
        }

        private static string BuildTransactionCode(int orderId)
        {
            var suffix = Guid.NewGuid().ToString("N");
            var code = $"SIM-O{orderId:D}-{suffix}";
            return code.Length <= 64 ? code : code[..64];
        }
    }
}
