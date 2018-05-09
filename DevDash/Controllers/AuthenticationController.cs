using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DevDash.Data;
using DevDash.Services;
using Octokit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using DevDash.Models;
using DevDash.Models.AuthorizationViewModels;
using Microsoft.Extensions.Configuration;

namespace DevDash.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly GitHubAPI _gitHubApi;
        private readonly TrelloAPI _trelloApi;

        public AuthenticationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _gitHubApi = new GitHubAPI(configuration);
            _trelloApi = new TrelloAPI(configuration);
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user.GithubAuthenticated && user.TrelloAuthenticated)
            {
                HttpContext.Session.TryGetValue("GithubToken", out byte[] tokenArray);
                if (tokenArray == null) return Redirect(_gitHubApi.GitHubAuthURL());
                var trelloToken = user.TrelloKey;
                HttpContext.Session.SetString("TrelloToken", trelloToken);
                return RedirectToAction("Index", "ApplicationHome");

            }

            string githubAuthUrl = _gitHubApi.GitHubAuthURL();
            string trelloAuthUrl = _trelloApi.GetTrelloAuthUrl();
            AuthorizationViewModel vm = new AuthorizationViewModel
            {
                GithubAuthURL = githubAuthUrl,
                TrelloAuthURL = trelloAuthUrl,
                GithubAuthorized = user.GithubAuthenticated,
                TrelloAuthorized = user.TrelloAuthenticated

            };
            return View(vm);
        }

        public async Task<IActionResult> AuthorizeGithub(string code, string state = "")
        {
            var user = await _userManager.GetUserAsync(User);
            OauthToken token = await _gitHubApi.Authorize(code, state);
            HttpContext.Session.SetString("GithubToken", token.AccessToken);
            if (!user.GithubAuthenticated)
            {
                user.GithubAuthenticated = true;
            }
            await _userManager.UpdateAsync(user);
            return RedirectToAction("Index");
        }

        public IActionResult AuthorizeTrelloAjax()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AuthorizeTrello()
        {
            string id = Request.Form["trelloToken"];
            var user = await _userManager.GetUserAsync(User);
            HttpContext.Session.SetString("TrelloToken", id);
            user.TrelloKey = id;
            user.TrelloAuthenticated = true;
            await _userManager.UpdateAsync(user);
            return Json(new {result = "Redirect", url = Url.Action("Index", "Authentication") });
        }

    }
}
