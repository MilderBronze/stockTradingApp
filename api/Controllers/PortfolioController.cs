using api.Extensions;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/portfolio")]
    [ApiController]
    public class PortfolioController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IStockRepository _stockRepo;
        private readonly IPortfolioRepository _portfolioRepo;
        private readonly IFMPService _fmpService;

        public PortfolioController(
            UserManager<AppUser> userManager,
            IStockRepository stockRepo,
            IPortfolioRepository portfolioRepo,
            IFMPService fmpService
        )
        {
            _userManager = userManager;
            _stockRepo = stockRepo;
            _portfolioRepo = portfolioRepo;
            _fmpService = fmpService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserPortfolio()
        {
            // You don’t import User directly. It’s part of the controller base class.
            //You import Microsoft.AspNetCore.Mvc to get access to ControllerBase and its properties.

            /**
            You get access to many properties and methods, such as:
            User (for authentication info)
            Request, Response (HTTP context)
            ModelState (validation)
            HttpContext (full context)
            Url, ControllerContext, etc.
            All these are available because your controller inherits from ControllerBase or Controller.
            */
            var username = User.GetUsername();
            // var username = User.Identity?.Name;
            var appUser = await _userManager.FindByNameAsync(username);
            var userPortfolio = await _portfolioRepo.GetUserPortfolio(appUser);
            return Ok(userPortfolio);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddPortfolio(string symbol)
        {
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);
            var stock = await _stockRepo.GetBySymbolAsync(symbol);

            if (stock == null)
            {
                stock = await _fmpService.FindStockBySymbolAsync(symbol);
                if (stock == null)
                {
                    return BadRequest("Stock does not exists");
                }
                else
                {
                    await _stockRepo.CreateAsync(stock);
                }
            }

            if (stock == null)
                return BadRequest("Stock not found");

            var userPortfolio = await _portfolioRepo.GetUserPortfolio(appUser);

            if (userPortfolio.Any(e => e.Symbol.ToLower() == symbol.ToLower()))
                return BadRequest("Cannot add same stock to portfolio");

            var portfolioModel = new Portfolio { StockId = stock.Id, AppUserId = appUser.Id };

            await _portfolioRepo.CreateAsync(portfolioModel);

            if (portfolioModel == null)
            {
                return StatusCode(500, "Could not create");
            }
            else
            {
                return Created();
            }
        }

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeletePortfolio(string symbol)
        {
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            var userPortfolio = await _portfolioRepo.GetUserPortfolio(appUser); // user portfolio is nothing but the stocks he owns.

            var filteredStock = userPortfolio
                .Where(s => s.Symbol.ToLower() == symbol.ToLower())
                .ToList();

            if (filteredStock.Count() == 1) // user portfolio me ek stock ek hii quantity me ho skta hai.
            {
                await _portfolioRepo.DeletePortfolio(appUser, symbol);
            }
            else
            {
                return BadRequest("Stock not in your portfolio");
            }

            return Ok();
        }
    }
}

/***
Summary of the Flow
User logs in → server creates JWT with claims.
Client sends JWT in requests.
Authentication middleware validates JWT, extracts claims.
Claims are put into HttpContext.User (ClaimsPrincipal).
Controller accesses User to get info about the authenticated user



---

in detail:
Here’s the full flow of how User (the ClaimsPrincipal) is populated in ASP.NET Core:

1. Request Comes In
A client (browser, mobile app, etc.) sends an HTTP request to your API.
If the endpoint requires authentication (e.g., [Authorize]), the client must include a JWT token in the Authorization header.

2. Middleware Pipeline
ASP.NET Core’s middleware pipeline processes the request.
The authentication middleware (app.UseAuthentication()) looks for the Authorization header.
If a JWT token is present, the middleware validates it using the settings from Program.cs (issuer, audience, signing key).

3. Token Validation
If the token is valid, the middleware extracts claims from the token (like username, email, roles).
These claims are used to create a ClaimsPrincipal object.

4. HttpContext.User
The ClaimsPrincipal is assigned to HttpContext.User.
In controllers, the User property is just a shortcut for HttpContext.User.

5. Accessing Claims
You can access claims directly (e.g., User.Identity.Name) or via extension methods (like your GetUsername()).
The claims come from the JWT payload, which was created when the token was issued (see your TokenService).

6. Who Puts Claims in the Token?
When a user logs in, your TokenService.CreateToken() creates a JWT.
It adds claims (username, email, etc.) to the token.
The client receives this token and uses it for future requests.
*/
