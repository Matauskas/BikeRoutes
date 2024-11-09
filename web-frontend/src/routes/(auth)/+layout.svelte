<script>
	import * as api from '$lib/api';
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { setup } from '$lib/store';

	let loading = true;
	onMount(async () => {
		if (!api.isLoggedIn()) {
			await goto('/login');
			return;
		}
		await setup();
		loading = false;
	});
</script>

{#if !loading}
	<slot />
{/if}
