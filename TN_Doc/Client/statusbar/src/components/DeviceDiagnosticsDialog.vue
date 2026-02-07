<template>
  <Teleport to="body">
    <Transition name="glass-dialog">
      <div
        v-if="store.selectedDevice"
        class="glass-container"
        @click.self="store.closeDeviceDiagnostics"
      >
        <div
          ref="dialogRef"
          class="glass-dialog"
          :style="dialogStyle"
        >
          <!-- Draggable Header -->
          <header
            class="glass-header"
            @mousedown="startDrag"
            @touchstart.passive="startDrag"
          >
            <div class="glass-header__title-row">
              <span class="glass-header__device-name">{{ store.selectedDevice.name }}</span>
              <span class="glass-header__separator">:</span>
              <span class="glass-header__status" :class="headerStatusClass">
                {{ headerStatusText }}
              </span>
            </div>
            <span class="glass-header__channels">
              {{ connectedChannelsCount }}/{{ totalChannelsCount }}
            </span>
          </header>

          <!-- Content Area -->
          <div class="glass-content">
            <!-- Status Badge -->
            <div class="status-badge">
              <span class="status-badge__text">{{ statusText }}</span>
              <div class="status-badge__meta">
                <span v-if="store.selectedDevice.latencyMs" class="status-badge__latency">
                  <i class="pi pi-clock"></i>
                  {{ store.selectedDevice.latencyMs }}ms
                </span>
                <span v-if="store.selectedDevice.lastChecked" class="status-badge__time">
                  {{ formatTime(store.selectedDevice.lastChecked) }}
                </span>
                <span v-if="store.selectedDevice.lastChecked" class="status-badge__date">
                  {{ formatDate(store.selectedDevice.lastChecked) }}
                </span>
              </div>
            </div>

            <!-- Connection Diagnostic Alert -->
            <div
              v-if="hasConnectionDiagnosticIssue"
              class="cb-alert"
              :class="alertClass"
            >
              <div class="cb-alert__header">
                <i class="pi pi-shield"></i>
                <span class="cb-alert__title">Диагностика</span>
                <span class="cb-alert__badge">{{ connectionDiagnosticBadge }}</span>
              </div>
              <p class="cb-alert__message">{{ connectionDiagnosticMessage }}</p>
              <div class="cb-alert__stats">
                <span v-if="deviceConnectionDiagnostic?.failureCount">
                  Попыток: <strong>{{ deviceConnectionDiagnostic.failureCount }}</strong>
                </span>
                <span v-if="deviceConnectionDiagnostic?.currentPollSeconds">
                  Интервал: <strong>{{ deviceConnectionDiagnostic.currentPollSeconds }}с</strong>
                </span>
                <span v-if="deviceConnectionDiagnostic?.secondsUntilNextAttempt">
                  Retry: <strong>{{ deviceConnectionDiagnostic.secondsUntilNextAttempt }}с</strong>
                </span>
              </div>
            </div>

            <!-- Channels Section -->
            <div class="channels-section">
              <h3 class="channels-section__title">
                <i class="pi pi-sitemap"></i>
                Каналы связи
              </h3>
              <div class="channels-list">
                <div
                  v-for="(channel, index) in store.selectedDevice.channels"
                  :key="index"
                  class="channel-item"
                  :class="{ 'channel-item--ok': channel.isConnected }"
                >
                  <span class="channel-item__name">{{ channel.name }}</span>
                  <span v-if="channel.isConnected && channel.latencyMs" class="channel-item__latency">
                    {{ channel.latencyMs }} мс
                  </span>
                  <span v-if="!channel.isConnected && channel.error" class="channel-item__error">
                    {{ truncateError(channel.error) }}
                  </span>
                </div>
              </div>
            </div>
          </div>

          <!-- Footer -->
          <footer class="glass-footer">
            <button
              class="action-btn"
              :disabled="isRetrying"
              @click="handleRetry"
            >
              <i v-if="isRetrying" class="pi pi-spin pi-spinner"></i>
              <span>{{ isRetrying ? 'Проверка...' : 'Проверить' }}</span>
            </button>
          </footer>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, reactive, onMounted, onUnmounted } from 'vue';
