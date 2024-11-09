<script lang="ts">
	import { page } from '$app/stores';
	import MapLayout from '$lib/components/MapLayout.svelte';
	import { createMarkers, updateRouteLayer, centerOnRoute } from '$lib/tomtom';
	import {
		getRoute,
		type Route,
		loggedInUserId,
		addOrUpdateEntityById,
		routes,
		type Point
	} from '$lib/store';
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import type { Map, Marker } from '@tomtom-international/web-sdk-maps';
	import { updateRoute } from '$lib/api';
	import StartRouteButton from '$lib/components/StartRouteButton.svelte';
	import * as api from '$lib/api';
	import tt from '@tomtom-international/web-sdk-maps';
	import { services } from '@tomtom-international/web-sdk-services';
	let title1: any;

	let map: Map;
	let routeId = Number($page.params.id);
	getRoute(routeId).then((titleObject) => {
		title1 = titleObject?.title;
		console.log(title1);

		// Now you can use the title as needed
	});
	let route: Route = {
		id: routeId,
		points: [],
		distance: 0,
		time: 0,
		ownerId: 0,
		route: null,
		title: title1
	};

	let showUnsavedChanges = false;
	let showModal = false;
	let preferredDistance = 1000; // Default value
	let selectedCategory = '9362008'; // Default category (Parks)
	const apiKey = '5zOoRjf2ubXpghwAWI0VqykIEWORa1mo';

	let markers; // array to put markers coordinates
	let arr = markers || []; // showed an error so i googled it and it said to do this (no clue why)
	let amount = 0; // index to the array
	let button;
	let coordinates: any[] = [];

	let optimalPoints;
	let username;

	function metersToKilometers(meters: number) {
		return meters / 1000;
	}

	function secondsToHours(seconds: number) {
		const minutes = seconds / 60;
		return minutes / 60;
	}

	class SearchMarkersManager {
		private map: Map;
		private _options: any;
		private markers: any;
		constructor(map: Map, options?: {}) {
			this.map = map;
			this._options = options || {};
			this.markers = {};
		}

		draw(poiList: any[]) {
			this.clear();
			poiList.forEach(
				(poi: {
					id: any;
					poi: { name: any; classifications: { code: any }[] };
					address: { freeformAddress: any };
					dist: any;
					position: any;
					entryPoints: any;
				}) => {
					const markerId = poi.id;
					const poiOpts = {
						name: poi.poi ? poi.poi.name : '',
						address: poi.address ? poi.address.freeformAddress : '',
						distance: poi.dist,
						classification: poi.poi ? poi.poi.classifications[0].code : '',
						position: poi.position,
						entryPoints: poi.entryPoints
					};
					const marker = new SearchMarker(poiOpts, this._options);
					marker.addTo(this.map);
					this.markers[markerId] = marker;
				}
			);
		}

		clear() {
			for (const markerId in this.markers) {
				this.markers[markerId].remove();
			}
			this.markers = {};
		}
	}

	class SearchMarker {
		private poiData: any;
		private options: any;
		private marker: any;
		private _map: any;
		constructor(
			poiData: {
				name: any;
				address: any;
				distance: any;
				classification: any;
				position: any;
				entryPoints: any;
			},
			options: {}
		) {
			this.poiData = poiData;
			this.options = options || {};
			this.marker = new tt.Marker({
				element: this.createMarker(),
				anchor: 'bottom'
			});
			const lon = this.poiData.position.lng || this.poiData.position.lon;
			this.marker.setLngLat([lon, this.poiData.position.lat]);
		}

		addTo(map: any) {
			this.marker.addTo(map);
			this._map = map;
			return this;
		}

		createMarker() {
			const elem = document.createElement('div');
			elem.className = 'tt-icon-marker-black tt-search-marker';
			if (this.options.markerClassName) {
				elem.className += ` ${this.options.markerClassName}`;
			}
			const innerElem = document.createElement('div');
			innerElem.style.cssText =
				'background: white; width: 10px; height: 10px; border-radius: 50%; border: 3px solid black;';
			elem.appendChild(innerElem);
			return elem;
		}

		remove() {
			this.marker.remove();
			this._map = null;
		}
	}

	let searchMarkersManager: SearchMarkersManager;

	onMount(async () => {
		const tt = await import('@tomtom-international/web-sdk-maps');
		const { services } = await import('@tomtom-international/web-sdk-services');
		const { default: SearchBox } = await import('@tomtom-international/web-sdk-plugin-searchbox');

		map = tt.map({
			key: apiKey,
			container: 'map-container',
			dragPan: true,
			center: [23.922041, 54.906227], // Centered on Kaunas
			zoom: 7
		});

		map.addControl(new tt.FullscreenControl());
		map.addControl(new tt.NavigationControl());
		map.addControl(new tt.GeolocateControl());

		const userResponse = await api.getLoggedInUser();
		username = userResponse.body.username;

		let options = {
			idleTimePress: 100,
			minNumberOfCharacters: 0,
			searchOptions: {
				key: apiKey,
				language: 'lt-LT'
			},
			autocompleteOptions: {
				key: apiKey,
				language: 'lt-LT'
			},
			noResultsMessage: 'No results found.'
		};

		searchMarkersManager = new SearchMarkersManager(map);

		const ttSearchBox = new SearchBox(services, options);
		ttSearchBox.on('tomtom.searchbox.resultsfound', handleResultsFound);
		ttSearchBox.on('tomtom.searchbox.resultselected', handleResultSelection);
		ttSearchBox.on('tomtom.searchbox.resultfocused', handleResultSelection);
		ttSearchBox.on('tomtom.searchbox.resultscleared', handleResultClearing);
		map.addControl(ttSearchBox, 'top-left');
	});

	const getCurrentPosition = () => {
		return new Promise((resolve, reject) => {
			navigator.geolocation.getCurrentPosition(resolve, reject);
		});
	};

	async function findOptimalRoute(coordinates: any[]) {
		const optimalRoute = { points: null as any[] | null, distance: null as number | null };
		const tolerance = 1000;
		let r = 2;
		const targetDistance = preferredDistance;

		for (r; r <= coordinates.length; r++) {
			const combinations = getCombinations(coordinates, r);
			for (const combination of combinations) {
				let distance = await calculateRouteDistance(combination);
				if (distance !== null && Math.abs(distance - targetDistance) <= tolerance) {
					optimalRoute.points = combination;
					optimalRoute.distance = distance;
					return optimalRoute;
				}
			}
		}
		return null;
	}

	function getCombinations(points: any, r: any) {
		const results: any[][] = [];
		const helper = (
			arr: { [x: string]: any },
			data: any[],
			start: number,
			end: number,
			index: number
		) => {
			if (index === r) {
				results.push([...data]);
				return;
			}
			for (let i = start; i <= end && end - i + 1 >= r - index; i++) {
				data[index] = arr[i];
				helper(arr, data, i + 1, end, index + 1);
			}
		};
		helper(points, new Array(r), 0, points.length - 1, 0);
		return results;
	}

	async function calculateRouteDistance(points: any[]) {
		try {
			const routeData = await services.calculateRoute({
				key: apiKey,
				locations: points.map((p: { lng: any; lat: any }) => `${p.lng},${p.lat}`).join(':'),
				travelMode: 'bicycle'
			});
			return routeData.routes[0].summary.lengthInMeters;
		} catch (error) {
			console.error('Error calculating route:', error);
			return null;
		}
	}

	async function findRouteCustom() {
		const coordinates = await searchForParks();
		const optimalRoute = await findOptimalRoute(coordinates);
		if (!optimalRoute || !optimalRoute.points) {
			alert('No optimal route found');
			return;
		}
		const routeData = await services.calculateRoute({
			key: apiKey,
			locations: optimalRoute.points
				.map((p: { lng: any; lat: any }) => `${p.lng},${p.lat}`)
				.join(':'),
			travelMode: 'bicycle'
		});
		const geojson = routeData.toGeoJson();
		if (map.getLayer('route')) {
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
			layout: {},
			paint: {
				'line-color': '#4a90e2',
				'line-width': 5
			}
		});

		// Add Start and Finish Markers
		const startPoint = optimalRoute.points[0];
		const endPoint = optimalRoute.points[optimalRoute.points.length - 1];

		// Create and add start marker
		addDraggableMarker(startPoint, 0);

		// Create and add finish marker
		addDraggableMarker(endPoint, optimalRoute.points.length - 1);
	}

	// Function to add a draggable marker
	async function addDraggableMarker(position: { lng: number; lat: number }, index: number) {
		const tt = await import('@tomtom-international/web-sdk-maps');
		const point = {
			longtitude: position.lng,
			latitude: position.lat
		};
		const marker = new tt.Marker({ draggable: true });
		marker.setLngLat([position.lng, position.lat]);
		marker.on('dragend', () => onMarkerDrag(marker, route.points[index]));
		marker.addTo(map);
		showUnsavedChanges = true;

		route.points.push(point);
		await onMarkerDrag(marker, point);
	}

	function handleResultsFound(event: any) {
		const results = event?.data?.results?.fuzzySearch?.results || [];
		if (results.length === 0) {
			searchMarkersManager.clear();
		} else {
			searchMarkersManager.draw(results);
			fitToViewport(results);
		}
	}

	function handleResultSelection(event: any) {
		const result = event?.data?.result;
		if (result && (result.type === 'category' || result.type === 'brand')) {
			return;
		}
		if (result) {
			searchMarkersManager.draw([result]);
			fitToViewport(result);
		}
	}

	function fitToViewport(markerData: any[]) {
		const bounds = new tt.LngLatBounds();
		if (Array.isArray(markerData)) {
			markerData.forEach((marker) => {
				const markerBounds = getBounds(marker);
				if (markerBounds) {
					bounds.extend(markerBounds);
				}
			});
		} else {
			const markerBounds = getBounds(markerData);
			if (markerBounds) {
				bounds.extend(markerBounds);
			}
		}
		if (!bounds.isEmpty()) {
			map.fitBounds(bounds, { padding: 100, linear: true });
		}
	}

	function getBounds(data: {
		viewport: {
			btmRightPoint: { lng: number; lat: number };
			topLeftPoint: { lng: number; lat: number };
		};
	}) {
		if (data.viewport) {
			const southwest = new tt.LngLat(
				data.viewport.btmRightPoint.lng,
				data.viewport.btmRightPoint.lat
			);
			const northeast = new tt.LngLat(
				data.viewport.topLeftPoint.lng,
				data.viewport.topLeftPoint.lat
			);
			return new tt.LngLatBounds(southwest, northeast);
		}
		return undefined; // Ensure we return undefined if no viewport data is present
	}

	function handleResultClearing() {
		searchMarkersManager.clear();
	}

	function extractCoordinates(results: any[]) {
		return results.map((result: { position: { lng: any; lat: any } }) => {
			if (result.position) {
				return { lng: result.position.lng, lat: result.position.lat };
			}
		});
	}

	async function searchForParks() {
		try {
			const query = 'in Kaunas';
			const response = await services.poiSearch({
				key: apiKey,
				categorySet: selectedCategory.toString(),
				query: query,
				limit: 12,
				countrySet: 'LT'
			});
			if (response && response.results) {
				console.log('POI search results:', response.results);
				handleResultsFound(response);
				return extractCoordinates(response.results);
			} else {
				console.error('Invalid response from POI search');
				return [];
			}
		} catch (error) {
			console.error('Error during POI search:', error);
			return [];
		}
	}

	function toggleModal() {
		showModal = !showModal;
	}

	async function handleModalSubmit() {
		try {
			await findRouteCustom();
		} catch (error) {
			console.error('Error handling modal submission:', error);
		}
		showModal = false;
	}

	async function saveChanges() {
		const response = await updateRoute(route.id, route);
		if (!response.ok) {
			// TODO: Show error message
			return;
		}

		addOrUpdateEntityById(routes, route);
		showUnsavedChanges = false;
	}

	async function onMarkerDrag(marker: Marker, point: Point) {
		var lngLat = marker.getLngLat();
		point.longtitude = lngLat.lng;
		point.latitude = lngLat.lat;

		if (route.points.length >= 2) {
			const routeData = await updateRouteLayer(map, route.points);
			const routeSummary = routeData.routes[0].summary;
			route.distance = metersToKilometers(routeSummary.lengthInMeters);
			route.time = secondsToHours(routeSummary.travelTimeInSeconds);
		}
		showUnsavedChanges = true;
	}

	async function mapLoaded(e: CustomEvent) {
		map = e.detail.map;

		if (route.points.length >= 2) {
			const routeData = await updateRouteLayer(map, route.points);
			const routeSummary = routeData.routes[0].summary;
			route.distance = metersToKilometers(routeSummary.lengthInMeters);
			route.time = secondsToHours(routeSummary.travelTimeInSeconds);
			await centerOnRoute(map, routeData);
		}

		const markers = await createMarkers(map, route.points);
		for (let i = 0; i < markers.length; i++) {
			const marker = markers[i];
			const point = route.points[i];
			marker.setDraggable(true);
			marker.on('dragend', () => onMarkerDrag(marker, point));
		}
	}

	async function addPoint() {
		const tt = await import('@tomtom-international/web-sdk-maps');

		const markerPos = map.getCenter();
		const point = {
			longtitude: markerPos.lng,
			latitude: markerPos.lat
		};
		const marker = new tt.Marker({ draggable: true });
		marker.setLngLat(markerPos);
		marker.on('dragend', () => onMarkerDrag(marker, point));
		marker.addTo(map);
		showUnsavedChanges = true;

		route.points.push(point);
		await onMarkerDrag(marker, point);
	}
