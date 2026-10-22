
using DemoMVC.Models;

namespace DemoMVC.Data
{
    public static class DbInitializer
    {
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
        }
    }
}
