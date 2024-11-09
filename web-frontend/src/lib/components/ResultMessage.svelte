<script>
	export let successMessage = '';
	export let errorMessage = '';
</script>

{#if successMessage}
	<p class="h-4 text-green-600">{successMessage}</p>
{:else}
	<p class="h-4 text-red-600">{errorMessage}</p>
{/if}
