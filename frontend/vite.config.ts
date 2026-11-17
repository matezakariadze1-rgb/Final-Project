import { defineConfig } from 'vite';
export default defineConfig({
  base: '/web/',
  build: { outDir: '../src/SpaceHub.Api/wwwroot/web', emptyOutDir: true },
  server: { port: 5173, strictPort: true, proxy: { '/api': { target: process.env.SPACEHUB_API_URL || 'http://localhost:53425', changeOrigin: true } } }
});
