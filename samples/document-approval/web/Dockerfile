FROM node:22.22.3-alpine AS build
WORKDIR /source
RUN corepack enable && corepack prepare pnpm@11.3.0 --activate
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml ./
COPY src/Fotbiler.RuleGate.Angular/package.json src/Fotbiler.RuleGate.Angular/package.json
COPY samples/document-approval/web/package.json samples/document-approval/web/package.json
RUN pnpm install --frozen-lockfile --filter rulegate-document-approval-web...
COPY samples/document-approval/web samples/document-approval/web
RUN pnpm --filter rulegate-document-approval-web build

FROM nginx:1.29-alpine
COPY samples/document-approval/web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /source/samples/document-approval/web/dist/rulegate-document-approval-web/browser /usr/share/nginx/html
EXPOSE 80
