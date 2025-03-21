using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vacation_Manager.Controllers;
using Vacation_Manager.Models;
using Xunit;

namespace UnitTests
{
    public class RoleControllerTests
    {
        private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly RoleController _controller;

        public RoleControllerTests()
        {
            // Setup RoleManager mock
            var roleStore = new Mock<IRoleStore<IdentityRole>>();
            var roleValidators = new List<IRoleValidator<IdentityRole>>();
            _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                roleStore.Object,
                roleValidators,
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null
            );

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

            _controller = new RoleController(_roleManagerMock.Object, _userManagerMock.Object);
        }


        [Fact]
        public async Task UsersInRole_WithValidId_ReturnsViewWithUsers()
        {
            // Arrange
            var roleId = "1";
            var roleName = "CEO";
            var role = new IdentityRole { Id = roleId, Name = roleName };

            _roleManagerMock.Setup(rm => rm.FindByIdAsync(roleId))
                .ReturnsAsync(role);

            var users = new List<User>
            {
                new User { Id = "1", UserName = "user1", FirstName = "Иван", LastName = "Иванов" },
                new User { Id = "2", UserName = "user2", FirstName = "Петър", LastName = "Петров" }
            };

            _userManagerMock.Setup(um => um.GetUsersInRoleAsync(roleName))
                .ReturnsAsync(users);

            // Act
            var result = await _controller.UsersInRole(roleId, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<User>>(viewResult.Model);
            Assert.Equal(2, model.Count);
            Assert.Equal("Иван", model[0].FirstName);
            Assert.Equal(roleName, viewResult.ViewData["RoleName"]);
            Assert.Equal(roleId, viewResult.ViewData["RoleId"]);
        }

        [Fact]
        public async Task UsersInRole_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidRoleId = "999";
            _roleManagerMock.Setup(rm => rm.FindByIdAsync(invalidRoleId))
                .ReturnsAsync((IdentityRole)null);

            // Act
            var result = await _controller.UsersInRole(invalidRoleId, null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

      

        [Fact]
        public async Task UsersInRole_WithEmptySearchString_ReturnsAllUsers()
        {
            // Arrange
            var roleId = "1";
            var roleName = "CEO";
            var role = new IdentityRole { Id = roleId, Name = roleName };
            var searchString = "";

            _roleManagerMock.Setup(rm => rm.FindByIdAsync(roleId))
                .ReturnsAsync(role);

            var users = new List<User>
            {
                new User { Id = "1", UserName = "user1", FirstName = "Иван", LastName = "Иванов" },
                new User { Id = "2", UserName = "user2", FirstName = "Петър", LastName = "Петров" }
            };

            _userManagerMock.Setup(um => um.GetUsersInRoleAsync(roleName))
                .ReturnsAsync(users);

            // Act
            var result = await _controller.UsersInRole(roleId, searchString);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<User>>(viewResult.Model);
            Assert.Equal(2, model.Count());
        }
    }
} 