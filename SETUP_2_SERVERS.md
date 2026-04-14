# 🏢 Hệ Thống Quản Trị Phân Tán: Dự Án & Nhân Sự (VPN Lab)

Dự án này mô phỏng kiến trúc mạng nội bộ của một công ty, sử dụng OpenVPN để kết nối từ xa và áp dụng chính sách bảo mật dựa trên vai trò (RBAC).

## 🏗️ Kiến Trúc Hệ Thống

Hệ thống được chia làm 2 máy ảo (VM) độc lập:

1.  **VM 1 (Business Database Server):** Lưu trữ dữ liệu về dự án, đối tác, công việc. Cho phép nhân viên (Staff) truy cập qua VPN để làm việc.
2.  **VM 2 (HR Management Server):** Lưu trữ thông tin nhân sự, lương thưởng. Chỉ cho phép **CEO** truy cập, nhân viên thường bị chặn ở lớp ứng dụng.

---

## 🛠️ PHẦN 1: Cấu Hình VM 1 - Business Database

Máy ảo này đóng vai trò là kho lưu trữ dữ liệu nghiệp vụ.

### 1. Cài đặt PostgreSQL
```bash
sudo apt update && sudo apt install postgresql postgresql-contrib -y
```

### 2. Thiết lập Database & User
Truy cập `psql` (`sudo -i -u postgres psql`) và chạy:
```sql
CREATE DATABASE business_project_db;
CREATE USER project_admin WITH ENCRYPTED PASSWORD 'BizPass123!';
GRANT ALL PRIVILEGES ON DATABASE business_project_db TO project_admin;

-- Tạo bảng mẫu
\c business_project_db
CREATE TABLE projects (id SERIAL PRIMARY KEY, name TEXT, partner TEXT, status TEXT);
INSERT INTO projects (name, partner, status) VALUES ('Hợp đồng A', 'Đối tác X', 'Đang chạy');
```

### 3. Mở mạng cho VPN
* Trong `/etc/postgresql/*/main/postgresql.conf`: Sửa `listen_addresses = '*'`
* Trong `/etc/postgresql/*/main/pg_hba.conf`: Thêm dòng:
    `host  business_project_db  project_admin  10.8.0.0/24  scram-sha-256`
* Khởi động lại: `sudo systemctl restart postgresql`
* Mở firewall: `sudo ufw allow 5432/tcp`

---

## 🛡️ PHẦN 2: Cấu Hình VM 2 - HR Management Server (Tuyệt Mật)

Máy ảo này chạy một API Node.js đóng vai trò "Người gác cổng".

### 1. Cài đặt môi trường
```bash
sudo apt update && sudo apt install nodejs npm postgresql -y
```

### 2. Thiết lập HR Database (Nội bộ VM 2)
Tạo database `hr_internal_db` và bảng nhân viên:
```sql
CREATE DATABASE hr_internal_db;
CREATE USER hr_admin WITH ENCRYPTED PASSWORD 'HrPass123!';
-- (Thực hiện cấp quyền và tạo bảng internal_employees như hướng dẫn trước)
```

### 3. Mã nguồn API Server (Node.js)
Tạo file `server.js` với logic phân quyền chỉ dành cho **CEO**:

```javascript
const express = require('express');
const { Pool } = require('pg');
const app = express();
app.use(express.json());

const pool = new Pool({ /* Cấu hình kết nối hr_internal_db */ });

// Middleware kiểm tra VPN & Quyền CEO
const authorizeCEO = (req, res, next) => {
  const clientIp = req.ip || req.connection.remoteAddress;
  const userRole = req.headers['role']; 

  if (!clientIp.includes('10.8.0')) {
    return res.status(403).json({ error: "Chưa bật VPN!" });
  }
  if (userRole !== 'CEO') {
    return res.status(401).json({ error: "Cảnh báo: Chỉ CEO mới có quyền vào đây!" });
  }
  next();
};

app.get('/api/hr/employees', authorizeCEO, async (req, res) => {
  const result = await pool.query('SELECT * FROM internal_employees');
  res.json(result.rows);
});

app.listen(8080, '0.0.0.0', () => console.log('HR Server running on port 8080'));
```

---

## 🧪 PHẦN 3: Kịch Bản Kiểm Thử (Test Cases)

Mục tiêu là chứng minh nhân viên vào được mạng nhưng không vào được dữ liệu mật.



| Đối tượng | Hành động | Mục tiêu | Kết quả mong đợi |
| :--- | :--- | :--- | :--- |
| **Nhân viên (STAFF)** | Kết nối VPN | Truy cập VM 1 (Dự án) | **Thành công** (Làm việc bình thường) |
| **Nhân viên (STAFF)** | Gọi API VM 2 | Truy cập VM 2 (HR) | **Thất bại** (Lỗi 401 Unauthorized) |
| **Giám đốc (CEO)** | Kết nối VPN | Truy cập VM 2 (HR) | **Thành công** (Xem được lương/nhân sự) |
| **Người lạ** | Không bật VPN | Truy cập VM 1/VM 2 | **Thất bại** (Chặn ngay từ vòng ngoài) |

---

## 🚀 Cách Triển Khai Nhanh
1.  Clone Repository này về cả 2 máy ảo.
2.  Tại VM 1: Chạy file SQL cài đặt Database.
3.  Tại VM 2: Chạy `npm install` và `node server.js`.
4.  Trên máy thật: Bật OpenVPN và dùng Postman để kiểm tra các Role.
