#!/bin/bash
set -e

echo "Starting deployment for IceBot-Backend..."

git pull origin main

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