import { useStatusStore } from '../stores/statusStore';

const store = useStatusStore();
const isRetrying = ref(false);
const dialogRef = ref<HTMLElement | null>(null);

// Drag state
const dragState = reactive({
  isDragging: false,
  startX: 0,
  startY: 0,
  offsetX: 0,
  offsetY: 0
});

const dialogStyle = computed(() => {
  if (dragState.offsetX === 0 && dragState.offsetY === 0) {
    return {};
  }
  return {
    transform: `translate(${dragState.offsetX}px, ${dragState.offsetY}px)`,
    transition: dragState.isDragging ? 'none' : 'transform 0.1s ease-out'
  };
});

function startDrag(e: MouseEvent | TouchEvent) {
  const clientX = 'touches' in e ? e.touches[0].clientX : e.clientX;
  const clientY = 'touches' in e ? e.touches[0].clientY : e.clientY;

  dragState.isDragging = true;
  dragState.startX = clientX - dragState.offsetX;
  dragState.startY = clientY - dragState.offsetY;

  document.addEventListener('mousemove', onDrag);
  document.addEventListener('mouseup', stopDrag);
  document.addEventListener('touchmove', onDrag);
  document.addEventListener('touchend', stopDrag);
}

function onDrag(e: MouseEvent | TouchEvent) {
  if (!dragState.isDragging) return;

  const clientX = 'touches' in e ? e.touches[0].clientX : e.clientX;
  const clientY = 'touches' in e ? e.touches[0].clientY : e.clientY;

  dragState.offsetX = clientX - dragState.startX;
  dragState.offsetY = clientY - dragState.startY;
}

function stopDrag() {
  dragState.isDragging = false;
  document.removeEventListener('mousemove', onDrag);
  document.removeEventListener('mouseup', stopDrag);
  document.removeEventListener('touchmove', onDrag);
  document.removeEventListener('touchend', stopDrag);
}

// Reset position when dialog opens
onMounted(() => {
  dragState.offsetX = 0;
  dragState.offsetY = 0;
});

onUnmounted(() => {
  document.removeEventListener('mousemove', onDrag);
  document.removeEventListener('mouseup', stopDrag);
  document.removeEventListener('touchmove', onDrag);
  document.removeEventListener('touchend', stopDrag);
});

const deviceConnectionDiagnostic = computed(() => store.selectedDevice?.deviceConnectionDiagnostic);

const hasConnectionDiagnosticIssue = computed(() => {
  const cd = deviceConnectionDiagnostic.value;
  if (!cd) return false;
  return cd.isBlocked || cd.state !== 'Active' || cd.failureCount > 0;
});

const headerStatusClass = computed(() => {
  const device = store.selectedDevice;
  if (!device) return '';

  if (device.isFullyConnected) return 'header-status--ok';
  if (device.isConnected) return 'header-status--warning';
  return 'header-status--error';
});

const headerStatusText = computed(() => {
  const device = store.selectedDevice;
  if (!device) return '';

  if (device.isFullyConnected) return 'ПОДКЛЮЧЕНО';
  if (device.isConnected) return 'ЧАСТИЧНО';
  return 'ОТКЛЮЧЕНО';
});

const statusText = computed(() => {
  const device = store.selectedDevice;
  if (!device) return '';

  if (device.isFullyConnected) return 'Все каналы подключены';
  if (device.isConnected) return 'Частичное подключение';
  return 'Нет подключения';
});

const alertClass = computed(() => {
  const cd = deviceConnectionDiagnostic.value;
  if (!cd) return '';

  if (cd.errorCategory === 'Authentication') return 'cb-alert--auth';
  if (cd.isBlocked || cd.maxRetryReached) return 'cb-alert--blocked';
  return 'cb-alert--warning';
});

