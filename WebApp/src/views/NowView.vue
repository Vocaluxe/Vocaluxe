<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import AppIcon from '@/components/AppIcon.vue'
import NowPanel from '@/components/NowPanel.vue'
import { session } from '@/stores/session'

const { t } = useI18n()
</script>

<template>
  <div class="page">
    <header class="page-header">
      <h1>{{ t('now.title') }}</h1>
      <RouterLink v-if="session.can('UseKeyboard')" to="/remote" class="btn">
        <AppIcon name="keyboard" /> {{ t('now.remote') }}
      </RouterLink>
    </header>
    <NowPanel :up-next-count="10" />
  </div>
</template>
