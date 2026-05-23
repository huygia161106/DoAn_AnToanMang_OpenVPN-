# 1. QUẢN LÝ NGƯỜI DÙNG (USER & IP)
* Vì hệ thống dùng MySQL làm trung tâm xác thực, mọi thao tác tạo/khóa tài khoản đều thực hiện qua lệnh SQL. Bạn chỉ cần mở Terminal Ubuntu và chạy thẳng các lệnh sau:
1. Xem danh sách tất cả nhân viên trong hệ thống:
```
sudo mysql -u root -e "SELECT id, username, full_name, role, status FROM vpn_db.vpn_users;"
```
* Status = 1 là đang hoạt động, Status = 0 là bị khóa.
2. Khóa khẩn cấp một tài khoản (Thu hồi quyền lập tức):
```
# Ví dụ khóa tài khoản của IT01
sudo mysql -u root -e "UPDATE vpn_db.vpn_users SET status = 0 WHERE username = 'it01';"
```

3. Tạo nhân viên mới (Mật khẩu mặc định là 123456):
```
sudo mysql -u root -e "INSERT INTO vpn_db.vpn_users (username, full_name, password_hash, role, status) VALUES ('nhanvienmoi', 'Nguyễn Văn Mới', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 'nv', 1);"
```
4. Cấp IP tĩnh (Ghế VIP) cho nhân viên vừa tạo: Tên file phải trùng khớp 100% với username vừa tạo ở trên(Cái này chỉ dành cho nhân vật lớn còn nv quèn thì cút).
```
echo "ifconfig-push 10.9.0.60 255.255.255.0" | sudo tee /etc/openvpn/ccd/nhanvienmoi
```
# 2. QUẢN LÝ LOG VÀ TRUY VẾT (AUDIT)
* Xem logs bằng sql:
1. Xem ai đang ONLINE (Đang kết nối) ngay lúc này:
```
sudo mysql -u root -e "SELECT username, assigned_ip, connect_time FROM vpn_db.vpn_logs WHERE session_status = 'ONLINE';"
```
2. Xem 10 phiên kết nối gần nhất (Nhật ký chung):
```
sudo mysql -u root -e "SELECT username, assigned_ip, connect_time, disconnect_time, session_status FROM vpn_db.vpn_logs ORDER BY connect_time DESC LIMIT 10;"
```
3. Truy vết thủ phạm theo địa chỉ IP: Ví dụ, hệ thống báo IP 10.9.0.50 đang có dấu hiệu bất thường, gõ lệnh này lòi ra ngay người đang cầm IP đó:
```
sudo mysql -u root -e "SELECT * FROM vpn_db.vpn_logs WHERE assigned_ip = '10.9.0.50' ORDER BY connect_time DESC;"
```
# 3. QUẢN LÝ TƯỜNG LỬA (ĐỘNG)
* Mỗi khi có người kết nối, Python sẽ tự động "đục lỗ" Tường lửa. Để kiểm tra xem Python có đang làm việc nghiêm túc không, bạn dùng lệnh này:
1. Xem các luật Tường lửa đang được cấp phát thực tế:
```
sudo iptables -L FORWARD -n -v --line-numbers
```
* Cách đọc hiểu: Bạn sẽ thấy những dòng có chữ ACCEPT đứng ở ngay đầu danh sách, chỉ định rõ IP nguồn (Source) nào đang được phép đi tới IP đích (Destination) nào.
2. Cứu hộ Tường lửa (Nếu Python lỗi kẹt luật): Nếu lỡ hệ thống sập nguồn, script disconnect.py chưa kịp dọn rác, bạn gõ lệnh này để khởi động lại Tường lửa về trạng thái mặc định (Zero-Trust) ban đầu:
```
sudo netfilter-persistent reload
```
# 4. QUẢN LÝ DỊCH VỤ OPENVPN
* Đây là các lệnh quản trị dịch vụ (Daemon) của hệ điều hành Linux.
1. Xem trạng thái sống chết của Server OpenVPN:
```
sudo systemctl status openvpn@server_ecdsa
```
2. Khởi động lại Server (Áp dụng ngay khi sửa file .conf hoặc topology):
```
sudo systemctl restart openvpn@server_ecdsa
```
3. Xem Log lỗi thô của Hệ thống (Dùng để bắt bệnh khi khởi động lỗi):
```
sudo journalctl -u openvpn@server_ecdsa -xe -n 50
```
4. Xem danh sách IP đang kết nối theo góc nhìn của chính OpenVPN (Real-time):
```
cat /etc/openvpn/openvpn-status.log
```
