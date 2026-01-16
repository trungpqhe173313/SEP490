using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NB.API.Controllers;
using NB.Service.Common;
using NB.Service.Dto;
using NB.Service.JobService;
using NB.Service.JobService.Dto;
using Xunit;

namespace NB.Tests.Controllers
{
    public class JobControllerTests
    {
        private readonly Mock<IJobService> _mockJobService;
        private readonly Mock<ILogger<JobController>> _mockLogger;
        private readonly JobController _controller;

        public JobControllerTests()
        {
            _mockJobService = new Mock<IJobService>();
            _mockLogger = new Mock<ILogger<JobController>>();
            _controller = new JobController(_mockJobService.Object, _mockLogger.Object);
        }

        /// <summary>
        /// TCID01: Kiem tra lay danh sach cong viec tra ve OK voi danh sach thanh cong
        /// Input: Khong co tham so
        /// Expected: Tra ve OkObjectResult voi Success true va Data co 2 JobDto
        /// </summary>
        [Fact]
        public async Task GetAllJobs_WithValidRequest_ReturnsOkWithList()
        {
            // Arrange
            var jobs = new List<JobDto>
            {
                new JobDto { Id = 1, JobName = "Cong viec 1", PayType = "Per_Ngay", Rate = 100000, IsActive = true },
                new JobDto { Id = 2, JobName = "Cong viec 2", PayType = "Per_Tan", Rate = 200000, IsActive = true }
            };
            _mockJobService.Setup(s => s.GetAllJobsAsync()).ReturnsAsync(jobs);

            // Act
            var result = await _controller.GetAllJobs();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<List<JobDto>>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().HaveCount(2);
            apiResponse.Data.Should().BeEquivalentTo(jobs);
        }

        /// <summary>
        /// TCID02: Kiem tra lay danh sach cong viec khi co loi xay ra
        /// Input: Service nem exception voi thong bao loi
        /// Expected: Tra ve BadRequestObjectResult voi Success false va Message chua loi
        /// </summary>
        [Fact]
        public async Task GetAllJobs_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            _mockJobService.Setup(s => s.GetAllJobsAsync()).ThrowsAsync(new Exception("Loi lay danh sach"));

            // Act
            var result = await _controller.GetAllJobs();

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var apiResponse = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<List<JobDto>>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Loi lay danh sach");
        }

        /// <summary>
        /// TCID03: Kiem tra lay chi tiet cong viec theo ID tra ve OK voi du lieu thanh cong
        /// Input: Id cong viec hop le la 1
        /// Expected: Tra ve OkObjectResult voi Success true va Data chua JobDto co Id 1
        /// </summary>
        [Fact]
        public async Task GetJobById_WithValidId_ReturnsOkWithJob()
        {
            // Arrange
            var job = new JobDto { Id = 1, JobName = "Cong viec 1", PayType = "Per_Ngay", Rate = 100000, IsActive = true };
            _mockJobService.Setup(s => s.GetJobByIdAsync(1)).ReturnsAsync(job);

            // Act
            var result = await _controller.GetJobById(1);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<JobDto>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().BeEquivalentTo(job);
        }

        /// <summary>
        /// TCID04: Kiem tra lay chi tiet cong viec khi co loi xay ra
        /// Input: Id cong viec la 1 va service nem exception
        /// Expected: Tra ve BadRequestObjectResult voi Success false va Message chua loi
        /// </summary>
        [Fact]
        public async Task GetJobById_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            _mockJobService.Setup(s => s.GetJobByIdAsync(1)).ThrowsAsync(new Exception("Khong tim thay cong viec"));

