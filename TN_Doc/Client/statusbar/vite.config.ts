import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { resolve } from 'node:path';
import { createBaseConfig } from '../vite.config.base';

export default defineConfig({
  ...createBaseConfig(__dirname),
  plugins: [vue()],
  build: {
    outDir: resolve(__dirname, '../../wwwroot/statusbar'),
    emptyOutDir: true,
    sourcemap: process.env.NODE_ENV !== 'production',
    rollupOptions: {
      input: resolve(__dirname, 'src/main.ts'),
      output: {
        entryFileNames: 'status-bar.js',
        chunkFileNames: 'status-bar.[hash].js',
        assetFileNames: (assetInfo) => {
          if (assetInfo.name === 'style.css') return 'status-bar.css';
          return 'status-bar.[ext]';
        }
      }
    }
  },
  server: {
    port: 5173,
    strictPort: true
  }
});