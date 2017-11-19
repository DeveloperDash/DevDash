﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DevDash.Data;
using DevDash.Models;
using Microsoft.AspNetCore.Identity;
using DevDash.Services;
using Microsoft.Extensions.Configuration;
using DevDash.Models.AuthorizationViewModels;
using DevDash.Models.ApplicationHomeViewModels;

namespace DevDash.Controllers
{
    public class ApplicationHomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;
        GitHubAPI gitHubAPI;
        TrelloAPI trelloApi;


        public ApplicationHomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration Configuration)
        {
            gitHubAPI = new GitHubAPI(Configuration);
            trelloApi = new TrelloAPI(Configuration);
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var trelloToken = user.TrelloKey;
            HttpContext.Session.TryGetValue("GithubToken", out byte[] githubTokenByteArray);
            var githubToken = githubTokenByteArray.ToString();

            var repos = await gitHubAPI.getRepositoriesAsync(githubToken);
            var boards = trelloApi.getUserTrelloBoards(trelloToken);

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
                _context.Add(gitHub);
                repoSelectListItem.Add(new SelectListItem { Text = repo.Name, Value = repo.Id.ToString() });
            }

            foreach(TrelloNet.Board board in boards)
            {
                Trello trello = new Trello
                {
                    UserId = user.Id,
                    BoardId = board.Id,
                    BoardName = board.Name,
                };
                _context.Add(trello);
                boardSelectListItem.Add(new SelectListItem { Text = board.Name, Value = board.Id});

            }


            await _context.SaveChangesAsync();
            var dashboards = user.Dashboard;
            var repoSelectList = new SelectList(repoSelectListItem, "Value", "Text");
            var boardSelectList = new SelectList(boardSelectListItem, "Value", "Text");

            ApplicationHomeViewModel viewmodel = new ApplicationHomeViewModel
            {
                UserDashboards = dashboards,
                GithubRepos = repoSelectList,
                TrelloBoard = boardSelectList
            };

            return View(viewmodel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDashboard(CreateDashboardViewModel model)
        {
            if (ModelState.IsValid)
            {
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
            return View(model);
        }

    }
}