// Controllers/DetectionController.cs
using CustomIdentity.Models;
using CustomIdentity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace CustomIdentity.Controllers
{
    [Authorize]
    public class DetectionController : Controller
    {
        private readonly DetectionRepository _repository;

        public DetectionController(DetectionRepository repository)
        {
            _repository = repository;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetBatches(int lastBatchId)
        {
            List<DetectionBatch> batches = _repository.GetBatches(lastBatchId);
            return Json(batches);
        }

        [HttpGet]
        public JsonResult GetLatestBatchSummary(int sessionId)
        {
            var summary = _repository.GetLatestBatchSummary(sessionId);
            return Json(summary);
        }
    }
}
