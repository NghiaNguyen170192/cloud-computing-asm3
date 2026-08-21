using Microsoft.AspNetCore.Mvc;

namespace NetCore.Donation.Api.Controllers;

// [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
// [Authorize(Roles = "user")]
// [EnableCors]
[ApiController]
public class AuthorizedBaseController : ControllerBase
{
}