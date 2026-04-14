#!/bin/bash
# Gọi sang VM2 để đóng tường lửa
ssh -i /etc/openvpn/ssh_keys/id_ed25519 -o StrictHostKeyChecking=no vtbox@192.168.1.2 "sudo /usr/local/bin/manage_access.sh del $ifconfig_pool_remote_ip $common_name" > /dev/null 2>&1
exit 0