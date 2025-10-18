import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { resolve } from 'node:path';
import { createBaseConfig } from '../vite.config.base';

export default defineConfig({
  ...createBaseConfig(__dirname),
  plugins: [vue()],
  base: '/document-editor/',
  build: {
    outDir: resolve(__dirname, '../../wwwroot/document-editor'),
    emptyOutDir: true,
    sourcemap: process.env.NODE_ENV !== 'production',
    rollupOptions: {
      input: resolve(__dirname, 'index.html')
    }
  },
  server: {
    port: 5174,
    strictPort: true
  }
});
