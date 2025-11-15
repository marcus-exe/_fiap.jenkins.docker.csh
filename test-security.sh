#!/bin/bash

# Security Testing Script
# This script tests all security features implemented in the microservices

echo "🔒 Security Features Test Script"
echo "================================"
echo ""

BASE_URL_PRODUCTS="http://localhost:8082"
BASE_URL_ORDERS="http://localhost:8083"

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test counter
PASSED=0
FAILED=0

# Function to print test result
test_result() {
    if [ $1 -eq 0 ]; then
        echo -e "${GREEN}✅ PASS${NC}: $2"
        ((PASSED++))
    else
        echo -e "${RED}❌ FAIL${NC}: $2"
        ((FAILED++))
    fi
}

# Test 1: Health check (should work without auth)
echo "1. Testing Health Endpoints (Public)..."
HEALTH_PRODUCTS=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL_PRODUCTS/health")
HEALTH_ORDERS=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL_ORDERS/health")
test_result $([ "$HEALTH_PRODUCTS" = "200" ] && [ "$HEALTH_ORDERS" = "200" ] && echo 0 || echo 1) "Health checks accessible"
echo ""

# Test 2: Protected endpoints without token (should fail)
echo "2. Testing Protected Endpoints Without Token..."
PRODUCTS_NO_AUTH=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL_PRODUCTS/api/products")
ORDERS_NO_AUTH=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL_ORDERS/api/orders")
test_result $([ "$PRODUCTS_NO_AUTH" = "401" ] && [ "$ORDERS_NO_AUTH" = "401" ] && echo 0 || echo 1) "Protected endpoints reject requests without token"
echo ""

# Test 3: Login with correct credentials
echo "3. Testing Login with Correct Credentials..."
LOGIN_RESPONSE=$(curl -s -X POST "$BASE_URL_PRODUCTS/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}')

if echo "$LOGIN_RESPONSE" | grep -q "token"; then
    TOKEN=$(echo "$LOGIN_RESPONSE" | grep -o '"token":"[^"]*' | cut -d'"' -f4)
    test_result 0 "Login successful and token received"
    echo "   Token: ${TOKEN:0:50}..."
else
    test_result 1 "Login failed or no token received"
    TOKEN=""
fi
echo ""

# Test 4: Login with incorrect password
echo "4. Testing Login with Incorrect Password..."
WRONG_PASS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL_PRODUCTS/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"wrongpassword"}')
test_result $([ "$WRONG_PASS" = "401" ] && echo 0 || echo 1) "Login with wrong password rejected"
echo ""

# Test 5: Login with non-existent user (should not reveal user exists)
echo "5. Testing Login with Non-existent User..."
NON_EXISTENT=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL_PRODUCTS/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"nonexistent","password":"anypassword"}')
test_result $([ "$NON_EXISTENT" = "401" ] && echo 0 || echo 1) "Non-existent user returns 401 (doesn't reveal user existence)"
echo ""

# Test 6: Rate limiting (5 attempts should trigger 429)
echo "6. Testing Rate Limiting..."
echo "   Attempting 6 login requests with wrong password..."
RATE_LIMIT_TRIGGERED=false
for i in {1..6}; do
    STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL_PRODUCTS/api/auth/login" \
      -H "Content-Type: application/json" \
      -d "{\"username\":\"ratetest\",\"password\":\"wrong$i\"}")
    if [ "$STATUS" = "429" ]; then
        RATE_LIMIT_TRIGGERED=true
        break
    fi
    sleep 0.5
done
test_result $([ "$RATE_LIMIT_TRIGGERED" = true ] && echo 0 || echo 1) "Rate limiting triggered after multiple failed attempts"
echo ""

# Test 7: Access protected endpoint with valid token
if [ -n "$TOKEN" ]; then
    echo "7. Testing Protected Endpoint Access with Valid Token..."
    PRODUCTS_WITH_TOKEN=$(curl -s -o /dev/null -w "%{http_code}" -X GET "$BASE_URL_PRODUCTS/api/products" \
      -H "Authorization: Bearer $TOKEN")
    test_result $([ "$PRODUCTS_WITH_TOKEN" = "200" ] && echo 0 || echo 1) "Protected endpoint accessible with valid token"
    echo ""
fi

# Test 8: Input validation - invalid product data
if [ -n "$TOKEN" ]; then
    echo "8. Testing Input Validation..."
    INVALID_PRODUCT=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL_PRODUCTS/api/products" \
      -H "Authorization: Bearer $TOKEN" \
      -H "Content-Type: application/json" \
      -d '{"name":"","price":-10,"stock":-5}')
    test_result $([ "$INVALID_PRODUCT" = "400" ] && echo 0 || echo 1) "Invalid input rejected (empty name, negative price/stock)"
    echo ""
fi

# Test 9: Input validation - invalid order data
if [ -n "$TOKEN" ]; then
    INVALID_ORDER=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL_ORDERS/api/orders" \
      -H "Authorization: Bearer $TOKEN" \
      -H "Content-Type: application/json" \
      -d '{"customerName":"","productId":0,"quantity":-1}')
    test_result $([ "$INVALID_ORDER" = "400" ] && echo 0 || echo 1) "Invalid order data rejected"
    echo ""
fi

# Test 10: Login with empty credentials
echo "10. Testing Input Validation on Login..."
EMPTY_CREDS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL_PRODUCTS/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"","password":""}')
test_result $([ "$EMPTY_CREDS" = "400" ] && echo 0 || echo 1) "Empty credentials rejected"
echo ""

# Test 11: Access with invalid token
echo "11. Testing Invalid Token Rejection..."
INVALID_TOKEN=$(curl -s -o /dev/null -w "%{http_code}" -X GET "$BASE_URL_PRODUCTS/api/products" \
  -H "Authorization: Bearer invalid_token_here")
test_result $([ "$INVALID_TOKEN" = "401" ] && echo 0 || echo 1) "Invalid token rejected"
echo ""

# Summary
echo "================================"
echo "Test Summary:"
echo -e "${GREEN}Passed: $PASSED${NC}"
echo -e "${RED}Failed: $FAILED${NC}"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}🎉 All security tests passed!${NC}"
    exit 0
else
    echo -e "${RED}⚠️  Some tests failed. Please review the results above.${NC}"
    exit 1
fi

