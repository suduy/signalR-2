using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;


namespace BinanceRSIAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatusController : ControllerBase
    {
        [HttpGet]
        public IActionResult Status ()
        {
            return Ok();
        }
    }
}