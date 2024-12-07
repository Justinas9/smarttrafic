using CustomIdentity.Data;
using CustomIdentity.Models;
using CustomIdentity.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CustomIdentity.Controllers
{
    [Authorize] // Ensure only logged-in users can access this
    public class VideoController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VideoController> _logger;

        // Constructor to inject DbContext and Logger
        public VideoController(AppDbContext context, ILogger<VideoController> logger)
        {
            _context = context;
            _logger = logger;
        }
        [HttpGet]
        public IActionResult ReviewData(int videoRequestId)
        {
            ViewBag.VideoRequestId = videoRequestId;
            // Query for ObjectDetections related to the given videoRequestId
            var objectDetectionsQuery = _context.ObjectDetections
                .Where(od => od.RequestID == videoRequestId)
                .OrderBy(od => od.DetectionTime);

            // Map the results to a list of view model objects
            var objectDetections = objectDetectionsQuery
                .Select(od => new ObjectDetectionViewModel
                {
                    ID = od.ID,
                    ObjectID = od.ObjectID,
                    Probability = od.Probability,
                    DetectionTime = od.DetectionTime,
                    ObjectCount = od.ObjectCount,
                    BatchNumber = od.BatchNumber
                })
                .ToList();

            // If no detections found, return a view with an empty list or appropriate message
            if (!objectDetections.Any())
            {
                ViewBag.Message = "No object detections found for this video request.";
            }

            // Return the view with the object detections data
            return View("ViewData", objectDetections);
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
        [HttpPost]
        public async Task<IActionResult> Upload(VideoUploadViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Step 1: Create a new Intersection record
                    var intersection = new Intersection
                    {
                        CoordinatesX = float.Parse(model.CoordinateX),
                        CoordinatesY = float.Parse(model.CoordinateY),
                        City = null,  // City is null
                        Country = null,
                        Description = null  // Description is null
                    };
                    _context.Intersections.Add(intersection);
                    await _context.SaveChangesAsync();

                    // Step 2: Generate a unique filename and save the video file
                    var videoFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.VideoFile.FileName);
                    var videoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/videos", videoFileName);

                    using (var stream = new FileStream(videoPath, FileMode.Create))
                    {
                        await model.VideoFile.CopyToAsync(stream);
                    }

                    // Step 3: Log video file size to check for any discrepancies
                    _logger.LogInformation("Video File Length: {FileLength}", model.VideoFile.Length);
                    _logger.LogInformation("Video File Path: {FilePath}", videoPath);

                    // Step 4: Create a new Video record (do not set DurationInSeconds, it will be updated by Python)
                    var video = new Video
                    {
                        Path = videoPath,
                        Extension = Path.GetExtension(model.VideoFile.FileName),
                        SizeInBytes = model.VideoFile.Length,
                        DurationInSeconds = null,  // Duration is null, will be set by Python script
                        CameraID = null  // Camera is null (or can be updated later)
                    };
                    _context.Videos.Add(video);
                    await _context.SaveChangesAsync();

                    // Step 5: Create a new VideoRequest record with the VideoID and IntersectionID
                    var videoRequest = new VideoRequest
                    {
                        UserID = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                        IntersectionID = intersection.ID,  // Link to the Intersection table
                        VideoID = video.ID,  // Link to the Videos table
                        StartTime = model.StartTime,  // Assuming this is in the ViewModel
                        EndTime = null,  // This will be updated later when recognition is done
                    };
                    _context.VideoRequests.Add(videoRequest);
                    await _context.SaveChangesAsync();

                    // Set the VideoRequestID in TempData
                    TempData["VideoRequestID"] = videoRequest.ID;

                    // Step 6: Prepare arguments for the Python script
                    var pythonScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "recognition.py");
                    var startTimeString = videoRequest.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
                    var coordinatesX = intersection.CoordinatesX;
                    var coordinatesY = intersection.CoordinatesY;
                    var sessionId = videoRequest.ID;

                    // Prepare the full argument string for the Python script
                    var arguments = $"\"{videoPath}\" \"{startTimeString}\" {coordinatesX} {coordinatesY} {sessionId}";

                    // Step 7: Run the Python recognition script (sending relevant details)
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = $"{pythonScriptPath} {arguments}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    // Start the Python process with improved error handling
                    using (var process = Process.Start(processInfo))
                    {
                        if (process != null)
                        {
                            // Capture Python output and errors
                            string output = await process.StandardOutput.ReadToEndAsync();
                            string error = await process.StandardError.ReadToEndAsync();
                            process.WaitForExit();
                            process.Close();  // Explicitly close the process

                            // Log the output and errors
                            if (!string.IsNullOrEmpty(output))
                            {
                                _logger.LogInformation("Python Output: {Output}", output);
                            }
                            if (!string.IsNullOrEmpty(error))
                            {
                                _logger.LogError("Python Error: {Error}", error);
                            }
                        }
                        else
                        {
                            _logger.LogError("Failed to start Python process.");
                        }
                    }

                    var lastDetectionTime = _context.ObjectDetections
                        .Where(od => od.RequestID == videoRequest.ID)
                        .OrderByDescending(od => od.DetectionTime)
                        .Select(od => od.DetectionTime)
                        .FirstOrDefault();

                    if (lastDetectionTime != null)
                    {
                        videoRequest.EndTime = lastDetectionTime;
                    }
                    await _context.SaveChangesAsync();

                    // Step 9: Validate and Update Video with its duration (received from Python script)
                    if (videoRequest.EndTime.HasValue)
                    {
                        _logger.LogInformation("Start Time: {StartTime}, End Time: {EndTime}", model.StartTime, videoRequest.EndTime);

                        // Ensure the EndTime is later than StartTime to avoid negative durations
                        if (videoRequest.EndTime.Value > model.StartTime)
                        {
                            video.DurationInSeconds = (int)(videoRequest.EndTime.Value - model.StartTime).TotalSeconds;
                        }
                        else
                        {
                            _logger.LogWarning("Invalid EndTime: {EndTime} is earlier than StartTime: {StartTime}", videoRequest.EndTime, model.StartTime);
                            video.DurationInSeconds = 0;  // Or some default behavior
                        }
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Vaizdo įrašas apdorotas sėkmingai";
                    return Json(new
                    {
                        success = true,
                        videoRequestId = videoRequest.ID,
                        message = "Vaizdo įrašas apdorotas sėkmingai"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while uploading the video.");
                    ModelState.AddModelError(string.Empty, "An error occurred while uploading the video. Please try again.");
                }
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Model state is invalid: {Errors}", string.Join(", ", errors));
                return Json(new
                {
                    success = false,
                    errors = errors
                });
            }

            return View(model); // Return to the view if model state is invalid
        }
        [HttpGet]
        public IActionResult AllObjectDetections()
        {
            // Retrieve all object detections from the database
            var objectDetections = _context.ObjectDetections
                .OrderBy(od => od.DetectionTime)
                .Select(od => new ObjectDetectionViewModel
                {
                    ID = od.ID,
                    ObjectID = od.ObjectID,
                    Probability = od.Probability,
                    DetectionTime = od.DetectionTime,
                    ObjectCount = od.ObjectCount,
                    BatchNumber = od.BatchNumber
                })
                .ToList();

            return View("ViewData", objectDetections);
        }
        [HttpGet]
        public IActionResult GetObjectDetectionCounts(int? requestId)
        {
            try
            {
                // Check if requestId is null and set it to DBNull for SQL execution
                var requestIdParam = requestId.HasValue ? new SqlParameter("@RequestID", requestId.Value)
                                                         : new SqlParameter("@RequestID", DBNull.Value);

                // Use FromSqlRaw to execute the stored procedure with the parameter
                var objectCounts = _context.ObjectDetectionCounts
                    .FromSqlRaw("EXEC GetObjectDetectionCounts @RequestID", requestIdParam)
                    .ToList();

                // Return the data as JSON
                return Json(objectCounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving object detection counts");
                return Json(new List<object>());
            }
        }
        public IActionResult GetProbabilityDistributionByObjectID(int? requestId)
        {
            try
            {
                // Check if requestId is null and set it to DBNull for SQL execution
                var requestIdParam = requestId.HasValue ? new SqlParameter("@RequestID", requestId.Value)
                                                         : new SqlParameter("@RequestID", DBNull.Value);

                // Use FromSqlRaw to execute the stored procedure with the parameter
                var objectCounts = _context.GetProbabilityDistributionByObjectID
                    .FromSqlRaw("EXEC GetProbabilityDistributionByObjectID @RequestID", requestIdParam)
                    .ToList();

                // Return the data as JSON
                return Json(objectCounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving object detection counts");
                return Json(new List<object>());
            }
        }
        
       public IActionResult GetDetectionTimeObjectCountsByRequestID(int? requestId)
        {
            try
            {
                // Check if requestId is null and set it to DBNull for SQL execution
                var requestIdParam = requestId.HasValue ? new SqlParameter("@RequestID", requestId.Value)
                                                         : new SqlParameter("@RequestID", DBNull.Value);

                // Use FromSqlRaw to execute the stored procedure with the parameter
                var objectCounts = _context.GetDetectionTimeObjectCountsByRequestID
                    .FromSqlRaw("EXEC GetDetectionTimeObjectCountsByRequestID @RequestID", requestIdParam)
                    .ToList();

                // Return the data as JSON
                return Json(objectCounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving object detection counts");
                return Json(new List<object>());
            }
        }


    }
}
