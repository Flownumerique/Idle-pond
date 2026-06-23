# --- Étape 1 : build de production ---
FROM node:20-alpine AS build

WORKDIR /app

# Installation déterministe à partir du lockfile
COPY package*.json ./
RUN npm ci

# Build statique -> /app/dist
COPY . .
RUN npm run build

# --- Étape 2 : service des fichiers statiques ---
FROM nginx:1.27-alpine AS production

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
