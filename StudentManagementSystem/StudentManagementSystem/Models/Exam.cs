using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public class Exam
    {
        [Key]
        public int ExamId { get; set; }

        [Required, StringLength(100)]
        public string ExamName { get; set; }
        [Required(ErrorMessage = "Course is required")]
        public int? CourseId { get; set; }
        [Required]
        public int Semester {  get; set; }
        [Required]
        public DateTime ExamDate { get; set; }
        [Required, StringLength(50)]
        public string ExamType { get; set; } // Midterm, Final, Practical

        [Required]
        public int DurationMinutes { get; set; } // e.g. 120

        public int MaxMarks { get; set; }
        public int MinMarks { get; set; }
        public bool IsPublished { get; set; } = false;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public Course Course { get; set; }
        public List<Result> Result { get; set; } = new();
    }
}