            // Act
            var result = await _controller.GetJobById(1);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var apiResponse = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<JobDto>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Khong tim thay cong viec");
        }

        /// <summary>
        /// TCID05: Kiem tra tao cong viec moi voi du lieu hop le tra ve thanh cong
        /// Input: CreateJobDto voi JobName Cong viec moi, PayType Per_Ngay, Rate 150000
        /// Expected: Tra ve OkObjectResult voi Success true va Data chua JobDto moi duoc tao
        /// </summary>
        [Fact]
        public async Task CreateJob_WithValidData_ReturnsOk()
        {
            // Arrange
            var createDto = new CreateJobDto { JobName = "Cong viec moi", PayType = "Per_Ngay", Rate = 150000 };
            var createdJob = new JobDto { Id = 1, JobName = "Cong viec moi", PayType = "Per_Ngay", Rate = 150000, IsActive = true };
            _mockJobService.Setup(s => s.CreateJobAsync(createDto)).ReturnsAsync(createdJob);

            // Act
            var result = await _controller.CreateJob(createDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<JobDto>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().BeEquivalentTo(createdJob);
        }

        /// <summary>
        /// TCID06: Kiem tra tao cong viec moi voi ModelState khong hop le
        /// Input: CreateJobDto va ModelState co loi khong hop le
        /// Expected: Tra ve BadRequestObjectResult voi Success false va Message chua loi validation
        /// </summary>
        [Fact]
        public async Task CreateJob_WithInvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new CreateJobDto();
            _controller.ModelState.AddModelError("JobName", "Ten cong viec bat buoc");

            // Act
            var result = await _controller.CreateJob(createDto);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var apiResponse = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<JobDto>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Ten cong viec bat buoc");
        }

        /// <summary>
        /// TCID07: Kiem tra tao cong viec moi khi co loi xay ra
        /// Input: CreateJobDto hop le va service nem exception
        /// Expected: Tra ve BadRequestObjectResult voi Success false va Message chua loi
        /// </summary>
        [Fact]
        public async Task CreateJob_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new CreateJobDto { JobName = "Cong viec moi", PayType = "Per_Ngay", Rate = 150000 };
            _mockJobService.Setup(s => s.CreateJobAsync(createDto)).ThrowsAsync(new Exception("Loi tao cong viec"));

            // Act
            var result = await _controller.CreateJob(createDto);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var apiResponse = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<JobDto>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Loi tao cong viec");
        }

        /// <summary>
        /// TCID08: Kiem tra cap nhat cong viec voi du lieu hop le tra ve thanh cong
        /// Input: UpdateJobDto voi Id 1, JobName Cong viec cap nhat, PayType Per_Tan, Rate 200000
        /// Expected: Tra ve OkObjectResult voi Success true va Data chua JobDto da cap nhat
        /// </summary>
        [Fact]
        public async Task UpdateJob_WithValidData_ReturnsOk()
        {
            // Arrange
            var updateDto = new UpdateJobDto { Id = 1, JobName = "Cong viec cap nhat", PayType = "Per_Tan", Rate = 200000, IsActive = true };
            var updatedJob = new JobDto { Id = 1, JobName = "Cong viec cap nhat", PayType = "Per_Tan", Rate = 200000, IsActive = true };
            _mockJobService.Setup(s => s.UpdateJobAsync(updateDto)).ReturnsAsync(updatedJob);

            // Act
            var result = await _controller.UpdateJob(updateDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<JobDto>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().BeEquivalentTo(updatedJob);
        }

        /// <summary>
        /// TCID09: Kiem tra cap nhat cong viec voi ModelState khong hop le
        /// Input: UpdateJobDto va ModelState co loi khong hop le
        /// Expected: Tra ve BadRequestObjectResult voi Success false va Message chua loi validation
        /// </summary>
        [Fact]
        public async Task UpdateJob_WithInvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new UpdateJobDto { Id = 1 };
            _controller.ModelState.AddModelError("JobName", "Ten cong viec bat buoc");

            // Act
            var result = await _controller.UpdateJob(updateDto);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var apiResponse = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<JobDto>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Ten cong viec bat buoc");
        }

        /// <summary>
        /// TCID10: Kiem tra cap nhat cong viec khi co loi xay ra
        /// Input: UpdateJobDto hop le va service nem exception
        /// Expected: Tra ve BadRequestObjectResult voi Success false va Message chua loi
        /// </summary>
        [Fact]
        public async Task UpdateJob_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new UpdateJobDto { Id = 1, JobName = "Cong viec cap nhat", PayType = "Per_Tan", Rate = 200000, IsActive = true };
            _mockJobService.Setup(s => s.UpdateJobAsync(updateDto)).ThrowsAsync(new Exception("Loi cap nhat cong viec"));

            // Act
            var result = await _controller.UpdateJob(updateDto);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var apiResponse = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<JobDto>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Loi cap nhat cong viec");
        }

        /// <summary>
        /// TCID11: Kiem tra xoa cong viec voi ID hop le tra ve thanh cong
        /// Input: Id cong viec hop le la 1
        /// Expected: Tra ve OkObjectResult voi Success true va Data la true
        /// </summary>
        [Fact]
        public async Task DeleteJob_WithValidId_ReturnsOk()
        {
            // Arrange
            _mockJobService.Setup(s => s.DeleteJobAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteJob(1);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<bool>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().BeTrue();
        }

        /// <summary>
        /// TCID12: Kiem tra xoa cong viec khi co loi xay ra
        /// Input: Id cong viec la 1 va service nem exception
        /// Expected: Tra ve BadRequestObjectResult voi Success false va Message chua loi
        /// </summary>
        [Fact]
        public async Task DeleteJob_WithServiceException_ReturnsBadRequest()
        {
            // Arrange
            _mockJobService.Setup(s => s.DeleteJobAsync(1)).ThrowsAsync(new Exception("Loi xoa cong viec"));

            // Act
            var result = await _controller.DeleteJob(1);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var apiResponse = badRequestResult.Value.Should().BeAssignableTo<ApiResponse<bool>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Error!.Message.Should().Contain("Loi xoa cong viec");
        }
    }
}
