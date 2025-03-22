using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Vacation_Manager.Data;
using Vacation_Manager.Models;
using Xunit;

namespace UnitTests
{
    public class VacationRequestControllerTests : IDisposable
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly ApplicationDbContext _context;
        private readonly Mock<IWebHostEnvironment> _webHostEnvironmentMock;
        private readonly VacationRequestController _controller;
        private readonly Mock<HttpContext> _httpContextMock;

        public VacationRequestControllerTests()
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

            // Setup IWebHostEnvironment mock
            _webHostEnvironmentMock = new Mock<IWebHostEnvironment>();
            _webHostEnvironmentMock.Setup(x => x.WebRootPath).Returns("webroot");

            // Setup real in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // Setup HttpContext mock for role checks
            _httpContextMock = new Mock<HttpContext>();
            var claimsPrincipalMock = new Mock<ClaimsPrincipal>();
            _httpContextMock.Setup(h => h.User).Returns(claimsPrincipalMock.Object);

            // Setup TempData for controller
            var tempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>()
            );

            _controller = new VacationRequestController(_context, _userManagerMock.Object, _webHostEnvironmentMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _httpContextMock.Object
                },
                TempData = tempData
            };
        }

        [Fact]
        public async Task Index_ReturnsViewWithUserRequests()
        {
            // Arrange
            var currentUser = new User { Id = "user1", FirstName = "Test", LastName = "User" };
            
            var requests = new List<VacationRequest>
            {
                new VacationRequest { RequestId = 1, RequesterId = "user1", Requester = currentUser },
                new VacationRequest { RequestId = 2, RequesterId = "user1", Requester = currentUser },
                new VacationRequest { RequestId = 3, RequesterId = "user2", Requester = new User { Id = "user2", FirstName = "Other", LastName = "User" } }
            };

            foreach (var request in requests)
            {
                await _context.VacationRequests.AddAsync(request);
            }
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);

            // Act
            var result = await _controller.Index(null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<VacationRequest>>(viewResult.Model);
            Assert.Equal(2, model.Count);
            Assert.All(model, request => Assert.Equal("user1", request.RequesterId));
        }

        [Fact]
        public async Task Details_WithValidId_ReturnsViewWithRequest()
        {
            // Arrange
            var currentUser = new User { Id = "user1", FirstName = "Test", LastName = "User" };
            var request = new VacationRequest
            {
                RequestId = 1,
                RequesterId = "user1",
                Requester = currentUser,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(5)
            };

            await _context.VacationRequests.AddAsync(request);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<VacationRequest>(viewResult.Model);
            Assert.Equal(1, model.RequestId);
        }

        [Fact]
        public async Task Details_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.Details(999); // Невалидно ID

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_Get_ReturnsView()
        {
            // Act
            var result = _controller.Create();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData["RequestTypes"]);
        }

        [Fact]
        public async Task Create_Post_WithStartDateAfterEndDate_ReturnsViewWithError()
        {
            // Arrange
            var currentUser = new User { Id = "user1", FirstName = "Test", LastName = "User" };
            _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);

            var request = new VacationRequest
            {
                StartDate = DateTime.Now.AddDays(5),  // Начална дата след крайната
                EndDate = DateTime.Now,
                RequestType = RequestType.Paid,
                Requester = currentUser
            };

            // Act
            var result = await _controller.Create(request, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<VacationRequest>(viewResult.Model);
            Assert.False(_context.VacationRequests.Any()); // Няма създадена заявка
        }
     

        [Fact]
        public async Task Edit_WithValidId_ReturnsViewWithRequest()
        {
            // Arrange
            var currentUser = new User { Id = "user1", FirstName = "Test", LastName = "User" };
            var request = new VacationRequest
            {
                RequestId = 1,
                RequesterId = "user1",
                Requester = currentUser,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(5),
                RequestType = RequestType.Paid,
                Approved = false
            };

            await _context.VacationRequests.AddAsync(request);
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);

            // Act
            var result = await _controller.Edit(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<VacationRequest>(viewResult.Model);
            Assert.Equal(1, model.RequestId);
        }

        [Fact]
        public async Task Edit_WithApprovedRequest_RedirectsToIndex()
        {
            // Arrange
            var currentUser = new User { Id = "user1", FirstName = "Test", LastName = "User" };
            var request = new VacationRequest
            {
                RequestId = 1,
                RequesterId = "user1",
                Requester = currentUser,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(5),
                RequestType = RequestType.Paid,
                Approved = true  // Заявката е вече одобрена
            };

            await _context.VacationRequests.AddAsync(request);
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);

            // Act
            var result = await _controller.Edit(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Contains("cannot be edited", _controller.TempData["ErrorMessage"]?.ToString());
        }

        [Fact]
        public async Task Edit_Post_WithStartDateAfterEndDate_ReturnsViewWithError()
        {
            // Arrange
            var currentUser = new User { Id = "user1", FirstName = "Test", LastName = "User" };
            var originalRequest = new VacationRequest
            {
                RequestId = 1,
                RequesterId = "user1",
                Requester = currentUser,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(5),
                RequestType = RequestType.Paid,
                Approved = false
            };

            await _context.VacationRequests.AddAsync(originalRequest);
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);

            var editedRequest = new VacationRequest
            {
                RequestId = 1,
                Requester = currentUser,
                StartDate = DateTime.Now.AddDays(10),  // След крайната дата
                EndDate = DateTime.Now.AddDays(5),
                RequestType = RequestType.Paid
            };

            // Act
            var result = await _controller.Edit(1, editedRequest, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<VacationRequest>(viewResult.Model);
            
            var dbRequest = await _context.VacationRequests.FindAsync(1);
            Assert.Equal(DateTime.Now.Date, dbRequest.StartDate.Date); // Оригиналната дата не е променена
        }

        [Fact]
        public async Task Delete_WithValidId_ReturnsViewWithRequest()
        {
            // Arrange
            var currentUser = new User { Id = "user1", FirstName = "Test", LastName = "User" };
            var request = new VacationRequest
            {
                RequestId = 1,
                RequesterId = "user1",
                Requester = currentUser,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(5),
                Approved = false
            };

            await _context.VacationRequests.AddAsync(request);
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);
            _httpContextMock.Setup(h => h.User.IsInRole(It.IsAny<string>())).Returns(false);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<VacationRequest>(viewResult.Model);
            Assert.Equal(1, model.RequestId);
        }

        [Fact]
        public async Task DeleteConfirmed_DeletesRequest()
        {
            // Arrange
            var currentUser = new User { Id = "user1", FirstName = "Test", LastName = "User" };
            var request = new VacationRequest
            {
                RequestId = 1,
                RequesterId = "user1",
                Requester = currentUser,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(5),
                Approved = false
            };

            await _context.VacationRequests.AddAsync(request);
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);
            _httpContextMock.Setup(h => h.User.IsInRole(It.IsAny<string>())).Returns(false);

            // Act
            var result = await _controller.DeleteConfirmed(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.False(await _context.VacationRequests.AnyAsync(v => v.RequestId == 1));
        }

        [Fact]
        public async Task DeleteConfirmed_ApprovedByRequester_RedirectsToIndex()
        {
            // Arrange
            var currentUser = new User { Id = "user1", FirstName = "Test", LastName = "User" };
            var request = new VacationRequest
            {
                RequestId = 1,
                RequesterId = "user1",
                Requester = currentUser,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(5),
                Approved = true  // Заявката е одобрена
            };

            await _context.VacationRequests.AddAsync(request);
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);
            _httpContextMock.Setup(h => h.User.IsInRole(It.IsAny<string>())).Returns(false);

            // Act
            var result = await _controller.DeleteConfirmed(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Contains("cannot be deleted", _controller.TempData["ErrorMessage"]?.ToString());
            Assert.True(await _context.VacationRequests.AnyAsync(v => v.RequestId == 1)); // Заявката не е изтрита
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
} 