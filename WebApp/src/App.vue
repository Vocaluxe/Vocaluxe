<script setup lang="ts">
import { toasts } from '@/stores/toast'
</script>

<template>
  <RouterView />
  <div class="toasts" aria-live="polite">
    <TransitionGroup name="toast">
      <div v-for="t in toasts" :key="t.id" class="toast" :class="`toast--${t.kind}`">{{ t.text }}</div>
    </TransitionGroup>
  </div>
</template>

<style scoped>
.toasts {
  position: fixed;
  left: 50%;
  bottom: calc(var(--tabbar-height) + var(--nowbar-height) + 16px + env(safe-area-inset-bottom));
  transform: translateX(-50%);
  display: grid;
  gap: 8px;
  width: min(92vw, 420px);
  z-index: 50;
  pointer-events: none;
}

@media (min-width: 768px) {
  .toasts {
    bottom: calc(var(--nowbar-height) + 24px);
  }
}

@media (min-width: 1200px) {
  .toasts {
    bottom: 24px;
  }
}

.toast {
  padding: 12px 16px;
  border-radius: var(--radius);
  background: var(--surface-raised);
  color: var(--text);
  box-shadow: var(--shadow);
  text-align: center;
}

.toast--error {
  background: var(--error);
  color: #fff;
}

.toast-enter-from,
.toast-leave-to {
  opacity: 0;
  transform: translateY(8px);
}

.toast-enter-active,
.toast-leave-active {
  transition: all 0.2s ease;
}
</style>