const connectionDiagnosticBadge = computed(() => {
  const cd = deviceConnectionDiagnostic.value;
  if (!cd) return '';

  if (cd.errorCategory === 'Authentication') return 'AUTH';
  if (cd.maxRetryReached) return 'MAX RETRY';
  return cd.state;
});

const connectionDiagnosticMessage = computed(() => {
  const cd = deviceConnectionDiagnostic.value;
  if (!cd) return '';

  if (cd.errorCategory === 'Authentication') {
    return 'Ошибка аутентификации. Проверьте учётные данные.';
  }
  if (cd.maxRetryReached) {
    return 'Превышено максимальное количество попыток.';
  }
  if (cd.isBlocked) {
    return 'Устройство заблокировано. Требуется проверка.';
  }
  if (cd.lastError) {
    return cd.lastError;
  }
  return 'Обнаружены проблемы с подключением.';
});

const connectedChannelsCount = computed(() => {
  return store.selectedDevice?.channels?.filter(c => c.isConnected).length ?? 0;
});

const totalChannelsCount = computed(() => {
  return store.selectedDevice?.channels?.length ?? 0;
});

function formatDate(date: Date | string | undefined): string {
  if (!date) return '';
  const d = typeof date === 'string' ? new Date(date) : date;
  const day = d.getDate().toString().padStart(2, '0');
  const month = (d.getMonth() + 1).toString().padStart(2, '0');
  const year = d.getFullYear().toString().slice(-2);
  return `${day}.${month}.${year}`;
}

function formatTime(date: Date | string | undefined): string {
  if (!date) return '';
  const d = typeof date === 'string' ? new Date(date) : date;
  return d.toLocaleTimeString('ru-RU', {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  });
}

function truncateError(error: string, maxLength = 40): string {
  if (error.length <= maxLength) return error;
  return error.slice(0, maxLength) + '...';
}

async function handleRetry() {
  if (!store.selectedDevice || isRetrying.value) return;

  isRetrying.value = true;
  try {
    await store.retryDevice(store.selectedDevice.id);
  } catch (err) {
    console.error('Failed to retry device:', err);
  } finally {
    isRetrying.value = false;
  }
}
</script>

<style lang="scss" scoped>
@use 'sass:color';

// ═══════════════════════════════════════════════════════════════
// GLASSMORPHISM DESIGN TOKENS
// ═══════════════════════════════════════════════════════════════

// Light gray glass (#F1F3F4 based)
$glass-bg: rgba(225, 228, 230, 0.82);
$glass-border: rgba(0, 0, 0, 0.08);
$glass-border-light: rgba(0, 0, 0, 0.05);
$glass-blur: 20px;
$glass-shadow:
  0 8px 32px rgba(0, 0, 0, 0.12),
  0 2px 8px rgba(0, 0, 0, 0.08);

// Status colors
$color-ok: #22c55e;
$color-ok-soft: rgba(34, 197, 94, 0.12);
$color-warning: #eab308;
$color-warning-soft: rgba(234, 179, 8, 0.12);
$color-error: #ef4444;
$color-error-soft: rgba(239, 68, 68, 0.12);
$color-auth: #a855f7;
$color-auth-soft: rgba(168, 85, 247, 0.12);

// Typography (dark text for light background)
$text-primary: rgba(0, 0, 0, 0.87);
$text-secondary: rgba(0, 0, 0, 0.6);
$text-muted: rgba(0, 0, 0, 0.4);

// ═══════════════════════════════════════════════════════════════
// CONTAINER (no darkening overlay)
// ═══════════════════════════════════════════════════════════════

.glass-container {
  position: fixed;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  pointer-events: auto;
  background: transparent;
}

// ═══════════════════════════════════════════════════════════════
// GLASS DIALOG
// ═══════════════════════════════════════════════════════════════

