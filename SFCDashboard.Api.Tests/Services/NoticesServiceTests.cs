using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SFCDashboard.Api.Data;
using SFCDashboard.Api.Models;
using SFCDashboard.Api.Services;

namespace SFCDashboard.Api.Tests.Services
{
    public class NoticesServiceTests : IDisposable
    {
        private readonly SomsDbContext _context;
        private readonly Mock<ILogger<NoticesService>> _mockLogger;
        private readonly NoticesService _service;

        public NoticesServiceTests()
        {
            // Set up In-Memory Database
            var options = new DbContextOptionsBuilder<SomsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new SomsDbContext(options);
            _mockLogger = new Mock<ILogger<NoticesService>>();

            _service = new NoticesService(_context, _mockLogger.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task GetActiveNoticesAsync_ReturnsOnlyActiveAndNonExpiredNotices()
        {
            // Arrange
            var notices = new List<Notice>
            {
                new Notice { ID = 1, Title = "Active Notice 1", IsActive = true, CreatedDate = DateTime.Now.AddDays(-1), CreatedBy = 1, UpdatedBy = 1 },
                new Notice { ID = 2, Title = "Expired Notice", IsActive = true, ExpireDate = DateTime.Now.AddDays(-1), CreatedDate = DateTime.Now, CreatedBy = 1, UpdatedBy = 1 },
                new Notice { ID = 3, Title = "Inactive Notice", IsActive = false, CreatedDate = DateTime.Now, CreatedBy = 1, UpdatedBy = 1 },
                new Notice { ID = 4, Title = "Pinned Notice", IsActive = true, IsPinned = true, CreatedDate = DateTime.Now.AddDays(-2), CreatedBy = 1, UpdatedBy = 1 }
            };

            await _context.Notices.AddRangeAsync(notices);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetActiveNoticesAsync();
            var resultList = result.ToList();

            // Assert
            Assert.Equal(2, resultList.Count); // Should only get ID 1 and 4
            Assert.Contains(resultList, n => n.ID == 1);
            Assert.Contains(resultList, n => n.ID == 4);
            
            // Verify ordering: Pinned notice (ID 4) should be first
            Assert.Equal(4, resultList.First().ID);
        }

        [Fact]
        public async Task CreateNoticeAsync_AddsNoticeToDatabase()
        {
            // Arrange
            var newNotice = new Notice
            {
                Title = "Test Notice",
                Description = "Test Description",
                IsActive = true,
                CreatedBy = 1,
                UpdatedBy = 1,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            // Act
            var result = await _service.CreateNoticeAsync(newNotice);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(0, result.ID); // ID should be generated

            var dbNotice = await _context.Notices.FirstOrDefaultAsync(n => n.ID == result.ID);
            Assert.NotNull(dbNotice);
            Assert.Equal("Test Notice", dbNotice.Title);
        }

        [Fact]
        public async Task UpdateNoticeAsync_UpdatesExistingNotice()
        {
            // Arrange
            var existingNotice = new Notice { ID = 10, Title = "Old Title", IsActive = true, CreatedBy = 1, UpdatedBy = 1 };
            await _context.Notices.AddAsync(existingNotice);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.UpdateNoticeAsync(10, "New Title", "New Desc", 2, "AdminUser", null, null, true);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Title", result.Title);
            Assert.Equal("New Desc", result.Description);
            Assert.Equal(2, result.UpdatedBy);
            Assert.Equal("AdminUser", result.UpdatedUserName);

            var dbNotice = await _context.Notices.FindAsync(10);
            Assert.NotNull(dbNotice);
            Assert.Equal("New Title", dbNotice.Title);
        }

        [Fact]
        public async Task UpdateNoticeAsync_ReturnsNullIfNoticeNotFound()
        {
            // Act
            var result = await _service.UpdateNoticeAsync(999, "Title", "Desc", 1, "User", null, null, true);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteNoticeAsync_SoftDeletesNotice()
        {
            // Arrange
            var existingNotice = new Notice { ID = 20, Title = "To Delete", IsActive = true, CreatedBy = 1, UpdatedBy = 1 };
            await _context.Notices.AddAsync(existingNotice);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteNoticeAsync(20, 2, "AdminUser");

            // Assert
            Assert.True(result);

            var dbNotice = await _context.Notices.FindAsync(20);
            Assert.NotNull(dbNotice);
            Assert.False(dbNotice.IsActive); // Should be soft deleted
            Assert.Equal(2, dbNotice.UpdatedBy);
        }
    }
}
