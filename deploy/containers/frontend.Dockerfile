FROM node:22.23.3-alpine3.24@sha256:0a7108bf6c7bf5de370ffb1a3ed6be93d405b43ff159f681a8d18c0e2bc2e402 AS build
WORKDIR /src/src/WildBunch.Web
COPY src/WildBunch.Web/package.json src/WildBunch.Web/package-lock.json ./
RUN npm ci
COPY src/WildBunch.Web/ ./
COPY src/WildBunch.Assets/production/ /src/src/WildBunch.Assets/production/
ARG VITE_API_BASE_URL=
ARG WILD_BUNCH_RELEASE
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
RUN npm run build \
    && printf '%s\n' "$WILD_BUNCH_RELEASE" > dist/release.txt

FROM nginxinc/nginx-unprivileged:1-alpine3.24@sha256:b9241c6e7b8e9a862f129d8d4199ab64b10390949a78bdd5603379b32c844083 AS runtime
COPY deploy/containers/nginx.conf /etc/nginx/templates/default.conf.template
COPY --from=build --chown=nginx:nginx /src/src/WildBunch.Web/dist/ /usr/share/nginx/html/
ENV PORT=8080 \
    WILD_BUNCH_RELEASE=unknown
EXPOSE 8080
