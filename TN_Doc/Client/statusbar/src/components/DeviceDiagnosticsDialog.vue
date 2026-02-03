<template>
  <Teleport to="body">
    <Transition name="dialog">
      <div
        v-if="store.selectedDevice"
        class="diagnostics-overlay"
        @click.self="store.closeDeviceDiagnostics"
      >
        <div class="diagnostics-dialog" @click.stop>
          <!-- Header -->
          <header class="diagnostics-header">
            <div class="diagnostics-header__title-block">
              <div class="diagnostics-header__icon" :class="statusClass">
                <i :class="statusIcon"></i>
              </div>
              <div class="diagnostics-header__text">
                <h2 class="diagnostics-header__title">{{ store.selectedDevice.name }}</h2>
                <span class="diagnostics-header__subtitle">Диагностика</span>
              </div>
            </div>
            <button
              class="diagnostics-header__close"
              @click="store.closeDeviceDiagnostics"
              aria-label="Закрыть"
            >
              <i class="pi pi-times"></i>
            </button>
          </header>

          <!-- Status Summary -->
          <section class="diagnostics-status">
            <div class="diagnostics-status__indicator" :class="statusClass">
              <span class="diagnostics-status__dot"></span>
              <span class="diagnostics-status__text">{{ statusText }}</span>
            </div>
            <div class="diagnostics-status__meta">
              <span v-if="store.selectedDevice.latencyMs" class="diagnostics-status__latency">
                <i class="pi pi-clock"></i>
                {{ store.selectedDevice.latencyMs }}ms
              </span>
              <span v-if="store.selectedDevice.lastChecked" class="diagnostics-status__time">
                <i class="pi pi-calendar"></i>
                {{ formatTime(store.selectedDevice.lastChecked) }}
              </span>
            </div>
          </section>

          <!-- Circuit Breaker Alert -->
          <section
            v-if="hasCircuitBreakerIssue"
            class="diagnostics-alert"
            :class="alertClass"
          >
            <div class="diagnostics-alert__header">
              <i class="pi pi-shield diagnostics-alert__icon"></i>
              <span class="diagnostics-alert__title">Circuit Breaker</span>
              <span class="diagnostics-alert__badge">{{ circuitBreakerBadge }}</span>
            </div>
            <div class="diagnostics-alert__body">
              <p class="diagnostics-alert__message">{{ circuitBreakerMessage }}</p>
              <div class="diagnostics-alert__details">
                <span v-if="circuitBreaker?.failureCount">
                  <strong>Попыток:</strong> {{ circuitBreaker.failureCount }}
                </span>
                <span v-if="circuitBreaker?.currentBackoffSeconds">
                  <strong>Backoff:</strong> {{ circuitBreaker.currentBackoffSeconds }}с
                </span>
                <span v-if="circuitBreaker?.secondsUntilNextAttempt">
                  <strong>Следующая попытка:</strong> {{ circuitBreaker.secondsUntilNextAttempt }}с
                </span>
              </div>
            </div>
          </section>

          <!-- Channels List -->
          <section class="diagnostics-channels">
            <h3 class="diagnostics-channels__title">
              <i class="pi pi-sitemap"></i>
              Каналы связи
              <span class="diagnostics-channels__count">
                {{ connectedChannelsCount }}/{{ totalChannelsCount }}
              </span>
            </h3>
            <div class="diagnostics-channels__grid">
              <div
                v-for="(channel, index) in store.selectedDevice.channels"
                :key="index"
                class="channel-card"
                :class="{ 'channel-card--connected': channel.isConnected }"
              >
                <div class="channel-card__header">
                  <span class="channel-card__indicator" :class="channel.isConnected ? 'channel-card__indicator--ok' : 'channel-card__indicator--error'">
                    <i :class="channel.isConnected ? 'pi pi-check' : 'pi pi-times'"></i>
                  </span>
                  <span class="channel-card__name">{{ channel.name }}</span>
                </div>
                <div class="channel-card__details">
                  <span v-if="channel.isConnected && channel.latencyMs" class="channel-card__latency">
                    {{ channel.latencyMs }}ms
                  </span>
                  <span v-if="!channel.isConnected && channel.error" class="channel-card__error">
                    {{ truncateError(channel.error) }}
                  </span>
                </div>
              </div>
            </div>
          </section>

          <!-- Actions -->
          <footer class="diagnostics-actions">
            <button
              class="retry-button"
              :disabled="isRetrying"
              @click="handleRetry"
            >
              <i v-if="!isRetrying" class="pi pi-refresh"></i>
              <i v-else class="pi pi-spin pi-spinner"></i>
              <span>{{ isRetrying ? 'Проверка...' : 'Проверить' }}</span>
            </button>
          </footer>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useStatusStore } from '../stores/statusStore';

