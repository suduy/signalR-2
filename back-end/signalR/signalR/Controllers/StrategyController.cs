using Binance.Net.Clients;
using BinanceRSIAPI.Utils;
using CryptoExchange.Net.Authentication;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using RestSharp;
using Skender.Stock.Indicators;
using System.Collections;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BinanceRSIAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StrategyController : ControllerBase
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> LastSuccessfulOrders = new();

        [HttpGet]
        public async Task<IActionResult> GET()
        {
            Console.WriteLine("Bot status is: " + GlobalVariables.IsWorking);
            if (GlobalVariables.IsWorking == false)
            {
                Console.WriteLine("Not Working");
                return Ok();
            }
            Console.WriteLine("Working");

            var openPositionsCount = 0;
            // Thêm đoạn kiểm tra tại đây
            var binanceClient = new BinanceRestClient(options =>
            {
                options.ApiCredentials = new ApiCredentials("6Uhdhc6PHqeRuIE7CD3NobFcBDnG0bPa9AqnPLdq6DVxZCClKzkAYE8gwpyzRD8A", "4ViFm99MfDmv4rdlhmbKkY2c2kS49wlve3AoPNCqgRuMmQxTQsGdxTm4unhyqnmt");
            });

            var exchangeInfoResult = await binanceClient.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync();
            if (!exchangeInfoResult.Success)
            {
                Console.WriteLine($"Không lấy được danh sách coin từ Futures: {exchangeInfoResult.Error}");
                return Ok();
            }

            string[] activeFuturesSymbols = new string[]
           {
                "GOATUSDT", "MOODENGUSDT", "SAFEUSDT", "SANTOSUSDT", "COWUSDT", "CETUSUSDT", "1000000MOGUSDT", "GRASSUSDT", "DRIFTUSDT", "ACTUSDT", "PNUTUSDT", "HIPPOUSDT", "DEGENUSDT", "BANUSDT", "AKTUSDT", "SCRTUSDT", "1000CHEEMSUSDT", "1000WHYUSDT", "THEUSDT", "MORPHOUSDT", "CHILLGUYUSDT", "KAIAUSDT", "AEROUSDT", "ACXUSDT", "ORCAUSDT", "MOVEUSDT", "RAYSOLUSDT", "KOMAUSDT", "VIRTUALUSDT", "SPXUSDT", "MEUSDT", "AVAUSDT", "DEGOUSDT", "VELODROMEUSDT", "MOCAUSDT", "VANAUSDT", "PENGUUSDT", "LUMIAUSDT", "USUALUSDT", "AIXBTUSDT", "FARTCOINUSDT", "KMNOUSDT", "CGPTUSDT", "HIVEUSDT", "DEXEUSDT", "PHAUSDT", "GRIFFAINUSDT", "ZEREBROUSDT", "BIOUSDT", "COOKIEUSDT", "ALCHUSDT", "SWARMSUSDT", "SONICUSDT", "DUSDT", "PROMUSDT", "SUSDT", "SOLVUSDT", "ARCUSDT", "AVAAIUSDT", "TRUMPUSDT", "MELANIAUSDT", "VTHOUSDT", "ANIMEUSDT", "VINEUSDT", "PIPPINUSDT", "VVVUSDT", "BERAUSDT", "TSTUSDT", "LAYERUSDT", "HEIUSDT", "B3USDT", "IPUSDT", "GPSUSDT", "SHELLUSDT", "KAITOUSDT", "REDUSDT", "VICUSDT", "EPICUSDT", "BMTUSDT", "MUBARAKUSDT", "FORMUSDT", "TUTUSDT", "BROCCOLI714USDT", "BROCCOLIF3BUSDT", "SIRENUSDT", "BANANAS31USDT", "BRUSDT", "PLUMEUSDT", "NILUSDT", "PARTIUSDT", "JELLYJELLYUSDT", "MAVIAUSDT", "PAXGUSDT", "WALUSDT", "FUNUSDT", "MLNUSDT", "GUNUSDT", "ATHUSDT", "BABYUSDT", "FORTHUSDT", "PROMPTUSDT", "STOUSDT", "FHEUSDT", "KERNELUSDT", "WCTUSDT", "INITUSDT", "AERGOUSDT", "BANKUSDT", "DEEPUSDT", "HYPERUSDT", "JSTUSDT", "SIGNUSDT", "PUNDIXUSDT", "CTKUSDT", "AIOTUSDT", "DOLOUSDT", "HAEDALUSDT", "SXTUSDT", "ASRUSDT", "ALPINEUSDT", "B2USDT", "SYRUPUSDT", "DOODUSDT", "OGUSDT", "ZKJUSDT", "SKYAIUSDT", "NXPCUSDT", "CVCUSDT", "AGTUSDT", "AWEUSDT", "BUSDT", "SOONUSDT", "HUMAUSDT", "AUSDT", "SOPHUSDT", "MERLUSDT", "HYPEUSDT", "BDXNUSDT", "PUFFERUSDT", "1000000BOBUSDT", "LAUSDT", "HOMEUSDT", "RESOLVUSDT", "TAIKOUSDT", "SQDUSDT", "PUMPBTCUSDT", "SPKUSDT", "MYXUSDT", "FUSDT", "NEWTUSDT", "HUSDT", "OLUSDT", "SAHARAUSDT", "ICNTUSDT", "BULLAUSDT", "IDOLUSDT", "MUSDT", "PUMPUSDT", "CROSSUSDT", "AINUSDT", "CUSDT", "VELVETUSDT", "TACUSDT", "ERAUSDT", "TAUSDT", "CVXUSDT", "SLPUSDT", "ZORAUSDT", "TAGUSDT", "ESPORTSUSDT", "TREEUSDT", "A2ZUSDT", "PLAYUSDT", "NAORISUSDT", "TOWNSUSDT", "PROVEUSDT", "ALLUSDT", "INUSDT", "CARVUSDT", "AIOUSDT", "XNYUSDT", "USELESSUSDT", "DAMUSDT", "SAPIENUSDT", "XPLUSDT", "WLFIUSDT", "SOMIUSDT", "BASUSDT", "BTRUSDT", "MITOUSDT", "HEMIUSDT", "LINEAUSDT", "QUSDT", "ARIAUSDT", "TAKEUSDT", "PTBUSDT", "OPENUSDT", "FLOCKUSDT", "SKYUSDT", "AVNTUSDT", "HOLOUSDT", "XPINUSDT", "UBUSDT", "ZKCUSDT", "TOSHIUSDT", "STBLUSDT", "0GUSDT", "BARDUSDT", "ASTERUSDT", "TRADOORUSDT", "BLESSUSDT", "FLUIDUSDT", "COAIUSDT", "BTCUSDT_260327", "ETHUSDT_260327", "HANAUSDT", "MIRAUSDT", "AKEUSDT", "ORDERUSDT", "LIGHTUSDT", "XANUSDT", "FFUSDT", "VFYUSDT", "EDENUSDT", "NOMUSDT", "TRUTHUSDT", "2ZUSDT", "EVAAUSDT", "LYNUSDT", "KGENUSDT", "4USDT", "GIGGLEUSDT", "MONUSDT", "YBUSDT", "METUSDT", "EULUSDT", "ENSOUSDT", "CLOUSDT", "RECALLUSDT", "ZBTUSDT", "LABUSDT", "RIVERUSDT", "币安人生USDT", "BLUAIUSDT", "TURTLEUSDT", "APRUSDT", "ONUSDT", "KITEUSDT", "ATUSDT", "CCUSDT", "MMTUSDT", "TRUSTUSDT", "UAIUSDT", "FOLKSUSDT", "STABLEUSDT", "JCTUSDT", "ALLOUSDT", "CLANKERUSDT", "BEATUSDT", "PIEVERSEUSDT", "SENTUSDT", "BOBUSDT", "IRYSUSDT", "RLSUSDT", "POWERUSDT", "WETUSDT", "NIGHTUSDT", "XAUUSDT", "USUSDT", "CYSUSDT", "RAVEUSDT", "ZKPUSDT", "GUAUSDT", "IRUSDT", "LITUSDT", "BTCUSDT_260626", "ETHUSDT_260626", "BREVUSDT", "COLLECTUSDT", "MAGMAUSDT", "XAGUSDT", "ZAMAUSDT", "FOGOUSDT", "FRAXUSDT", "SPORTFUNUSDT", "AIAUSDT", "ACUUSDT", "我踏马来了USDT", "ELSAUSDT", "SKRUSDT", "SPACEUSDT", "FIGHTUSDT", "TSLAUSDT", "BIRBUSDT", "GWEIUSDT", "XPTUSDT", "XPDUSDT", "MEGAUSDT", "INXUSDT", "INTCUSDT", "HOODUSDT", "TRIAUSDT", "MSTRUSDT", "AMZNUSDT", "CRCLUSDT", "COINUSDT", "PLTRUSDT", "ESPUSDT", "AZTECUSDT", "OPNUSDT", "ROBOUSDT"
           };
            string joinedSymbols = string.Join(", ", activeFuturesSymbols);
            Console.WriteLine($"Danh sách các mã: {joinedSymbols}");

            var positionsResult = await binanceClient.UsdFuturesApi.Account.GetPositionInformationAsync();

            if (!positionsResult.Success)
            {
                Console.WriteLine($"Failed to get positions: {positionsResult.Error}");
                return Ok();
            }
            openPositionsCount = positionsResult.Data.Count(p => Math.Abs(p.Quantity) > 0);
            if (openPositionsCount > GlobalVariables.MaximumOrders)
            {
                Console.WriteLine("Too many open positions: " + GlobalVariables.MaximumOrders);
                return Ok();
            }
            Console.WriteLine($"GlobalVariables.MaximumOrders {GlobalVariables.MaximumOrders}. openPositionsCount {openPositionsCount}");

            foreach (var item in activeFuturesSymbols)
            {
                if (LastSuccessfulOrders.TryGetValue(item, out DateTime lastOrderTime))
                {
                    if ((DateTime.UtcNow - lastOrderTime).TotalHours < 4)
                    {
                        Console.WriteLine($"[Cooldown] Bỏ qua {item}. Lệnh gần nhất đặt lúc {lastOrderTime} UTC. Chưa đủ 4 giờ.");
                        continue;
                    }
                }
                var position = positionsResult.Data.FirstOrDefault(p => p.Symbol == item);
                if (position != null && position.Quantity != 0)
                {
                    Console.WriteLine($"A position is already open for {item}. Skipping to avoid DCA.");
                    continue;
                }
                if (openPositionsCount >= GlobalVariables.MaximumOrders)
                {
                    return Ok();
                }

                try
                {
                    await Task.Delay(1400);

                    var rawM15 = GetKlines(item, "1h", 200);
                    var candlesM15 = ParseKlines(rawM15);
                    List<Quote> m15Quotes = candlesM15.Select(k => new Quote
                    {
                        Date = k.OpenTime,
                        Open = k.Open,
                        High = k.High,
                        Low = k.Low,
                        Close = k.Close,
                        Volume = k.Volume
                    }).ToList();

                    var rsi15 = m15Quotes.GetRsi(9).ToList();

                    var currentRsi = rsi15[^1].Rsi;      // RSI nến đang chạy (real-time)
                    var previousRsi = rsi15[^2].Rsi;     // RSI nến vừa đóng xong
                    var olderRsi = rsi15[^3].Rsi;


                    if (olderRsi < 20 && previousRsi >= 20)
                    {
                        var resultOrder = await BinanceApi.PlaceFuturesOrderWithTpSlPriceAsync(item, "buy");
                        LastSuccessfulOrders[item] = DateTime.UtcNow;
                        string message = $"📉 LONG signal on {item}. H4 Uptrend, M30 RSI oversold. ";
                        var botToken = "8111665564:AAEEe3_pDSrbBt6Fnx1l_kMdf1dhC6oRL10";
                        var channel = "@ai_signal_notification";

                        // Gửi thông báo Telegram
                        using (HttpClient client = new HttpClient())
                        {
                            var url = $"https://api.telegram.org/bot{botToken}/sendMessage?chat_id={channel}&text={Uri.EscapeDataString(message)}";
                            var response = await client.GetAsync(url);
                            Console.WriteLine(await response.Content.ReadAsStringAsync());
                        }
                        openPositionsCount++;
                    }
                    else if (olderRsi > 80 && previousRsi <= 80)
                    {
                        var resultOrder = await BinanceApi.PlaceFuturesOrderWithTpSlPriceAsync(item, "sell");
                        LastSuccessfulOrders[item] = DateTime.UtcNow;
                        string message = $"📈 SHORT signal on {item}. H4 Downtrend, M30 RSI overbought";
                        // Gửi tín hiệu và đặt lệnh
                        var botToken = "8111665564:AAEEe3_pDSrbBt6Fnx1l_kMdf1dhC6oRL10";
                        var channel = "@ai_signal_notification";

                        // Gửi thông báo Telegram
                        using (HttpClient client = new HttpClient())
                        {
                            var url = $"https://api.telegram.org/bot{botToken}/sendMessage?chat_id={channel}&text={Uri.EscapeDataString(message)}";
                            var response = await client.GetAsync(url);
                            Console.WriteLine(await response.Content.ReadAsStringAsync());
                        }
                        openPositionsCount++;
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            Console.WriteLine($"Done calculated");
            return Ok();
        }

        public class Kline
        {
            public DateTime OpenTime { get; set; }
            public decimal Open { get; set; }
            public decimal High { get; set; }
            public decimal Low { get; set; }
            public decimal Close { get; set; }
            public decimal Volume { get; set; }
        }

        private List<JArray> GetKlines(string symbol, string interval, int limit)
        {
            try
            {

                var client = new RestClient("https://fapi.binance.com");
                var request = new RestRequest("/fapi/v1/klines", Method.Get);
                request.AddParameter("symbol", symbol);
                request.AddParameter("interval", interval);
                request.AddParameter("limit", limit);

                var response = client.Execute(request);
                var content = response.Content;

                if (string.IsNullOrEmpty(content))
                {
                    throw new Exception("Empty response from Binance.");
                }

                // Try parse as JToken and inspect type
                var json = JToken.Parse(content);

                if (json.Type == JTokenType.Array)
                {
                    return json.ToObject<List<JArray>>();
                }
                else if (json.Type == JTokenType.Object)
                {
                    // log the error message
                    var error = (JObject)json;
                    var code = error["code"]?.ToString();
                    var msg = error["msg"]?.ToString();
                    throw new Exception($"Binance API error: {code} - {msg}");
                }
                else
                {
                    throw new Exception("Unexpected JSON format from Binance.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw new Exception("Unexpected JSON format from Binance.");

            }
        }
        public List<Kline> ParseKlines(List<JArray> jarrays)
        {
            var klines = new List<Kline>();

            foreach (var item in jarrays)
            {
                try
                {
                    klines.Add(new Kline
                    {
                        OpenTime = DateTimeOffset.FromUnixTimeMilliseconds((long)item[0]).UtcDateTime,
                        Open = Convert.ToDecimal(item[1]),
                        High = Convert.ToDecimal(item[2]),
                        Low = Convert.ToDecimal(item[3]),
                        Close = Convert.ToDecimal(item[4]),
                        Volume = Convert.ToDecimal(item[5])
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing kline: {ex.Message}");
                }
            }

            return klines;
        }
    }
}