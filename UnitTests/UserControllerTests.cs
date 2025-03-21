using Microsoft.AspNetCore.Identity;
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
    public class UserControllerTests : IDisposable
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
        private readonly ApplicationDbContext _context;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            // Setup UserManager mock
            var userStore = new Mock<IUserStore<User>>();
            var userManager = new Mock<UserManager<User>>(
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
            _userManagerMock = userManager;

            // Setup RoleManager mock with correct parameters
            var roleStore = new Mock<IRoleStore<IdentityRole>>();
            roleStore.Setup(x => x.FindByIdAsync("1", CancellationToken.None))
                    .ReturnsAsync(new IdentityRole { Id = "1", Name = "CEO" });

            var roleValidators = new List<IRoleValidator<IdentityRole>>();
            var roleManager = new Mock<RoleManager<IdentityRole>>(
                roleStore.Object,
                roleValidators,
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null
            );

            // Setup async enumerable for roles
            var roles = new List<IdentityRole>
                {
                    new IdentityRole { Id = "1", Name = "CEO" },
                    new IdentityRole { Id = "2", Name = "Developer" }
                }.AsQueryable();

            roleManager.Setup(rm => rm.Roles).Returns(roles);
            _roleManagerMock = roleManager;

            // Setup real in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // Добавяне на допълнителни mock setups за методите на UserManager, които използва контролера
            _userManagerMock.Setup(x => x.RemoveFromRolesAsync(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.DeleteAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Success);

            _controller = new UserController(_userManagerMock.Object, _roleManagerMock.Object, _context);
        }

      
        

        

        [Fact]
        public async Task Details_WithValidId_ReturnsViewWithUser()
        {
            // Arrange
            var userId = "1";
            var user = new User { Id = userId, FirstName = "John", LastName = "Doe", Email = "john@example.com" };
            var roles = new List<string> { "CEO" };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Details(userId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<User>(viewResult.Model);
            Assert.Equal(userId, model.Id);
        }

        [Fact]
        public async Task Details_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var userId = "invalid-id";
            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync((User)null);

            // Act
            var result = await _controller.Details(userId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

      
        public async Task Edit_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var userId = "invalid-id";
            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync((User)null);

            // Act
            var result = await _controller.Edit(userId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_UpdatesUserDetails()
        {
            // Arrange
            var userId = "1";
            var user = new User { Id = userId, FirstName = "John", LastName = "Doe", Email = "john@example.com" };
            var updatedUser = new User { Id = userId, FirstName = "Updated", LastName = "User", Email = "updated@example.com" };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "CEO" });

            // Act
            var result = await _controller.Edit(userId, updatedUser, "CEO", null);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            _userManagerMock.Verify(x => x.UpdateAsync(It.Is<User>(u => u.FirstName == "Updated" && u.LastName == "User" && u.Email == "updated@example.com")), Times.Once);
            _userManagerMock.Verify(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()), Times.Once);
            _userManagerMock.Verify(x => x.AddToRoleAsync(user, "CEO"), Times.Once);
        }

    
        

        [Fact]
        public async Task Delete_WithValidId_ReturnsViewWithUser()
        {
            // Arrange
            var userId = "1";
            var user = new User { Id = userId, FirstName = "John", LastName = "Doe", Email = "john@example.com" };
            var roles = new List<string> { "CEO" };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);

            // Act
            var result = await _controller.Delete(userId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<User>(viewResult.Model);
            Assert.Equal(userId, model.Id);
        }

        [Fact]
        public async Task DeleteConfirmed_WithTeamLeader_ReturnsViewWithError()
        {
            // Arrange
            var userId = "1";
            var user = new User { Id = userId, FirstName = "John", LastName = "Doe", Email = "john@example.com" };
            var team = new Team { TeamId = 1, TeamLeaderId = userId, TeamName = "Test Team" };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            await _context.Teams.AddAsync(team);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteConfirmed(userId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains("Потребителят е лидер на екип и не може да бъде изтрит.",
                _controller.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            _userManagerMock.Verify(x => x.DeleteAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task DeleteConfirmed_WithNonTeamLeader_DeletesUser()
        {
            // Arrange
            var userId = "1";
            var user = new User { Id = userId, FirstName = "John", LastName = "Doe", Email = "john@example.com" };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.DeleteConfirmed(userId);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            _userManagerMock.Verify(x => x.DeleteAsync(user), Times.Once);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
} 