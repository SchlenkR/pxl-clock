import { defineConfig } from 'vite';

export default defineConfig({
  // Served from /pxl-clock/ on GitHub Pages (project site).
  base: '/pxl-clock/',
  server: {
    port: 5173,
    host: '127.0.0.1',
  },
  build: {
    outDir: 'dist',
    emptyOutDir: true,
    sourcemap: false,
  },
});
