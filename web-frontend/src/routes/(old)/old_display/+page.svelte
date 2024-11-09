<script lang="ts">
	//@ts-nocheck
	import { onMount, onDestroy } from 'svelte';
	import { saveRouteData } from '$lib/store';
	import { goto } from '$app/navigation';
	import {
		friends,
		users,
		type User,
		type Id,
		removeFriend as removeFriend,
		getUser,
		refreshFriends
	} from '$lib/store';
	import { writable } from 'svelte/store';
	import Button from '$lib/components/Button.svelte';

	let map, routeOnMap;
	let isLoading = false;
	let center = [23.922041, 54.906227];
	const apiKey = '5zOoRjf2ubXpghwAWI0VqykIEWORa1mo';
	let tt;

	let seconds = 0;
	let timerInterval;
	let routeInterval;
	let showStartButton = true;
	let isPaused = true;
	let isModalOpen = false;
	let isFriendsModalOpen = false;
	let traveledDistance = '';

	let startTime = '';
	let endTime = '';
	let startAddress = '';
	let endAddress = '';
	let currentLocation;
	let allPoints = '';

	$: route = $saveRouteData.route;
	$: end = [
		$saveRouteData.route.points[$saveRouteData.route.points.length - 1].longtitude,
		$saveRouteData.route.points[$saveRouteData.route.points.length - 1].latitude
	];
	$: start = [$saveRouteData.route.points[0].longtitude, $saveRouteData.route.points[0].latitude];
	function formatTimer(seconds) {
		const hrs = Math.floor(seconds / 3600);
		const mins = Math.floor((seconds % 3600) / 60);
		const secs = seconds % 60;
		return [hrs, mins, secs].map((val) => val.toString().padStart(2, '0')).join(':');
	}

	function formatPoints() {
		for (let i = 0; i < route.points.length; i++) {
			if (i != 0) {
				allPoints += ':' + route.points[i].longtitude + ',' + route.points[i].latitude;
			} else {
				allPoints += route.points[i].longtitude + ',' + route.points[i].latitude;
			}
		}
		console.log(allPoints);
	}

	onMount(() => {
		Promise.all([
			import('@tomtom-international/web-sdk-maps'),
			import('@tomtom-international/web-sdk-maps/dist/maps.css')
		])
			.then(([loadedTT]) => {
				console.log('Map and CSS modules loaded successfully.');
				tt = loadedTT;
				map = tt.map({
					key: apiKey,
					container: 'map-container',
					center: center,
					zoom: 7
				});

				map.addControl(new tt.FullscreenControl());
				map.addControl(new tt.NavigationControl());
				const geolocateControl = new tt.GeolocateControl({
					trackUserLocation: true
				});
				map.addControl(geolocateControl);

				map.on('load', () => {
					console.log('Map loaded successfully.');
				});
			})
			.catch((error) => {
				console.error('Error loading map modules:', error);
			});
	});

	function startTimer() {
		navigator.geolocation.getCurrentPosition((position) => {
			currentLocation = [position.coords.longitude, position.coords.latitude];
		}),
			(showStartButton = false);
		isPaused = false;
		startTime = new Date().toLocaleTimeString('en-GB', {
			hour: '2-digit',
			minute: '2-digit',
			second: '2-digit',
			hour12: false
		});

		if (!timerInterval) {
			timerInterval = setInterval(() => {
				seconds += 1;
			}, 1000);

			routeInterval = setInterval(() => {
				calculateRouteWithCurrentLocation();
			}, 5000);
		}
	}

	function pauseResumeTimer() {
		if (isPaused) {
			timerInterval = setInterval(() => {
				seconds += 1;
			}, 1000);
			isPaused = false;
		} else {
			clearInterval(timerInterval);
			timerInterval = null;
			isPaused = true;
		}
	}

	async function finishTimer() {
		clearInterval(timerInterval);
		clearInterval(routeInterval);
		timerInterval = null;
		showStartButton = true;
		endTime = new Date().toLocaleTimeString('en-GB', {
			hour: '2-digit',
			minute: '2-digit',
			second: '2-digit',
			hour12: false
		});
		isPaused = true;
		startAddress = await reverseGeocode(start);
		endAddress = await reverseGeocode(end);
		isModalOpen = true;
	}

	// This function is called when navigating away from the page
	function cleanup() {
		clearInterval(timerInterval);
		clearInterval(routeInterval);
	}

	// Call cleanup function when component is destroyed
	onDestroy(() => {
		cleanup();
	});

	$: if (map && start && end && start.every(Number.isFinite) && end.every(Number.isFinite)) {
		findRoute();
	}
	function createMarkerElement(color) {
		const markerElement = document.createElement('div');
		markerElement.innerHTML = `<svg height="30" width="30" viewBox="0 0 24 24" fill="${color}" xmlns="http://www.w3.org/2000/svg"><path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5S10.62 6.5 12 6.5s2.5 1.12 2.5 2.5S13.38 11.5 12 11.5z"/></svg>`;
		return markerElement.firstChild;
	}

	function formatDistance(distanceInMeters) {
		if (distanceInMeters > 999) {
			return (distanceInMeters / 1000).toFixed(2) + ' km';
		} else {
			return distanceInMeters + ' m';
		}
	}

	function formatTime(timeInSeconds) {
		if (timeInSeconds < 60) {
			return timeInSeconds + ' s';
		} else if (timeInSeconds < 3600) {
			return Math.round(timeInSeconds / 60) + ' min';
		} else {
			const hours = Math.floor(timeInSeconds / 3600);
			const minutes = Math.round((timeInSeconds % 3600) / 60);
			return hours + ' hr ' + minutes + ' min';
		}
	}

	async function reverseGeocode(coordinate) {
		const reversedCoordinate = [coordinate[1], coordinate[0]].join(',');
		const url = `https://api.tomtom.com/search/2/reverseGeocode/${reversedCoordinate}.json?key=${apiKey}`;

		try {
			const response = await fetch(url);
			const data = await response.json();

			return data.addresses ? data.addresses[0].address.freeformAddress : 'Unknown address';
		} catch (error) {
			console.error('Error during reverse geocoding:', error);
			return 'Error retrieving address';
		}
	}

	function findRoute() {
		if (!start || !end || !map) return;
		isLoading = true;
		formatPoints();

		if (currentLocation) {
			calculateRouteWithCurrentLocation();
		} else {
			navigator.geolocation.getCurrentPosition(
				(position) => {
					currentLocation = [position.coords.longitude, position.coords.latitude];
					calculateRouteWithCurrentLocation();
				},
				(error) => {
					console.error('Error getting user location:', error);
					alert(
						'Location permission is required to show the complete route. Please allow location access and try again.'
					);
					isLoading = false;
				}
			);
		}
	}

	function calculateRouteWithCurrentLocation() {
		/*async () => {
			const tt = await import('@tomtom-international/web-sdk-maps');
			let points: tt.Point[];
			for (let i = 0; i < route.points.length; i++) {
				points[i].x = route.points[i].lattitude;
				points[i].y = route.points[i].longtitude;
			}*/

		navigator.geolocation.getCurrentPosition((position) => {
			currentLocation = [position.coords.longitude, position.coords.latitude];
		}),
			import('@tomtom-international/web-sdk-services')
				.then((services) => {
					services.services
						.calculateRoute({
							key: apiKey,
							locations: [currentLocation, start, end], // reikes sudeti visus taskus
							travelMode: 'bicycle'
						})
						.then((routeData) => {
							const geojson = routeData.toGeoJson();
							const summary = routeData.routes[0].summary;
							const { lengthInMeters, travelTimeInSeconds } = summary;

							document.getElementById('distance').innerText = formatDistance(lengthInMeters);
							document.getElementById('travelTime').innerText = formatTime(travelTimeInSeconds);

							if (routeOnMap) {
								map.removeLayer('route');
								map.removeSource('route');
							}
							map.addSource('route', {
								type: 'geojson',
								data: geojson
							});
							map.addLayer({
								id: 'route',
								type: 'line',
								source: 'route',
								paint: {
									'line-color': '#4a90e2',
									'line-width': 5
								}
							});
							routeOnMap = 'route';

							//prevCoordinate = currentCoordinate;
							//currentCoordinate = currentLocation;
							// Remove the previous marker

							// Rotate the map to the direction of travel
							//rotateMap(); // nera tokios funkcijos

							const startMarkerElement = createMarkerElement('#4caf50');
							const endMarkerElement = createMarkerElement('#f44336');
							//const middleMarkerElement = createMarkerElement('#4caf50'); // reikia ideti markeriams

							new tt.Marker({ element: startMarkerElement }).setLngLat(start).addTo(map);
							new tt.Marker({ element: endMarkerElement }).setLngLat(end).addTo(map);
							console.log(route.points.length);
							/*if (route.points.length > 2) {
								for (let i = 1; i < route.points.length - 1; i++) {
									const point = route.points[i];
									const marker = new tt.Marker({ element: middleMarkerElement }).setLngLat([
										point.longtitude,
										point.latitude
									]);
									marker.addTo(map);
								}
							}*/
							const bounds = new tt.LngLatBounds(start, start);
							bounds.extend(end);
							map.fitBounds(bounds, { padding: 50 });
							traveledDistance = formatDistance(summary.lengthInMeters);
						})
						.catch((error) => {
							console.error('Error calculating route:', error);
							alert('Failed to calculate route. Please try again.');
						})
						.finally(() => {
							isLoading = false;
						});
				})
				.catch((error) => {
					console.error('Error loading route calculation services:', error);
					alert('Failed to load route calculation services. Please try again.');
					isLoading = false;
				});
	}

	function closeAndRedirect() {
		isModalOpen = false;
		goto('../routes');
	}

	function openFriendsModal() {
		isFriendsModalOpen = true;
	}

	function closeAndRedirectFriendsModal() {
		isFriendsModalOpen = false;
		goto('../display');
	}

	let friendRows = writable<User[]>([]);
	async function updateFriendRows() {
		$friendRows = [];
		for (const friendId of $friends) {
			const friend = await getUser(friendId);
			if (friend) {
				$friendRows.push(friend);
			}
		}

		$friendRows = $friendRows; // Trigger re-render
	}

	$: updateFriendRows().catch(console.error);

	async function inviteFriend(friend: User) {
		navigator.geolocation.getCurrentPosition((e) => {
			console.log(e.coords.latitude + ' ' + e.coords.longitude);
		});
	}
