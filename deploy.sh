#!/bin/bash
set -e

echo "Starting deployment for IceBot-Backend..."

# 1. Tải toàn bộ thông tin mới nhất từ GitHub về mà chưa áp dụng ngay
git fetch origin

# 2. Bắt buộc chuyển sang nhánh main
git checkout main

# 3. Ép code trên VPS phải giống hệt 100% với code trên GitHub (xóa bỏ mọi thay đổi lỡ tay viết nháp trên VPS)
git reset --hard origin/main

docker compose build

# Chạy bundle migration. Nó sẽ tự động lấy chuỗi kết nối từ môi trường.
echo "Running database migrations..."
docker compose run --rm \
  -e ConnectionStrings__IceBot_DB="Host=postgres;Port=5432;Database=IceBotDB;Username=postgres;Password=p@ssw0rd12345" \
  icebot-backend-api \
  ./efbundle

echo "Starting application services..."
docker compose up -d

echo "Deployment successful!"
