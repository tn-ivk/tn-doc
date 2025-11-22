<template>
  <section class="panel">
    <header class="panel__header">
      <div>
        <p class="panel__subtitle">Результаты</p>
        <h3 class="panel__title">Найденные документы</h3>
      </div>
      <span class="badge" v-if="rows.length">{{ rows.length }}</span>
    </header>

    <div class="table" :class="{ 'table--empty': !rows.length }">
      <template v-if="loading">
        <div class="placeholder">Загрузка...</div>
      </template>
      <template v-else-if="!rows.length">
        <div class="placeholder">Нет данных за выбранный период</div>
      </template>
      <template v-else>
        <article
          v-for="row in rows"
          :key="row.id"
          class="row"
          :class="{ 'row--active': row.id === selectedId }"
          @click="emit('select', row.id)"
        >
          <div class="row__date">{{ row.dt }}</div>
          <div class="row__description">{{ row.description }}</div>
        </article>
      </template>
    </div>
  </section>
</template>

<script setup lang="ts">
import type { DocumentRow } from '../types';

defineProps<{
  rows: DocumentRow[];
  selectedId: number | null;
  loading: boolean;
}>();

const emit = defineEmits<{
  (e: 'select', id: number): void;
}>();
</script>

<style scoped>
.panel {
  background: linear-gradient(145deg, rgba(17, 24, 39, 0.95), rgba(30, 41, 59, 0.92));
  border-radius: var(--doc-radius);
  border: 1px solid var(--doc-border);
  padding: 14px;
  box-shadow: var(--doc-shadow);
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.panel__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.panel__title {
  margin: 0;
  color: var(--doc-text);
  font-size: 18px;
}

.panel__subtitle {
  margin: 0;
  color: var(--doc-text-dim);
  font-size: 12px;
}

.badge {
  background: rgba(56, 189, 248, 0.18);
  border: 1px solid rgba(56, 189, 248, 0.5);
  padding: 6px 10px;
  border-radius: 999px;
  color: var(--doc-text);
  font-size: 12px;
}

.table {
  border: 1px solid var(--doc-border);
  border-radius: var(--doc-radius);
  background: var(--doc-surface);
  min-height: 180px;
  max-height: 60vh;
  overflow: auto;
}

.row {
  padding: 12px 14px;
  border-bottom: 1px solid var(--doc-border);
  cursor: pointer;
  transition: background var(--doc-transition), border-color var(--doc-transition);
  display: grid;
  grid-template-columns: 160px 1fr;
  gap: 10px;
}

.row:last-child {
  border-bottom: none;
}

.row:hover {
  background: rgba(56, 189, 248, 0.08);
}

.row--active {
  background: rgba(56, 189, 248, 0.12);
  border-color: rgba(56, 189, 248, 0.35);
}

.row__date {
  color: var(--doc-accent);
  font-weight: 600;
}

.row__description {
  color: var(--doc-text);
}

.placeholder {
  padding: 28px;
  text-align: center;
  color: var(--doc-text-dim);
}

@media (max-width: 900px) {
  .row {
    grid-template-columns: 1fr;
  }
}
</style>
