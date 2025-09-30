<template>
  <svg :width="size" :height="size" viewBox="0 0 24 24">
    <path
      fill="currentColor"
      d="M12 2L2 20h20L12 2zm0 3.5L19.5 18.5h-15L12 5.5zM11 10v4h2v-4h-2zm0 6v2h2v-2h-2z"
    />
  </svg>
</template>

<script setup lang="ts">
interface Props {
  size?: number;
}

withDefaults(defineProps<Props>(), {
  size: 16
});
</script>


