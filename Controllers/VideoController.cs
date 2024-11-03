using CustomIdentity.Data; // Ensure the namespace for your DbContext is included
using CustomIdentity.Models; // Include your VideoRequest model
using CustomIdentity.ViewModels; // Include your VideoUploadViewModel
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // Import the logging namespace
using System;
using System.Diagnostics; // Import for Process
using System.IO;
using System.Linq; // Add this for IEnumerable extension methods
using System.Security.Claims;
using System.Threading.Tasks;

namespace CustomIdentity.Controllers
{
    [Authorize] // Ensuring only logged-in users can access this
    public class VideoController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VideoController> _logger; // Declare the logger

        // Inject the logger through the constructor
        public VideoController(AppDbContext context, ILogger<VideoController> logger)
        {
            _context = context;
            _logger = logger; // Assign the logger to the field
        }

        // GET: Video/Upload
        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        // POST: Video/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(VideoUploadViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Generate a unique filename
                    var videoFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.VideoFile.FileName);
                    var videoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/videos", videoFileName);

                    using (var stream = new FileStream(videoPath, FileMode.Create))
                    {
                        await model.VideoFile.CopyToAsync(stream);
                    }

                    // Create and save the video request (your VideoRequest class)
                    var videoRequest = new VideoRequest
                    {
                        UserId = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                        Name = User.Identity.Name,
                        UserName = User.Identity.Name,
                        CoordinatesX = float.Parse(model.CoordinateX),
                        CoordinatesY = float.Parse(model.CoordinateY),
                        StartTime = model.StartTime,
                        VideoFile = Path.Combine("videos", videoFileName)
                    };

                    await _context.VideoRequests.AddAsync(videoRequest);
                    await _context.SaveChangesAsync();

                    // Run the Python script after saving the video request
                    var pythonScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "recognition.py"); ; 
                    var startTimeString = videoRequest.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
                    var coordinatesX = videoRequest.CoordinatesX;
                    var coordinatesY = videoRequest.CoordinatesY;
                    var sessionId = videoRequest.Id; // Assuming this is the ID from VideoRequest table

                    // Create the arguments for the Python script
                    var arguments = $"\"{videoPath}\" \"{startTimeString}\" {coordinatesX} {coordinatesY} {sessionId}";

                    // Start the Python process
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "python", // Ensure 'python' is in your PATH or provide the full path to python.exe
                        Arguments = $"{pythonScriptPath} {arguments}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true // If you want to run it hidden
                    };

                    using (var process = Process.Start(processInfo))
                    {
                        // Capture output (optional)
                        string output = await process.StandardOutput.ReadToEndAsync();
                        string error = await process.StandardError.ReadToEndAsync();
                        process.WaitForExit(); // Wait for the process to complete

                        // Log any output or errors
                        if (!string.IsNullOrEmpty(output))
                        {
                            _logger.LogInformation("Python Output: {Output}", output);
                        }
                        if (!string.IsNullOrEmpty(error))
                        {
                            _logger.LogError("Python Error: {Error}", error);
                        }
                    }

                    TempData["SuccessMessage"] = "Vaizdo įrašas apdorotas sėkmingai";
                    return RedirectToAction("Upload"); // Redirect back to the upload page
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while uploading video.");
                    ModelState.AddModelError(string.Empty, "An error occurred while uploading the video. Please try again.");
                }
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Model state is invalid: {Errors}", string.Join(", ", errors));
            }

            return View(model);
        }
    }
}
