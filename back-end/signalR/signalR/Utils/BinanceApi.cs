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
        private static readonly HttpClient client = new HttpClient();
        private static List<String> openOrder = new List<string>();
        public static async Task<List<Candle>> GetKlinesAsync(string symbol = "BTCUSDT", string interval = "15m", int limit = 100)
        {
            var url = $"https://api.binance.com/api/v3/klines?symbol={symbol.ToUpper()}&interval={interval}&limit={limit}";
            var response = await client.GetStringAsync(url);

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
        public static async Task<string> PlaceFuturesOrderWithTpSlPriceAsync(string symbol, string direction, decimal tpPrice, decimal slPrice, decimal entry, decimal marginAmount = 1m, int leverage = 20)
        {
           // openOrder.Add(symbol);
            var binanceClient = new BinanceRestClient(options =>
            {
                options.ApiCredentials = new ApiCredentials("6Uhdhc6PHqeRuIE7CD3NobFcBDnG0bPa9AqnPLdq6DVxZCClKzkAYE8gwpyzRD8A", "4ViFm99MfDmv4rdlhmbKkY2c2kS49wlve3AoPNCqgRuMmQxTQsGdxTm4unhyqnmt");
            });
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

            // Step 2: Get current market price
            var priceResult = await binanceClient.UsdFuturesApi.ExchangeData.GetPriceAsync(symbol);
            if (!priceResult.Success)
            {
                Console.WriteLine($"Failed to get price: {priceResult.Error}");
                return "false";
            }
            decimal currentPrice = priceResult.Data.Price;
            decimal pnlSlTarget = 0.25m; // 50% PnL
            decimal deltaSl = pnlSlTarget / leverage;
            decimal pnTpTarget = 0.25m; // 50% PnL
            decimal deltaTp = pnlSlTarget / leverage;
            // TP = Entry Price + (Entry Price * delta) (nếu mua) hoặc ngược lại (nếu bán)
            if (direction.ToLower() == "buy")
            {
                tpPrice = currentPrice * (1 + deltaSl); // optional: đặt SL đối xứng

                slPrice = currentPrice * (1- deltaSl); // optional: đặt SL đối xứng
            }
            else
            {
                tpPrice = currentPrice * (1 - deltaSl); // optional: đặt SL đối xứng
                slPrice = currentPrice * (1 + deltaSl); // optional: đặt SL đối xứng
            }
            // Step 3: Calculate quantity
            decimal positionValue = marginAmount * leverage;
            decimal quantity = positionValue / currentPrice;

            // Step 4: Adjust precision
            var exchangeInfoResult = await binanceClient.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync();
            var symbolInfo = exchangeInfoResult.Data.Symbols.FirstOrDefault(s => s.Name == symbol);
            var lotSizeFilter = symbolInfo?.LotSizeFilter;

            if (lotSizeFilter != null)
            {
                quantity = Math.Floor(quantity / lotSizeFilter.StepSize) * lotSizeFilter.StepSize;
                quantity = Math.Max(lotSizeFilter.MinQuantity, Math.Min(quantity, lotSizeFilter.MaxQuantity));
            }

            var orderSide = direction.ToLower() == "buy" ? OrderSide.Buy : OrderSide.Sell;
            var oppositeSide = direction.ToLower() == "buy" ? OrderSide.Sell : OrderSide.Buy;

            // Step 5: Place initial Market Order
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

            // B8: Tính activation price cho trailing stop (nếu có)
            decimal? activationPrice = null;
            if (direction.ToLower() == "buy")
                activationPrice = currentPrice * (1 + 1.3m / 100m);
            else
                activationPrice = currentPrice * (1 - 1.3m / 100m);
            var priceFilter = symbolInfo?.PriceFilter;

            if (priceFilter != null && activationPrice.HasValue)
                activationPrice = Math.Floor(activationPrice.Value / priceFilter.TickSize) * priceFilter.TickSize;

            // B9: Đặt lệnh Trailing Stop
            //var trailingStopOrderResult = await binanceClient.UsdFuturesApi.Trading.PlaceConditionalOrderAsync(
            //    symbol: symbol,
            //    side: oppositeSide,
            //    type: ConditionalOrderType.TrailingStopMarket,
            //    quantity: quantity,
            //    activationPrice: activationPrice,
            //    callbackRate: 0.8m,
            //    workingType: WorkingType.Mark,
            //    reduceOnly: true
            //);

            var tpMarketOrderResult = await binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol: symbol,
                side: oppositeSide,
                type: FuturesOrderType.TakeProfitMarket,
                stopPrice: tpPrice,                             // Dùng stopPrice làm giá kích hoạt
                quantity: quantity,
                reduceOnly: true
            );

            if (!tpMarketOrderResult.Success)
            {
                Console.WriteLine($"Failed to place Trailing Stop order: {tpMarketOrderResult.Error}");
            }
            else
            {
                Console.WriteLine("Trailing Stop order placed successfully!");
            }

            Console.WriteLine("TP and Trailing Stop orders placed successfully!");


            Console.WriteLine($"Market {direction.ToUpper()} order placed for {symbol} | Quantity: {quantity} | Entry: {currentPrice}");
            
            var priceFilter1 = symbolInfo?.PriceFilter;
            if (priceFilter1 != null)
            {
                slPrice = Math.Floor(slPrice / priceFilter1.TickSize) * priceFilter1.TickSize;
            }
            var slOrderResult = await binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol: symbol,
                side: oppositeSide,
                type: FuturesOrderType.StopMarket,
                stopPrice: slPrice,  // Tham số này có sẵn ở đây
                quantity: quantity,
                reduceOnly: true
            );

            if (!slOrderResult.Success)
            {
                Console.WriteLine($"Failed to place Stop loss order: {slOrderResult.Error}");
            }

            if (!slOrderResult.Success)
            {
                {
                    var message1 = $"Failed to place TP order: {slOrderResult.Error} {symbol}";
                    var botToken1 = "8111665564:AAEEe3_pDSrbBt6Fnx1l_kMdf1dhC6oRL10";
                    var channelUsername1 = "@ai_signal_notification"; // include the @

                    using (HttpClient client = new HttpClient())
                    {
                        var url = $"https://api.telegram.org/bot{botToken1}/sendMessage?chat_id={channelUsername1}&text={Uri.EscapeDataString(message1)}";
                        var response = await client.GetAsync(url);
                        var result = await response.Content.ReadAsStringAsync();

                        Console.WriteLine(result); // Shows Telegram's API response
                    }
                }
            }

            return "true";
        }
    }
}
