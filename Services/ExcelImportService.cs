using System.Globalization;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using DemoMVC.Data;
using DemoMVC.Models;
using DemoMVC.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace DemoMVC.Services
{
    public class ExcelImportService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ExcelImportService> _logger;

        // Danh sách trắng bảng nghiệp vụ (loại trừ toàn bộ bảng Identity)
        private static readonly HashSet<string> AllowedBusinessTables = new(StringComparer.OrdinalIgnoreCase)
        {
            "Major",
            "Student"
        };

        private static readonly Regex TwoDigitsRegex = new(@"^\d{2}$", RegexOptions.Compiled);

        public ExcelImportService(ApplicationDbContext dbContext, ILogger<ExcelImportService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public bool IsAllowedTable(string tableName)
        {
            return !string.IsNullOrWhiteSpace(tableName) && AllowedBusinessTables.Contains(tableName);
        }

        public byte[] GenerateTemplateWorkbook(string tableName)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(tableName);

            if (tableName.Equals("Major", StringComparison.OrdinalIgnoreCase))
            {
                ws.Cell(1, 1).Value = "MajorCode";
                ws.Cell(1, 2).Value = "MajorName";

                ws.Cell(2, 1).Value = "05";
                ws.Cell(2, 2).Value = "Công nghệ thông tin";
            }
            else
            {
                ws.Cell(1, 1).Value = "StudentCode";
                ws.Cell(1, 2).Value = "FullName";
                ws.Cell(1, 3).Value = "Address";
                ws.Cell(1, 4).Value = "Email";
                ws.Cell(1, 5).Value = "AdmissionYear";
                ws.Cell(1, 6).Value = "TrainingSystemCode";
                ws.Cell(1, 7).Value = "MajorCode";

                ws.Cell(2, 1).Value = "2221050999";
                ws.Cell(2, 2).Value = "Nguyễn Văn A";
                ws.Cell(2, 3).Value = "Hà Nội";
                ws.Cell(2, 4).Value = "nguyenvana@example.com";
                ws.Cell(2, 5).Value = 2022;
                ws.Cell(2, 6).Value = "21";
                ws.Cell(2, 7).Value = "05";
            }

            ws.Columns().AdjustToContents();
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private async Task<string> GenerateStudentCodeAsync(int admissionYear, string trainingSystemCode, string majorCode, HashSet<string> usedCodes)
        {
            var yy = (admissionYear % 100).ToString("D2");
            var prefix = $"{yy}{trainingSystemCode}{majorCode}"; // ví dụ 222105 -> tổng 10 ký tự nếu thêm 4 số

            int seq = 1;
            while (true)
            {
                var candidate = $"{prefix}{seq:D4}";
                if (candidate.Length > 10)
                    candidate = candidate[..10];

                if (!usedCodes.Contains(candidate))
                {
                    usedCodes.Add(candidate);
                    return candidate;
                }
                seq++;
            }
        }

        public async Task<ExcelImportViewModel> BuildPreviewAsync(Stream excelStream, string fileName, string tableName, ExcelImportMode mode)
        {
            var vm = new ExcelImportViewModel
            {
                SelectedTable = tableName,
                ImportMode = mode,
                UploadedFileName = fileName
            };

            if (!IsAllowedTable(tableName))
            {
                vm.PreviewRows.Add(new ExcelImportPreviewRow
                {
                    RowNumber = 0,
                    IsValid = false,
                    StatusText = "Lỗi",
                    ErrorMessage = "Bảng dữ liệu không hợp lệ hoặc thuộc nhóm bảng Identity."
                });
                return vm;
            }

            using var wb = new XLWorkbook(excelStream);
            var ws = wb.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                vm.PreviewRows.Add(new ExcelImportPreviewRow
                {
                    RowNumber = 0,
                    IsValid = false,
                    StatusText = "Lỗi",
                    ErrorMessage = "File Excel không có worksheet nào."
                });
                return vm;
            }

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 1;

            var headers = new List<string>();
            for (int c = 1; c <= lastCol; c++)
            {
                headers.Add(ws.Cell(1, c).GetString().Trim());
            }

            var majors = await _dbContext.Majors.ToListAsync();
            var majorByCode = majors
                .Where(m => !string.IsNullOrWhiteSpace(m.MajorCode))
                .ToDictionary(m => m.MajorCode.Trim(), StringComparer.OrdinalIgnoreCase);

            var existingStudentCodes = await _dbContext.Students
                .Where(s => !string.IsNullOrWhiteSpace(s.StudentCode))
                .Select(s => s.StudentCode)
                .ToListAsync();
            var studentCodeSet = new HashSet<string>(existingStudentCodes, StringComparer.OrdinalIgnoreCase);

            for (int r = 2; r <= lastRow; r++)
            {
                var rowDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                bool hasAnyValue = false;

                for (int c = 1; c <= lastCol; c++)
                {
                    var h = headers[c - 1];
                    if (string.IsNullOrWhiteSpace(h)) continue;
                    var val = ws.Cell(r, c).GetString().Trim();
                    rowDict[h] = val;
                    if (!string.IsNullOrWhiteSpace(val)) hasAnyValue = true;
                }

                if (!hasAnyValue) continue;

                var previewRow = new ExcelImportPreviewRow
                {
                    RowNumber = r,
                    Values = rowDict,
                    IsValid = true,
                    IsDuplicate = false,
                    StatusText = "Hợp lệ (Thêm mới)"
                };

                if (tableName.Equals("Major", StringComparison.OrdinalIgnoreCase))
                {
                    var majorCode = rowDict.GetValueOrDefault("MajorCode", "");
                    var majorName = rowDict.GetValueOrDefault("MajorName", "");

                    var errors = new List<string>();
                    if (string.IsNullOrWhiteSpace(majorCode))
                        errors.Add("Mã khoa không được để trống");
                    else if (majorCode.Length > 2)
                        errors.Add("Mã khoa không quá 2 ký tự");

                    if (string.IsNullOrWhiteSpace(majorName))
                        errors.Add("Tên khoa không được để trống");
                    else if (majorName.Length > 100)
                        errors.Add("Tên khoa không quá 100 ký tự");

                    if (errors.Any())
                    {
                        previewRow.IsValid = false;
                        previewRow.StatusText = "Lỗi dữ liệu";
                        previewRow.ErrorMessage = string.Join("; ", errors);
                    }
                    else if (majorByCode.ContainsKey(majorCode))
                    {
                        previewRow.IsDuplicate = true;
                        previewRow.StatusText = mode == ExcelImportMode.Overwrite
                            ? "Trùng mã khoa (Sẽ ghi đè)"
                            : "Trùng mã khoa (Bỏ qua khi ghi bổ sung)";
                    }
                }
                else if (tableName.Equals("Student", StringComparison.OrdinalIgnoreCase))
                {
                    var studentCode = rowDict.GetValueOrDefault("StudentCode", "");
                    var fullName = rowDict.GetValueOrDefault("FullName", "");
                    var address = rowDict.GetValueOrDefault("Address", "");
                    var email = rowDict.GetValueOrDefault("Email", "");
                    var admissionYearStr = rowDict.GetValueOrDefault("AdmissionYear", "");
                    var trainingSystemCode = rowDict.GetValueOrDefault("TrainingSystemCode", "");
                    var majorCode = rowDict.GetValueOrDefault("MajorCode", "");

                    var errors = new List<string>();

                    if (!string.IsNullOrWhiteSpace(studentCode) && studentCode.Length > 10)
                        errors.Add("Mã sinh viên không quá 10 ký tự");

                    if (string.IsNullOrWhiteSpace(fullName))
                        errors.Add("Tên không được để trống");
                    else if (fullName.Length > 100)
                        errors.Add("Họ và tên không quá 100 ký tự");

                    if (string.IsNullOrWhiteSpace(address))
                        errors.Add("Địa chỉ không được để trống");
                    else if (address.Length > 100)
                        errors.Add("Địa chỉ không quá 100 ký tự");

                    if (string.IsNullOrWhiteSpace(email))
                        errors.Add("Email không được để trống");
                    else if (email.Length > 100)
                        errors.Add("Email không quá 100 ký tự");
                    else if (!new EmailAddressAttribute().IsValid(email))
                        errors.Add("Email không đúng định dạng");

                    if (!int.TryParse(admissionYearStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int admissionYear)
                        || admissionYear < 2000 || admissionYear > 2099)
                    {
                        errors.Add("Năm nhập học không hợp lệ (2000 - 2099)");
                    }

                    if (string.IsNullOrWhiteSpace(trainingSystemCode) || !TwoDigitsRegex.IsMatch(trainingSystemCode))
                    {
                        errors.Add("Mã hệ đào tạo phải gồm 2 chữ số");
                    }

                    if (string.IsNullOrWhiteSpace(majorCode) || !TwoDigitsRegex.IsMatch(majorCode))
                    {
                        errors.Add("Mã ngành phải gồm 2 chữ số");
                    }
                    else if (!majorByCode.ContainsKey(majorCode))
                    {
                        errors.Add($"Mã ngành '{majorCode}' chưa tồn tại trong bảng Major");
                    }

                    if (errors.Any())
                    {
                        previewRow.IsValid = false;
                        previewRow.StatusText = "Lỗi dữ liệu";
                        previewRow.ErrorMessage = string.Join("; ", errors);
                    }
                    else if (!string.IsNullOrWhiteSpace(studentCode) && studentCodeSet.Contains(studentCode))
                    {
                        previewRow.IsDuplicate = true;
                        previewRow.StatusText = mode == ExcelImportMode.Overwrite
                            ? "Trùng mã SV (Sẽ ghi đè)"
                            : "Trùng mã SV (Bỏ qua khi ghi bổ sung)";
                    }
                }

                vm.PreviewRows.Add(previewRow);
            }

            vm.StagedPayloadJson = JsonSerializer.Serialize(vm.PreviewRows.Where(x => x.IsValid).ToList());
            return vm;
        }

        public async Task<(bool Success, string Message, int Inserted, int Updated, int Skipped)> ExecuteConfirmedImportAsync(string tableName, ExcelImportMode mode, string? stagedPayloadJson)
        {
            (bool Success, string Message, int Inserted, int Updated, int Skipped) Failure(string message)
            {
                _logger.LogWarning("Excel import rejected for {Table}: {Reason}", tableName, message);
                return (false, message, 0, 0, 0);
            }

            if (!IsAllowedTable(tableName))
                return Failure("Bảng dữ liệu không hợp lệ.");

            if (!Enum.IsDefined(mode))
                return Failure("Chế độ nhập dữ liệu không hợp lệ.");

            if (string.IsNullOrWhiteSpace(stagedPayloadJson))
                return Failure("Dữ liệu Preview bị rỗng hoặc bị mất. Vui lòng tải file lên và xem trước lại.");

            List<ExcelImportPreviewRow>? validRows;
            try
            {
                validRows = JsonSerializer.Deserialize<List<ExcelImportPreviewRow>>(stagedPayloadJson);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid staged JSON for Excel import into {Table}", tableName);
                return Failure("Dữ liệu Preview sai định dạng hoặc bị cắt. Vui lòng xem trước lại.");
            }

            if (validRows == null || !validRows.Any())
                return Failure("Không có dòng hợp lệ nào để ghi vào cơ sở dữ liệu.");

            try
            {
                foreach (var row in validRows)
                {
                    if (row == null || !row.IsValid || row.Values == null)
                        return Failure("Dữ liệu xác nhận không khớp Preview. Vui lòng xem trước lại.");

                    // JSON does not preserve the dictionary's case-insensitive comparer.
                    row.Values = new Dictionary<string, string>(row.Values, StringComparer.OrdinalIgnoreCase);
                }

                await using var transaction = await _dbContext.Database.BeginTransactionAsync();
                int skipped = 0;
                var databasePath = _dbContext.Database.GetDbConnection().DataSource;
                _logger.LogInformation(
                    "ConfirmImport: table={Table}, mode={Mode}, rows={Rows}, payloadChars={PayloadLength}, database={DatabasePath}",
                    tableName, mode, validRows.Count, stagedPayloadJson.Length, databasePath);

                if (tableName.Equals("Major", StringComparison.OrdinalIgnoreCase))
                {
                    var majors = await _dbContext.Majors.ToListAsync();
                    var majorByCode = majors.ToDictionary(m => m.MajorCode.Trim(), StringComparer.OrdinalIgnoreCase);
                    foreach (var row in validRows)
                    {
                        var majorCode = (row.Values.GetValueOrDefault("MajorCode") ?? "").Trim();
                        var majorName = (row.Values.GetValueOrDefault("MajorName") ?? "").Trim();
                        var candidate = new Major { MajorCode = majorCode, MajorName = majorName };
                        Validator.ValidateObject(candidate, new ValidationContext(candidate), validateAllProperties: true);

                        if (majorByCode.TryGetValue(majorCode, out var existing))
                        {
                            if (mode == ExcelImportMode.Overwrite)
                            {
                                existing.MajorName = majorName;
                            }
                            else
                            {
                                skipped++;
                            }
                        }
                        else
                        {
                            _dbContext.Majors.Add(candidate);
                            majorByCode.Add(majorCode, candidate);
                        }
                    }
                }
                else if (tableName.Equals("Student", StringComparison.OrdinalIgnoreCase))
                {
                    var majors = await _dbContext.Majors.ToListAsync();
                    var majorByCode = majors
                        .Where(m => !string.IsNullOrWhiteSpace(m.MajorCode))
                        .ToDictionary(m => m.MajorCode.Trim(), StringComparer.OrdinalIgnoreCase);

                    var students = await _dbContext.Students.ToListAsync();
                    var studentByCode = students.Where(s => !string.IsNullOrWhiteSpace(s.StudentCode))
                        .ToDictionary(s => s.StudentCode.Trim(), StringComparer.OrdinalIgnoreCase);
                    var usedCodes = new HashSet<string>(studentByCode.Keys, StringComparer.OrdinalIgnoreCase);

                    foreach (var row in validRows)
                    {
                        var studentCode = (row.Values.GetValueOrDefault("StudentCode") ?? "").Trim();
                        var fullName = (row.Values.GetValueOrDefault("FullName") ?? "").Trim();
                        var address = (row.Values.GetValueOrDefault("Address") ?? "").Trim();
                        var email = (row.Values.GetValueOrDefault("Email") ?? "").Trim();
                        if (!int.TryParse(row.Values.GetValueOrDefault("AdmissionYear"), NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out var admissionYear))
                            throw new ValidationException($"Dòng {row.RowNumber}: năm nhập học không hợp lệ.");
                        var trainingSystemCode = (row.Values.GetValueOrDefault("TrainingSystemCode") ?? "").Trim();
                        var majorCode = (row.Values.GetValueOrDefault("MajorCode") ?? "").Trim();

                        if (!majorByCode.TryGetValue(majorCode, out var matchedMajor))
                            throw new ValidationException($"Dòng {row.RowNumber}: mã ngành '{majorCode}' không còn tồn tại. Vui lòng xem trước lại.");

                        var candidate = new Student
                        {
                            StudentCode = studentCode,
                            FullName = fullName,
                            Address = address,
                            Email = email,
                            AdmissionYear = admissionYear,
                            TrainingSystemCode = trainingSystemCode,
                            MajorCode = matchedMajor.MajorCode,
                            MajorId = matchedMajor.MajorId
                        };
                        Validator.ValidateObject(candidate, new ValidationContext(candidate), validateAllProperties: true);

                        if (!string.IsNullOrWhiteSpace(studentCode))
                        {
                            if (studentByCode.TryGetValue(studentCode, out var existing))
                            {
                                if (mode == ExcelImportMode.Overwrite)
                                {
                                    existing.FullName = fullName;
                                    existing.Address = address;
                                    existing.Email = email;
                                    existing.AdmissionYear = admissionYear;
                                    existing.TrainingSystemCode = trainingSystemCode;
                                    existing.MajorCode = matchedMajor.MajorCode;
                                    existing.MajorId = matchedMajor.MajorId;
                                }
                                else
                                    skipped++;
                                continue;
                            }

                            usedCodes.Add(studentCode);
                        }
                        else
                        {
                            studentCode = await GenerateStudentCodeAsync(admissionYear, trainingSystemCode, matchedMajor.MajorCode, usedCodes);
                        }

                        candidate.StudentCode = studentCode;
                        _dbContext.Students.Add(candidate);
                        studentByCode.Add(studentCode, candidate);
                    }
                }

                var entries = _dbContext.ChangeTracker.Entries()
                    .Where(e => tableName.Equals("Major", StringComparison.OrdinalIgnoreCase)
                        ? e.Entity is Major : e.Entity is Student).ToList();
                int inserted = entries.Count(e => e.State == EntityState.Added);
                int updated = entries.Count(e => e.State == EntityState.Modified);
                int saved = await _dbContext.SaveChangesAsync();
                if (saved != inserted + updated)
                    throw new InvalidOperationException($"Số bản ghi lưu ({saved}) không khớp số thay đổi ({inserted + updated}).");
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Excel import committed: table={Table}, inserted={Inserted}, updated={Updated}, skipped={Skipped}, saved={Saved}, database={DatabasePath}",
                    tableName, inserted, updated, skipped, saved, databasePath);
                var counts = $"thêm mới (inserted): {inserted}, cập nhật (updated): {updated}, bỏ qua trùng mã: {skipped}, đã lưu: {saved}";
                if (saved == 0)
                {
                    _logger.LogWarning("Excel import made no changes: table={Table}, mode={Mode}, skipped={Skipped}",
                        tableName, mode, skipped);
                    return (false, $"Không có bản ghi nào được thêm/cập nhật trong bảng {tableName}: {counts}. Bản ghi có thể đã tồn tại hoặc không thay đổi; chọn Ghi đè nếu cần cập nhật.", 0, 0, skipped);
                }

                var modeText = mode == ExcelImportMode.Overwrite ? "Ghi đè" : "Ghi bổ sung";
                return (true, $"Nhập Excel thành công ({modeText}) cho bảng {tableName}: {counts}.", inserted, updated, skipped);
            }
            catch (Exception ex) when (ex is DbUpdateException or SqliteException or ValidationException or InvalidOperationException or ArgumentException)
            {
                _dbContext.ChangeTracker.Clear();
                _logger.LogError(ex, "Excel import failed; no changes committed. Table={Table}, database={DatabasePath}",
                    tableName, _dbContext.Database.GetDbConnection().DataSource);
                return (false, $"Không thể nhập dữ liệu; không có thay đổi nào được commit (transaction được rollback): {ex.GetBaseException().Message}", 0, 0, 0);
            }
        }
    }
}