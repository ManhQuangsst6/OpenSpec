#!/bin/sh
# Áp dụng migration của từng module theo thứ tự; dừng ngay khi có lỗi.
set -eu
: "${ConnectionStrings__Default:?Thiếu biến môi trường ConnectionStrings__Default}"
for bundle in ./*-efbundle; do
  echo "==> $bundle"
  "$bundle" --connection "$ConnectionStrings__Default"
done
echo "Migrations applied."
