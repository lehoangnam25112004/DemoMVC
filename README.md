
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
