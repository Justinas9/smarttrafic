using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

public class VideoUploadViewModel
{
    [Required]
    [Display(Name = "Coordinate X")]
    public string CoordinateX { get; set; }

    [Required]
    [Display(Name = "Coordinate Y")]
    public string CoordinateY { get; set; }

    [Required]
    [Display(Name = "Start Time")]
    public DateTime StartTime { get; set; }

    [Required]
    [Display(Name = "Upload Video")]
    public IFormFile VideoFile { get; set; }
    public int VideoRequestID { get; set; }
}
