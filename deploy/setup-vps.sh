#!/bin/bash
# ==============================================================================
# SCRIPT HƯỚNG DẪN DEPLOY PRODUCTION VPS CHO INTERNAL MAINTENANCE MANAGEMENT
# ==============================================================================
# QUAN TRỌNG: Script này được thiết kế an toàn, TUYỆT ĐỐI KHÔNG làm gián đoạn
# tiến trình Node.js E-commerce đang chạy trên cùng VPS.
# ==============================================================================

set -e

echo "=== 1. KIỂM TRA TÀI NGUYÊN VÀ TIẾN TRÌNH HIỆN TẠI TRÊN VPS ==="
echo "--- Thông tin HĐH ---"
uname -a
echo "--- RAM hiện tại ---"
free -h
echo "--- Dung lượng ổ cứng ---"
df -h /
echo "--- Swap hiện tại ---"
swapon --show

echo ""
echo "=== 2. TẠO 2GB SWAP PHÒNG HỜ OOM (NẾU CHƯA CÓ) ==="
if [ $(swapon --show | wc -l) -le 1 ]; then
    echo "Tạo 2GB swap file..."
    sudo fallocate -l 2G /swapfile
    sudo chmod 600 /swapfile
    sudo mkswap /swapfile
    sudo swapon /swapfile
    echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
    echo "Swap 2GB đã được kích hoạt thành công!"
else
    echo "VPS đã có swap. Bỏ qua bước này."
fi

echo ""
echo "=== 3. KHỞI CHẠY SQL SERVER 2022 (DOCKER) ==="
sudo mkdir -p /var/lib/maintenance-mssql/data
sudo mkdir -p /var/lib/maintenance-mssql/backup
sudo chmod 777 /var/lib/maintenance-mssql/data
sudo chmod 777 /var/lib/maintenance-mssql/backup

# Chạy SQL Server với docker-compose.prod.yml (khống chế 1200MB RAM, bind 127.0.0.1:1433)
sudo docker compose -f docker-compose.prod.yml up -d
echo "SQL Server container đang chạy nội bộ trên 127.0.0.1:1433."

echo ""
echo "=== 4. THIẾT LẬP THƯ MỤC ỨNG DỤNG VÀ BIẾN MÔI TRƯỜNG ==="
sudo mkdir -p /var/www/internal-maintenance/api
sudo mkdir -p /etc/internal-maintenance

if [ ! -f /etc/internal-maintenance/internal-maintenance.env ]; then
    echo "Sao chép template env..."
    sudo cp deploy/internal-maintenance.env.template /etc/internal-maintenance/internal-maintenance.env
    sudo chmod 600 /etc/internal-maintenance/internal-maintenance.env
    echo "Vui lòng mở file /etc/internal-maintenance/internal-maintenance.env và điền các secret (Resend API key, R2 key, DB password)."
fi

echo ""
echo "=== 5. CÀI ĐẶT SYSTEMD SERVICE CHO .NET 10 API ==="
sudo cp deploy/internal-maintenance-api.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable internal-maintenance-api

echo ""
echo "=== 6. CẤU HÌNH NGINX (ĐỘC LẬP VỚI E-COMMERCE) ==="
sudo cp deploy/api-maintenance.shopmini.io.vn.conf /etc/nginx/sites-available/
sudo ln -sf /etc/nginx/sites-available/api-maintenance.shopmini.io.vn.conf /etc/nginx/sites-enabled/

echo "Kiểm tra cú pháp Nginx trước khi reload..."
sudo nginx -t
sudo systemctl reload nginx

echo ""
echo "=== 7. CÀI ĐẶT SSL VỚI CERTBOT ==="
echo "Chạy lệnh sau để cấp chứng chỉ HTTPS:"
echo "sudo certbot --nginx -d api-maintenance.shopmini.io.vn"

echo ""
echo "=== HOÀN TẤT BƯỚC THIẾT LẬP BAN ĐẦU ==="
