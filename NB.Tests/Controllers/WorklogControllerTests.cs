using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.WorklogService;
using NB.Service.WorklogService.Dto;
using NB.Service.WorklogService.ViewModels;
using Xunit;
using FluentAssertions;

namespace NB.Tests.Controllers
{
    public class WorklogControllerTests
    {
        private readonly Mock<IWorklogService> _worklogMock = new();
        private readonly Mock<ILogger<WorklogController>> _loggerMock = new();

        private WorklogController CreateController()
        {
            return new WorklogController(
                _worklogMock.Object,
                _loggerMock.Object
            );
        }

        #region CreateWorklog Tests

        [Fact]
        public async Task CreateWorklog_WithValidDto_ReturnsOk()
        {
            // Arrange
            var controller = CreateController();
            var dto = new CreateWorklogBatchDto
            {
                EmployeeId = 1,
                WorkDate = DateTime.Today,
                Jobs = new List<WorklogJobItemDto>
                {
                    new WorklogJobItemDto { JobId = 1, Quantity = 1, Note = "Test job" }
                }
            };
            var response = new CreateWorklogBatchResponseVM
            {
                TotalCount = 1,
                Worklogs = new List<WorklogResponseVM>()
            };

            _worklogMock.Setup(w => w.CreateWorklogBatchAsync(dto)).ReturnsAsync(response);

            // Act
            var result = await controller.CreateWorklog(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task CreateWorklog_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            var dto = new CreateWorklogBatchDto
            {
                EmployeeId = 1,
                WorkDate = DateTime.Today,
                Jobs = new List<WorklogJobItemDto>
                {
                    new WorklogJobItemDto { JobId = 999, Quantity = 1 }
                }
            };

            _worklogMock.Setup(w => w.CreateWorklogBatchAsync(dto))
                .ThrowsAsync(new Exception("Không tìm thấy Job"));

            // Act
            var result = await controller.CreateWorklog(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task CreateWorklog_WithMultipleJobs_ReturnsOk()
        {
            // Arrange
            var controller = CreateController();
            var dto = new CreateWorklogBatchDto
            {
                EmployeeId = 1,
                WorkDate = DateTime.Today,
                Jobs = new List<WorklogJobItemDto>
                {
                    new WorklogJobItemDto { JobId = 1, Quantity = 1, Note = "Job 1" },
                    new WorklogJobItemDto { JobId = 2, Quantity = 5, Note = "Job 2" }
                }
            };
            var response = new CreateWorklogBatchResponseVM
            {
                TotalCount = 2,
                Worklogs = new List<WorklogResponseVM>()
            };

            _worklogMock.Setup(w => w.CreateWorklogBatchAsync(dto)).ReturnsAsync(response);

            // Act
            var result = await controller.CreateWorklog(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        #endregion

        #region GetWorklogsByEmployeeAndDate Tests

        [Fact]
        public async Task GetWorklogsByEmployeeAndDate_WithValidDto_ReturnsOk()
        {
            // Arrange
            var controller = CreateController();
            var dto = new GetWorklogsByEmployeeDto
            {
                EmployeeId = 1,
                WorkDate = DateTime.Today
            };
            var worklogs = new List<WorklogResponseVM>
            {
                new WorklogResponseVM
                {
                    Id = 1,
                    EmployeeId = 1,
                    EmployeeName = "John Doe",
                    JobId = 1,
                    JobName = "Packaging",
                    WorkDate = DateTime.Today,
                    Quantity = 1,
                    TotalAmount = 50000,
                    IsActive = true
                }
            };

            _worklogMock.Setup(w => w.GetWorklogsByEmployeeAndDateAsync(1, DateTime.Today))
                .ReturnsAsync(worklogs);

            // Act
            var result = await controller.GetWorklogsByEmployeeAndDate(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetWorklogsByEmployeeAndDate_WithNoWorklogs_ReturnsOkWithEmptyList()
        {
            // Arrange
            var controller = CreateController();
            var dto = new GetWorklogsByEmployeeDto
            {
                EmployeeId = 1,
                WorkDate = DateTime.Today
            };

            _worklogMock.Setup(w => w.GetWorklogsByEmployeeAndDateAsync(1, DateTime.Today))
                .ReturnsAsync(new List<WorklogResponseVM>());

            // Act
            var result = await controller.GetWorklogsByEmployeeAndDate(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetWorklogsByEmployeeAndDate_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            var dto = new GetWorklogsByEmployeeDto
            {
                EmployeeId = 999,
                WorkDate = DateTime.Today
            };

            _worklogMock.Setup(w => w.GetWorklogsByEmployeeAndDateAsync(999, DateTime.Today))
                .ThrowsAsync(new Exception("Không tìm thấy nhân viên"));

            // Act
            var result = await controller.GetWorklogsByEmployeeAndDate(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        #endregion

        #region GetWorklogsByDate Tests

        [Fact]
        public async Task GetWorklogsByDate_WithValidDate_ReturnsOk()
        {
            // Arrange
            var controller = CreateController();
            var dto = new GetWorklogsByDateDto { WorkDate = DateTime.Today };
            var worklogs = new List<WorklogResponseVM>
            {
                new WorklogResponseVM { Id = 1, EmployeeId = 1, EmployeeName = "John", JobId = 1, WorkDate = DateTime.Today },
                new WorklogResponseVM { Id = 2, EmployeeId = 2, EmployeeName = "Jane", JobId = 1, WorkDate = DateTime.Today }
            };

            _worklogMock.Setup(w => w.GetWorklogsByDateAsync(DateTime.Today))
                .ReturnsAsync(worklogs);

            // Act
            var result = await controller.GetWorklogsByDate(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetWorklogsByDate_WithNoWorklogs_ReturnsOkWithEmptyList()
        {
            // Arrange
            var controller = CreateController();
            var dto = new GetWorklogsByDateDto { WorkDate = DateTime.Today.AddDays(-30) };

            _worklogMock.Setup(w => w.GetWorklogsByDateAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<WorklogResponseVM>());

            // Act
            var result = await controller.GetWorklogsByDate(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetWorklogsByDate_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            var dto = new GetWorklogsByDateDto { WorkDate = DateTime.Today };

            _worklogMock.Setup(w => w.GetWorklogsByDateAsync(DateTime.Today))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await controller.GetWorklogsByDate(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        #endregion

        #region GetWorklogById Tests

        [Fact]
        public async Task GetWorklogById_WithValidId_ReturnsOk()
        {
            // Arrange
            var controller = CreateController();
            var worklog = new WorklogResponseVM
            {
                Id = 1,
                EmployeeId = 1,
                EmployeeName = "John Doe",
                JobId = 1,
                JobName = "Packaging",
                WorkDate = DateTime.Today,
                Quantity = 1,
                TotalAmount = 50000,
                IsActive = true
            };

            _worklogMock.Setup(w => w.GetWorklogByIdAsync(1)).ReturnsAsync(worklog);

            // Act
            var result = await controller.GetWorklogById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetWorklogById_WithInvalidId_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();

            _worklogMock.Setup(w => w.GetWorklogByIdAsync(999))
                .ThrowsAsync(new Exception("Không tìm thấy worklog"));

            // Act
            var result = await controller.GetWorklogById(999);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        #endregion

        #region UpdateWorklog Tests

        [Fact]
        public async Task UpdateWorklog_WithValidDto_ReturnsOk()
        {
            // Arrange
            var controller = CreateController();
            var dto = new UpdateWorklogDto
            {
                EmployeeId = 1,
                WorkDate = DateTime.Today,
                JobId = 1,
                Quantity = 10,
                Note = "Updated note"
            };
            var updatedWorklog = new WorklogResponseVM
            {
                Id = 1,
                EmployeeId = 1,
                JobId = 1,
                WorkDate = DateTime.Today,
                Quantity = 10,
                Note = "Updated note"
            };

            _worklogMock.Setup(w => w.UpdateWorklogAsync(dto)).ReturnsAsync(updatedWorklog);

            // Act
            var result = await controller.UpdateWorklog(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task UpdateWorklog_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            var dto = new UpdateWorklogDto
            {
                EmployeeId = 999,
                WorkDate = DateTime.Today,
                JobId = 1,
                Quantity = 5
            };

            _worklogMock.Setup(w => w.UpdateWorklogAsync(dto))
                .ThrowsAsync(new Exception("Không tìm thấy worklog"));

            // Act
            var result = await controller.UpdateWorklog(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        #endregion

        #region UpdateWorklogBatch Tests

        [Fact]
        public async Task UpdateWorklogBatch_WithValidDto_ReturnsOk()
        {
            // Arrange
            var controller = CreateController();
            var dto = new UpdateWorklogBatchDto
            {
                EmployeeId = 1,
                WorkDate = DateTime.Today,
                Jobs = new List<WorklogJobItemDto>
                {
                    new WorklogJobItemDto { JobId = 1, Quantity = 5, Note = "Updated" }
                }
            };
            var response = new CreateWorklogBatchResponseVM
            {
                TotalCount = 1,
                Worklogs = new List<WorklogResponseVM>()
            };

            _worklogMock.Setup(w => w.UpdateWorklogBatchAsync(dto)).ReturnsAsync(response);

            // Act
            var result = await controller.UpdateWorklogBatch(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task UpdateWorklogBatch_WithEmptyJobsList_ReturnsOk()
        {
            // Arrange
            var controller = CreateController();
            var dto = new UpdateWorklogBatchDto
            {
                EmployeeId = 1,
                WorkDate = DateTime.Today,
                Jobs = new List<WorklogJobItemDto>()
            };
            var response = new CreateWorklogBatchResponseVM
            {
                TotalCount = 0,
                Worklogs = new List<WorklogResponseVM>()
            };

            _worklogMock.Setup(w => w.UpdateWorklogBatchAsync(dto)).ReturnsAsync(response);

            // Act
            var result = await controller.UpdateWorklogBatch(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task UpdateWorklogBatch_WithMixedOperations_ReturnsOk()
        {
            // Arrange
            var controller = CreateController();
            var dto = new UpdateWorklogBatchDto
            {
                EmployeeId = 1,
                WorkDate = DateTime.Today,
                Jobs = new List<WorklogJobItemDto>
                {
                    new WorklogJobItemDto { JobId = 1, Quantity = 5, Note = "Updated" },
                    new WorklogJobItemDto { JobId = 2, Quantity = 10, Note = "New job" }
                }
            };
            var response = new CreateWorklogBatchResponseVM
            {
                TotalCount = 2,
                Worklogs = new List<WorklogResponseVM>()
            };

            _worklogMock.Setup(w => w.UpdateWorklogBatchAsync(dto)).ReturnsAsync(response);

            // Act
            var result = await controller.UpdateWorklogBatch(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task UpdateWorklogBatch_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            var dto = new UpdateWorklogBatchDto
            {
                EmployeeId = 999,
                WorkDate = DateTime.Today,
                Jobs = new List<WorklogJobItemDto>()
            };

            _worklogMock.Setup(w => w.UpdateWorklogBatchAsync(dto))
                .ThrowsAsync(new Exception("Không tìm thấy nhân viên"));

            // Act
            var result = await controller.UpdateWorklogBatch(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        #endregion

        #region ConfirmWorklog Tests

        [Fact]
        public async Task ConfirmWorklog_WithValidDto_ReturnsOk()
        {
            // Arrange
            var controller = CreateController();
            var dto = new ConfirmWorklogDto
            {
                EmployeeId = 1,
                WorkDate = DateTime.Today
            };
            var confirmedWorklogs = new List<WorklogResponseVM>
            {
                new WorklogResponseVM
                {
                    Id = 1,
                    EmployeeId = 1,
                    JobId = 1,
                    WorkDate = DateTime.Today,
                    IsActive = true
                },
                new WorklogResponseVM
                {
                    Id = 2,
                    EmployeeId = 1,
                    JobId = 2,
                    WorkDate = DateTime.Today,
                    IsActive = true
                }
            };

            _worklogMock.Setup(w => w.ConfirmWorklogAsync(dto)).ReturnsAsync(confirmedWorklogs);

            // Act
            var result = await controller.ConfirmWorklog(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task ConfirmWorklog_WithNoWorklogs_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            var dto = new ConfirmWorklogDto
            {
                EmployeeId = 2,
                WorkDate = DateTime.Today
            };

            _worklogMock.Setup(w => w.ConfirmWorklogAsync(dto))
                .ThrowsAsync(new Exception("Không tìm thấy worklog nào để xác nhận"));

            // Act
            var result = await controller.ConfirmWorklog(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task ConfirmWorklog_WithAlreadyConfirmedWorklogs_ReturnsOk()
        {
            // Arrange
            var controller = CreateController();
            var dto = new ConfirmWorklogDto
            {
                EmployeeId = 3,
                WorkDate = DateTime.Today
            };
            var confirmedWorklogs = new List<WorklogResponseVM>
            {
                new WorklogResponseVM
                {
                    Id = 1,
                    EmployeeId = 3,
                    JobId = 1,
                    WorkDate = DateTime.Today,
                    IsActive = true
                }
            };

            _worklogMock.Setup(w => w.ConfirmWorklogAsync(dto)).ReturnsAsync(confirmedWorklogs);

            // Act
            var result = await controller.ConfirmWorklog(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task ConfirmWorklog_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            var dto = new ConfirmWorklogDto
            {
                EmployeeId = 999,
                WorkDate = DateTime.Today
            };

            _worklogMock.Setup(w => w.ConfirmWorklogAsync(dto))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await controller.ConfirmWorklog(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        #endregion
    }
}
