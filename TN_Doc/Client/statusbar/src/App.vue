<template>
  <StatusBar />
</template>

<script setup lang="ts">
import StatusBar from './components/StatusBar.vue';
</script>

<style lang="scss">
// ═══════════════════════════════════════════════════════════════
// GLASSMORPHISM TOOLTIP STYLES
// Стили идентичны DeviceDiagnosticsDialog.vue
// ═══════════════════════════════════════════════════════════════

$glass-bg: rgba(225, 228, 230, 0.82);
$glass-border: rgba(0, 0, 0, 0.08);
$glass-blur: 20px;
$glass-shadow:
  0 8px 32px rgba(0, 0, 0, 0.12),
  0 2px 8px rgba(0, 0, 0, 0.08);
$text-primary: rgba(0, 0, 0, 0.87);

.p-tooltip {
  // Glassmorphism effect
  .p-tooltip-text {
    background: $glass-bg !important;
    backdrop-filter: blur($glass-blur) saturate(1.1);
    -webkit-backdrop-filter: blur($glass-blur) saturate(1.1);
    border: 1px solid $glass-border;
    border-radius: 8px;
    box-shadow: $glass-shadow;
    color: $text-primary !important;
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
    font-size: 0.8125rem;
    line-height: 1.45;
    padding: 0.5rem 0.75rem;
    white-space: pre-line;
    width: 220px !important;
  }

  // Стрелка tooltip
  &.p-tooltip-top .p-tooltip-arrow {
    border-top-color: $glass-bg !important;
  }

  &.p-tooltip-bottom .p-tooltip-arrow {
    border-bottom-color: $glass-bg !important;
  }

  &.p-tooltip-left .p-tooltip-arrow {
    border-left-color: $glass-bg !important;
  }

  &.p-tooltip-right .p-tooltip-arrow {
    border-right-color: $glass-bg !important;
  }
}
</style>