const store = useStatusStore();
const isRetrying = ref(false);

const circuitBreaker = computed(() => store.selectedDevice?.circuitBreaker);

const hasCircuitBreakerIssue = computed(() => {
  const cb = circuitBreaker.value;
  if (!cb) return false;
  return cb.isBlocked || cb.state !== 'Closed' || cb.failureCount > 0;
});

const statusClass = computed(() => {
  const device = store.selectedDevice;
  if (!device) return '';

  if (device.isFullyConnected) return 'status--ok';
  if (device.isConnected) return 'status--warning';
  return 'status--error';
});

const statusIcon = computed(() => {
  const device = store.selectedDevice;
  if (!device) return 'pi pi-question';

  if (device.isFullyConnected) return 'pi pi-check-circle';
  if (device.isConnected) return 'pi pi-exclamation-triangle';
  return 'pi pi-times-circle';
});

const statusText = computed(() => {
  const device = store.selectedDevice;
  if (!device) return '';

  if (device.isFullyConnected) return 'Все каналы работают';
  if (device.isConnected) return 'Частичное подключение';
  return 'Нет подключения';
});

const alertClass = computed(() => {
  const cb = circuitBreaker.value;
  if (!cb) return '';

  if (cb.errorCategory === 'Authentication') return 'alert--auth';
  if (cb.isBlocked || cb.maxRetryReached) return 'alert--blocked';
  return 'alert--warning';
});

const circuitBreakerBadge = computed(() => {
  const cb = circuitBreaker.value;
  if (!cb) return '';

  if (cb.errorCategory === 'Authentication') return 'AUTH';
  if (cb.maxRetryReached) return 'MAX RETRY';
  return cb.state;
});

const circuitBreakerMessage = computed(() => {
  const cb = circuitBreaker.value;
  if (!cb) return '';

  if (cb.errorCategory === 'Authentication') {
    return 'Ошибка аутентификации. Проверьте учётные данные в конфигурации.';
  }
  if (cb.maxRetryReached) {
    return 'Превышено максимальное количество попыток подключения.';
  }
  if (cb.isBlocked) {
    return 'Устройство заблокировано. Требуется ручная проверка.';
  }
  if (cb.lastError) {
    return cb.lastError;
  }
  return 'Обнаружены проблемы с подключением.';
});

const connectedChannelsCount = computed(() => {
  return store.selectedDevice?.channels?.filter(c => c.isConnected).length ?? 0;
});

const totalChannelsCount = computed(() => {
  return store.selectedDevice?.channels?.length ?? 0;
});

function formatTime(date: Date | string | undefined): string {
  if (!date) return '';
  const d = typeof date === 'string' ? new Date(date) : date;
  return d.toLocaleTimeString('ru-RU', {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  });
}

function truncateError(error: string, maxLength = 50): string {
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
// Design tokens
$glass-bg: rgba(255, 255, 255, 0.12);
$glass-border: rgba(255, 255, 255, 0.18);
$glass-blur: 16px;
$shadow-heavy: 0 25px 50px -12px rgba(0, 0, 0, 0.4);

$color-ok: rgba(34, 197, 94, 0.9);
$color-ok-bg: rgba(34, 197, 94, 0.15);
$color-warning: rgba(234, 179, 8, 0.9);
$color-warning-bg: rgba(234, 179, 8, 0.15);
$color-error: rgba(239, 68, 68, 0.9);
$color-error-bg: rgba(239, 68, 68, 0.15);
$color-auth: rgba(168, 85, 247, 0.9);
$color-auth-bg: rgba(168, 85, 247, 0.15);

$text-primary: rgba(255, 255, 255, 0.95);
$text-secondary: rgba(255, 255, 255, 0.7);
$text-muted: rgba(255, 255, 255, 0.5);

// Overlay
.diagnostics-overlay {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.75);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  padding: 1rem;
}

