using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using SolutionsTools.WebApi.Models;

namespace SolutionsTools.WebApi.Controllers
{   [RoutePrefix("")]
    public class LoginController : ApiController
    {
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        public HttpResponseMessage Validate([FromBody] string token)
        {
            var t = new TokenManager();
            var ret = t.ValidaToken(token);


            if (ret.ret_code.Equals(0))
                return Request.CreateResponse(HttpStatusCode.OK, ret);

            return Request.CreateResponse(HttpStatusCode.BadRequest, ret);
        }

        [HttpPost]
        [Route("token")]
        public HttpResponseMessage Login(User user)
        {
            var t = new TokenManager();
            var ret = t.Login(user.Username, user.Password, "pt-BR");

            if (t == null)
                return Request.CreateResponse(HttpStatusCode.NotFound, "The user was not found.");

            var token = new TokenInfo { token = ret.ret_info };

            return Request.CreateResponse(HttpStatusCode.OK, ret.ret_info);
        }

        private struct TokenInfo 
        {
            public string token;
        }
    }
}
