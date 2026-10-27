<script setup lang="ts">
// Bottom sheet on phones, centered dialog on larger screens. Based on <dialog> for focus handling and Escape.
import { onMounted, ref, watch } from 'vue'
import AppIcon from './AppIcon.vue'

const props = defineProps<{ open: boolean; title?: string }>()
const emit = defineEmits<{ close: [] }>()
const dialog = ref<HTMLDialogElement>()

function sync() {
  if (!dialog.value) return
  if (props.open && !dialog.value.open) dialog.value.showModal()
  if (!props.open && dialog.value.open) dialog.value.close()
}

onMounted(sync)
watch(() => props.open, sync)

function onClick(event: MouseEvent) {
  if (event.target === dialog.value) emit('close')
}
</script>

<template>
  <dialog ref="dialog" class="sheet" @close="emit('close')" @click="onClick">
    <div class="sheet-body">
      <header class="sheet-header">
        <h2>{{ title }}</h2>
        <button class="icon-btn" type="button" :aria-label="$t('common.close')" @click="emit('close')">
          <AppIcon name="close" />
        </button>
      </header>
      <slot />
    </div>
  </dialog>
</template>

<style scoped>
.sheet {
  width: 100%;
  max-width: 100%;
  margin: auto 0 0;
  padding: 0;
  border: 0;
  border-radius: 20px 20px 0 0;
  background: var(--surface);
  color: var(--text);
  max-height: 85dvh;
}

.sheet::backdrop {
  background: rgba(0, 0, 0, 0.55);
}

.sheet-body {
  display: grid;
  gap: var(--gutter);
  padding: var(--gutter) var(--gutter) calc(var(--gutter) + env(safe-area-inset-bottom));
}

.sheet-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.sheet-header h2 {
  overflow: hidden;
  text-overflow: ellipsis;
}

@media (min-width: 768px) {
  .sheet {
    width: 480px;
    margin: auto;
    border-radius: 20px;
  }
}
</style>