// Dialog
.diagnostics-dialog {
  width: 100%;
  max-width: 480px;
  max-height: 90vh;
  overflow-y: auto;
  background: linear-gradient(135deg,
    rgba(30, 41, 59, 0.95) 0%,
    rgba(15, 23, 42, 0.98) 100%
  );
  backdrop-filter: blur($glass-blur);
  -webkit-backdrop-filter: blur($glass-blur);
  border: 1px solid $glass-border;
  border-radius: 20px;
  box-shadow: $shadow-heavy;
  color: $text-primary;

  // Custom scrollbar
  &::-webkit-scrollbar {
    width: 6px;
  }

  &::-webkit-scrollbar-track {
    background: rgba(255, 255, 255, 0.05);
    border-radius: 3px;
  }

  &::-webkit-scrollbar-thumb {
    background: rgba(255, 255, 255, 0.2);
    border-radius: 3px;

    &:hover {
      background: rgba(255, 255, 255, 0.3);
    }
  }
}

// Header
.diagnostics-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.25rem 1.5rem;
  border-bottom: 1px solid $glass-border;
  background: rgba(255, 255, 255, 0.03);

  &__title-block {
    display: flex;
    align-items: center;
    gap: 0.875rem;
  }

  &__icon {
    width: 44px;
    height: 44px;
    border-radius: 12px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.25rem;

    &.status--ok {
      background: $color-ok-bg;
      color: $color-ok;
    }

    &.status--warning {
      background: $color-warning-bg;
      color: $color-warning;
    }

    &.status--error {
      background: $color-error-bg;
      color: $color-error;
    }
  }

  &__text {
    display: flex;
    flex-direction: column;
    gap: 0.125rem;
  }

  &__title {
    font-size: 1.125rem;
    font-weight: 600;
    margin: 0;
    letter-spacing: -0.01em;
  }

  &__subtitle {
    font-size: 0.75rem;
    color: $text-muted;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    font-weight: 500;
  }

  &__close {
    width: 36px;
    height: 36px;
    border-radius: 10px;
    border: 1px solid transparent;
    background: rgba(255, 255, 255, 0.06);
    color: $text-secondary;
    cursor: pointer;
    transition: all 0.2s ease;
    display: flex;
    align-items: center;
    justify-content: center;

    &:hover {
      background: rgba(255, 255, 255, 0.12);
      color: $text-primary;
      border-color: $glass-border;
    }

    &:active {
      transform: scale(0.95);
    }
  }
}

// Status Summary
.diagnostics-status {
  padding: 1rem 1.5rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 0.75rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);

  &__indicator {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.375rem 0.75rem;
    border-radius: 20px;
    font-size: 0.8125rem;
    font-weight: 500;

    &.status--ok {
      background: $color-ok-bg;
      color: $color-ok;
    }

    &.status--warning {
      background: $color-warning-bg;
      color: $color-warning;
    }

    &.status--error {
      background: $color-error-bg;
      color: $color-error;
    }
  }

  &__dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: currentColor;
    animation: pulse 2s ease-in-out infinite;
  }

  &__meta {
    display: flex;
    align-items: center;
    gap: 1rem;
  }

  &__latency,
  &__time {
    display: flex;
    align-items: center;
    gap: 0.375rem;
    font-size: 0.75rem;
    color: $text-muted;

    i {
      font-size: 0.7rem;
    }
  }
}

// Alert Section
.diagnostics-alert {
  margin: 1rem 1.5rem;
  border-radius: 12px;
  overflow: hidden;

  &.alert--auth {
    background: $color-auth-bg;
    border: 1px solid rgba(168, 85, 247, 0.3);
  }

  &.alert--blocked {
    background: $color-error-bg;
    border: 1px solid rgba(239, 68, 68, 0.3);
  }

  &.alert--warning {
    background: $color-warning-bg;
    border: 1px solid rgba(234, 179, 8, 0.3);
  }

  &__header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.75rem 1rem;
    background: rgba(0, 0, 0, 0.15);
  }

  &__icon {
    font-size: 0.875rem;
    opacity: 0.9;
  }

  &__title {
    font-size: 0.75rem;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    flex: 1;
  }

  &__badge {
    font-size: 0.625rem;
    font-weight: 700;
    padding: 0.25rem 0.5rem;
    border-radius: 4px;
    background: rgba(0, 0, 0, 0.25);
    letter-spacing: 0.03em;
  }

  &__body {
    padding: 0.875rem 1rem;
  }

  &__message {
    margin: 0 0 0.625rem;
    font-size: 0.8125rem;
    line-height: 1.5;
    opacity: 0.9;
  }

  &__details {
    display: flex;
    flex-wrap: wrap;
    gap: 0.75rem;
    font-size: 0.75rem;
    opacity: 0.8;

    strong {
      opacity: 0.7;
    }
  }
}

