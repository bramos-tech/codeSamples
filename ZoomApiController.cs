// Api Controller for Zoom React Component

namespace Web.Api.Controllers
{
    [Route("api/zoom")]
    [ApiController]
    public class ZoomApiController : Web.Controllers.BaseApiController
    {
        static readonly char[] padding = { '=' };
        private ZoomKeys _zoom = null;

        public ZoomApiController(ILogger<ZoomApiController> logger, IOptions<ZoomKeys> zoom) : base(logger)
        {
            _zoom = zoom.Value;
        }

        public static long ToTimestamp(DateTime value)
        {
            long epoch = (value.Ticks - 621355968000000000) / 10000;
            return epoch;
        }

        public static string GenerateToken (string apiKey, string apiSecret, string meetingNumber, string ts, string role)
        {
            string message = String.Format("{0}{1}{2}{3}", apiKey, meetingNumber, ts, role);
            apiSecret = apiSecret ?? "";
            var encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = encoding.GetBytes(apiSecret);
            byte[] messageBytesTest = encoding.GetBytes(message);
            string msgHashPreHmac = System.Convert.ToBase64String(messageBytesTest);
            byte[] messageBytes = encoding.GetBytes(msgHashPreHmac);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                string msgHash = System.Convert.ToBase64String(hashmessage);
                string token = String.Format("{0}.{1}.{2}.{3}.{4}", apiKey, meetingNumber, ts, role, msgHash);
                var tokenBytes = System.Text.Encoding.UTF8.GetBytes(token);
                return System.Convert.ToBase64String(tokenBytes).TrimEnd(padding);
            }
        }


        [HttpPost]
        public ActionResult<ItemResponse<string>> Create(ZoomMeetingAddRequest request)
        {
            ObjectResult result = null;

            try
            {
                string apiKey = _zoom.ApiKey;
                string apiSecret = _zoom.ApiSecret;
                string meetingNumber = request.MeetingNumber;
                string ts = ToTimestamp(DateTime.UtcNow.ToUniversalTime()).ToString();
                string role = "0";
                string token = GenerateToken(apiKey, apiSecret, meetingNumber, ts, role);

                ItemResponse<string> response = new ItemResponse<string>() { Item = token };
                result = Created201(response);
            }
            catch (Exception ex)
            {
                base.Logger.LogError(ex.ToString());
                ErrorResponse response = new ErrorResponse(ex.Message);

                result = StatusCode(500, response);
            }

            return result;
        }
    }
}
