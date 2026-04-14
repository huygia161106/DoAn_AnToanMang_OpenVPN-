# DoAn_AnToanMang_OpenVPN-

Cấu trúc thư mục (Repository Structure)

```
vpn-forwarding-project/
├── .gitignore                 # BẢO VỆ KEY: Chặn Git tải file nhạy cảm lên
├── README.md                  
├── vm1-openvpn/
│   ├── remote_trigger.sh      
│   └── remote_untouch.sh      
├── vm2-forwarding/
│   └── manage_access.sh       
└── openvpn-config/
    └── server_ecdsa.conf      # File cấu hình chuẩn của OpenVPN
```
