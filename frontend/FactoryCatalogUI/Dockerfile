FROM node:20-alpine AS build
WORKDIR /src
COPY package*.json ./
RUN npm ci --legacy-peer-deps
COPY . .
RUN npm run build


FROM nginx:alpine
WORKDIR /public
EXPOSE 8080
COPY --from=build /src/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist .
COPY --from=build /src/docker-entrypoint.sh /docker-entrypoint.sh

ENTRYPOINT ["sh", "/docker-entrypoint.sh"]
