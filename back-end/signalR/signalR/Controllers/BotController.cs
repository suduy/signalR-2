using BinanceRSIAPI.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BinanceRSIAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BotController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> BotControl([FromBody] BotControlRequestDto requestDto)
        {
            Console.WriteLine("Change bot status to: " + requestDto.IsWorking);
            if (requestDto.IsWorking)
            {
                GlobalVariables.IsWorking = true;
            } else
            {
                GlobalVariables.IsWorking = false;
            }
            GlobalVariables.IsNeedLimit = requestDto.IsNeedLimit;
            GlobalVariables.MaximumOrders = requestDto.MaximumOrders;
            return Ok();
        }
    }
    public class BotControlRequestDto
    {
        public bool IsWorking { get; set; }
        public int MaximumOrders { get; set; }
        public bool IsNeedLimit { get; set; }
    }
}
