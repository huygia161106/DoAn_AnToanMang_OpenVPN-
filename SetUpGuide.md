# Hệ thống VPN Gateway với Auto-Forwarding Rules

Đồ án triển khai hệ thống OpenVPN kết hợp phân quyền truy cập tự động (Zero-Trust Network Access) sử dụng iptables.

##  Kiến trúc hệ thống
- **VM1 (OpenVPN Server):** Cấp phát IP VPN (`10.9.0.x`), xác thực chứng chỉ ECDSA. Xử lý IP Forwarding sang mạng nội bộ.
- **VM2 (Forwarding Gateway):** Đóng vai trò Firewall nội bộ (`192.168.1.2`). Chạy lệnh iptables động dựa trên profile người dùng.

##  Hướng dẫn Setup

### Bước 1: Setup Mạng (VirtualBox)
1. Cấu hình card mạng `Internal Network` cho cả VM1 và VM2.
2. Đặt IP tĩnh: VM1 (`192.168.1.1`), VM2 (`192.168.1.2`).
3. Bật IP Forwarding trên cả 2 máy: `sudo sysctl -w net.ipv4.ip_forward=1`.

### Bước 2: Setup VM1 (OpenVPN Server)
1. Cách cài server OpenVPN từ folder trên github:
```
# Copy file cấu hình chính
sudo cp easy-rsa-ecdsa/server_ecdsa.conf /etc/openvpn/server_ecdsa/

# Copy Chứng chỉ và Khóa
sudo cp easy-rsa-ecdsa/ca.crt /etc/openvpn/server_ecdsa/
sudo cp easy-rsa-ecdsa/server.crt /etc/openvpn/server_ecdsa/
sudo cp easy-rsa-ecdsa/server.key /etc/openvpn/server_ecdsa/
sudo cp easy-rsa-ecdsa/ta.key /etc/openvpn/server_ecdsa/

#Cấp quyền
sudo chmod 600 /etc/openvpn/server_ecdsa/server.key
sudo chmod 600 /etc/openvpn/server_ecdsa/ta.key

#Chạy
sudo systemctl start openvpn@server_ecdsa (bật Openvpn server)
sudo systemctl restart openvpn@server_ecdsa (restart nếu thay đổi cấu hình Openvpn server)
sudo systemctl status openvpn@server_ecdsa (xem trạng thái server)
```

2. Copy 2 file `remote_trigger.sh` và `remote_untouch.sh` vào `/etc/openvpn/` và cấp quyền thực thi.

3. Mở luồng Forwarding (Không NAT):
 ```
sudo iptables -I FORWARD 1 -i tun0 -o enp0s8 -j ACCEPT
sudo iptables -I FORWARD 1 -i enp0s8 -o tun0 -j ACCEPT
 ```

### Bước 3: Setup VM2 (Forwarding Gateway)
1. Cài đặt OpenSSH Server.
2. Copy file `manage_access.sh` vào `/usr/local/bin/` và cấp quyền thực thi (`chmod +x`).
3. Cấp quyền `visudo` cho script:
   `vtbox ALL=(ALL) NOPASSWD: /usr/local/bin/manage_access.sh, /usr/sbin/iptables`
4. Cấu hình Route & NAT để gói tin biết đường về:
   `sudo ip route add 10.9.0.0/24 via 192.168.1.1`
   `sudo iptables -t nat -A POSTROUTING -o enp0s8 -j MASQUERADE`

# Cách Demo
* Trên VM1, gõ lệnh 
    ```
    sudo systemctl start openvpn@server_ecdsa (bật Openvpn server)
    sudo systemctl status openvpn@server_ecdsa (xem trạng thái server)
    ```
* Trên Windows, connect OpenVPN bằng profile ceo hoặc nv.
* Trên VM2, gõ lệnh: watch -n 1 "sudo iptables -L FORWARD -n -v" để xem luật tường lửa tự động bật/tắt.