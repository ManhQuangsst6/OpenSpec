# Spec Delta

## Purpose

Quản lý danh mục sản phẩm: tạo, xem, tìm kiếm, lọc, sắp xếp có phân trang, cập nhật và xoá sản phẩm, đảm bảo SKU duy nhất và giá, tồn kho hợp lệ.

## ADDED Requirements

### Requirement: Tạo sản phẩm
Hệ thống SHALL cho phép tạo sản phẩm với SKU, tên, mô tả (tuỳ chọn), giá, tồn kho và trạng thái đang bán. SKU MUST được cắt khoảng trắng và chuyển chữ hoa trước khi lưu. Khi thành công trả về sản phẩm kèm id, thời điểm tạo và cập nhật.

#### Scenario: Tạo thành công
- **WHEN** client tạo sản phẩm SKU ` ao-thun-01 `, tên `Áo thun`, giá 150000, tồn kho 10, đang bán
- **THEN** hệ thống trả về 201 Created kèm header Location, body có SKU `AO-THUN-01` và các trường đã gửi

#### Scenario: Dữ liệu không hợp lệ
- **WHEN** client tạo sản phẩm có SKU chứa ký tự ngoài chữ, số, dấu gạch ngang, hoặc SKU dài ngoài khoảng 3–32 ký tự, hoặc tên rỗng/dài hơn 200 ký tự, hoặc mô tả dài hơn 2000 ký tự, hoặc giá âm/lớn hơn 1.000.000.000/có quá 2 chữ số thập phân, hoặc tồn kho âm
- **THEN** hệ thống trả về 400 kèm ProblemDetails có lỗi theo từng trường sai (`sku`, `name`, `description`, `price`, `stock`) và không tạo sản phẩm

### Requirement: SKU là duy nhất
Hệ thống MUST NOT cho phép hai sản phẩm có cùng SKU (so sánh sau khi chuẩn hoá chữ hoa).

#### Scenario: Tạo trùng SKU
- **WHEN** đã có sản phẩm `AO-THUN-01` và client tạo sản phẩm SKU `ao-thun-01`
- **THEN** hệ thống trả về 409 Conflict và không tạo sản phẩm

#### Scenario: Cập nhật sang SKU của sản phẩm khác
- **WHEN** client cập nhật sản phẩm B sang SKU đang thuộc sản phẩm A
- **THEN** hệ thống trả về 409 Conflict và sản phẩm B giữ nguyên dữ liệu

### Requirement: Xem chi tiết sản phẩm
Hệ thống SHALL trả về một sản phẩm theo id.

#### Scenario: Sản phẩm tồn tại
- **WHEN** client yêu cầu sản phẩm theo id đang tồn tại
- **THEN** hệ thống trả về 200 với đầy đủ thông tin sản phẩm

#### Scenario: Sản phẩm không tồn tại
- **WHEN** client yêu cầu sản phẩm theo id không tồn tại
- **THEN** hệ thống trả về 404 kèm ProblemDetails

### Requirement: Liệt kê sản phẩm có phân trang
Hệ thống SHALL trả về danh sách sản phẩm có phân trang kèm tổng số bản ghi. Trang mặc định 1, kích thước mặc định 20, tối đa 100. Tham số ngoài giới hạn trả về 400.

#### Scenario: Phân trang
- **WHEN** có 25 sản phẩm và client yêu cầu trang 2 kích thước 20
- **THEN** hệ thống trả về 5 sản phẩm, tổng số 25

#### Scenario: Tham số phân trang không hợp lệ
- **WHEN** client yêu cầu trang 0 hoặc kích thước 101
- **THEN** hệ thống trả về 400 kèm ProblemDetails

### Requirement: Tìm kiếm và lọc sản phẩm
Danh sách SHALL hỗ trợ từ khoá khớp một phần (không phân biệt hoa thường) với SKU hoặc tên, và lọc theo trạng thái đang bán/ngừng bán. Tổng số phản ánh kết quả sau lọc.

