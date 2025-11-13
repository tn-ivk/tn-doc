import { resolve } from 'node:path';
import type { UserConfig } from 'vite';

export function createBaseConfig(dirname: string): UserConfig {
  return {
    resolve: {
      alias: {
        '@': resolve(dirname, 'src'),
        '@shared': resolve(dirname, '../shared/src')
      }
    },
    server: {
      proxy: {
        '/api': {
          target: 'http://localhost:5000',
          changeOrigin: true
        },
        '/statusHub': {
          target: 'http://localhost:5000',
          ws: true,
          changeOrigin: true
        }
      }
    }
  };
}