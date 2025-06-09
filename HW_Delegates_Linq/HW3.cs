using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace DelegatesLinQ.Homework
{
    public class Student
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string Major { get; set; }
        public double GPA { get; set; }
        public List<Course> Courses { get; set; } = new List<Course>();
        public DateTime EnrollmentDate { get; set; }
        public string Email { get; set; }
        public Address Address { get; set; }
    }

    public class Course
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public int Credits { get; set; }
        public double Grade { get; set; }
        public string Semester { get; set; }
        public string Instructor { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
    }

    public class StudentStatistics
    {
        public double MeanGPA { get; set; }
        public double MedianGPA { get; set; }
        public double StdDevGPA { get; set; }
        public int Count { get; set; }
        public double CorrelationAgeGPA { get; set; }
    }

    public static class StudentExtensions
    {
        public static IEnumerable<Student> FilterByAgeRange(this IEnumerable<Student> students, int minAge, int maxAge)
        {
            return students.Where(s => s.Age >= minAge && s.Age <= maxAge);
        }

        public static Dictionary<string, double> AverageGPAByMajor(this IEnumerable<Student> students)
        {
            return students
                .GroupBy(s => s.Major)
                .ToDictionary(g => g.Key, g => g.Average(s => s.GPA));
        }

        public static string ToGradeReport(this Student student)
        {
            return $"Student: {student.Name}, Major: {student.Major}, GPA: {student.GPA:F2}";
        }

        public static StudentStatistics CalculateStatistics(this IEnumerable<Student> students)
        {
            var gpas = students.Select(s => s.GPA).OrderBy(g => g).ToList();
            var ages = students.Select(s => (double)s.Age).ToList();

            int count = gpas.Count;
            if (count == 0) return new StudentStatistics();

            double meanGPA = gpas.Average();
            double medianGPA = (count % 2 == 1) ? gpas[count / 2] : (gpas[count / 2 - 1] + gpas[count / 2]) / 2.0;

            double variance = gpas.Select(g => (g - meanGPA) * (g - meanGPA)).Sum() / count;
            double stdDev = Math.Sqrt(variance);

            double meanAge = ages.Average();
            double cov = 0, varAge = 0;
            for (int i = 0; i < count; i++)
            {
                cov += (ages[i] - meanAge) * (gpas[i] - meanGPA);
                varAge += (ages[i] - meanAge) * (ages[i] - meanAge);
            }
            double correlation = varAge == 0 ? 0 : cov / Math.Sqrt(varAge * count * variance);

            return new StudentStatistics
            {
                MeanGPA = meanGPA,
                MedianGPA = medianGPA,
                StdDevGPA = stdDev,
                Count = count,
                CorrelationAgeGPA = correlation
            };
        }
    }

    public class LinqDataProcessor
    {
        private List<Student> _students;

        public LinqDataProcessor()
        {
            _students = GenerateSampleData();
        }

        public void BasicQueries()
        {
            Console.WriteLine("=== BASIC LINQ QUERIES ===");

            var highGPAStudents = _students.Where(s => s.GPA > 3.5);
            Console.WriteLine("Students with GPA > 3.5:");
            foreach (var s in highGPAStudents)
                Console.WriteLine($"- {s.Name} ({s.GPA})");

            var groupByMajor = _students.GroupBy(s => s.Major);
            Console.WriteLine("\nStudents grouped by major:");
            foreach (var group in groupByMajor)
            {
                Console.WriteLine($"Major: {group.Key}");
                foreach (var s in group)
                    Console.WriteLine($"  - {s.Name}");
            }

            var avgGpaByMajor = _students
                .GroupBy(s => s.Major)
                .Select(g => new { Major = g.Key, AvgGPA = g.Average(s => s.GPA) });
            Console.WriteLine("\nAverage GPA per major:");
            foreach (var item in avgGpaByMajor)
                Console.WriteLine($"- {item.Major}: {item.AvgGPA:F2}");

            var cs101Students = _students
                .Where(s => s.Courses.Any(c => c.Code == "CS101"));
            Console.WriteLine("\nStudents enrolled in CS101:");
            foreach (var s in cs101Students)
                Console.WriteLine($"- {s.Name}");

            var sortedByEnrollment = _students.OrderBy(s => s.EnrollmentDate);
            Console.WriteLine("\nStudents sorted by Enrollment Date:");
            foreach (var s in sortedByEnrollment)
                Console.WriteLine($"- {s.Name}, Enrolled: {s.EnrollmentDate.ToShortDateString()}");
        }

        public void CustomExtensionMethods()
        {
            Console.WriteLine("\n=== CUSTOM EXTENSION METHODS ===");

            var ageFiltered = _students.FilterByAgeRange(20, 22);
            Console.WriteLine("Students age 20-22:");
            foreach (var s in ageFiltered)
                Console.WriteLine($"- {s.Name}, Age: {s.Age}");

            var avgGpaDict = _students.AverageGPAByMajor();
            Console.WriteLine("\nAverage GPA by major:");
            foreach (var kvp in avgGpaDict)
                Console.WriteLine($"- {kvp.Key}: {kvp.Value:F2}");

            Console.WriteLine("\nGrade reports:");
            foreach (var s in _students)
                Console.WriteLine(s.ToGradeReport());

            var stats = _students.CalculateStatistics();
            Console.WriteLine($"\nStatistics on GPA: Count={stats.Count}, Mean={stats.MeanGPA:F2}, Median={stats.MedianGPA:F2}, StdDev={stats.StdDevGPA:F2}, Correlation Age-GPA={stats.CorrelationAgeGPA:F2}");
        }

        public void DynamicQueries()
        {
            Console.WriteLine("\n=== DYNAMIC QUERIES ===");

            var filteredStudents = _students.Where(BuildDynamicFilter<Student>("GPA", ">", 3.5));
            Console.WriteLine("Dynamic filter: GPA > 3.5");
            foreach (var s in filteredStudents)
                Console.WriteLine($"- {s.Name}, GPA: {s.GPA}");
        }

        public static Func<T, bool> BuildDynamicFilter<T>(string propertyName, string op, object value)
        {
            var param = Expression.Parameter(typeof(T), "x");
            var member = Expression.PropertyOrField(param, propertyName);
            var constant = Expression.Constant(Convert.ChangeType(value, member.Type));

            Expression body = op switch
            {
                ">" => Expression.GreaterThan(member, constant),
                "<" => Expression.LessThan(member, constant),
                ">=" => Expression.GreaterThanOrEqual(member, constant),
                "<=" => Expression.LessThanOrEqual(member, constant),
                "==" => Expression.Equal(member, constant),
                "!=" => Expression.NotEqual(member, constant),
                _ => throw new NotSupportedException("Operator not supported")
            };

            var lambda = Expression.Lambda<Func<T, bool>>(body, param);
            return lambda.Compile();
        }

        public void StatisticalAnalysis()
        {
            Console.WriteLine("\n=== STATISTICAL ANALYSIS ===");

            var stats = _students.CalculateStatistics();

            Console.WriteLine($"Count: {stats.Count}");
            Console.WriteLine($"Mean GPA: {stats.MeanGPA:F2}");
            Console.WriteLine($"Median GPA: {stats.MedianGPA:F2}");
            Console.WriteLine($"Std Dev GPA: {stats.StdDevGPA:F2}");
            Console.WriteLine($"Correlation Age-GPA: {stats.CorrelationAgeGPA:F2}");

            var outliers = _students.Where(s => s.GPA < stats.MeanGPA - 2 * stats.StdDevGPA || s.GPA > stats.MeanGPA + 2 * stats.StdDevGPA);
            Console.WriteLine("Outlier Students (GPA outside mean ± 2*stddev):");
            foreach (var s in outliers)
                Console.WriteLine($"- {s.Name}, GPA: {s.GPA}");
        }

        public void PivotOperations()
        {
            Console.WriteLine("\n=== PIVOT OPERATIONS ===");

            string GetGpaRange(double gpa)
            {
                if (gpa < 3.0) return "<3.0";
                if (gpa < 3.5) return "3.0-3.5";
                if (gpa <= 4.0) return "3.5-4.0";
                return "Unknown";
            }

            var pivot = _students
                .GroupBy(s => s.Major)
                .Select(g => new
                {
                    Major = g.Key,
                    GPAGroupCounts = g.GroupBy(s => GetGpaRange(s.GPA))
                                      .ToDictionary(gr => gr.Key, gr => gr.Count())
                });

            Console.WriteLine("Pivot Table: Student count by Major and GPA Range");
            foreach (var row in pivot)
            {
                Console.WriteLine($"Major: {row.Major}");
                foreach (var gpaGroup in row.GPAGroupCounts)
                    Console.WriteLine($"  GPA Range {gpaGroup.Key}: {gpaGroup.Value}");
            }

            var coursePivot = _students
                .SelectMany(s => s.Courses, (s, c) => new { s.Major, c.Semester })
                .GroupBy(x => new { x.Semester, x.Major })
                .Select(g => new { g.Key.Semester, g.Key.Major, Count = g.Count() })
                .OrderBy(x => x.Semester).ThenBy(x => x.Major);

            Console.WriteLine("\nCourse enrollment by Semester and Major:");
            foreach (var row in coursePivot)
                Console.WriteLine($"Semester: {row.Semester}, Major: {row.Major}, Enrollment Count: {row.Count}");

            var gradeDist = _students
                .SelectMany(s => s.Courses)
                .GroupBy(c => c.Instructor)
                .Select(g => new
                {
                    Instructor = g.Key,
                    GradeAvg = g.Average(c => c.Grade),
                    GradeCount = g.Count()
                });

            Console.WriteLine("\nGrade distribution by Instructor:");
            foreach (var row in gradeDist)
                Console.WriteLine($"Instructor: {row.Instructor}, Average Grade: {row.GradeAvg:F2}, Number of Grades: {row.GradeCount}");
        }

        private List<Student> GenerateSampleData()
        {
            return new List<Student>
            {
                new Student
                {
                    Id = 1, Name = "Alice Johnson", Age = 20, Major = "Computer Science",
                    GPA = 3.8, EnrollmentDate = new DateTime(2022, 9, 1),
                    Email = "alice.j@university.edu",
                    Address = new Address { Street = "123 Elm St", City = "Seattle", State = "WA", ZipCode = "98101" },
                    Courses = new List<Course>
                    {
                        new Course { Code = "CS101", Name = "Intro to CS", Credits = 4, Grade = 3.9, Semester = "Fall2022", Instructor = "Dr. Smith" },
                        new Course { Code = "MATH101", Name = "Calculus I", Credits = 4, Grade = 3.7, Semester = "Fall2022", Instructor = "Prof. Adams" }
                    }
                },
                new Student
                {
                    Id = 2, Name = "Bob Smith", Age = 21, Major = "Mathematics",
                    GPA = 3.2, EnrollmentDate = new DateTime(2021, 9, 1),
                    Email = "bob.s@university.edu",
                    Address = new Address { Street = "456 Oak St", City = "Portland", State = "OR", ZipCode = "97205" },
                    Courses = new List<Course>
                    {
                        new Course { Code = "MATH201", Name = "Linear Algebra", Credits = 3, Grade = 3.5, Semester = "Spring2023", Instructor = "Prof. Adams" },
                        new Course { Code = "CS101", Name = "Intro to CS", Credits = 4, Grade = 2.8, Semester = "Fall2022", Instructor = "Dr. Smith" }
                    }
                },
                new Student
                {
                    Id = 3, Name = "Carol Lee", Age = 22, Major = "Physics",
                    GPA = 3.6, EnrollmentDate = new DateTime(2020, 9, 1),
                    Email = "carol.l@university.edu",
                    Address = new Address { Street = "789 Pine St", City = "San Francisco", State = "CA", ZipCode = "94107" },
                    Courses = new List<Course>
                    {
                        new Course { Code = "PHYS101", Name = "General Physics", Credits = 4, Grade = 3.8, Semester = "Fall2022", Instructor = "Dr. Yang" },
                        new Course { Code = "MATH101", Name = "Calculus I", Credits = 4, Grade = 3.4, Semester = "Fall2022", Instructor = "Prof. Adams" }
                    }
                },
                new Student
                {
                    Id = 4, Name = "David Kim", Age = 23, Major = "Computer Science",
                    GPA = 2.9, EnrollmentDate = new DateTime(2021, 9, 1),
                    Email = "david.k@university.edu",
                    Address = new Address { Street = "321 Maple St", City = "Seattle", State = "WA", ZipCode = "98101" },
                    Courses = new List<Course>
                    {
                        new Course { Code = "CS201", Name = "Data Structures", Credits = 4, Grade = 3.0, Semester = "Spring2023", Instructor = "Dr. Smith" },
                        new Course { Code = "CS101", Name = "Intro to CS", Credits = 4, Grade = 2.7, Semester = "Fall2022", Instructor = "Dr. Smith" }
                    }
                },
                new Student
                {
                    Id = 5, Name = "Eva Green", Age = 20, Major = "Mathematics",
                    GPA = 3.9, EnrollmentDate = new DateTime(2023, 1, 15),
                    Email = "eva.g@university.edu",
                    Address = new Address { Street = "654 Cedar St", City = "Portland", State = "OR", ZipCode = "97205" },
                    Courses = new List<Course>
                    {
                        new Course { Code = "MATH101", Name = "Calculus I", Credits = 4, Grade = 4.0, Semester = "Winter2023", Instructor = "Prof. Adams" },
                        new Course { Code = "CS101", Name = "Intro to CS", Credits = 4, Grade = 3.8, Semester = "Winter2023", Instructor = "Dr. Smith" }
                    }
                }
            };
        }
    }
}
