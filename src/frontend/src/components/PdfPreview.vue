<script setup lang="ts">
defineProps<{
  url: string | null
  fileName: string | null
}>()
</script>

<template>
  <div v-if="url" class="pdf-preview">
    <h3>{{ fileName }}</h3>
    <iframe :src="url" class="pdf-frame" />
  </div>
</template>

<style scoped>
.pdf-preview {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
}

.pdf-preview h3 {
  margin-bottom: 1rem;
  color: #0f3460;
  font-size: 0.95rem;
}

.pdf-frame {
  width: 100%;
  height: 600px;
  border: 1px solid #eee;
  border-radius: 8px;
}
</style>
