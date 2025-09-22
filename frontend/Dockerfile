# Step 1: Build React app
FROM node:20-alpine AS build

RUN apk update && apk upgrade

WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .

ARG VITE_REACT_API_URL
ENV VITE_REACT_API_URL=$VITE_REACT_API_URL
ARG VITE_REACT_GOOGLE_CLIENT
ENV VITE_REACT_GOOGLE_CLIENT=$VITE_REACT_GOOGLE_CLIENT

RUN npm run build

# Step 2: Serve with nginx
FROM nginx:stable-alpine

COPY --from=build /app/dist /usr/share/nginx/html
COPY ./nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]