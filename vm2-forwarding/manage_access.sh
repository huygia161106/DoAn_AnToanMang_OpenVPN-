#!/bin/bash
ACTION=$1 # add hoặc del
CLIENT_IP=$2
CLIENT_NAME=$3

# ================= PHÂN QUYỀN (DÙNG KÝ TỰ ĐẠI DIỆN *) =================
# Chú ý: Phải dùng 2 dấu ngoặc vuông [[ ]] để bash hiểu ký tự *

if [[ "$CLIENT_NAME" == nv_* ]]; then
    # Tất cả những ai có tên bắt đầu bằng "nv_" (nv_long, nv_lan...) sẽ vào đây
    TARGET="192.168.1.100" 
    
elif [[ "$CLIENT_NAME" == ceo_* ]]; then
    # Tất cả những ai bắt đầu bằng "ceo_" sẽ vào đây
    TARGET="any"           

elif [[ "$CLIENT_NAME" == it_* ]]; then
    TARGET="192.168.1.0/24" # Nhóm IT được vào cả dải mạng LAN, trừ DB nhạy cảm
    
else
    TARGET="none" # Khách lạ -> Khóa mõm
fi

if [ "$ACTION" == "add" ]; then
    if [ "$TARGET" == "any" ]; then
        sudo iptables -A FORWARD -s $CLIENT_IP -j ACCEPT
    elif [ ! -z "$TARGET" ]; then
        sudo iptables -A FORWARD -s $CLIENT_IP -d $TARGET -j ACCEPT
    fi
else
    # Xóa luật khi ngắt kết nối
    sudo iptables -D FORWARD -s $CLIENT_IP -j ACCEPT 2>/dev/null
    sudo iptables -D FORWARD -s $CLIENT_IP -d $TARGET -j ACCEPT 2>/dev/null
fi