// Channels Section
.diagnostics-channels {
  padding: 1rem 1.5rem 1.25rem;

  &__title {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin: 0 0 0.875rem;
    font-size: 0.8125rem;
    font-weight: 600;
    color: $text-secondary;
    text-transform: uppercase;
    letter-spacing: 0.04em;

    i {
      font-size: 0.875rem;
      opacity: 0.7;
    }
  }

  &__count {
    margin-left: auto;
    font-size: 0.75rem;
    font-weight: 500;
    color: $text-muted;
    background: rgba(255, 255, 255, 0.08);
    padding: 0.25rem 0.5rem;
    border-radius: 4px;
    letter-spacing: 0;
    text-transform: none;
  }

  &__grid {
    display: flex;
    flex-direction: column;
    gap: 0.625rem;
  }
}

// Channel Card
.channel-card {
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 10px;
  padding: 0.75rem 1rem;
  transition: all 0.2s ease;

  &:hover {
    background: rgba(255, 255, 255, 0.06);
    border-color: rgba(255, 255, 255, 0.12);
  }

  &--connected {
    border-left: 3px solid $color-ok;
  }

  &:not(&--connected) {
    border-left: 3px solid $color-error;
  }

  &__header {
    display: flex;
    align-items: center;
    gap: 0.625rem;
  }

  &__indicator {
    width: 22px;
    height: 22px;
    border-radius: 6px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 0.625rem;

    &--ok {
      background: $color-ok-bg;
      color: $color-ok;
    }

    &--error {
      background: $color-error-bg;
      color: $color-error;
    }
  }

  &__name {
    font-size: 0.875rem;
    font-weight: 500;
    flex: 1;
  }

  &__details {
    margin-top: 0.375rem;
    padding-left: calc(22px + 0.625rem);
  }

  &__latency {
    font-size: 0.75rem;
    color: $color-ok;
    font-weight: 500;
  }

  &__error {
    font-size: 0.75rem;
    color: $color-error;
    line-height: 1.4;
  }
}

// Actions Footer
.diagnostics-actions {
  padding: 1rem 1.5rem 1.25rem;
  border-top: 1px solid rgba(255, 255, 255, 0.06);
  background: rgba(0, 0, 0, 0.1);
}

.retry-button {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.875rem 1.5rem;
  background: linear-gradient(135deg,
    rgba(59, 130, 246, 0.85) 0%,
    rgba(37, 99, 235, 0.9) 100%
  );
  border: 1px solid rgba(255, 255, 255, 0.15);
  border-radius: 12px;
  color: white;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
  box-shadow: 0 4px 15px rgba(59, 130, 246, 0.3);

  &:hover:not(:disabled) {
    transform: translateY(-2px);
    box-shadow: 0 8px 25px rgba(59, 130, 246, 0.4);
    background: linear-gradient(135deg,
      rgba(59, 130, 246, 0.95) 0%,
      rgba(37, 99, 235, 1) 100%
    );
  }

  &:active:not(:disabled) {
    transform: translateY(0);
    box-shadow: 0 2px 10px rgba(59, 130, 246, 0.3);
  }

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }

  i {
    font-size: 1rem;
  }
}

// Animations
@keyframes pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.5;
  }
}

// Dialog transition
.dialog-enter-active {
  animation: overlay-in 0.3s ease-out;

  .diagnostics-dialog {
    animation: dialog-in 0.3s ease-out;
  }
}

.dialog-leave-active {
  animation: overlay-out 0.2s ease-in;

  .diagnostics-dialog {
    animation: dialog-out 0.2s ease-in;
  }
}

@keyframes overlay-in {
  from { opacity: 0; }
  to { opacity: 1; }
}

@keyframes overlay-out {
  from { opacity: 1; }
  to { opacity: 0; }
}

@keyframes dialog-in {
  from {
    opacity: 0;
    transform: scale(0.95) translateY(10px);
  }
  to {
    opacity: 1;
    transform: scale(1) translateY(0);
  }
}

@keyframes dialog-out {
  from {
    opacity: 1;
    transform: scale(1) translateY(0);
  }
  to {
    opacity: 0;
    transform: scale(0.98) translateY(5px);
  }
}

// Responsive
@media (max-width: 480px) {
  .diagnostics-dialog {
    max-width: 100%;
    margin: 0.5rem;
    border-radius: 16px;
  }

  .diagnostics-header,
  .diagnostics-status,
  .diagnostics-channels,
  .diagnostics-actions {
    padding-left: 1rem;
    padding-right: 1rem;
  }

  .diagnostics-alert {
    margin-left: 1rem;
    margin-right: 1rem;
  }
}
</style>
