<script lang="ts">
	import { page } from '$app/stores';
	import MapLayout from '$lib/components/MapLayout.svelte';
	import { centerOnRoute, createMarkers, updateRouteLayer } from '$lib/tomtom';
	import { getRoute, type Route, loggedInUserId } from '$lib/store';
	import { onMount, afterUpdate } from 'svelte';
	import { goto } from '$app/navigation';
	import type { Map } from '@tomtom-international/web-sdk-maps';
	import StartRouteButton from '$lib/components/StartRouteButton.svelte';
	import Popup from '$lib/components/ReviewPopup.svelte';
	import StarRating from '$lib/components/StarRating.svelte';
	import Reviews from '$lib/components/Reviews.svelte';
	import { listReviews } from '$lib/api';

	let routeId = Number($page.params.id);
	let route: Route;
	let isReviewPopupOpen = false;
	let isPopupOpen = false;
	let reviews: Reviews[] = [];

	async function loadReviews() {
		const response = await listReviews(routeId);
		if (response.ok) {
			reviews = response.body;
		}
	}

	onMount(async () => {
		await loadReviews();

		let maybeRoute = await getRoute(routeId);
		if (!maybeRoute) {
			await goto('/routes');
		}
		route = maybeRoute as Route;
	});

	afterUpdate(async () => {
		await loadReviews();
	});

	async function mapLoaded(e: CustomEvent) {
		const map: Map = e.detail.map;

		const tt = await import('@tomtom-international/web-sdk-maps');
		map.addControl(new tt.GeolocateControl({ trackUserLocation: true }));

		if (route.points.length >= 2) {
			const routeData = await updateRouteLayer(map, route.points);
			await centerOnRoute(map, routeData);
		}
		await createMarkers(map, route.points);
		await loadReviews();
	}
</script>

<MapLayout on:loaded={mapLoaded}>
	<div slot="right-column" class="flex flex-col gap-4 md:flex-row">
		<a
			class="
			 box-shadow-black-3 rounded-l-lg bg-slate-50 p-3
			 md:rounded-b-lg md:rounded-tl-none
		  "
			href={`/routes/${routeId}/edit`}
			class:hidden={$loggedInUserId !== route?.ownerId}
		>
			<i class="fa-solid fa-pen fa-lg" />
		</a>

		<StartRouteButton {routeId} />

		<button
			class="
		  box-shadow-black-3 rounded-l-lg bg-slate-50 p-3
		  md:rounded-b-lg md:rounded-tl-none
	   "
			on:click={() => (isReviewPopupOpen = true)}
			class:hidden={$loggedInUserId == route?.ownerId}
		>
			<i class="fa-regular fa-star"></i>
		</button>

		<button
			class="
		 box-shadow-black-3 rounded-l-lg bg-slate-50 p-3
		 md:rounded-b-lg md:rounded-tl-none
	  "
			on:click={() => (isPopupOpen = true)}
		>
			<i class="fa-regular fa-comments"></i>
		</button>
	</div>
</MapLayout>

<Popup bind:isOpen={isReviewPopupOpen}>
	<center>Įvertinkite maršrutą</center>
	<br />
	<StarRating {routeId} />
</Popup>

<Popup bind:isOpen={isPopupOpen}>
	<Reviews {reviews} />
</Popup>
