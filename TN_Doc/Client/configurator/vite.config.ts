import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { resolve } from 'node:path';
import { createBaseConfig } from '../vite.config.base';

export default defineConfig({
  ...createBaseConfig(__dirname),
  plugins: [vue()],
  base: '/configurator/',
  build: {
    outDir: resolve(__dirname, '../../wwwroot/configurator'),
    emptyOutDir: true,
    sourcemap: process.env.NODE_ENV !== 'production',
    manifest: true,  // Генерирует .vite/manifest.json для Razor view
    rollupOptions: {
      input: resolve(__dirname, 'src/main.ts'),
      output: {
        // Hash в имени entry file для совпадения URL при lazy loading
        entryFileNames: 'configurator.[hash].js',
        chunkFileNames: 'configurator.[hash].js',
        assetFileNames: (assetInfo) => {
          if (assetInfo.name === 'style.css') return 'configurator.[hash].css';
          return 'configurator.[hash].[ext]';
        }
      }
    }
  },
  server: {
    port: 5174,
    strictPort: true
  }
});
