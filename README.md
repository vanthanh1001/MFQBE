## FitnessApp API

### Thiết lập

1. Clone dự án về máy của bạn
2. Tạo file `appsettings.json` từ `appsettings.example.json` và cập nhật:
   - JWT Secret Key
   - Connection string Database
   - Thông tin Firebase

3. Tạo file `credentials/firebase-adminsdk.json` từ `credentials/credentials.example.json`:
   - Tạo service account trên Firebase Console
   - Tải xuống file JSON chứa private key
   - Đặt file vào thư mục `credentials` và đổi tên thành `firebase-adminsdk.json`

4. Cài đặt các package:
   ```
   dotnet restore
   ```

5. Cập nhật database:
   ```
   dotnet ef database update
   ```

6. Chạy ứng dụng:
   ```
   dotnet run
   ```

### Lưu ý bảo mật

- Không bao giờ commit các file chứa thông tin nhạy cảm lên Git
- Các file nhạy cảm bao gồm:
  - `appsettings.json`
  - `credentials/firebase-adminsdk.json`
- File `.gitignore` đã được cấu hình để bỏ qua các file này

### Swagger

- API được tài liệu hóa với Swagger
- Truy cập Swagger UI: `https://localhost:5001/swagger` 