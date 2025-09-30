<template>
  <svg :width="size" :height="size" viewBox="0 0 24 24" :class="{ 'spin': spinning }">
    <path
      fill="currentColor"
      :d="spinning ? spinPath : refreshPath"
    />
  </svg>
</template>

<script setup lang="ts">
interface Props {
  size?: number;
  spinning?: boolean;
}

withDefaults(defineProps<Props>(), {
  size: 16,
  spinning: false
});

const refreshPath = "M12 6V3L8 7l4 4V8c2.76 0 5 2.24 5 5a5 5 0 0 1-9.9 1H5.02A7.002 7.002 0 0 0 19 13c0-3.87-3.13-7-7-7z";
const spinPath = "M12 4V2A10 10 0 1 0 22 12h-2a8 8 0 1 1-8-8z";
</script>

<style scoped>
.spin {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}
</style>