</script>

<div id="map-container"></div>
<div id="timer-control">
	<p>Laikas: {formatTimer(seconds)}</p>
	<p>Maršruto atstumas: <span id="distance"></span></p>
	<p>Kelionės laikas: <span id="travelTime"></span></p>
	<div id="button-container">
		{#if showStartButton}
			<Button on:click={startTimer} class="pradeti">Pradėti</Button>
			<Button on:click={openFriendsModal} class="draugai" variant="blue">Pridėti draugą</Button>
		{/if}
		{#if !showStartButton}
			<Button on:click={pauseResumeTimer} variant="blue"
				>{isPaused ? 'Pratęsti' : 'Sustabdyti'}</Button
			>
			<Button on:click={finishTimer} variant="darkRed">Užbaigti</Button>
		{/if}
	</div>
</div>

{#if isModalOpen}
	<!-- svelte-ignore a11y-click-events-have-key-events -->
	<!-- svelte-ignore a11y-click-events-have-key-events -->
	<!-- svelte-ignore a11y-no-static-element-interactions -->
	<div class="modal-background" on:click={() => (isModalOpen = false)}>
		<div class="modal" on:click|stopPropagation>
			<h2>Maršruto informacija</h2>
			<p>Laikas: {formatTimer(seconds)}</p>
			<p>Pradžios laikas: {startTime}</p>
			<p>Pabaigos laikas: {endTime}</p>
			<p>Pradžios adresas: {startAddress}</p>
			<p>Pabaigos adresas: {endAddress}</p>
			<p>Nukeliautas atstumas: {traveledDistance}</p>
			<Button on:click={closeAndRedirect} style="margin: auto; display: block;">Uždaryti</Button>
		</div>
	</div>
{/if}
{#if isFriendsModalOpen}
	<!-- svelte-ignore a11y-click-events-have-key-events -->
	<!-- svelte-ignore a11y-click-events-have-key-events -->
	<!-- svelte-ignore a11y-no-static-element-interactions -->
	<div class="modal-background" on:click={() => (isModalOpen = false)}>
		<div class="modal" on:click|stopPropagation>
			<div class="mb-4 mt-8 border-b border-slate-400 text-2xl font-semibold">Draugai</div>
			{#if $friends.length === 0}
				<div>
					Neturi draugų <i class="fa-regular fa-face-sad-tear align-middle" />
				</div>
			{:else}
				{#each $friendRows as friend}
					<div class="flex flex-row items-center rounded border border-slate-700 px-6 py-3">
						<div class="my-auto mr-2">
							{friend.firstName}
							{friend.lastName}
						</div>

						<Button
							size="sm"
							variant="primary"
							class="h-8 w-8 bg-gray-400"
							on:click={inviteFriend(friend)}
						>
							<!-- Added bg-gray-400 class -->
							<i class="fa-solid fa-check" />
						</Button>
					</div>
				{/each}
			{/if}
			<Button
				on:click={closeAndRedirectFriendsModal}
				style="margin: auto; display: block; margin-top: 40px;">Uždaryti</Button
			>
		</div>
	</div>
{/if}

<style>
	#map-container {
		height: 500px;
		width: 600px;
	}
	#timer-control {
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 10px;
		margin-top: 20px;
	}
	#button-container {
		display: flex;
		gap: 10px;
	}
	.modal-background {
		position: fixed;
		top: 0;
		left: 0;
		width: 100%;
		height: 100%;
		background-color: rgba(0, 0, 0, 0.6);
		display: flex;
		align-items: center;
		justify-content: center;
	}

	.modal {
		background-color: white;
		padding: 30px;
		border-radius: 8px;
		box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2);
		width: auto;
		max-width: 600px;
	}

	.modal h2 {
		margin-top: 0;
		font-weight: bold;
	}

	.modal p {
		margin: 20px 0;
		font-weight: normal;
	}
	p {
		margin: 0;
		font-size: 24px;
		font-weight: bold;
	}
</style>
