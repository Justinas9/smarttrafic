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
    [Authorize] 
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
                if (model.VideoFile == null || !IsValidVideoFile(model.VideoFile))
                {
                    return Json(new
                    {
                        success = false,
                        message = "The uploaded file must be a valid video format (e.g., .mp4, .avi, .mov, .mkv, .wmv)."
                    });
                }


                try
                {
                    var intersection = new Intersection
                    {
                        CoordinatesX = float.Parse(model.CoordinateX),
                        CoordinatesY = float.Parse(model.CoordinateY),
                        City = null, 
                        Country = null,
                        Description = null 
                    };
                    _context.Intersections.Add(intersection);
                    await _context.SaveChangesAsync();

                    var videoFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.VideoFile.FileName);
                    var videoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/videos", videoFileName);

                    using (var stream = new FileStream(videoPath, FileMode.Create))
                    {
                        await model.VideoFile.CopyToAsync(stream);
                    }

                    var video = new Video
                    {
                        Path = videoPath,
                        Extension = Path.GetExtension(model.VideoFile.FileName),
                        SizeInBytes = model.VideoFile.Length,
                        DurationInSeconds = null, 
                        CameraID = null 
                    };
                    _context.Videos.Add(video);
                    await _context.SaveChangesAsync();
                    var videoRequest = new VideoRequest
                    {
                        UserID = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                        IntersectionID = intersection.ID,  
                        VideoID = video.ID,  
                        StartTime = model.StartTime, 
                        EndTime = null,  
                    };
                    _context.VideoRequests.Add(videoRequest);
                    await _context.SaveChangesAsync();

                    TempData["VideoRequestID"] = videoRequest.ID;

                    //yolo8 arguments
                    var pythonScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "recognition.py");
                    var startTimeString = videoRequest.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
                    var coordinatesX = intersection.CoordinatesX;
                    var coordinatesY = intersection.CoordinatesY;
                    var sessionId = videoRequest.ID;

                    var arguments = $"\"{videoPath}\" \"{startTimeString}\" {coordinatesX} {coordinatesY} {sessionId}";

                    //run video recognition
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = $"{pythonScriptPath} {arguments}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(processInfo))
                    {
                        if (process != null)
                        {
                            string output = await process.StandardOutput.ReadToEndAsync();
                            string error = await process.StandardError.ReadToEndAsync();
                            process.WaitForExit();
                            process.Close();  

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

                    video.DurationInSeconds = CalculateVideoDuration(model.StartTime, videoRequest.EndTime);

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
                    return Json(new
                    {
                        success = false,
                        message = "An error occurred while uploading the video. Please try again."
                    });
                }
            }
            else
            {
                var errors = ModelState.Values
                           .SelectMany(v => v.Errors)
                           .Select(e => e.ErrorMessage)
                           .ToList();

                return Json(new
                {
                    success = false,
                    message = "Validation failed.",
                    errors
                });
            }

            return View(model); 
        }
        public bool IsValidVideoFile(IFormFile file)
        {
            var allowedExtensions = new[] { ".mp4", ".avi", ".mov", ".mkv", ".wmv" };

            if (file != null)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                return allowedExtensions.Contains(extension);
            }

            return false;
        }

        public int CalculateVideoDuration(DateTime startTime, DateTime? endTime)
        {
            if (endTime.HasValue)
            {
                if (endTime.Value > startTime)
                {
                    return (int)(endTime.Value - startTime).TotalSeconds;
                }
                else
                {
                    _logger.LogWarning("Invalid EndTime: {EndTime} is earlier than StartTime: {StartTime}", endTime, startTime);
                    return 0;
                }
            }
            else
            {
                return 0;  
            }
        }
        [HttpGet]
        public IActionResult AllObjectDetections()
        {
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
                var requestIdParam = requestId.HasValue ? new SqlParameter("@RequestID", requestId.Value)
                                                         : new SqlParameter("@RequestID", DBNull.Value);

                var objectCounts = _context.ObjectDetectionCounts
                    .FromSqlRaw("EXEC GetObjectDetectionCounts @RequestID", requestIdParam)
                    .ToList();

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
                var requestIdParam = requestId.HasValue ? new SqlParameter("@RequestID", requestId.Value)
                                                         : new SqlParameter("@RequestID", DBNull.Value);

                var objectCounts = _context.GetProbabilityDistributionByObjectID
                    .FromSqlRaw("EXEC GetProbabilityDistributionByObjectID @RequestID", requestIdParam)
                    .ToList();

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
                var requestIdParam = requestId.HasValue ? new SqlParameter("@RequestID", requestId.Value)
                                                         : new SqlParameter("@RequestID", DBNull.Value);

                var objectCounts = _context.GetDetectionTimeObjectCountsByRequestID
                    .FromSqlRaw("EXEC GetDetectionTimeObjectCountsByRequestID @RequestID", requestIdParam)
                    .ToList();

                return Json(objectCounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving object detection counts");
                return Json(new List<object>());
            }
        }
       public IActionResult GetCalculatedPolutionByRequestID(int? requestId)
        {
            try
            {
                var requestIdParam = requestId.HasValue ? new SqlParameter("@RequestID", requestId.Value)
                                                         : new SqlParameter("@RequestID", DBNull.Value);

                var objectCounts = _context.GetCalculatedPolutionByRequestID
                    .FromSqlRaw("EXEC GetCalculatedPolutionByRequestID @RequestID", requestIdParam)
                    .ToList();

                return Json(objectCounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving object detection counts");
                return Json(new List<object>());
            }
        }
        [HttpGet]
        public IActionResult ReviewData(int videoRequestId)
        {
            ViewBag.VideoRequestId = videoRequestId;
            var objectDetectionsQuery = _context.ObjectDetections
                .Where(od => od.RequestID == videoRequestId)
                .OrderBy(od => od.DetectionTime);
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

            if (!objectDetections.Any())
            {
                ViewBag.Message = "No object detections found for this video request.";
            }

            return View("ViewData", objectDetections);
        }


    }
}
