using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public class Student
    {
        [Key]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "User is required")]
        public int UserId { get; set; }
        public User User { get; set; }
        [Required(ErrorMessage = "Course is required")]
        public int CourseId { get; set; }
        public Course Course { get; set; }

        [Required, StringLength(20)]
        public string EnrollmentNo { get; set; }
        [Required]
        [Range(1, 8, ErrorMessage = "Semester must be between 1 and 8")]

        public int Semester {  get; set; }
        [Required]
        [DataType(DataType.Date)]
        public DateTime DOB { get; set; }

        [StringLength(1000)]
        public string? Photo { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public List<Result> Results { get; set; } = new();
    }
}
