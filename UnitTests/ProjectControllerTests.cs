using Microsoft.AspNetCore.Mvc;
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
    public class ProjectControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ProjectController _controller;

        public ProjectControllerTests()
        {
            // Setup real in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            _controller = new ProjectController(_context);
        }

        [Fact]
        public async Task Index_ReturnsViewWithProjects()
        {
            // Arrange
            var projects = new List<Project>
            {
                new Project { ProjectId = 1, ProjectName = "Проект 1", ProjectDescription = "Описание 1" },
                new Project { ProjectId = 2, ProjectName = "Проект 2", ProjectDescription = "Описание 2" }
            };

            await _context.Projects.AddRangeAsync(projects);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Project>>(viewResult.Model);
            Assert.Equal(2, model.Count());
        }

        [Fact]
        public async Task Details_WithValidId_ReturnsViewWithProject()
        {
            // Arrange
            var projectId = 1;
            var project = new Project { ProjectId = projectId, ProjectName = "Тестов проект", ProjectDescription = "Тестово описание" };
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Details(projectId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Project>(viewResult.Model);
            Assert.Equal(projectId, model.ProjectId);
            Assert.Equal("Тестов проект", model.ProjectName);
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
        public void Create_ReturnsView()
        {
            // Act
            var result = _controller.Create();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Create_WithValidModel_CreatesProject()
        {
            // Arrange
            var project = new Project { ProjectName = "Нов проект", ProjectDescription = "Ново описание" };

            // Act
            var result = await _controller.Create(project);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            
            var createdProject = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectName == "Нов проект");
            Assert.NotNull(createdProject);
            Assert.Equal("Ново описание", createdProject.ProjectDescription);
        }

        [Fact]
        public async Task Create_WithInvalidModel_ReturnsView()
        {
            // Arrange
            var project = new Project { ProjectName = "Нов проект", ProjectDescription = "Ново описание" };
            _controller.ModelState.AddModelError("ProjectName", "Полето е задължително");

            // Act
            var result = await _controller.Create(project);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Project>(viewResult.Model);
            Assert.Equal(project.ProjectName, model.ProjectName);
        }

        [Fact]
        public async Task Edit_WithValidId_ReturnsViewWithProject()
        {
            // Arrange
            var projectId = 1;
            var project = new Project { ProjectId = projectId, ProjectName = "Тестов проект", ProjectDescription = "Тестово описание" };
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Edit(projectId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Project>(viewResult.Model);
            Assert.Equal(projectId, model.ProjectId);
            Assert.Equal("Тестов проект", model.ProjectName);
        }

        [Fact]
        public async Task Edit_WithInvalidId_ReturnsNotFound()
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
            var projectId = 1;
            var project = new Project { ProjectId = projectId, ProjectName = "Тестов проект", ProjectDescription = "Тестово описание" };
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            var updatedProject = new Project { ProjectId = projectId, ProjectName = "Обновен проект", ProjectDescription = "Обновено описание" };
            _controller.ModelState.AddModelError("ProjectName", "Полето е задължително");

            // Act
            var result = await _controller.Edit(projectId, updatedProject);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Project>(viewResult.Model);
            Assert.Equal(updatedProject.ProjectName, model.ProjectName);
        }

        [Fact]
        public async Task Delete_WithValidId_ReturnsViewWithProject()
        {
            // Arrange
            var projectId = 1;
            var project = new Project { ProjectId = projectId, ProjectName = "Тестов проект", ProjectDescription = "Тестово описание" };
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Delete(projectId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Project>(viewResult.Model);
            Assert.Equal(projectId, model.ProjectId);
        }

        [Fact]
        public async Task Delete_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = 999;

            // Act
            var result = await _controller.Delete(invalidId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteConfirmed_WithNoTeams_DeletesProject()
        {
            // Arrange
            var projectId = 1;
            var project = new Project { ProjectId = projectId, ProjectName = "Тестов проект", ProjectDescription = "Тестово описание" };
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteConfirmed(projectId);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            
            var dbProject = await _context.Projects.FindAsync(projectId);
            Assert.Null(dbProject); // Проверяваме дали проектът е изтрит
        }

        [Fact]
        public async Task DeleteConfirmed_WithTeams_ReturnsViewWithError()
        {
            // Arrange
            var projectId = 1;
            var project = new Project { ProjectId = projectId, ProjectName = "Тестов проект", ProjectDescription = "Тестово описание" };
            var team = new Team { TeamId = 1, TeamName = "Тестов екип", ProjectId = projectId };
            
            await _context.Projects.AddAsync(project);
            await _context.Teams.AddAsync(team);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteConfirmed(projectId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains("Не може да изтриете проект, който има асоциирани екипи.",
                _controller.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
} 