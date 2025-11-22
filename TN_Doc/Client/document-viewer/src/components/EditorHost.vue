<template>
  <section class="editor">
    <header class="editor__bar">
      <div>
        <p class="editor__subtitle">Редактирование</p>
        <h3 class="editor__title">Document Editor</h3>
      </div>
      <div class="editor__actions">
        <button class="ghost" type="button" @click="emit('close')">
          К просмотру
        </button>
        <button class="primary" type="button" :disabled="!isSaveActive" @click="emit('save')">
          <span v-if="saving">Сохранение…</span>
          <span v-else>{{ saveText }}</span>
        </button>
      </div>
    </header>

    <div class="editor__frame">
      <div v-if="!src" class="placeholder">Документ не выбран</div>
      <iframe
        v-else
        ref="frameRef"
        :src="src"
        class="frame"
        title="Редактор документа"
        @load="emit('ready')"
      ></iframe>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';

const props = defineProps<{
  src: string;
  canSave: boolean;
  saving: boolean;
  saveText: string;
}>();

const emit = defineEmits<{
  (e: 'save'): void;
  (e: 'close'): void;
  (e: 'ready'): void;
  (e: 'canSaveChange', value: boolean): void;
}>();

const frameRef = ref<HTMLIFrameElement | null>(null);
const childWantsSave = ref(true);

const isSaveActive = computed(() => props.canSave && childWantsSave.value && !props.saving && !!props.src);

defineExpose({
  getFrame: () => frameRef.value
});

function handleMessage(event: MessageEvent) {
  if (event.data === 'ButtonSaveOn') {
    childWantsSave.value = true;
    emit('canSaveChange', true);
  } else if (event.data === 'ButtonSaveOff') {
    childWantsSave.value = false;
    emit('canSaveChange', false);
  }
}

onMounted(() => {
  window.addEventListener('message', handleMessage);
});

onBeforeUnmount(() => {
  window.removeEventListener('message', handleMessage);
});
</script>

<style scoped>
.editor {
  background: linear-gradient(175deg, rgba(30, 41, 59, 0.95), rgba(15, 23, 42, 0.95));
  border: 1px solid var(--doc-border);
  border-radius: var(--doc-radius);
  box-shadow: var(--doc-shadow);
  padding: 14px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.editor__bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.editor__subtitle {
  margin: 0;
  color: var(--doc-text-dim);
  font-size: 12px;
}

.editor__title {
  margin: 0;
  color: var(--doc-text);
}

.editor__actions {
  display: flex;
  gap: 10px;
}

.ghost {
  background: transparent;
  border: 1px solid var(--doc-border);
  color: var(--doc-text);
  border-radius: 12px;
  padding: 10px 16px;
  cursor: pointer;
}

.ghost:hover {
  border-color: var(--doc-accent);
}

.primary {
  background: linear-gradient(90deg, var(--doc-accent-strong), #93c5fd);
  border: none;
  color: #0f172a;
  padding: 10px 18px;
  border-radius: 12px;
  font-weight: 700;
  cursor: pointer;
}

.primary:disabled {
  opacity: 0.6;
  cursor: default;
}

.editor__frame {
  border: 1px solid var(--doc-border);
  border-radius: 14px;
  overflow: hidden;
  min-height: 520px;
  background: var(--doc-panel);
  position: relative;
}

.frame {
  width: 100%;
  height: 75vh;
  border: none;
  display: block;
}

.placeholder {
  padding: 32px;
  text-align: center;
  color: var(--doc-text-dim);
}

@media (max-width: 960px) {
  .editor__bar {
    flex-direction: column;
    align-items: flex-start;
  }

  .editor__actions {
    width: 100%;
    justify-content: flex-start;
  }
}
</style>
