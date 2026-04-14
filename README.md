# DoAn_AnToanMang_OpenVPN-

Cấu trúc thư mục (Repository Structure)

```
vpn-forwarding-project/
├── README.md                  # File hướng dẫn cài đặt & báo cáo
├── vm1-openvpn/
│   ├── remote_trigger.sh      # Script chạy khi Connect
│   └── remote_untouch.sh      # Script chạy khi Disconnect
└── vm2-forwarding/
    └── manage_access.sh       # Script phân quyền iptables
```