.glass-dialog {
  pointer-events: auto;
  width: 100%;
  max-width: 420px;
  max-height: 85vh;
  margin: 1rem;
  display: flex;
  flex-direction: column;

  // Glassmorphism effect - light gray
  background: $glass-bg;
  backdrop-filter: blur($glass-blur) saturate(1.1);
  -webkit-backdrop-filter: blur($glass-blur) saturate(1.1);

  // Borders and radius
  border: 1px solid $glass-border;
  border-radius: 14px;

  // Shadow for depth
  box-shadow: $glass-shadow;

  // Text
  color: $text-primary;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;

  overflow: hidden;
}

// ═══════════════════════════════════════════════════════════════
// HEADER (Draggable, no background color)
// ═══════════════════════════════════════════════════════════════

.glass-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.875rem 1rem;
  border-bottom: 1px solid $glass-border-light;
  cursor: grab;
  user-select: none;

  &:active {
    cursor: grabbing;
  }

  &__title-row {
    display: flex;
    align-items: center;
    gap: 0.375rem;
    font-size: 0.9375rem;
    font-weight: 500;
    letter-spacing: -0.01em;
  }

  &__device-name {
    color: $text-primary;
    font-weight: 600;
  }

  &__separator {
    color: $text-muted;
  }

  &__status {
    font-weight: 600;
    text-transform: uppercase;
    font-size: 0.8125rem;
    letter-spacing: 0.02em;

    &.header-status--ok {
      color: var(--p-green-700);
    }

    &.header-status--warning {
      color: var(--p-yellow-700);
    }

    &.header-status--error {
      color: var(--p-red-700);
    }
  }

  &__channels {
    font-size: 0.75rem;
    font-weight: 500;
    color: $text-secondary;
  }
}

// ═══════════════════════════════════════════════════════════════
// CONTENT AREA
// ═══════════════════════════════════════════════════════════════

.glass-content {
  flex: 1;
  overflow-y: auto;
  padding: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.875rem;

  // Subtle scrollbar
  &::-webkit-scrollbar {
    width: 5px;
  }

  &::-webkit-scrollbar-track {
    background: transparent;
  }

  &::-webkit-scrollbar-thumb {
    background: rgba(0, 0, 0, 0.12);
    border-radius: 3px;

    &:hover {
      background: rgba(0, 0, 0, 0.2);
    }
  }
}

// ═══════════════════════════════════════════════════════════════
// STATUS BADGE
// ═══════════════════════════════════════════════════════════════

.status-badge {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0;
  font-size: 0.8125rem;
  font-weight: 500;
  color: $text-primary;

  &__text {
    flex: 1;
  }

  &__meta {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    font-size: 0.6875rem;
    color: $text-primary;
  }

  &__latency,
  &__time,
  &__date {
    display: flex;
    align-items: center;
    gap: 0.25rem;

    i {
      font-size: 0.625rem;
    }
  }
}

// ═══════════════════════════════════════════════════════════════
// CONNECTION DIAGNOSTIC ALERT
// ═══════════════════════════════════════════════════════════════

.cb-alert {
  border-radius: 10px;
  overflow: hidden;
  font-size: 0.8125rem;

  &--warning {
    background: $color-warning-soft;
    border: 1px solid rgba($color-warning, 0.25);
    color: color.adjust($color-warning, $lightness: -15%);
  }

  &--blocked {
    background: $color-error-soft;
    border: 1px solid rgba($color-error, 0.25);
    color: color.adjust($color-error, $lightness: -10%);
  }

  &--auth {
    background: $color-auth-soft;
    border: 1px solid rgba($color-auth, 0.25);
    color: color.adjust($color-auth, $lightness: -10%);
  }

  &__header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.5rem 0.75rem;
    background: rgba(0, 0, 0, 0.05);
    font-size: 0.6875rem;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.04em;

    i {
      font-size: 0.75rem;
    }
  }

  &__title {
    flex: 1;
  }

  &__badge {
    padding: 0.125rem 0.375rem;
    background: rgba(0, 0, 0, 0.1);
    border-radius: 3px;
    font-size: 0.5625rem;
    font-weight: 700;
  }

  &__message {
    margin: 0;
    padding: 0.625rem 0.75rem 0.5rem;
    line-height: 1.45;
    opacity: 0.9;
  }

  &__stats {
    display: flex;
    flex-wrap: wrap;
    gap: 0.625rem;
    padding: 0 0.75rem 0.625rem;
    font-size: 0.6875rem;
    opacity: 0.75;

    strong {
      font-weight: 600;
      opacity: 1;
    }
  }
}

