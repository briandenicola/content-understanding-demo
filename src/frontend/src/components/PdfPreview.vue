<script setup lang="ts">
defineProps<{
  url: string | null
  fileName: string | null
}>()
</script>

<template>
  <div v-if="url" class="pdf-preview">
    <iframe :src="url" class="pdf-frame" />
  </div>
</template>

<style scoped>
.pdf-preview {
  background: #f8f9fa;
  border-radius: 8px;
  padding: 0.5rem;
}

.pdf-frame {
  width: 100%;
  height: 500px;
  border: 1px solid #eee;
  border-radius: 6px;
}
</style>
