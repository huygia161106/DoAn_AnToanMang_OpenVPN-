# 🛠️ Hướng dẫn Cài đặt & Cấu hình Database Server (PostgreSQL)

Tài liệu này hướng dẫn cách thiết lập một máy chủ PostgreSQL trên Linux (Ubuntu/Debian) để phục vụ quá trình kiểm thử kết nối qua OpenVPN hoặc mạng nội bộ (Internal Network).

## 📋 Yêu cầu hệ thống
* **Hệ điều hành:** Ubuntu 22.04 LTS (khuyên dùng).
* **Mạng:** Máy ảo đã được cấu hình card mạng **Internal Network** (Mạng nội bộ) hoặc **Bridged Adapter**.
* **Quyền hạn:** Có quyền `sudo`.

---

## 1. Cài đặt PostgreSQL
Cập nhật danh sách gói phần mềm và tiến hành cài đặt:

```bash
sudo apt update
sudo apt install postgresql postgresql-contrib -y
```

Sau khi cài đặt, đảm bảo dịch vụ đang chạy:
```bash
sudo systemctl status postgresql
```

---

## 2. Thiết lập Database và User
Chúng ta sẽ tạo một Database riêng và một User có mật khẩu để ứng dụng (WinForms) có thể kết nối.

1. Truy cập vào tài khoản quản trị PostgreSQL:
   ```bash
   sudo -i -u postgres psql
   ```

2. Chạy các lệnh SQL sau:
   ```sql
   -- 1. Tạo Database
   CREATE DATABASE db_test_doanhnghiep;

   -- 2. Tạo User (Thay đổi mật khẩu nếu muốn)
   CREATE USER tester_user WITH ENCRYPTED PASSWORD 'MatKhauTest123!';

   -- 3. Cấp quyền quản lý Database cho User vừa tạo
   GRANT ALL PRIVILEGES ON DATABASE db_test_doanhnghiep TO tester_user;

   -- 4. Thoát khỏi psql
   \q
   ```

---

## 3. Cấu hình kết nối từ xa (Remote Access)
Mặc định PostgreSQL chỉ cho phép kết nối tại chỗ (localhost). Cần thực hiện 2 bước sau để mở cổng cho mạng VPN.

### Bước 3.1: Cho phép lắng nghe từ mọi địa chỉ IP
Mở file cấu hình chính:
```bash
# Lưu ý: Phiên bản (14, 15, 16) tùy thuộc vào bản bạn cài. Sử dụng phím Tab để tự điền đường dẫn.
sudo nano /etc/postgresql/*/main/postgresql.conf
```
Tìm đến dòng `#listen_addresses = 'localhost'`. Sửa lại thành:
```text
listen_addresses = '*'
```
*(Lưu ý: Bỏ dấu `#` ở đầu dòng)*.

### Bước 3.2: Cấu hình danh sách IP được phép truy cập (White-list)
Mở file `pg_hba.conf`:
```bash
sudo nano /etc/postgresql/*/main/pg_hba.conf
```
Cuộn xuống dưới cùng và thêm các dòng sau để cho phép dải IP VPN và Internal Network:
```text
# Cho phép nhân viên từ dải OpenVPN
host    all             all             10.8.0.0/24             scram-sha-256

# Cho phép các máy trong mạng nội bộ (Internal Network)
host    all             all             192.168.50.0/24         scram-sha-256
```

---

## 4. Áp dụng cấu hình và Mở tường lửa
Khởi động lại dịch vụ PostgreSQL:
```bash
sudo systemctl restart postgresql
```

Nếu máy chủ có bật tường lửa (UFW), hãy mở cổng `5432`:
```bash
sudo ufw allow 5432/tcp
sudo ufw reload
```

---

## 5. Thông tin kết nối để kiểm thử
Sau khi bạn kết nối thành công vào OpenVPN, hãy sử dụng các thông tin sau trên phần mềm quản lý Database (DBeaver, Navicat) hoặc ứng dụng Client:

* **Host:** `[Địa chỉ IP máy ảo Database]` (Ví dụ: `192.168.50.100`)
* **Port:** `5432`
* **Database:** `db_test_doanhnghiep`
* **Username:** `tester_user`
* **Password:** `MatKhauTest123!`

---

## 🧪 Kiểm tra trạng thái (Testing)
Để biết gói tin có đi qua đúng cổng hay không, bạn có thể đứng từ máy Client (đã bật VPN) và chạy lệnh:
```powershell
# Trên Windows (PowerShell)
Test-NetConnection -ComputerName [IP_DATABASE] -Port 5432
```
Nếu kết quả `TcpTestSucceeded : True` nghĩa là hệ thống đã thông suốt!