import { type Component } from 'vue';

export interface VisualEditorInfo {
  type: 'visual' | 'json';
  component: Component | null;
  label: string;
}

// Реестр визуальных редакторов (расширяемый)
const VISUAL_EDITOR_PATTERNS: Array<{
  pattern: RegExp;
  component: () => Promise<{ default: Component }>;
  label: string;
}> = [
  {
    pattern: /CfgEditPassport.*\.json$/i,
    component: () => import('../components/visual-editors/PassportConfigEditor.vue'),
    label: 'Паспорт качества'
  }
  // Здесь можно добавить другие типы в будущем:
  // { pattern: /CfgEditKMH.*\.json$/i, component: () => import(...), label: 'КМХ' }
];

export function useVisualEditor() {

  function getEditorInfo(configPath: string): VisualEditorInfo {
    const filename = configPath.split(/[/\\]/).pop() || '';

    for (const entry of VISUAL_EDITOR_PATTERNS) {
      if (entry.pattern.test(filename)) {
        return {
          type: 'visual',
          component: null,
          label: entry.label
        };
      }
    }

    return { type: 'json', component: null, label: 'JSON' };
  }

  async function loadVisualEditor(configPath: string): Promise<Component | null> {
    const filename = configPath.split(/[/\\]/).pop() || '';

    for (const entry of VISUAL_EDITOR_PATTERNS) {
      if (entry.pattern.test(filename)) {
        const module = await entry.component();
        return module.default;
      }
    }

    return null;
  }

  function hasVisualEditor(configPath: string): boolean {
    return getEditorInfo(configPath).type === 'visual';
  }

  return { getEditorInfo, loadVisualEditor, hasVisualEditor };
}
