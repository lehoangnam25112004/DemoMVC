
using DemoMVC.Data;
using Microsoft.EntityFrameworkCore;

namespace DemoMVC.Services
{
    public class StudentCodeGenerator
    {
        private readonly ApplicationDbContext _context;

        public StudentCodeGenerator(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateAsync(
            int admissionYear,
            string trainingSystemCode,
            string majorCode)
        {
            // Lấy 2 số cuối của năm nhập học
            string yearCode = (admissionYear % 100).ToString("D2");

            // Ghép tiền tố mã sinh viên
            string prefix = yearCode + trainingSystemCode + majorCode;

            // Tìm mã sinh viên lớn nhất đang tồn tại cùng tiền tố
            var existingCodes = await _context.Students
                .Where(s => s.StudentCode.StartsWith(prefix))
                .Select(s => s.StudentCode)
                .ToListAsync();

            int maxSequence = existingCodes
                .Where(code => code.Length == 10 &&
                    code.Substring(6, 4).All(char.IsDigit))
                .Select(code => int.Parse(code.Substring(6, 4)))
                .DefaultIfEmpty(0)
                .Max();

            if (maxSequence >= 9999)
            {
                throw new InvalidOperationException(
                    "Đã vượt quá số lượng mã sinh viên cho tiền tố này.");
            }

            // Tăng số thứ tự lên 1
            int nextSequence = maxSequence + 1;

            return prefix + nextSequence.ToString("D4");
        }
    }
}
