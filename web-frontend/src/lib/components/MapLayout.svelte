<script lang="ts">
	import { onMount, createEventDispatcher } from 'svelte';
	import Navbar from '$lib/components/Navbar.svelte';
	import type { Map } from '@tomtom-international/web-sdk-maps';
	import { apiKey } from '$lib/tomtom';
	import { page } from '$app/stores';

	const dispatch = createEventDispatcher();

	let mapContainer: HTMLElement;
	let isMapLoading = true;

	let map: Map;

	onMount(async () => {
		const tt = await import('@tomtom-international/web-sdk-maps');

		map = tt.map({
			key: apiKey,
			container: mapContainer,
			dragPan: true,
			center: [23.922041, 54.906227], // the center (Kaunas)
			zoom: 11 // Adjusted for better visibility of the route
		});

		// TODO: Add back fullscreen support, does not work with z-index divs
		// map.addControl(new tt.FullscreenControl()); // adds full screen icon in the map
		map.addControl(new tt.NavigationControl()); // adds another icon in the map

		map.on('load', () => {
			console.log('Map loaded successfully.');
			isMapLoading = false;
			dispatch('loaded', { map, tt });
		});
	});
</script>

<div class="flex h-screen w-screen flex-col md:relative md:flex-none">
	<div bind:this={mapContainer} class="bg-stripped h-full w-full md:absolute md:left-0 md:top-0">
		{#if isMapLoading}
			<i
				class="fa-regular fa-compass fa-2xl absolute left-1/2 top-1/2 animate-spin text-[#65635f] [animation-duration:_2s]"
			/>
		{/if}
	</div>

	<Navbar
		class="md:absolute md:left-8 md:top-0"
		showBackButton={$page.route.id !== '/(auth)/map'}
	/>

	<div class="z-10 flex flex-row gap-3 md:absolute md:left-96 md:top-0">
		<div class="mb-auto ml-auto flex flex-row gap-4">
			<div class="fixed bottom-28 left-0 md:static">
				<slot name="left-column" />
			</div>
			<div class="fixed bottom-28 right-0 md:static">
				<slot name="right-column" />
			</div>
		</div>
	</div>

	<slot {map} />
</div>
