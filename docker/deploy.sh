#!/bin/bash
set -e
echo "Starting deployment for IceBot-Backend..."

# 1. Kéo code chuẩn xác 100% từ GitHub (Combo xe tăng)
git fetch origin
git checkout main
git reset --hard origin/main

cd docker
# 2. Build lại các Docker images
docker compose build

# 3. Chạy EF Core Migration bằng container tạm thời (Đã fix lỗi treo bằng --entrypoint)
echo "Running database migrations..."
docker compose run --rm \
  --entrypoint ./efbundle \
  -e ConnectionStrings__IceBot_DB="Host=postgres;Port=5432;Database=IceBotDB;Username=postgres;Password=p@ssw0rd12345" \
  icebot-backend-api

# 4. Khởi động lại hệ thống ở chế độ background
echo "Starting application services..."
docker compose up -d

echo "Deployment successful!"