// ═══════════════════════════════════════════════════════════════
// CHANNELS SECTION
// ═══════════════════════════════════════════════════════════════

.channels-section {
  &__title {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin: 0 0 0.625rem;
    font-size: 0.875rem;
    font-weight: 600;
    color: $text-primary;
    text-transform: uppercase;
    letter-spacing: 0.04em;

    i {
      font-size: 0.875rem;
    }
  }
}

.channels-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.channel-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  padding: 0.625rem 0.75rem;
  background: transparent;
  border: 1px solid $glass-border-light;
  border-radius: 8px;
  border-left: 3px solid var(--p-red-700);

  &--ok {
    border-left-color: var(--p-green-700);
  }

  &__name {
    font-size: 0.8125rem;
    font-weight: 500;
    color: $text-primary;
  }

  &__latency {
    font-size: 0.75rem;
    color: $text-primary;
    font-weight: 500;
    flex-shrink: 0;
  }

  &__error {
    font-size: 0.6875rem;
    color: var(--p-red-700);
    line-height: 1.35;
    text-align: right;
    flex-shrink: 0;
  }
}

// ═══════════════════════════════════════════════════════════════
// FOOTER (no background color)
// ═══════════════════════════════════════════════════════════════

.glass-footer {
  padding: 0.875rem 1rem;
  border-top: 1px solid $glass-border-light;
}

.action-btn {
  width: 100%;
  height: var(--md-control-height);
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0 1.25rem;

  background: var(--md-primary);
  border: 1px solid color-mix(in srgb, var(--md-primary) 80%, #000);
  border-radius: var(--md-radius);

  color: #fff;
  font-family: var(--md-font-family);
  font-size: var(--md-font-size-button);
  font-weight: var(--md-font-weight-semibold);
  cursor: pointer;
  transition: background 0.15s ease;

  &:hover:not(:disabled) {
    background: var(--md-primary-hover);
  }

  &:active:not(:disabled) {
    background: var(--md-primary-active);
  }

  &:disabled {
    background: var(--md-disabled-bg);
    color: var(--md-disabled-text);
    border-color: var(--md-disabled-border);
    cursor: not-allowed;
  }

  i {
    font-size: 1rem;
  }
}

// ═══════════════════════════════════════════════════════════════
// TRANSITIONS
// ═══════════════════════════════════════════════════════════════

.glass-dialog-enter-active {
  animation: dialog-appear 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}

.glass-dialog-leave-active {
  animation: dialog-disappear 0.2s cubic-bezier(0.4, 0, 1, 1);
}

@keyframes dialog-appear {
  from {
    opacity: 0;
    transform: scale(0.92) translateY(12px);
  }
  to {
    opacity: 1;
    transform: scale(1) translateY(0);
  }
}

@keyframes dialog-disappear {
  from {
    opacity: 1;
    transform: scale(1) translateY(0);
  }
  to {
    opacity: 0;
    transform: scale(0.96) translateY(8px);
  }
}

// ═══════════════════════════════════════════════════════════════
// RESPONSIVE
// ═══════════════════════════════════════════════════════════════

@media (max-width: 480px) {
  .glass-dialog {
    max-width: calc(100% - 1rem);
    margin: 0.5rem;
    border-radius: 12px;
  }

  .glass-header {
    padding: 0.75rem;

    &__title-row {
      font-size: 0.875rem;
    }
  }

  .glass-content {
    padding: 0.875rem;
  }

  .glass-footer {
    padding: 0.75rem;
  }
}
</style>
