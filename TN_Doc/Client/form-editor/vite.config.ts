import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { resolve } from 'node:path';
import { createBaseConfig } from '../vite.config.base';

export default defineConfig({
  ...createBaseConfig(__dirname),
  plugins: [vue()],
  base: '/form-editor/',
  build: {
    outDir: resolve(__dirname, '../../wwwroot/form-editor'),
    emptyOutDir: true,
    sourcemap: process.env.NODE_ENV !== 'production',
    rollupOptions: {
      input: resolve(__dirname, 'src/main.ts'),
      output: {
        entryFileNames: 'form-editor.js',
        chunkFileNames: 'form-editor.[hash].js',
        assetFileNames: (assetInfo) => {
          if (assetInfo.name === 'style.css') return 'form-editor.css';
          return 'form-editor.[ext]';
        }
      }
    }
  },
  server: {
    port: 5174,
    strictPort: true
  }
});
