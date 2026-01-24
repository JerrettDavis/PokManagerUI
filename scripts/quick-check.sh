#!/bin/bash
# Quick Health Check for PokManager
# Usage: ./quick-check.sh [host] [web_port] [api_port]
# Example: ./quick-check.sh 10.0.0.216 5207 5374

HOST="${1:-localhost}"
WEB_PORT="${2:-5207}"
API_PORT="${3:-5374}"

echo "🔍 Quick health check for PokManager on ${HOST}..."
echo ""

# Check Web Frontend
if curl -s -f -m 5 "http://${HOST}:${WEB_PORT}" > /dev/null 2>&1; then
    echo "✅ Web Frontend (${WEB_PORT}) - OK"
else
    echo "❌ Web Frontend (${WEB_PORT}) - FAILED"
fi

# Check API Service
if curl -s -f -m 5 "http://${HOST}:${API_PORT}/health" > /dev/null 2>&1; then
    echo "✅ API Service (${API_PORT}) - OK"
elif curl -s -f -m 5 "http://${HOST}:${API_PORT}" > /dev/null 2>&1; then
    echo "⚠️  API Service (${API_PORT}) - UP (health endpoint not configured)"
else
    echo "❌ API Service (${API_PORT}) - FAILED"
fi

echo ""
echo "Done! Run './scripts/verify-deployment.sh ${HOST} ${WEB_PORT} ${API_PORT}' for detailed checks."