</script>

<!-- svelte-ignore missing-declaration -->
<!-- svelte-ignore missing-declaration -->
<MapLayout on:loaded={mapLoaded}>
	<div id="map-container" class="map-container"></div>
	<div slot="right-column" class="flex flex-col gap-4 md:flex-row">
		<button
			class="
          box-shadow-green-3 pr-100 relative w-12 rounded-l-lg bg-green-300 p-3
          md:rounded-b-lg md:rounded-tl-none
        "
			on:click={saveChanges}
		>
			<i class="fa-regular fa-floppy-disk fa-xl" />
			{#if showUnsavedChanges}
				<i class="fa-solid fa-circle fa-xs absolute right-2 top-3 text-red-400" />
			{/if}
		</button>

		<button
			class="
          box-shadow-green-3 pr-100 relative w-12 rounded-l-lg bg-green-300 p-3
          md:rounded-b-lg md:rounded-tl-none
        "
			on:click={addPoint}
		>
			<i class="fa-solid fa-location-dot fa-xl" />
		</button>

		<button
			class="
          box-shadow-green-3 pr-100 relative w-12 rounded-l-lg bg-green-300 p-3
          md:rounded-b-lg md:rounded-tl-none
        "
			on:click={toggleModal}
		>
			<i class="fa-solid fa-map-marker-alt fa-xl" />
			<i class="fa-solid fa-plus fa-xs" style="position: absolute; top: 10px; right: 10px;" />
		</button>

		<StartRouteButton {routeId} />
	</div>

	{#if showModal}
		<div class="modal">
			<div class="modal-content">
				<span role="button" tabindex="0" on:click={toggleModal} on:keypress={toggleModal}
					>&times;</span
				>
				<h2>Kur norite pakeliauti?</h2>
				<form on:submit|preventDefault={handleModalSubmit}>
					<label>
						Kiek metrų norite nukeliauti?
						<input
							type="number"
							bind:value={preferredDistance}
							class="kilometers-input"
							min="1000"
							step="100"
						/>
					</label>
					<label>
						Ką norite aplankyti?
						<select bind:value={selectedCategory}>
							<option value="9362008">Parkai</option>
							<option value="9351005">Upės</option>
							<option value="9362007">Jūra</option>
							<option value="7320">Sporto centras</option>
							<option value="7315">Restoranas</option>
							<option value="9376">Kavinė / Pubas</option>
							<option value="9663">Sveikatos priežiūros paslaugos</option>
							<option value="7321">Ligoninė</option>
							<option value="9361">Parduotuvė</option>
							<option value="7332">Turgus</option>
							<option value="7369">Atvira automobilių stovėjimo aikštelė</option>
							<option value="9937">Klubas ir asociacija</option>
							<option value="7360">Kempingas</option>
						</select>
					</label>
					<button class="generate-button" type="submit">Sukurti!</button>
				</form>
			</div>
		</div>
	{/if}
</MapLayout>

<style>
	.modal {
		display: block;
		position: fixed;
		z-index: 1;
		left: 0;
		top: 0;
		width: 100%;
		height: 100%;
		overflow: auto;
		background-color: rgba(0, 0, 0, 0.4);
	}

	.modal-content {
		background-color: #fff;
		margin: 15% auto;
		padding: 20px;
		border: 1px solid #888;
		width: 40%;
		max-width: 400px;
		border-radius: 8px;
		box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2);
	}

	.close {
		color: #aaa;
		float: right;
		font-size: 28px;
		font-weight: bold;
	}

	.close:hover,
	.close:focus {
		color: black;
		text-decoration: none;
		cursor: pointer;
	}

	.kilometers-input {
		border: 2px solid #4caf50;
		padding: 5px;
		width: 100%;
		margin-bottom: 10px;
	}

	.generate-button {
		margin-top: 10px;
		background-color: #4caf50;
		color: white;
		border: none;
		padding: 10px 20px;
		text-align: center;
		text-decoration: none;
		display: inline-block;
		font-size: 16px;
		border-radius: 4px;
		cursor: pointer;
	}

	.generate-button:hover {
		background-color: #45a049;
	}

	label {
		display: block;
		margin-bottom: 10px;
	}

	select {
		width: 100%;
		padding: 5px;
		margin-bottom: 10px;
	}
</style>
