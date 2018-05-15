using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using DevDash.Data;
using DevDash.Models;
using Microsoft.AspNetCore.Identity;
using DevDash.Services;
using Microsoft.Extensions.Configuration;
using DevDash.Models.ApplicationHomeViewModels;
using DevDash.Extensions;

namespace DevDash.Controllers
{
    public class ApplicationHomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;
        GitHubAPI gitHubAPI;


        public ApplicationHomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration Configuration)
        {
            gitHubAPI = new GitHubAPI(Configuration);
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            HttpContext.Session.TryGetValue("GithubToken", out byte[] githubTokenByteArray);
            var githubToken = System.Text.Encoding.Default.GetString(githubTokenByteArray);

            var repos = await gitHubAPI.getRepositoriesAsync(githubToken);

            var repoSelectListItem = new List<SelectListItem>();
            var boardSelectListItem = new List<SelectListItem>();

            foreach (Octokit.Repository repo in repos)
            {
                GitHub gitHub = new GitHub
                {
                    UserId = user.Id,
                    RepoId = repo.Id,
                    RepoName = repo.Name,
                };
                _context.GitHub.AddIfNotExists(gitHub, x => x.UserId == user.Id && x.RepoId == repo.Id);
                repoSelectListItem.Add(new SelectListItem { Text = repo.Name, Value = repo.Id.ToString() });
            }


            await _context.SaveChangesAsync();
            _context.Entry(user).Collection(x => x.Dashboard).Load();
            var dashboards = user.Dashboard;
            var repoSelectList = new SelectList(repoSelectListItem, "Value", "Text");

            ApplicationHomeViewModel appviewmodel = new ApplicationHomeViewModel
            {
                UserDashboards = dashboards,
                GithubRepos = repoSelectList,
                TrelloBoard = boardSelectList
            };

            CombinedViewModel viewModel = new CombinedViewModel
            {
                ApplicationHomeViewModel = appviewmodel,
                CreateDashboardViewModel = new CreateDashboardViewModel()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDashboard(CombinedViewModel combinedModel)
        {
            if (ModelState.IsValid)
            {
                CreateDashboardViewModel model = combinedModel.CreateDashboardViewModel;
                long.TryParse(model.RepoId, out long repoId);
                Guid guid = Guid.NewGuid();
                var user = await _userManager.GetUserAsync(User);
                var dashboard = new Dashboard
                {
                    DashboardId = guid,
                    DashboardName = model.DashboardName,
                    RepoId = repoId,
                    BoardId = model.BoardId,
                    UserId = user.Id
                };

                _context.Add(dashboard);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Dashboards", new { id = guid.ToString() });
            }
            //FIXME: This error should be handled more gracefully and present a message to users
            return RedirectToAction("Index", "ApplicationHome"); 
        }

    }
}
