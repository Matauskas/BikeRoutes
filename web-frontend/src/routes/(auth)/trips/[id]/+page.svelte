<script lang="ts">
	import { page } from '$app/stores';
	import MapLayout from '$lib/components/MapLayout.svelte';
	import { centerOnRoute, createMarkers, updateRouteLayer } from '$lib/tomtom';
	import { getRoute, type Route, loggedInUserId, friends, type Id } from '$lib/store';
	import { onDestroy, onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import {
		type Map,
		type Marker,
		LngLat,
		NavigationControl
	} from '@tomtom-international/web-sdk-maps';
	import Modal from '$lib/components/Modal.svelte';
	import FriendRow from './FriendRow.svelte';
	import * as api from '$lib/api';

	let routeId = Number($page.params.id);
	let route: Route;
	let showAddFriendModal = false;
	let tripFriends: Id[] = [];
	let friendMarkers: Marker[] = [];
	let watchId: number | undefined = undefined;
	let updateFriendMarkersId: NodeJS.Timeout | undefined = undefined;
	let map: Map;

	let setCentruoti: boolean = false;

	onMount(async () => {
		let maybeRoute = await getRoute(routeId);
		if (!maybeRoute) {
			await goto('/trips');
		}
		route = maybeRoute as Route;

		const userResponse = await api.getLoggedInUser();

		watchId = navigator.geolocation.watchPosition(
			async (position) => {
				await api.apiFetch('POST', '/api/Users/update', {
					longitude: String(position.coords.longitude),
					latitude: String(position.coords.latitude),
					username: userResponse.body.username
				});
			},
			(err) => {
				console.log(err);
			},
			{
				enableHighAccuracy: true,
				timeout: 10_000
			}
		);

		updateFriendMarkersId = setInterval(async () => {
			for (let i = 0; i < tripFriends.length; i++) {
				const friendLocation = await getFriendLocation(tripFriends[i]);
				if (friendLocation) {
					friendMarkers[i].setLngLat(friendLocation as unknown as LngLat);
				}
			}
		}, 10_000);
	});

	onDestroy(() => {
		if (watchId) {
			navigator.geolocation.clearWatch(watchId);
		}
		if (updateFriendMarkersId) {
			clearInterval(updateFriendMarkersId);
		}
	});

	async function getFriendLocation(friendId: Id) {
		try {
			// Make a fetch request to an API endpoint to retrieve friend's location
			const response = await api.apiFetch('GET', `/api/Users/location?id=${friendId}`);
			if (!response.ok) {
				throw new Error(`Failed to fetch location for friend ${friendId}`);
			}
			// Parse the response JSON to extract friend's location data
			const locationDataLongitude = response.body.longitude;
			const locationDataLatitude = response.body.latitude;
			//console.log(locationData);
			// Assuming the location data contains latitude and longitude properties
			return [locationDataLongitude, locationDataLatitude];
		} catch (error) {
			console.error(error);
			return null; // Return null if there's an error retrieving friend's location
		}
	}

	async function mapLoaded(e: CustomEvent) {
		map = e.detail.map;

		const tt = await import('@tomtom-international/web-sdk-maps');
		const locateControl = new tt.GeolocateControl({ trackUserLocation: true });
		map.addControl(locateControl);

		if (route.points.length >= 2) {
			const routeData = await updateRouteLayer(map, route.points);
			await centerOnRoute(map, routeData);
		}
		locateControl.trigger();
	}

	async function addFriendToTrip(friendId: Id) {
		showAddFriendModal = false;
		if (tripFriends.includes(friendId)) {
			return;
		}

		const friendLocation = await getFriendLocation(friendId);
		if (friendLocation) {
			const tt = await import('@tomtom-international/web-sdk-maps');
			const marker = new tt.Marker();
			marker.setLngLat(friendLocation as unknown as LngLat);
			marker.addTo(map);
			friendMarkers.push(marker);
			tripFriends.push(friendId);
		}
	}
	function centravimas() {
		navigator.geolocation.getCurrentPosition((e) => {
			console.log(e);
			let coordinates = new LngLat(e.coords.longitude, e.coords.latitude);
			map.setCenter(coordinates);
			map.setZoom(11);
		});
		console.log('Veikia centravimas');
	}
</script>

<MapLayout on:loaded={mapLoaded}>
	<button
		slot="right-column"
		class="
			box-shadow-black-3 rounded-l-lg bg-slate-50 p-3
			md:rounded-b-lg md:rounded-tl-none
		"
		on:click={() => {
			showAddFriendModal = true;
			//console.log('Paspausta');
		}}
	>
		<i class="fa-regular fa-user fa-lg" />
	</button>
	<button
		slot="left-column"
		class="
			box-shadow-black-3 rounded-l-lg bg-slate-50 p-3
			md:rounded-b-lg md:rounded-tl-none
		"
		on:click={centravimas}
	>
		Centruoti
	</button>
</MapLayout>

<Modal bind:showModal={showAddFriendModal}>
	<h2 slot="title">Pridėti draugą į kelionę</h2>

	<div class="mt-4 flex flex-col gap-4">
		{#each $friends as friendId}
			<FriendRow {friendId} on:click={() => addFriendToTrip(friendId)} />
		{/each}
	</div>
</Modal>
