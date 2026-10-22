# Spec Delta

## Purpose

Quản lý hồ sơ người dùng của hệ thống: tạo, xem, tìm kiếm, cập nhật và xoá người dùng, đảm bảo email hợp lệ và duy nhất.

## ADDED Requirements

### Requirement: Tạo người dùng
Hệ thống SHALL cho phép tạo người dùng với email và họ tên. Email MUST được cắt khoảng trắng và chuyển về chữ thường trước khi lưu. Khi thành công, hệ thống trả về người dùng vừa tạo kèm id và thời điểm tạo.

#### Scenario: Tạo thành công
- **WHEN** client gửi yêu cầu tạo với email ` Alice@Example.COM ` và họ tên `Alice Nguyễn`
- **THEN** hệ thống trả về 201 Created, header Location trỏ tới người dùng mới, body có email `alice@example.com`, họ tên `Alice Nguyễn`, id và thời điểm tạo

#### Scenario: Email không đúng định dạng
- **WHEN** client gửi yêu cầu tạo với email `not-an-email`
- **THEN** hệ thống trả về 400 kèm ProblemDetails có lỗi cho trường `email` và không tạo người dùng

#### Scenario: Họ tên trống hoặc quá dài
- **WHEN** client gửi yêu cầu tạo với họ tên rỗng hoặc dài hơn 200 ký tự
- **THEN** hệ thống trả về 400 kèm ProblemDetails có lỗi cho trường `fullName`

### Requirement: Email là duy nhất
Hệ thống MUST NOT cho phép hai người dùng có cùng email (so sánh sau khi chuẩn hoá chữ thường).

#### Scenario: Tạo với email đã tồn tại
- **WHEN** đã có người dùng `alice@example.com` và client tạo người dùng với email `ALICE@example.com`
- **THEN** hệ thống trả về 409 Conflict kèm ProblemDetails và không tạo người dùng mới

#### Scenario: Cập nhật sang email của người khác
- **WHEN** client cập nhật người dùng B sang email đang thuộc về người dùng A
- **THEN** hệ thống trả về 409 Conflict và người dùng B giữ nguyên dữ liệu cũ

### Requirement: Xem chi tiết người dùng
Hệ thống SHALL trả về thông tin một người dùng theo id.

#### Scenario: Người dùng tồn tại
- **WHEN** client yêu cầu người dùng theo một id đang tồn tại
- **THEN** hệ thống trả về 200 với id, email, họ tên và thời điểm tạo

#### Scenario: Người dùng không tồn tại
- **WHEN** client yêu cầu người dùng theo id không tồn tại
- **THEN** hệ thống trả về 404 kèm ProblemDetails

### Requirement: Liệt kê và tìm kiếm người dùng
Hệ thống SHALL trả về danh sách người dùng có phân trang, sắp xếp theo thời điểm tạo mới nhất trước, kèm tổng số bản ghi. Trang mặc định là 1, kích thước mặc định 20, tối đa 100. Có thể lọc bằng từ khoá khớp một phần (không phân biệt hoa thường) với email hoặc họ tên.

#### Scenario: Phân trang
- **WHEN** có 25 người dùng và client yêu cầu trang 2 với kích thước 20
- **THEN** hệ thống trả về 5 người dùng, tổng số 25, trang 2, kích thước 20

#### Scenario: Tìm kiếm theo từ khoá
- **WHEN** client tìm với từ khoá `nguyen`
- **THEN** chỉ những người dùng có email hoặc họ tên chứa `nguyen` (không phân biệt hoa thường) được trả về, tổng số phản ánh kết quả đã lọc

#### Scenario: Tham số phân trang không hợp lệ
- **WHEN** client yêu cầu trang 0 hoặc kích thước lớn hơn 100
- **THEN** hệ thống trả về 400 kèm ProblemDetails

### Requirement: Cập nhật người dùng
Hệ thống SHALL cho phép cập nhật email và họ tên của người dùng hiện có, áp dụng cùng quy tắc hợp lệ và chuẩn hoá như khi tạo. Id và thời điểm tạo không thay đổi.

#### Scenario: Cập nhật thành công
- **WHEN** client cập nhật người dùng tồn tại với họ tên mới hợp lệ
- **THEN** hệ thống trả về 200 với dữ liệu đã cập nhật, id và thời điểm tạo giữ nguyên

#### Scenario: Cập nhật người dùng không tồn tại
- **WHEN** client cập nhật người dùng theo id không tồn tại
- **THEN** hệ thống trả về 404

### Requirement: Xoá người dùng
Hệ thống SHALL cho phép xoá vĩnh viễn một người dùng theo id. Sau khi xoá, email đó có thể dùng lại.

#### Scenario: Xoá thành công
- **WHEN** client xoá một người dùng tồn tại
- **THEN** hệ thống trả về 204 và yêu cầu xem chi tiết người dùng đó sau đó trả về 404

#### Scenario: Xoá người dùng không tồn tại
- **WHEN** client xoá theo id không tồn tại
- **THEN** hệ thống trả về 404

### Requirement: Giao diện quản lý người dùng
Trang Users trên frontend SHALL hiển thị danh sách người dùng có ô tìm kiếm và điều hướng trang, cho phép tạo, sửa (qua form) và xoá (sau khi người dùng xác nhận). Lỗi từ API MUST được hiển thị cho người dùng.

#### Scenario: Hiển thị danh sách
- **WHEN** người dùng mở trang Users
- **THEN** bảng hiển thị email, họ tên, ngày tạo của từng người dùng và tổng số bản ghi

#### Scenario: Tạo người dùng với email trùng
- **WHEN** người dùng gửi form tạo với email đã tồn tại
- **THEN** form vẫn mở và hiển thị thông báo email đã được sử dụng

#### Scenario: Xoá có xác nhận
- **WHEN** người dùng bấm xoá một dòng và xác nhận
- **THEN** người dùng đó biến mất khỏi danh sách; nếu huỷ xác nhận thì không có gì bị xoá
