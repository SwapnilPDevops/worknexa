# syntax=docker/dockerfile:1
# Build context: frontend/
#   docker build -t worknexa-web:1.4.0 frontend

FROM node:24-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY . .
# Empty: the browser calls /api on the same origin and nginx proxies it.
ENV VITE_API_URL=""
RUN npm run build

FROM nginxinc/nginx-unprivileged:1.29-alpine AS runtime
COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY nginx/security-headers.conf /etc/nginx/snippets/security-headers.conf
COPY --from=build /app/dist /usr/share/nginx/html

ENV API_UPSTREAM=http://api:8080 \
    TRUSTED_PROXY_CIDR=127.0.0.1/32
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
    CMD wget -q -O /dev/null http://127.0.0.1:8080/healthz || exit 1
