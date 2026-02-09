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
        public string StudentName { get; set; }
        public int Age { get; set; }
    }
}
