#!/bin/bash
# Test script for microservices

echo "=== Testing Microservices ==="
echo ""

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if services are running
echo "Checking if services are running..."
if ! docker ps | grep -q "micro-service-products"; then
    echo -e "${RED}Services not running. Starting them...${NC}"
    docker compose up -d
    echo "Waiting for services to be healthy..."
    sleep 10
fi

echo -e "${GREEN}Services are running!${NC}"
echo ""

# Test 1: Products Service Health
echo "1. Testing Products Service Health..."
RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" http://localhost:8082/health)
HTTP_CODE=$(echo "$RESPONSE" | grep "HTTP_CODE" | cut -d: -f2)
BODY=$(echo "$RESPONSE" | grep -v "HTTP_CODE")
if [ "$HTTP_CODE" == "200" ]; then
    echo -e "${GREEN}✓ Products Service is healthy${NC}"
    echo "$BODY" | jq .
else
    echo -e "${RED}✗ Products Service health check failed${NC}"
fi
echo ""

# Test 2: Orders Service Health
echo "2. Testing Orders Service Health..."
RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" http://localhost:8083/health)
HTTP_CODE=$(echo "$RESPONSE" | grep "HTTP_CODE" | cut -d: -f2)
BODY=$(echo "$RESPONSE" | grep -v "HTTP_CODE")
if [ "$HTTP_CODE" == "200" ]; then
    echo -e "${GREEN}✓ Orders Service is healthy${NC}"
    echo "$BODY" | jq .
else
    echo -e "${RED}✗ Orders Service health check failed${NC}"
fi
echo ""

# Test 3: Get Products
echo "3. Testing Get Products..."
PRODUCTS=$(curl -s http://localhost:8082/api/products)
if echo "$PRODUCTS" | jq empty 2>/dev/null; then
    PRODUCT_COUNT=$(echo "$PRODUCTS" | jq '. | length')
    echo -e "${GREEN}✓ Retrieved $PRODUCT_COUNT products${NC}"
    echo "$PRODUCTS" | jq . | head -10
else
    echo -e "${RED}✗ Failed to retrieve products${NC}"
fi
echo ""

# Test 4: Get Orders
echo "4. Testing Get Orders..."
ORDERS=$(curl -s http://localhost:8083/api/orders)
if echo "$ORDERS" | jq empty 2>/dev/null; then
    ORDER_COUNT=$(echo "$ORDERS" | jq '. | length')
    echo -e "${GREEN}✓ Retrieved $ORDER_COUNT orders${NC}"
    echo "$ORDERS" | jq . | head -10
else
    echo -e "${RED}✗ Failed to retrieve orders${NC}"
fi
echo ""

# Test 5: Create Order (Inter-service communication test)
echo "5. Testing Create Order (Inter-service communication)..."
NEW_ORDER=$(curl -s -X POST http://localhost:8083/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "Test User",
    "productId": 1,
    "quantity": 2
  }')
if echo "$NEW_ORDER" | jq empty 2>/dev/null && echo "$NEW_ORDER" | jq -e '.id' > /dev/null 2>&1; then
    ORDER_ID=$(echo "$NEW_ORDER" | jq -r '.id')
    echo -e "${GREEN}✓ Order created successfully (ID: $ORDER_ID)${NC}"
    echo "$NEW_ORDER" | jq .
else
    echo -e "${RED}✗ Failed to create order${NC}"
    echo "$NEW_ORDER"
fi
echo ""

# Test 6: Verify Order was created
echo "6. Verifying order count increased..."
NEW_ORDER_COUNT=$(curl -s http://localhost:8083/api/orders | jq '. | length')
if [ "$NEW_ORDER_COUNT" -gt "$ORDER_COUNT" ]; then
    echo -e "${GREEN}✓ Order count increased from $ORDER_COUNT to $NEW_ORDER_COUNT${NC}"
else
    echo -e "${YELLOW}⚠ Order count unchanged${NC}"
fi
echo ""

# Test 7: Test Swagger UI
echo "7. Testing Swagger UI..."
SWAGGER_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8082/swagger/index.html)
if [ "$SWAGGER_RESPONSE" == "200" ]; then
    echo -e "${GREEN}✓ Swagger UI is accessible${NC}"
    echo "   Products: http://localhost:8082/swagger"
    echo "   Orders: http://localhost:8083/swagger"
else
    echo -e "${RED}✗ Swagger UI not accessible${NC}"
fi
echo ""

# Summary
echo "=== Test Summary ==="
echo -e "${GREEN}All tests completed!${NC}"
echo ""
echo "Service URLs:"
echo "  Products API: http://localhost:8082"
echo "  Orders API:   http://localhost:8083"
echo "  Products Swagger: http://localhost:8082/swagger"
echo "  Orders Swagger:   http://localhost:8083/swagger"
echo ""
echo "To stop services: docker compose down"
echo "To view logs: docker compose logs -f"