#### Scenario: Tìm theo từ khoá
- **WHEN** client tìm với từ khoá `thun`
- **THEN** chỉ sản phẩm có SKU hoặc tên chứa `thun` được trả về

#### Scenario: Lọc theo trạng thái
- **WHEN** client lọc sản phẩm ngừng bán
- **THEN** chỉ sản phẩm có trạng thái ngừng bán được trả về

### Requirement: Sắp xếp danh sách sản phẩm
Danh sách SHALL sắp xếp được theo SKU, tên, giá, tồn kho hoặc thời điểm tạo, theo chiều tăng hoặc giảm. Mặc định là thời điểm tạo giảm dần. Thứ tự MUST ổn định giữa các trang khi giá trị cột sắp xếp trùng nhau. Cột sắp xếp không hỗ trợ trả về 400.

#### Scenario: Sắp xếp theo giá tăng dần
- **WHEN** client yêu cầu sắp xếp theo giá tăng dần
- **THEN** sản phẩm được trả về theo giá từ thấp đến cao

#### Scenario: Cột sắp xếp không hợp lệ
- **WHEN** client yêu cầu sắp xếp theo cột `password`
- **THEN** hệ thống trả về 400 kèm ProblemDetails

### Requirement: Cập nhật sản phẩm
Hệ thống SHALL cho phép cập nhật mọi trường của sản phẩm (trừ id và thời điểm tạo) với cùng quy tắc hợp lệ như khi tạo, và ghi nhận thời điểm cập nhật mới.

#### Scenario: Cập nhật thành công
- **WHEN** client cập nhật giá và chuyển sản phẩm sang ngừng bán
- **THEN** hệ thống trả về 200 với dữ liệu mới, thời điểm tạo giữ nguyên, thời điểm cập nhật thay đổi

#### Scenario: Cập nhật sản phẩm không tồn tại
- **WHEN** client cập nhật theo id không tồn tại
- **THEN** hệ thống trả về 404

### Requirement: Xoá sản phẩm
Hệ thống SHALL cho phép xoá vĩnh viễn sản phẩm theo id; SKU đã xoá có thể dùng lại.

#### Scenario: Xoá thành công
- **WHEN** client xoá sản phẩm tồn tại
- **THEN** hệ thống trả về 204 và yêu cầu xem sản phẩm đó sau đó trả về 404

#### Scenario: Xoá sản phẩm không tồn tại
- **WHEN** client xoá theo id không tồn tại
- **THEN** hệ thống trả về 404

### Requirement: Giao diện quản lý sản phẩm
Trang Sản phẩm SHALL hiển thị bảng sản phẩm có phân trang, sắp xếp khi bấm tiêu đề cột, ô tìm kiếm, bộ lọc trạng thái, form tạo/sửa và xoá có xác nhận. Giá MUST hiển thị theo định dạng tiền Việt Nam. Trang, kích thước trang, sắp xếp, bộ lọc và từ khoá MUST được phản ánh trên URL và khôi phục khi tải lại trang.

#### Scenario: Hiển thị và định dạng
- **WHEN** người dùng mở trang Sản phẩm
- **THEN** bảng hiển thị SKU, tên, giá dạng `150.000 ₫`, tồn kho, trạng thái và tổng số bản ghi

#### Scenario: Sắp xếp bằng tiêu đề cột
- **WHEN** người dùng bấm tiêu đề cột Giá
- **THEN** danh sách được tải lại theo giá tăng dần và URL chứa tham số sắp xếp tương ứng

#### Scenario: Khôi phục từ URL
- **WHEN** người dùng mở trang với URL chứa trang 2, lọc ngừng bán và từ khoá `thun`
- **THEN** bảng hiển thị trang 2 của kết quả đã lọc và các ô điều khiển hiển thị đúng giá trị đó

#### Scenario: Tạo trùng SKU trên form
- **WHEN** người dùng gửi form với SKU đã tồn tại
- **THEN** form vẫn mở và hiển thị thông báo SKU đã được sử dụng
