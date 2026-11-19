using Microsoft.AspNetCore.Identity;
using DemoMVC.Models;

namespace DemoMVC.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // --- PHẦN 1: GIỮ NGUYÊN SEED MAJOR VÀ 200 STUDENT CŨ ---
            Initialize(context);

            // --- PHẦN 2: KHỞI TẠO ROLE VÀ TÀI KHOẢN MẶC ĐỊNH ---
            string[] roles = { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Tạo tài khoản Admin mặc định
            var adminEmail = "admin@gmail.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Quản Trị Viên",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            // Tạo tài khoản User mặc định
            var userEmail = "user@gmail.com";
            if (await userManager.FindByEmailAsync(userEmail) == null)
            {
                var user = new ApplicationUser
                {
                    UserName = userEmail,
                    Email = userEmail,
                    FullName = "Người Dùng Thường",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(user, "User@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "User");
                }
            }
        }

        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            var items = new List<Major>
            {
                new Major { MajorCode = "01", MajorName = "Xây dựng" },
                new Major { MajorCode = "02", MajorName = "Cơ điện" },
                new Major { MajorCode = "03", MajorName = "Kinh tế và Quản trị kinh doanh" },
                new Major { MajorCode = "04", MajorName = "Trắc địa - Bản đồ và Quản lý đất đai" },
                new Major { MajorCode = "05", MajorName = "Công nghệ thông tin" },
                new Major { MajorCode = "06", MajorName = "Địa chất" },
                new Major { MajorCode = "07", MajorName = "Địa chất thủy văn" },
                new Major { MajorCode = "08", MajorName = "Địa vật lý" },
                new Major { MajorCode = "09", MajorName = "Dầu khí" },
                new Major { MajorCode = "10", MajorName = "Mỏ" }
            };

            foreach (var item in items)
            {
                var existing = context.Majors
                    .FirstOrDefault(m => m.MajorName == item.MajorName);

                if (existing != null)
                {
                    existing.MajorCode = item.MajorCode;
                }
                else
                {
                    context.Majors.Add(item);
                }
            }

            context.SaveChanges();

            var majorList = context.Majors.OrderBy(m => m.MajorId).ToList();
            if (majorList.Count == 0)
            {
                return;
            }

            var totalStudents = context.Students.Count();
            if (totalStudents >= 200)
            {
                return;
            }

            var sequenceStart = totalStudents + 1;
            const int admissionYear = 2027;
            const string trainingSystemCode = "24";

            for (int i = 0; i < 200 - totalStudents; i++)
            {
                var major = majorList[(sequenceStart + i - 1) % majorList.Count];
                var studentNumber = sequenceStart + i;
                var student = new Student
                {
                    StudentCode = $"{admissionYear % 100:D2}{trainingSystemCode}{major.MajorCode}{studentNumber:D4}",
                    FullName = $"Sinh viên {studentNumber}",
                    Address = $"Địa chỉ {studentNumber}",
                    Email = $"student{studentNumber}@demo.com",
                    AdmissionYear = admissionYear,
                    TrainingSystemCode = trainingSystemCode,
                    MajorCode = major.MajorCode,
                    MajorId = major.MajorId
                };

                context.Students.Add(student);
            }

            context.SaveChanges();
        }
    }
}