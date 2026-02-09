namespace StudentManagementSystem.Models.ViewModels
{
    public class StudentDashboardVM
    {
        public int StudentId { get; set; }
        public string EnrollmentNo { get; set; }
        public int Semester { get; set; }
        public DateTime DOB { get; set; }
        public string Photo { get; set; }
        public DateTime CreatedAt { get; set; }

        // Derived (allowed – calculated, not stored)
        public int Age { get; set; }
    }
}
