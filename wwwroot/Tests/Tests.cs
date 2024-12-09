using CustomIdentity.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;

[TestClass]
public class VideoControllerTests
{
    private VideoController _controller;

    [TestInitialize]
    public void Setup()
    {
        var logger = new NullLogger<VideoController>();
        _controller = new VideoController(null, logger);
    }

    [TestMethod]
    public void CalculateVideoDuration_WithValidEndTime_CalculatesDurationInSeconds()
    {
        // Arrange 
        var startTime = DateTime.Now.AddMinutes(-5);
        var endTime = startTime.AddMinutes(3);

        // Act: 
        var duration = _controller.CalculateVideoDuration(startTime, endTime);

        // Assert: 
        Assert.AreEqual(180, duration);
    }

    [TestMethod]
    public void CalculateVideoDuration_WithEndTimeEarlierThanStartTime_SetsDurationToZero()
    {
        // Arrange 
        var startTime = DateTime.Now.AddMinutes(-5);
        var endTime = startTime.AddMinutes(-3);

        // Act: 
        var duration = _controller.CalculateVideoDuration(startTime, endTime);

        // Assert: 
        Assert.AreEqual(0, duration);
    }

    [TestMethod]
    public void CalculateVideoDuration_WithNullEndTime_SetsDurationToZero()
    {
        // Arrange:
        var startTime = DateTime.Now.AddMinutes(-5);
        DateTime? endTime = null;

        // Act: 
        var duration = _controller.CalculateVideoDuration(startTime, endTime);

        // Assert: 
        Assert.AreEqual(0, duration);
    }

    [TestMethod]
    public void IsValidVideoFile_ValidFile_ReturnsTrue()
    {
        // Arrange
        var file = CreateTestFormFile("video.mp4");

        // Act
        var result = _controller.IsValidVideoFile(file);

        // Assert
        Assert.IsTrue(result, "Expected IsValidVideoFile to return true for a valid video file.");
    }

    [TestMethod]
    public void IsValidVideoFile_InvalidFile_ReturnsFalse()
    {
        // Arrange
        var file = CreateTestFormFile("invalid_file.txt");

        // Act
        var result = _controller.IsValidVideoFile(file);

        // Assert
        Assert.IsFalse(result, "Expected IsValidVideoFile to return false for an invalid video file.");
    }


    private IFormFile CreateTestFormFile(string fileName)
    {
        var content = "This is a dummy file content.";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        return new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };
    }
}
