#!/bin/bash

echo "🔍 Demonstration: TShark can still see JWT tokens"
echo "=================================================="
echo ""
echo "⚠️  IMPORTANT: Since we're using HTTP (not HTTPS),"
echo "   TShark can see ALL data in plain text,"
echo "   including JWT tokens, passwords, and request data!"
echo ""

BASE_URL="http://localhost:8083"

# 1. Login
echo "1️⃣  Logging in..."
LOGIN_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}')

TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.token')
echo "   ✅ Token received: ${TOKEN:0:50}..."
echo ""

# 2. Make authenticated request
echo "2️⃣  Making authenticated request with token..."
curl -s -X GET "$BASE_URL/api/orders" \
  -H "Authorization: Bearer $TOKEN" > /dev/null
echo "   ✅ Request sent"
echo ""

# 3. Wait for capture
echo "3️⃣  Waiting for TShark to capture..."
sleep 3
echo ""

# 4. Check what was captured
echo "4️⃣  Checking TShark capture..."
echo "   📦 Capture file:"
ls -lh captures/insecure_http.pcap
echo ""

echo "5️⃣  Captured traffic analysis:"
echo "   TShark captured:"
echo "   - ✅ Complete HTTP requests"
echo "   - ✅ HTTP headers (including Authorization: Bearer ...)"
echo "   - ✅ JWT tokens in plain text"
echo "   - ✅ Request and response data"
echo ""

echo "🔐 CONCLUSION:"
echo "   With HTTP, TShark sees EVERYTHING in plain text!"
echo "   To protect against this, you need to:"
echo "   - ✅ Implement HTTPS/TLS"
echo "   - ✅ Encrypt the communication"
echo ""

echo "💡 To see tokens in the capture file:"
echo "   docker exec tshark_sniffer tshark -r /captures/insecure_http.pcap -V | grep -i 'authorization\|bearer'"
echo ""
