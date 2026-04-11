FROM node:22-alpine AS build

WORKDIR /app

COPY package.json ./
RUN npm install

COPY index.html vite.config.ts svelte.config.js tsconfig.json tsconfig.app.json tsconfig.node.json ./
COPY scripts ./scripts
COPY public ./public
COPY src ./src

RUN node scripts/gen-notify.mjs
RUN npm run build

FROM nginx:1.27-alpine

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
