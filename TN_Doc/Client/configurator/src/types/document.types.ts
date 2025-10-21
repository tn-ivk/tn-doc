/**
 * Типы для работы с документами и их конфигурациями
 */

export interface DocumentTreeNode {
  key: string;
  label: string;
  icon?: string;
  type: 'document' | 'template';
  configPath?: string;
  editConfigPath?: string;
  children?: DocumentTreeNode[];
}

export interface DocumentConfig {
  path: string;
  content: string;
}
