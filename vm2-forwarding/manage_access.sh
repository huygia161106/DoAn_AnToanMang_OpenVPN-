#!/bin/bash
ACTION=$1 # add hoặc del
CLIENT_IP=$2
CLIENT_NAME=$3

# PHÂN QUYỀN TỰ ĐỘNG
if [ "$CLIENT_NAME" == "nv" ]; then
    TARGET="192.168.1.100" # Chỉ vào Database
elif [ "$CLIENT_NAME" == "ceo" ]; then
    TARGET="any"           # Đi mọi nơi
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