<script>
	import * as api from '$lib/api';
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';

	let loading = true;
	onMount(async () => {
		if (api.isLoggedIn()) {
			await goto('/map');
			return;
		}
		loading = false;
	});
</script>

{#if !loading}
	<slot />
{/if}
