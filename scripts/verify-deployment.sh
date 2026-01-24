#!/bin/bash
# Deployment Verification Script for PokManager
# Usage: ./verify-deployment.sh [host] [port]
# Example: ./verify-deployment.sh 10.0.0.216 8080

set -e

# Configuration
HOST="${1:-localhost}"
WEB_PORT="${2:-5207}"
API_PORT="${3:-5374}"
TIMEOUT=10

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Counters
PASSED=0
FAILED=0
WARNINGS=0

print_header() {
    echo -e "\n${BLUE}================================================${NC}"
    echo -e "${BLUE}  PokManager Deployment Verification${NC}"
    echo -e "${BLUE}================================================${NC}"
    echo -e "Host: ${HOST}"
    echo -e "Web Port: ${WEB_PORT}"
    echo -e "API Port: ${API_PORT}"
    echo -e "${BLUE}================================================${NC}\n"
}

print_test() {
    echo -e "${YELLOW}Testing:${NC} $1"
}

print_success() {
    echo -e "${GREEN}✓ PASS:${NC} $1"
    ((PASSED++))
}

print_failure() {
    echo -e "${RED}✗ FAIL:${NC} $1"
    ((FAILED++))
}

print_warning() {
    echo -e "${YELLOW}⚠ WARN:${NC} $1"
    ((WARNINGS++))
}

print_summary() {
    echo -e "\n${BLUE}================================================${NC}"
    echo -e "${BLUE}  Test Summary${NC}"
    echo -e "${BLUE}================================================${NC}"
    echo -e "${GREEN}Passed:${NC} ${PASSED}"
    echo -e "${RED}Failed:${NC} ${FAILED}"
    echo -e "${YELLOW}Warnings:${NC} ${WARNINGS}"
    echo -e "${BLUE}================================================${NC}\n"
    
    if [ $FAILED -eq 0 ]; then
        echo -e "${GREEN}🎉 All critical tests passed! Deployment appears healthy.${NC}\n"
        return 0
    else
        echo -e "${RED}❌ Some tests failed. Please investigate before proceeding.${NC}\n"
        return 1
    fi
}

# Test 1: Web Frontend Accessibility
test_web_frontend() {
    print_test "Web Frontend (http://${HOST}:${WEB_PORT})"
    
    if response=$(curl -s -o /dev/null -w "%{http_code}" --connect-timeout $TIMEOUT "http://${HOST}:${WEB_PORT}" 2>&1); then
        if [ "$response" = "200" ]; then
            print_success "Web frontend is accessible (HTTP 200)"
        else
            print_warning "Web frontend returned HTTP ${response}"
        fi
    else
        print_failure "Cannot connect to web frontend"
    fi
}

# Test 2: API Service Accessibility
test_api_service() {
    print_test "API Service (http://${HOST}:${API_PORT})"
    
    if response=$(curl -s -o /dev/null -w "%{http_code}" --connect-timeout $TIMEOUT "http://${HOST}:${API_PORT}/health" 2>&1); then
        if [ "$response" = "200" ]; then
            print_success "API service health check passed"
        elif [ "$response" = "404" ]; then
            print_warning "API service is up but health endpoint not found (check if health checks are enabled in production)"
        else
            print_failure "API service health check failed (HTTP ${response})"
        fi
    else
        print_failure "Cannot connect to API service"
    fi
}

# Test 3: Web Frontend Health Check
test_web_health() {
    print_test "Web Frontend Health Check"
    
    if response=$(curl -s -o /dev/null -w "%{http_code}" --connect-timeout $TIMEOUT "http://${HOST}:${WEB_PORT}/health" 2>&1); then
        if [ "$response" = "200" ]; then
            print_success "Web frontend health check passed"
        elif [ "$response" = "404" ]; then
            print_warning "Web frontend is up but health endpoint not found (check if health checks are enabled in production)"
        else
            print_failure "Web frontend health check failed (HTTP ${response})"
        fi
    else
        print_failure "Cannot reach web frontend health endpoint"
    fi
}

# Test 4: API Instances Endpoint
test_api_instances() {
    print_test "API Instances Endpoint"
    
    if response=$(curl -s --connect-timeout $TIMEOUT "http://${HOST}:${API_PORT}/api/instances" 2>&1); then
        if echo "$response" | grep -q "value\|instances\|data" 2>/dev/null || [ -n "$response" ]; then
            print_success "API instances endpoint is responding"
        else
            print_warning "API instances endpoint returned unexpected response"
        fi
    else
        print_failure "Cannot reach API instances endpoint"
    fi
}

# Test 5: Web Pages Load
test_web_pages() {
    print_test "Web Pages (Dashboard, Instances, Backups)"
    
    pages_ok=true
    
    for page in "/" "/instances" "/backups" "/configuration"; do
        if response=$(curl -s -o /dev/null -w "%{http_code}" --connect-timeout $TIMEOUT "http://${HOST}:${WEB_PORT}${page}" 2>&1); then
            if [ "$response" != "200" ]; then
                pages_ok=false
                break
            fi
        else
            pages_ok=false
            break
        fi
    done
    
    if [ "$pages_ok" = true ]; then
        print_success "All web pages are accessible"
    else
        print_failure "Some web pages failed to load"
    fi
}

# Test 6: Static Assets
test_static_assets() {
    print_test "Static Assets (CSS, JS)"
    
    if response=$(curl -s -o /dev/null -w "%{http_code}" --connect-timeout $TIMEOUT "http://${HOST}:${WEB_PORT}/_framework/blazor.web.js" 2>&1); then
        if [ "$response" = "200" ]; then
            print_success "Blazor framework assets are loading"
        else
            print_warning "Blazor framework assets may not be loading correctly"
        fi
    else
        print_failure "Cannot load static assets"
    fi
}

# Test 7: Response Time
test_response_time() {
    print_test "Response Time Check"
    
    if response_time=$(curl -s -o /dev/null -w "%{time_total}" --connect-timeout $TIMEOUT "http://${HOST}:${WEB_PORT}" 2>&1); then
        # Convert to milliseconds (multiply by 1000)
        response_ms=$(echo "$response_time * 1000" | bc 2>/dev/null || echo "0")
        
        if [ $(echo "$response_time < 2.0" | bc 2>/dev/null || echo "0") -eq 1 ]; then
            print_success "Response time is good (${response_ms}ms)"
        elif [ $(echo "$response_time < 5.0" | bc 2>/dev/null || echo "0") -eq 1 ]; then
            print_warning "Response time is acceptable but slow (${response_ms}ms)"
        else
            print_warning "Response time is very slow (${response_ms}ms)"
        fi
    else
        print_warning "Could not measure response time"
    fi
}

# Test 8: Process Check (if running on same machine)
test_processes() {
    print_test "Process Check"
    
    if [ "$HOST" = "localhost" ] || [ "$HOST" = "127.0.0.1" ]; then
        if pgrep -f "PokManager.Web" > /dev/null && pgrep -f "PokManager.ApiService" > /dev/null; then
            print_success "PokManager processes are running"
        else
            print_failure "PokManager processes not found"
        fi
    else
        print_warning "Skipping process check (remote host)"
    fi
}

# Main execution
main() {
    print_header
    
    test_web_frontend
    test_api_service
    test_web_health
    test_api_instances
    test_web_pages
    test_static_assets
    test_response_time
    test_processes
    
    print_summary
}

# Run tests
main
exit $?
