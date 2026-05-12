# 🌐 PHASE 0: Thiết lập IP Tĩnh cho Ubuntu (Cực kỳ quan trọng)
* Máy chủ VPN bắt buộc phải có IP cố định để file .ovpn của Client biết đường tìm tới.

* 1. Kiểm tra tên card mạng: Gõ ip a để xem tên card mạng (thường là ens33, eth0 hoặc enp0s3). Giả sử máy bạn là ens33 và bạn muốn set IP là 192.168.1.200.

* 2. Sửa file Netplan:
```
Bash
ls /etc/netplan/
# Giả sử file tên là 00-installer-config.yaml
sudo nano /etc/netplan/00-installer-config.yaml
```

* 3. Xóa hết nội dung cũ, gõ nội dung này vào (LƯU Ý: Không dùng phím Tab, chỉ dùng dấu Cách):
```
YAML
network:
  version: 2
  renderer: networkd
  ethernets:
    ens33:
      dhcp4: no
      addresses:
        - 192.168.1.200/24
      routes:
        - to: default
          via: 192.168.1.1
      nameservers:
        addresses:
          - 8.8.8.8
          - 8.8.4.4
Lưu lại (Ctrl+O -> Enter -> Ctrl+X) và áp dụng:
```
```
Bash
sudo netplan apply
```
# 🛠️ PHASE 1: Cài đặt gói phần mềm
```
Bash
sudo apt update
sudo apt install openvpn mysql-server python3 python3-pip iptables-persistent -y
# Khi bảng iptables-persistent hiện lên, dùng phím mũi tên chọn YES 2 lần.

# Cài thư viện kết nối MySQL cho Python
sudo pip3 install pymysql --break-system-packages
```

# 🗄️ PHASE 2: Xây dựng Database (MySQL)
* Cài đặt tài khoản Python, bảng Users (11 người) và bảng Logs (để không bị lỗi khi script chạy).

* 1. Mở MySQL:
```
Bash
sudo mysql -u root
```
* 2. Copy toàn bộ khối lệnh này dán vào Terminal (bấm Chuột phải để Paste -> Enter):
```
SQL
CREATE DATABASE IF NOT EXISTS vpn_db;
USE vpn_db;

-- 1. Tạo User cho Python
CREATE USER IF NOT EXISTS 'vpn_script'@'localhost' IDENTIFIED BY 'Vpn@12345';
GRANT ALL PRIVILEGES ON vpn_db.* TO 'vpn_script'@'localhost';
FLUSH PRIVILEGES;

-- 2. Tạo bảng Users
CREATE TABLE IF NOT EXISTS vpn_users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    full_name VARCHAR(100) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(20) NOT NULL,
    status INT DEFAULT 1
);

-- 3. Tạo bảng Logs (Bắt buộc phải có để truy vết)
CREATE TABLE IF NOT EXISTS vpn_logs (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) NOT NULL,
    assigned_ip VARCHAR(20) NOT NULL,
    connect_time DATETIME NOT NULL,
    disconnect_time DATETIME NULL,
    session_status VARCHAR(20) DEFAULT 'ONLINE'
);

-- 4. Bơm 11 User (Pass mặc định là 123456)
INSERT INTO vpn_users (username, full_name, password_hash, role, status) VALUES 
('ceo', 'CEO', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 'ceo', 1),

('nguyenvana', 'Nguyễn Văn A', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 'it', 1),
('tranthib', 'Trần Thị B', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 'it', 1),
('hoangvanc', 'Hoàng Văn C', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 'it', 1),
('phamthid', 'Phạm Thị D', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 'it', 1),
('dinhvane', 'Đinh Văn E', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 'it', 1),

('dangthif', 'Đặng Thị F', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 'nv', 1),
('buivang', 'Bùi Văn G', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 'nv', 1),
('dothih', 'Đỗ Thị H', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 'nv', 1),
('hovani', 'Hồ Văn I', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 'nv', 1),
('ngothik', 'Ngô Thị K', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 'nv', 1);

EXIT;
```
# 🔑 PHASE 3: Copy Chứng chỉ (Key/Cert)
* Truy cập vào thư mục /easy-rsa-ecdsa đó và chạy các lệnh sau (copy cert to openvpn)(you may have a different directory):
```
Bash
sudo cp pki/ca.crt /etc/openvpn/
sudo cp pki/issued/server.crt /etc/openvpn/
sudo cp pki/private/server.key /etc/openvpn/
sudo cp ta.key /etc/openvpn/
```
# ⚙️ PHASE 4: File Cấu hình OpenVPN
```
Bash
sudo nano /etc/openvpn/server_ecdsa.conf
```
```
port 1195
proto udp
dev tun

ca /etc/openvpn/ca.crt
cert /etc/openvpn/server.crt
key /etc/openvpn/server.key
tls-auth /etc/openvpn/ta.key 0
cipher AES-256-GCM
auth SHA256

# Cấp IP động (Cứ bốc ngẫu nhiên, Python sẽ lo phần còn lại)
server 10.9.0.0 255.255.255.0
push "route 192.168.1.0 255.255.255.0"
push "redirect-gateway def1 bypass-dhcp"
push "dhcp-option DNS 8.8.8.8"

# Bật tính năng gọi Python (Cực kỳ quan trọng)
script-security 3
username-as-common-name
verify-client-cert require

auth-user-pass-verify /etc/openvpn/scripts/auth.py via-env
client-connect /etc/openvpn/scripts/rbac_connect.py
client-disconnect /etc/openvpn/scripts/rbac_disconnect.py

keepalive 10 120
persist-key
persist-tun
status openvpn-status.log
verb 3
```

# 🐍 PHASE 5: Bộ 3 Kịch Bản Python "DevSecOps"
* Tạo thư mục trước:
```
Bash
sudo mkdir -p /etc/openvpn/scripts
```

