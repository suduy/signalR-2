using Binance.Net.Clients;
using Binance.Net.Enums;
using CryptoExchange.Net.Authentication;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace BinanceRSIAPI.Utils
{
    public class Candle
    {
        public DateTime OpenTime { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
    }
    public static class BinanceApi
    {
        private static readonly HttpClient teleClient = new HttpClient();
        private static List<String> openOrder = new List<string>();
        public static async Task<List<Candle>> GetKlinesAsync(string symbol = "BTCUSDT", string interval = "15m", int limit = 100)
        {
            var url = $"https://api.binance.com/api/v3/klines?symbol={symbol.ToUpper()}&interval={interval}&limit={limit}";
            var response = await teleClient.GetStringAsync(url);

            var json = JsonDocument.Parse(response).RootElement;
            var candles = new List<Candle>();

            foreach (var item in json.EnumerateArray())
            {
                candles.Add(new Candle
                {
                    OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(item[0].GetInt64()).UtcDateTime,
                    Open = decimal.Parse(item[1].GetString(), CultureInfo.InvariantCulture),
                    High = decimal.Parse(item[2].GetString(), CultureInfo.InvariantCulture),
                    Low = decimal.Parse(item[3].GetString(), CultureInfo.InvariantCulture),
                    Close = decimal.Parse(item[4].GetString(), CultureInfo.InvariantCulture),
                    Volume = decimal.Parse(item[5].GetString(), CultureInfo.InvariantCulture)
                });
            }

            return candles;
        }
        public static async Task<string> PlaceFuturesOrderWithTpSlPriceAsync(string symbol, string direction, decimal marginAmount = 1m, int leverage = 20)
        {
            // LƯU Ý: Tôi đã bỏ tpPrice và slPrice khỏi tham số truyền vào, vì đằng nào bạn cũng tự tính bên trong hàm.

            var binanceClient = new BinanceRestClient(options =>
            {
                options.ApiCredentials = new ApiCredentials("6Uhdhc6PHqeRuIE7CD3NobFcBDnG0bPa9AqnPLdq6DVxZCClKzkAYE8gwpyzRD8A", "4ViFm99MfDmv4rdlhmbKkY2c2kS49wlve3AoPNCqgRuMmQxTQsGdxTm4unhyqnmt");
            });

            // Step 0: Đổi Margin Type
            var marginTypeResult = await binanceClient.UsdFuturesApi.Account.ChangeMarginTypeAsync(symbol, FuturesMarginType.Isolated);
            if (!marginTypeResult.Success && marginTypeResult.Error?.Code != -4046) // -4046 = already isolated
            {
                Console.WriteLine($"Failed to set margin type: {marginTypeResult.Error}");
                return "false";
            }

            // Step 1: Set leverage
            var leverageResult = await binanceClient.UsdFuturesApi.Account.ChangeInitialLeverageAsync(symbol, leverage);
            if (!leverageResult.Success)
            {
                Console.WriteLine($"Failed to set leverage: {leverageResult.Error}");
                return "false";
            }

            // Lấy thông tin cấu hình làm tròn (Filter) của Coin trước
            var exchangeInfoResult = await binanceClient.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync();
            if (!exchangeInfoResult.Success) return "false";

            var symbolInfo = exchangeInfoResult.Data.Symbols.FirstOrDefault(s => s.Name == symbol);
            if (symbolInfo == null) return "false";

            var lotSizeFilter = symbolInfo.LotSizeFilter;
            var priceFilter = symbolInfo.PriceFilter;

            // Step 2: Lấy giá hiện tại
            var priceResult = await binanceClient.UsdFuturesApi.ExchangeData.GetPriceAsync(symbol);
            if (!priceResult.Success) return "false";

            decimal currentPrice = priceResult.Data.Price;

            // CÔNG THỨC TÍNH SL / TP
            decimal pnlSlTarget = 0.25m; // Mục tiêu PnL 25%
            decimal pnlTpTarget = 0.25m; // Mục tiêu PnL 25%
            decimal deltaSl = pnlSlTarget / leverage;
            decimal deltaTp = pnlTpTarget / leverage;

            decimal tpPrice, slPrice;

            if (direction.ToLower() == "buy")
            {
                tpPrice = currentPrice * (1 + deltaTp);
                slPrice = currentPrice * (1 - deltaSl);
            }
            else // Bán (Short)
            {
                tpPrice = currentPrice * (1 - deltaTp);
                slPrice = currentPrice * (1 + deltaSl);
            }

            // LÀM TRÒN GIÁ TP/SL THEO LUẬT CỦA SÀN (Tránh lỗi Precision)
            if (priceFilter != null)
            {
                tpPrice = Math.Floor(tpPrice / priceFilter.TickSize) * priceFilter.TickSize;
                slPrice = Math.Floor(slPrice / priceFilter.TickSize) * priceFilter.TickSize;
            }

            // Step 3: Tính toán Quantity và Làm tròn
            decimal positionValue = marginAmount * leverage;
            decimal quantity = positionValue / currentPrice;

            if (lotSizeFilter != null)
            {
                quantity = Math.Floor(quantity / lotSizeFilter.StepSize) * lotSizeFilter.StepSize;

                // Kiểm tra an toàn: Nếu tiền quá nhỏ không đủ mua MinQuantity của sàn
                if (quantity < lotSizeFilter.MinQuantity)
                {
                    Console.WriteLine($"[CẢNH BÁO] Số lượng tính toán ({quantity}) nhỏ hơn mức tối thiểu sàn cho phép ({lotSizeFilter.MinQuantity}). Bỏ qua lệnh.");
                    return "false";
                }

                quantity = Math.Max(lotSizeFilter.MinQuantity, Math.Min(quantity, lotSizeFilter.MaxQuantity));
            }

            var orderSide = direction.ToLower() == "buy" ? OrderSide.Buy : OrderSide.Sell;
            var oppositeSide = direction.ToLower() == "buy" ? OrderSide.Sell : OrderSide.Buy;

            // Step 4: Khớp lệnh Market (Vào lệnh)
            var marketOrderResult = await binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol: symbol,
                side: orderSide,
                type: FuturesOrderType.Market,
                quantity: quantity
            );

            if (!marketOrderResult.Success)
            {
                Console.WriteLine($"Failed to place market order: {marketOrderResult.Error}");
                return "false";
            }

            Console.WriteLine($"Market {direction.ToUpper()} order placed for {symbol} | Quantity: {quantity} | Entry: {currentPrice}");

            // Step 5: Đặt lệnh Take Profit
            var tpMarketOrderResult = await binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol: symbol,
                side: oppositeSide,
                type: FuturesOrderType.TakeProfitMarket,
                stopPrice: tpPrice,
                quantity: quantity,
                reduceOnly: true
            );

            if (!tpMarketOrderResult.Success)
            {
                Console.WriteLine($"Failed to place TP order: {tpMarketOrderResult.Error}");
            }
            else
            {
                Console.WriteLine($"TP order placed successfully at {tpPrice}!");
            }

            // Step 6: Đặt lệnh Stop Loss
            var slOrderResult = await binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol: symbol,
                side: oppositeSide,
                type: FuturesOrderType.StopMarket,
                stopPrice: slPrice,
                quantity: quantity,
                reduceOnly: true
            );

            if (!slOrderResult.Success)
            {
                Console.WriteLine($"Failed to place Stop Loss order: {slOrderResult.Error}");

                // Gửi Telegram cảnh báo nếu SL fail (Lưu ý: Đã dùng biến teleClient static)
                var message = $"[CẢNH BÁO] Đã vào lệnh {symbol} nhưng đặt SL THẤT BẠI: {slOrderResult.Error}";
                var botToken = "8111665564:AAEEe3_pDSrbBt6Fnx1l_kMdf1dhC6oRL10";
                var channelUsername = "@ai_signal_notification";

                try
                {
                    var url = $"https://api.telegram.org/bot{botToken}/sendMessage?chat_id={channelUsername}&text={Uri.EscapeDataString(message)}";
                    await teleClient.GetAsync(url);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi không gửi được Telegram: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Stop Loss order placed successfully at {slPrice}!");
            }

            return "true";
        }
    }
}
