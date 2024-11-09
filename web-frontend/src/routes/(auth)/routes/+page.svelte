<script lang="ts">
	import { onMount } from 'svelte';
	import { get } from 'svelte/store';
	import { routes } from '$lib/store';
	import CenterColumnLayout from '$lib/components/CenterColumnLayout.svelte';
	import RouteCard from '$lib/components/RouteCard.svelte';
	import type { Route } from '$lib/store';

	let uniqueRoutes: Route[] = [];

	onMount(() => {
		const allRoutes = get(routes);
		uniqueRoutes = removeDuplicateRoutes(allRoutes);
	});

	function removeDuplicateRoutes(routes: Route[]): Route[] {
		const seen = new Set();
		return routes.filter((route) => {
			const duplicate = seen.has(route.title);
			seen.add(route.title);
			return !duplicate;
		});
	}
</script>

<CenterColumnLayout>
	<div class="mt-4 flex flex-col gap-3">
		{#each uniqueRoutes as route}
			<RouteCard {route} />
		{/each}
	</div>
</CenterColumnLayout>
