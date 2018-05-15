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
    //Removed the trello auth flow to remove backend dependencies on Trello
    public class AuthenticationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly GitHubAPI _gitHubApi;

        public AuthenticationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _gitHubApi = new GitHubAPI(configuration);
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user.GithubAuthenticated)
            {
                HttpContext.Session.TryGetValue("GithubToken", out byte[] tokenArray);
                if (tokenArray == null) return Redirect(_gitHubApi.GitHubAuthURL());
                return RedirectToAction("Index", "ApplicationHome");

            }

            string githubAuthUrl = _gitHubApi.GitHubAuthURL();
            AuthorizationViewModel vm = new AuthorizationViewModel
            {
                GithubAuthURL = githubAuthUrl,
                GithubAuthorized = user.GithubAuthenticated,
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
    }
}
