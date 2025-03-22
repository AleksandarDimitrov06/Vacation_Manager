using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Vacation_Manager.Data;
using Vacation_Manager.Models;
using Xunit;

namespace UnitTests
{
    public class RequestApprovalControllerTests : IDisposable
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly ApplicationDbContext _context;
        private readonly RequestApprovalController _controller;
        private readonly Mock<HttpContext> _httpContextMock;
        private readonly ClaimsPrincipal _claimsPrincipal;

        public RequestApprovalControllerTests()
        {
            // Setup UserManager mock с допълнителна конфигурация
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

            // Подобрена настройка на HttpContext mock с ClaimsPrincipal
            _httpContextMock = new Mock<HttpContext>();
            var claimsPrincipalMock = new Mock<ClaimsPrincipal>();
            _claimsPrincipal = claimsPrincipalMock.Object;
            _httpContextMock.Setup(h => h.User).Returns(_claimsPrincipal);

            // Setup TempData for controller
            var tempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>()
            );

            _controller = new RequestApprovalController(_context, _userManagerMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _httpContextMock.Object
                },
                TempData = tempData
            };
        }

        [Fact]
        public async Task Index_AsCEO_ReturnsAllRequests()
        {
            // Arrange
            var currentUser = new User { Id = "user1", FirstName = "Admin", LastName = "User" };
            var requests = new List<VacationRequest>
            {
                new VacationRequest { 
                    RequestId = 1, 
                    RequesterId = "user2", 
                    Requester = new User { Id = "user2", FirstName = "User", LastName = "Two" },
                    CreationDate = DateTime.Now
                },
                new VacationRequest { 
                    RequestId = 2, 
                    RequesterId = "user3", 
                    Requester = new User { Id = "user3", FirstName = "User", LastName = "Three" },
                    CreationDate = DateTime.Now
                }
            };

            // Добавяме потребителите първо, за да се създадат правилно релациите
            foreach (var request in requests)
            {
                await _context.Users.AddAsync(request.Requester);
            }
            await _context.SaveChangesAsync();

            foreach (var request in requests)
            {
                await _context.VacationRequests.AddAsync(request);
            }
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);
            
            _httpContextMock.Setup(h => h.User.IsInRole("CEO")).Returns(true);

            // Act
            var result = await _controller.Index(null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<VacationRequest>>(viewResult.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task Index_AsTeamLead_ReturnsOnlyTeamRequests()
        {
            // Arrange
            var teamId = 1;
            var teamLeader = new User { Id = "leader1", FirstName = "Team", LastName = "Leader" };
            var teamMember = new User { Id = "member1", FirstName = "Team", LastName = "Member", TeamId = teamId };
            var otherMember = new User { Id = "other1", FirstName = "Other", LastName = "Member", TeamId = 2 };
            
            var team = new Team { 
                TeamId = teamId, 
                TeamName = "Test Team", 
                TeamLeaderId = "leader1" 
            };

            var requests = new List<VacationRequest>
            {
                new VacationRequest { 
                    RequestId = 1, 
                    RequesterId = "member1", 
                    Requester = teamMember,
                    CreationDate = DateTime.Now
                },
                new VacationRequest { 
                    RequestId = 2, 
                    RequesterId = "other1", 
                    Requester = otherMember,
                    CreationDate = DateTime.Now
                }
            };

            // Добавяме потребителите и екипа първо
            await _context.Users.AddAsync(teamLeader);
            await _context.Users.AddAsync(teamMember);
            await _context.Users.AddAsync(otherMember);
            await _context.SaveChangesAsync();

            await _context.Teams.AddAsync(team);
            await _context.SaveChangesAsync();

            // Актуализирайте екипите и връзките
            teamMember.TeamId = teamId;
            otherMember.TeamId = 2;
            await _context.SaveChangesAsync();

            foreach (var request in requests)
            {
                await _context.VacationRequests.AddAsync(request);
            }
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(teamLeader);
            
            _httpContextMock.Setup(h => h.User.IsInRole("CEO")).Returns(false);

            // Act
            var result = await _controller.Index(null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<VacationRequest>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal(1, model[0].RequestId);
        }

        [Fact]
        public async Task Index_AsTeamLead_FiltersSelfRequests()
        {
            // Arrange
            var teamId = 1;
            var teamLeader = new User { Id = "leader1", FirstName = "Team", LastName = "Leader" };
            var teamMember = new User { Id = "member1", FirstName = "Team", LastName = "Member", TeamId = teamId };
            
            var team = new Team { 
                TeamId = teamId, 
                TeamName = "Test Team", 
                TeamLeaderId = "leader1" 
            };

            var requests = new List<VacationRequest>
            {
                new VacationRequest { 
                    RequestId = 1, 
                    RequesterId = "member1", 
                    Requester = teamMember,
                    CreationDate = DateTime.Now
                },
                new VacationRequest { 
                    RequestId = 2, 
                    RequesterId = "leader1", 
                    Requester = teamLeader,
                    CreationDate = DateTime.Now
                }
            };

            // Добавяме потребителите и екипа първо
            await _context.Users.AddAsync(teamLeader);
            await _context.Users.AddAsync(teamMember);
            await _context.SaveChangesAsync();

            await _context.Teams.AddAsync(team);
            await _context.SaveChangesAsync();

            // Актуализирайте екипите и връзките
            teamMember.TeamId = teamId;
            teamLeader.TeamId = teamId;
            await _context.SaveChangesAsync();

            foreach (var request in requests)
            {
                await _context.VacationRequests.AddAsync(request);
            }
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(teamLeader);
            
            _httpContextMock.Setup(h => h.User.IsInRole("CEO")).Returns(false);

            // Act
            var result = await _controller.Index(null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<VacationRequest>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal(1, model[0].RequestId);  // Само заявката на редовия член, не и на лидера
        }

        [Fact]
        public async Task Approve_CanApproveAsTeamLead()
        {
            // Arrange
            var teamId = 1;
            var teamLeader = new User { Id = "leader1", FirstName = "Team", LastName = "Leader" };
            var teamMember = new User { Id = "member1", FirstName = "Team", LastName = "Member", TeamId = teamId };
            
            var team = new Team { 
                TeamId = teamId, 
                TeamName = "Test Team", 
                TeamLeaderId = "leader1" 
            };

            var request = new VacationRequest { 
                RequestId = 1, 
                RequesterId = "member1", 
                Requester = teamMember,
                Approved = false,
                CreationDate = DateTime.Now
            };

            // Добавяме потребителите и екипа първо
            await _context.Users.AddAsync(teamLeader);
            await _context.Users.AddAsync(teamMember);
            await _context.SaveChangesAsync();

            await _context.Teams.AddAsync(team);
            await _context.SaveChangesAsync();

            // Актуализирайте екипите и връзките
            teamMember.TeamId = teamId;
            await _context.SaveChangesAsync();

            await _context.VacationRequests.AddAsync(request);
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(teamLeader);
            
            _httpContextMock.Setup(h => h.User.IsInRole("CEO")).Returns(false);

            // Act
            var result = await _controller.Approve(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            // Проверка дали заявката е одобрена - изрично презареждаме от базата данни
            var updatedRequest = await _context.VacationRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RequestId == 1);
                
            Assert.True(updatedRequest.Approved);
            Assert.Equal("leader1", updatedRequest.ApproverId);
        }

        [Fact]
        public async Task Approve_CannotApproveSelfRequest()
        {
            // Arrange
            var teamId = 1;
            var teamLeader = new User { Id = "leader1", FirstName = "Team", LastName = "Leader", TeamId = teamId };
            
            var team = new Team { 
                TeamId = teamId, 
                TeamName = "Test Team", 
                TeamLeaderId = "leader1" 
            };

            var request = new VacationRequest { 
                RequestId = 1, 
                RequesterId = "leader1", 
                Requester = teamLeader,
                Approved = false,
                CreationDate = DateTime.Now
            };

            // Добавяме потребителя и екипа първо
            await _context.Users.AddAsync(teamLeader);
            await _context.SaveChangesAsync();

            await _context.Teams.AddAsync(team);
            await _context.SaveChangesAsync();

            // Актуализирайте екипите и връзките
            teamLeader.TeamId = teamId;
            await _context.SaveChangesAsync();

            await _context.VacationRequests.AddAsync(request);
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(teamLeader);
            
            _httpContextMock.Setup(h => h.User.IsInRole("CEO")).Returns(false);

            // Act
            var result = await _controller.Approve(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            // Проверка дали заявката НЕ е одобрена
            var updatedRequest = await _context.VacationRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RequestId == 1);
                
            Assert.False(updatedRequest.Approved);
            Assert.Null(updatedRequest.ApproverId);
            Assert.Contains("Не можете да одобрите собствената си заявка", _controller.TempData["ErrorMessage"]?.ToString());
        }

        [Fact]
        public async Task Approve_NotFoundWithInvalidId()
        {
            // Arrange
            var currentUser = new User { Id = "user1", FirstName = "Admin", LastName = "User" };
            _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            // Act
            var result = await _controller.Approve(999); // Несъществуващо ID

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
} 