* 1. File Xác thực (auth.py)
```
Bash
sudo nano /etc/openvpn/scripts/auth.py
```
```
Python
#!/usr/bin/env python3
import os, sys, pymysql, hashlib

username = os.environ.get('username')
password = os.environ.get('password')

if not username or not password: sys.exit(1)

input_pwd_hash = hashlib.sha256(password.encode()).hexdigest()

try:
    conn = pymysql.connect(host='localhost', user='vpn_script', password='Vpn@12345', database='vpn_db', cursorclass=pymysql.cursors.DictCursor)
    with conn.cursor() as cursor:
        cursor.execute("SELECT username FROM vpn_users WHERE username = %s AND password_hash = %s AND status = 1", (username, input_pwd_hash))
        result = cursor.fetchone()
    conn.close()
    if result: sys.exit(0)
    else: sys.exit(1)
except Exception: sys.exit(1)
```
* 2. File Bơm Tường lửa & Ghi Log Vào (rbac_connect.py)
```
Bash
sudo nano /etc/openvpn/scripts/rbac_connect.py
```

```
#!/usr/bin/env python3
import os, sys, pymysql, subprocess
from datetime import datetime

username = os.environ.get('common_name')
client_ip = os.environ.get('ifconfig_pool_remote_ip')

if not username or not client_ip: sys.exit(0)

role = 'nv'
try:
    conn = pymysql.connect(host='localhost', user='vpn_script', password='Vpn@12345', database='vpn_db', cursorclass=pymysql.cursors.DictCursor)
    with conn.cursor() as cursor:
        cursor.execute("SELECT role FROM vpn_users WHERE username = %s", (username,))
        res = cursor.fetchone()
        if res: role = res['role']
        
        now = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
        cursor.execute("INSERT INTO vpn_logs (username, assigned_ip, connect_time, session_status) VALUES (%s, %s, %s, 'ONLINE')", (username, client_ip, now))
    conn.commit()
    conn.close()
except Exception: pass

def run_firewall(cmd):
    subprocess.run(cmd.split(), stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

if role == 'ceo':
    run_firewall(f"/usr/sbin/iptables -I FORWARD -s {client_ip} -j ACCEPT")
elif role == 'it':
    run_firewall(f"/usr/sbin/iptables -I FORWARD -s {client_ip} -d 192.168.1.100 -j ACCEPT")
elif role == 'nv':
    run_firewall(f"/usr/sbin/iptables -I FORWARD -s {client_ip} -d 192.168.1.50 -j ACCEPT")

sys.exit(0)
```

* 3. File Dọn Tường lửa & Ghi Log Ra (rbac_disconnect.py)
```
Bash
sudo nano /etc/openvpn/scripts/rbac_disconnect.py
```
```
#!/usr/bin/env python3
import os, sys, pymysql, subprocess
from datetime import datetime

username = os.environ.get('common_name')
client_ip = os.environ.get('ifconfig_pool_remote_ip')

if not username or not client_ip: sys.exit(0)

# Dọn sạch tường lửa (Quét liên tục đến khi hết)
while True:
    r1 = subprocess.run(f"/usr/sbin/iptables -D FORWARD -s {client_ip} -d 192.168.1.100 -j ACCEPT".split(), stderr=subprocess.DEVNULL)
    r2 = subprocess.run(f"/usr/sbin/iptables -D FORWARD -s {client_ip} -d 192.168.1.50 -j ACCEPT".split(), stderr=subprocess.DEVNULL)
    r3 = subprocess.run(f"/usr/sbin/iptables -D FORWARD -s {client_ip} -j ACCEPT".split(), stderr=subprocess.DEVNULL)
    if r1.returncode != 0 and r2.returncode != 0 and r3.returncode != 0: break

# Ghi Log THOÁT
try:
    now = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    conn = pymysql.connect(host='localhost', user='vpn_script', password='Vpn@12345', database='vpn_db')
    with conn.cursor() as cursor:
        cursor.execute("UPDATE vpn_logs SET disconnect_time = %s, session_status = 'OFFLINE' WHERE username = %s AND assigned_ip = %s AND session_status = 'ONLINE'", (now, username, client_ip))
    conn.commit()
    conn.close()
except Exception: pass

sys.exit(0)
```
* 4. Cấp quyền thực thi tuyệt đối cho Python:
```
Bash
sudo chmod +x /etc/openvpn/scripts/*.py
```

# 🧱 PHASE 6: Mạng & Lưới Lọc Đáy (Zero-Trust)
* Đây là mảnh ghép cuối cùng để biến máy ảo thành Router.

* 1. Bật IP Forwarding:
```
Bash
sudo nano /etc/sysctl.conf
# Kéo xuống tìm dòng #net.ipv4.ip_forward=1 -> XÓA dấu # đi. Lưu lại.
sudo sysctl -p
```

* 2. Gõ lệnh iptables nền tảng (Nhớ thay ens33 thành tên card mạng thực tế của bạn):
```
Bash
sudo iptables -t nat -A POSTROUTING -s 10.9.0.0/24 -o ens33 -j MASQUERADE

# Chặn mọi gói tin VPN đi lung tung (Chỉ có Python mới được phép tiêm luật Mở Cửa (-I) lên trên luật này)
sudo iptables -A FORWARD -s 10.9.0.0/24 -j DROP

# Lưu lại vĩnh viễn
sudo netfilter-persistent save
```

# 🚀 PHASE 7: Khởi chạy Hệ Thống
```
Bash
sudo systemctl enable openvpn@server_ecdsa
sudo systemctl restart openvpn@server_ecdsa
sudo systemctl status openvpn@server_ecdsa
```
