using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RenewalWebsite.Helpers.CaptchaLib;
using System.IO;

namespace RenewalWebsite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CaptchaController : ControllerBase
    {
        [HttpGet("get-captcha-image")]
        public IActionResult GetCaptchaImage()
        {
            int width = 110;
            int height = 36;
            var captchaCode = Captcha.GenerateCaptchaCode();
            var result = Captcha.GenerateCaptchaImage(width, height, captchaCode);
            HttpContext.Session.SetString("CaptchaCode", result.CaptchaCode);
            Stream s = new MemoryStream(result.CaptchaByteData);
            return new FileStreamResult(s, "image/png");
        }
    }
}