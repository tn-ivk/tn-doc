import axios from 'axios';
import { logger } from '@tn-doc/shared';

const opcCacheApi = axios.create({
  baseURL: 'http://localhost:5010/api/OPCClientCache',
  timeout: 7000
});

export async function readOpcCachedTag(
  deviceName: string,
  tagName: string,
  namespaceIndex = 1,
  indexArray = 0
): Promise<any> {
  try {
    const url = `/${encodeURIComponent(deviceName)}/${encodeURIComponent(tagName)}/${namespaceIndex}/${indexArray}`;
    const { data } = await opcCacheApi.get(url);
    return data;
  } catch (error: any) {
    logger.error('[opcService] Ошибка чтения тега', {
      deviceName,
      tagName,
      error: error?.message || error?.toString()
    });
    throw error;
  }
}

export function buildFullTag(prefix: string, tag: string): string {
  return `${prefix}.${tag}`;
}
