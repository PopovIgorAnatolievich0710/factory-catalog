import { defineConfig, Plugin } from "vite";
import react from "@vitejs/plugin-react";
import { compression } from "vite-plugin-compression2";

const devEnvReplacements: Record<string, string> = {
  "${FACTORY_CATALOG_BASE_FRONTEND_URL_ENV_VAR}": "http://localhost:8080",
  "${FACTORY_CATALOG_BASE_API_URL_ENV_VAR}": "http://localhost:5010/",
  "${BALANCER_BASE_API_URL_ENV_VAR}": "http://localhost:5011/",
  "${AZURE_CLIENT_ID_ENV_VAR}": "c4e70d41-321e-4278-9271-73f277fd845b",
  "${AZURE_BACKEND_SCOPE_ENV_VAR}": "api://17fd6764-3d5e-40d7-9773-bd106a0dbfab/RnD.Access",
  "${AZURE_TENANT_ID_ENV_VAR}": "26c34b0b-8ce7-4c06-97fb-3d5915066e98",
  "${SERVICES_CONFIG_URL_ENV_VAR}":
    "https://cdn-mnn.s3.eu-west-1.amazonaws.com/Config/BackOfficeSystemsDev.json",
};

function htmlDevEnvPlugin(): Plugin {
  return {
    name: "html-dev-env",
    transformIndexHtml: {
      order: "pre",
      handler(html, ctx) {
        if (!ctx.server) return html;
        return Object.entries(devEnvReplacements).reduce(
          (result, [placeholder, value]) => result.replaceAll(placeholder, value),
          html,
        );
      },
    },
  };
}

export default ({ mode }) => {
  const isProd = mode === "production";

  return defineConfig({
    plugins: [
      react({ include: "**/*.{jsx,tsx}" }),
      htmlDevEnvPlugin(),
      ...(isProd
        ? [
            compression({
              algorithms: ["gzip"],
              exclude: [/\.(br|gz)$/],
            }),
            compression({
              algorithms: ["brotliCompress"],
              exclude: [/\.(br|gz)$/],
            }),
          ]
        : []),
    ],
    server: {
      port: 8080,
      strictPort: true,
      host: true,
      origin: "http://0.0.0.0:8080",
    },
    preview: {
      port: 8080,
      host: true,
      strictPort: true,
    },
    build: {
      outDir: "dist",
      emptyOutDir: true,
    },
    base: "/",
  });
};
