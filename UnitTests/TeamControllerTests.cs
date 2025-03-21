using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vacation_Manager.Controllers;
using Vacation_Manager.Data;
using Vacation_Manager.Models;
using Xunit;

namespace UnitTests
{
    public class TeamControllerTests : IDisposable
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly ApplicationDbContext _context;
        private readonly TeamController _controller;

        public TeamControllerTests()
        {
            // Setup UserManager mock
            var userStore = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                userStore.Object,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null
            );

            // Setup real in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            _controller = new TeamController(_context, _userManagerMock.Object);
        }

        [Fact]
        public async Task Index_ReturnsViewWithTeams()
        {
            // Arrange
            var teamLeader = new User { Id = "1", FirstName = "Иван", LastName = "Иванов", UserName = "teamlead@example.com", Email = "teamlead@example.com" };
            var project = new Project { ProjectId = 1, ProjectName = "Тестов проект", ProjectDescription = "Тестово описание" };
            var teams = new List<Team>
            {
                new Team { TeamId = 1, TeamName = "Екип 1", TeamLeaderId = teamLeader.Id, TeamLeader = teamLeader, ProjectId = project.ProjectId, Project = project },
                new Team { TeamId = 2, TeamName = "Екип 2", TeamLeaderId = null, TeamLeader = null, ProjectId = null, Project = null }
            };

            await _context.Projects.AddAsync(project);
            await _context.Users.AddAsync(teamLeader);
            await _context.Teams.AddRangeAsync(teams);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Team>>(viewResult.Model);
            Assert.Equal(2, model.Count);
            Assert.Equal("Екип 1", model[0].TeamName);
            Assert.Equal("Иван", model[0].TeamLeader.FirstName);
        }

        [Fact]
        public async Task Details_WithValidId_ReturnsViewWithTeam()
        {
            // Arrange
            var teamId = 1;
            var teamLeader = new User { Id = "1", FirstName = "Иван", LastName = "Иванов", UserName = "teamlead@example.com", Email = "teamlead@example.com" };
            var project = new Project { ProjectId = 1, ProjectName = "Тестов проект", ProjectDescription = "Тестово описание" };
            var team = new Team { TeamId = teamId, TeamName = "Тестов екип", TeamLeaderId = teamLeader.Id, TeamLeader = teamLeader, ProjectId = project.ProjectId, Project = project };

            await _context.Projects.AddAsync(project);
            await _context.Users.AddAsync(teamLeader);
            await _context.Teams.AddAsync(team);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Details(teamId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Team>(viewResult.Model);
            Assert.Equal(teamId, model.TeamId);
            Assert.Equal("Тестов екип", model.TeamName);
            Assert.Equal(teamLeader.Id, model.TeamLeaderId);
            Assert.Equal(project.ProjectId, model.ProjectId);
        }

        [Fact]
        public async Task Details_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = 999;

            // Act
            var result = await _controller.Details(invalidId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_Get_ReturnsViewWithSelectLists()
        {
            // Arrange
            var teamLeads = new List<User>
            {
                new User { Id = "1", FirstName = "Иван", LastName = "Иванов", UserName = "teamlead1@example.com" },
                new User { Id = "2", FirstName = "Петър", LastName = "Петров", UserName = "teamlead2@example.com" }
            };

            var projects = new List<Project>
            {
                new Project { ProjectId = 1, ProjectName = "Проект 1", ProjectDescription = "Описание 1" },
                new Project { ProjectId = 2, ProjectName = "Проект 2", ProjectDescription = "Описание 2" }
            };

            _userManagerMock.Setup(x => x.GetUsersInRoleAsync("Team Lead"))
                .ReturnsAsync(teamLeads);

            await _context.Projects.AddRangeAsync(projects);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Create();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData["TeamLeads"]);
            Assert.NotNull(viewResult.ViewData["Projects"]);
            
            var teamLeadsSelectList = Assert.IsType<SelectList>(viewResult.ViewData["TeamLeads"]);
            var projectsSelectList = Assert.IsType<SelectList>(viewResult.ViewData["Projects"]);
            
            Assert.Equal(2, teamLeadsSelectList.Count());
            Assert.Equal(2, projectsSelectList.Count());
        }

        [Fact]
        public async Task Create_Post_WithValidModel_CreatesTeam()
        {
            // Arrange
            var team = new Team { TeamName = "Нов екип", TeamLeaderId = "1", ProjectId = 1 };

            // Act
            var result = await _controller.Create(team);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            
            var createdTeam = await _context.Teams.FirstOrDefaultAsync(t => t.TeamName == "Нов екип");
            Assert.NotNull(createdTeam);
            Assert.Equal("1", createdTeam.TeamLeaderId);
            Assert.Equal(1, createdTeam.ProjectId);
        }

        [Fact]
        public async Task Create_Post_WithInvalidModel_ReturnsView()
        {
            // Arrange
            var team = new Team { TeamName = "Нов екип", TeamLeaderId = "1", ProjectId = 1 };
            _controller.ModelState.AddModelError("TeamName", "Полето е задължително");

            var teamLeads = new List<User>
            {
                new User { Id = "1", FirstName = "Иван", LastName = "Иванов", UserName = "teamlead1@example.com" }
            };

            var projects = new List<Project>
            {
                new Project { ProjectId = 1, ProjectName = "Проект 1", ProjectDescription = "Описание 1" }
            };

            _userManagerMock.Setup(x => x.GetUsersInRoleAsync("Team Lead"))
                .ReturnsAsync(teamLeads);

            await _context.Projects.AddRangeAsync(projects);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Create(team);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<Team>(viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Edit_Get_WithValidId_ReturnsViewWithTeam()
        {
            // Arrange
            var teamId = 1;
            var team = new Team { TeamId = teamId, TeamName = "Тестов екип", TeamLeaderId = "1", ProjectId = 1 };
            
            await _context.Teams.AddAsync(team);
            await _context.SaveChangesAsync();

            var teamLeads = new List<User>
            {
                new User { Id = "1", FirstName = "Иван", LastName = "Иванов", UserName = "teamlead1@example.com" }
            };

            var projects = new List<Project>
            {
                new Project { ProjectId = 1, ProjectName = "Проект 1", ProjectDescription = "Описание 1" }
            };

            _userManagerMock.Setup(x => x.GetUsersInRoleAsync("Team Lead"))
                .ReturnsAsync(teamLeads);

            await _context.Projects.AddRangeAsync(projects);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Edit(teamId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Team>(viewResult.Model);
            Assert.Equal(teamId, model.TeamId);
            Assert.Equal("Тестов екип", model.TeamName);
            
            Assert.NotNull(viewResult.ViewData["TeamLeads"]);
            Assert.NotNull(viewResult.ViewData["Projects"]);
        }

        [Fact]
        public async Task Edit_Get_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = 999;

            // Act
            var result = await _controller.Edit(invalidId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

       
        [Fact]
        public async Task Edit_Post_WithInvalidModel_ReturnsView()
        {
            // Arrange
            var teamId = 1;
            var team = new Team { TeamId = teamId, TeamName = "Тестов екип", TeamLeaderId = "1", ProjectId = 1 };
            
            await _context.Teams.AddAsync(team);
            await _context.SaveChangesAsync();

            var updatedTeam = new Team { TeamId = teamId, TeamName = "Обновен екип", TeamLeaderId = "2", ProjectId = 2 };
            _controller.ModelState.AddModelError("TeamName", "Полето е задължително");

            var teamLeads = new List<User>
            {
                new User { Id = "1", FirstName = "Иван", LastName = "Иванов", UserName = "teamlead1@example.com" }
            };

            var projects = new List<Project>
            {
                new Project { ProjectId = 1, ProjectName = "Проект 1", ProjectDescription = "Описание 1" }
            };

            _userManagerMock.Setup(x => x.GetUsersInRoleAsync("Team Lead"))
                .ReturnsAsync(teamLeads);

            await _context.Projects.AddRangeAsync(projects);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Edit(teamId, updatedTeam);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<Team>(viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Delete_Get_WithValidId_ReturnsViewWithTeam()
        {
            // Arrange
            var teamId = 1;
            var teamLeader = new User { Id = "1", FirstName = "Иван", LastName = "Иванов", UserName = "teamlead@example.com" };
            var project = new Project { ProjectId = 1, ProjectName = "Тестов проект", ProjectDescription = "Тестово описание" };
            var team = new Team { TeamId = teamId, TeamName = "Тестов екип", TeamLeaderId = teamLeader.Id, TeamLeader = teamLeader, ProjectId = project.ProjectId, Project = project };

            await _context.Projects.AddAsync(project);
            await _context.Users.AddAsync(teamLeader);
            await _context.Teams.AddAsync(team);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Delete(teamId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Team>(viewResult.Model);
            Assert.Equal(teamId, model.TeamId);
            Assert.Equal("Тестов екип", model.TeamName);
        }

        [Fact]
        public async Task Delete_Get_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = 999;

            // Act
            var result = await _controller.Delete(invalidId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteConfirmed_WithNoMembers_DeletesTeam()
        {
            // Arrange
            var teamId = 1;
            var team = new Team { TeamId = teamId, TeamName = "Тестов екип" };
            
            await _context.Teams.AddAsync(team);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteConfirmed(teamId);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            
            var dbTeam = await _context.Teams.FindAsync(teamId);
            Assert.Null(dbTeam); // Проверяваме дали екипът е изтрит
        }

        [Fact]
        public async Task DeleteConfirmed_WithMembers_ReturnsViewWithError()
        {
            // Arrange
            var teamId = 1;
            var member = new User { Id = "1", FirstName = "Петър", LastName = "Петров", UserName = "member@example.com", TeamId = teamId };
            var team = new Team { TeamId = teamId, TeamName = "Тестов екип", Members = new List<User> { member } };
            
            await _context.Users.AddAsync(member);
            await _context.Teams.AddAsync(team);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteConfirmed(teamId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains("Не може да изтриете екип, който има потребители.",
                _controller.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
} 