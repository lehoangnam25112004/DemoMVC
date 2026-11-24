
Routing trong .NET MVC

Routing là cơ chế xác định URL sẽ được chuyển đến Controller và Action nào.

Ví dụ:

/Product/Details/5

Routing sẽ hiểu:

Product → ProductController
Details → Action Details()
5       → id

1. Conventional Routing

Thường cấu hình trong Program.cs:

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

Trong đó:

controller: tên Controller
action: tên Action
id?: tham số id, không bắt buộc
Home/Index: giá trị mặc định
2. Attribute Routing

Khai báo trực tiếp trên Controller/Action:

[Route("products/details/{id}")]
public IActionResult Details(int id)
{
    return View();
}

URL:

/products/details/5
3. Các thành phần cần nhớ
URL
 ↓
Routing
 ↓
Controller
 ↓
Action
 ↓
View

## Import Excel (Preview -> ConfirmImport)

- Chỉ Admin được import bảng nghiệp vụ `Major` và `Student`; không áp dụng cho Identity.
- Preview tạo `StagedPayloadJson` cho các dòng hợp lệ. ConfirmImport kiểm tra lại JSON,
  tên cột không phân biệt hoa/thường và dữ liệu trước khi ghi trong một transaction.
- Append giữ nguyên bản ghi trùng mã (kể cả mã lặp trong cùng file). Overwrite cập nhật
  bản ghi trùng mã; dữ liệu không thay đổi không được tính là một lần cập nhật.
- Thông báo và log xác nhận ghi rõ `inserted`, `updated`, số dòng trùng mã bị bỏ qua
  và số bản ghi thực sự lưu. Không ghi được bản ghi nào sẽ có thông báo thay vì báo thành công.
  Lỗi ghi dữ liệu được log kèm exception và transaction tự rollback khi bị dispose.
- `DefaultConnection` dùng SQLite. Đường dẫn tương đối được tính từ
  `ContentRootPath`, không từ thư mục làm việc hiện tại. Log khởi động và ConfirmImport
  ghi đường dẫn database thực tế; với content root là thư mục dự án, file là `DemoMVC.db`
  ngay trong thư mục dự án. Khi chạy DLL từ thư mục khác, hãy chỉ định `--contentRoot`
  về thư mục dự án hoặc cấu hình `ConnectionStrings__DefaultConnection` bằng đường dẫn
  tuyệt đối để tiếp tục dùng cùng database. Không tự sao chép/di chuyển các database cũ.
- Sau import thành công, dùng liên kết xem danh sách. `Major/Index` đọc trực tiếp
  toàn bộ `Majors` từ cùng `ApplicationDbContext`, không cache, không lọc/phân trang.
