using FinancialApplication.Domain.Domain.Entity;
using FinancialApplication.Infrastructure.Data;
using FinancialApplication.Infrastructure.Services;
using FinancialApplication.Application.Interfaces;
using FinancialApp.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FinancialApplication.Tests
{
    public class AdminServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly AdminService _adminService;

        public AdminServiceTests()
        {
            // Create a unique in-memory database for each test instance
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);

            // Seed roles (matching the real seed data in AppDbContext)
            _context.Roles.AddRange(
                new Role { Id = 1, Name = "User", IsActive = true },
                new Role { Id = 2, Name = "Admin", IsActive = true },
                new Role { Id = 3, Name = "Manager", IsActive = true },
                new Role { Id = 4, Name = "Auditor", IsActive = true }
            );
            _context.SaveChanges();

            // Create AdminService with mocked dependencies (not needed for RevokeRoleAsync)
            var mockTokenGenerator = new Mock<IJwtTokenGenerator>();
            var refreshTokenGenerator = new RefreshTokenGenerator(); // Concrete class, no need to mock
            var mockPasswordHasher = new Mock<IPasswordHasher>();
            var mockConfiguration = new Mock<IConfiguration>();

            _adminService = new AdminService(
                _context,
                mockTokenGenerator.Object,
                refreshTokenGenerator,
                mockPasswordHasher.Object,
                mockConfiguration.Object
            );
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private User CreateTestUser(int roleId, string username = "testuser")
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = $"{username}@test.com",
                Password = "hashedpassword",
                RoleId = roleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            _context.SaveChanges();
            return user;
        }

        // =====================================================
        // RevokeRoleAsync Tests
        // =====================================================

        [Fact]
        public async Task RevokeRoleAsync_SetsRoleIdTo1_NotZero()
        {
            // Arrange: user with Admin role (RoleId = 2)
            var user = CreateTestUser(roleId: 2, username: "adminuser");

            // Act
            await _adminService.RevokeRoleAsync(user.Id, "Admin");

            // Assert: RoleId should be 1 (User), NOT 0
            var updatedUser = await _context.Users.FindAsync(user.Id);
            Assert.NotNull(updatedUser);
            Assert.Equal(1, updatedUser.RoleId);
        }

        [Fact]
        public async Task RevokeRoleAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var nonExistentUserId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _adminService.RevokeRoleAsync(nonExistentUserId, "Admin")
            );
        }

        [Fact]
        public async Task RevokeRoleAsync_RoleNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            var user = CreateTestUser(roleId: 2, username: "adminuser2");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _adminService.RevokeRoleAsync(user.Id, "NonExistentRole")
            );
        }

        [Fact]
        public async Task RevokeRoleAsync_UserDoesNotHaveRole_ThrowsInvalidOperationException()
        {
            // Arrange: user has "User" role (1), trying to revoke "Admin" role (2)
            var user = CreateTestUser(roleId: 1, username: "regularuser");

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _adminService.RevokeRoleAsync(user.Id, "Admin")
            );
            Assert.Contains("does not have role", ex.Message);
        }

        [Fact]
        public async Task RevokeRoleAsync_CannotRevokeDefaultUserRole_ThrowsInvalidOperationException()
        {
            // Arrange: user has "User" role (1), trying to revoke "User" role
            var user = CreateTestUser(roleId: 1, username: "baseuser");

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _adminService.RevokeRoleAsync(user.Id, "User")
            );
            Assert.Contains("Cannot revoke the default", ex.Message);
        }

        [Fact]
        public async Task RevokeRoleAsync_DoesNotMutateRoleTable()
        {
            // Arrange: user with Manager role (RoleId = 3)
            var user = CreateTestUser(roleId: 3, username: "manageruser");

            // Act
            await _adminService.RevokeRoleAsync(user.Id, "Manager");

            // Assert: the "Manager" role in the Roles table should be unchanged
            var managerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Id == 3);
            Assert.NotNull(managerRole);
            Assert.Equal("Manager", managerRole.Name); // Name should NOT have been mutated
        }

        [Fact]
        public async Task RevokeRoleAsync_UpdatesUpdatedAtTimestamp()
        {
            // Arrange
            var user = CreateTestUser(roleId: 2, username: "timestampuser");
            var originalUpdatedAt = user.UpdatedAt;

            // Small delay to ensure timestamp difference
            await Task.Delay(10);

            // Act
            await _adminService.RevokeRoleAsync(user.Id, "Admin");

            // Assert
            var updatedUser = await _context.Users.FindAsync(user.Id);
            Assert.NotNull(updatedUser);
            Assert.True(updatedUser.UpdatedAt >= originalUpdatedAt);
        }

        [Fact]
        public async Task RevokeRoleAsync_ReturnsDescriptiveMessage()
        {
            // Arrange
            var user = CreateTestUser(roleId: 4, username: "auditoruser");

            // Act
            var result = await _adminService.RevokeRoleAsync(user.Id, "Auditor");

            // Assert
            Assert.Contains("Auditor", result);
            Assert.Contains("auditoruser", result);
            Assert.Contains("User", result); // mentions the fallback role
        }

        [Fact]
        public async Task RevokeRoleAsync_RevokeManagerRole_ResetsToUser()
        {
            // Arrange
            var user = CreateTestUser(roleId: 3, username: "mgr");

            // Act
            await _adminService.RevokeRoleAsync(user.Id, "Manager");

            // Assert
            var updatedUser = await _context.Users.FindAsync(user.Id);
            Assert.NotNull(updatedUser);
            Assert.Equal(1, updatedUser.RoleId);
        }

        [Fact]
        public async Task RevokeRoleAsync_RevokeAuditorRole_ResetsToUser()
        {
            // Arrange
            var user = CreateTestUser(roleId: 4, username: "aud");

            // Act
            await _adminService.RevokeRoleAsync(user.Id, "Auditor");

            // Assert
            var updatedUser = await _context.Users.FindAsync(user.Id);
            Assert.NotNull(updatedUser);
            Assert.Equal(1, updatedUser.RoleId);
        }
    }
}
