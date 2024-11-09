<script lang="ts">
	import { onMount } from 'svelte';
	import MapLayout from '$lib/components/MapLayout.svelte';
	import * as tomtom from '$lib/tomtom';
	import * as api from '$lib/api';
	import type { Map } from '@tomtom-international/web-sdk-maps';
	import Modal from '$lib/components/Modal.svelte';
	import Form from '$lib/components/Form.svelte';
	import FormTextInput from '$lib/components/FormTextInput.svelte';
	import { addOrUpdateEntityById, routes, type Route } from '$lib/store';
	import Button from '$lib/components/Button.svelte';
	import { goto } from '$app/navigation';
	import tt from '@tomtom-international/web-sdk-maps';

	let mySearchBox: HTMLElement;
	let showAddRouteModal = false;
	let addRouteOptions: Partial<Route> = {};
	let map: Map;
	let searchMarkersManager: SearchMarkersManager;

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

	onMount(async () => {
		const { services } = await import('@tomtom-international/web-sdk-services');
		const { default: SearchBox } = await import('@tomtom-international/web-sdk-plugin-searchbox');

		var ttSearchBox = new SearchBox(services, {
			idleTimePress: 100,
			minNumberOfCharacters: 0,
			searchOptions: { key: tomtom.apiKey, language: tomtom.language },
			autocompleteOptions: { key: tomtom.apiKey, language: tomtom.language },
			labels: {
				noResultsMessage: 'Nieko nerasta',
				suggestions: {
					brand: 'Siūlomas prekės ženklas',
					category: 'Siūloma kategorija'
				}
			},
			showSearchButton: false
		});

		ttSearchBox.on('tomtom.searchbox.resultsfound', handleResultsFound);
		ttSearchBox.on('tomtom.searchbox.resultselected', handleResultSelection);
		ttSearchBox.on('tomtom.searchbox.resultfocused', handleResultSelection);
		ttSearchBox.on('tomtom.searchbox.resultscleared', handleResultClearing);
		mySearchBox.appendChild(ttSearchBox.getSearchBoxHTML());
	});

	function handleResultsFound(event: any) {
		const results = event?.data?.results?.fuzzySearch?.results || [];
		if (results.length === 0) {
			searchMarkersManager.clear();
		} else {
			searchMarkersManager.draw(results);
			fitToViewport(results);
		}
	}

	function handleResultSelection(event: { data: { result: any } }) {
		var result = event.data.result;
		if (result.type === 'category' || result.type === 'brand') {
			return;
		}

		searchMarkersManager.draw([result]);
		fitToViewport([result]);
	}

	function fitToViewport(markerData: any[]) {
		const bounds = new tt.LngLatBounds();
		markerData.forEach((marker) => {
			const markerBounds = getBounds(marker);
			if (markerBounds) {
				//bounds.extend(markerBounds);
			}
		});
		if (!bounds.isEmpty()) {
			map.fitBounds(bounds, { padding: 100, linear: true });
		}
	}
	function getBounds(data: any) {
		let btmRight;
		let topLeft;
		if (data.viewport) {
			btmRight = [data.viewport.btmRightPoint.lng, data.viewport.btmRightPoint.lat];
			topLeft = [data.viewport.topLeftPoint.lng, data.viewport.topLeftPoint.lat];
		}

		return btmRight && topLeft ? [btmRight, topLeft] : undefined;
	}

	function handleResultClearing() {
		searchMarkersManager.clear();
	}

	async function mapLoaded(e: CustomEvent) {
		const map: Map = e.detail.map;
		const tt = await import('@tomtom-international/web-sdk-maps');
		searchMarkersManager = new SearchMarkersManager(map);

		map.addControl(new tt.GeolocateControl());
	}

	async function addRoute() {
		if (!addRouteOptions.title) return;

		const response = await api.saveRoute({
			distance: 0,
			time: 0,
			points: [],
			title: addRouteOptions.title
		});
		if (!response.ok) {
			// TODO: Show error message
			return;
		}

		const route: Route = response.body;

		addOrUpdateEntityById(routes, route);
		await goto(`/routes/${route.id}/edit`);
	}

	function openActiveTrip() {
		console.log('// TODO: open active trip');
	}

	// TODO: Hook up 'showActiveTrip'
	const showActiveTrip = false; // Should a button be shown to redirect the user to an already active trip
</script>

<MapLayout on:loaded={mapLoaded}>
	<button
		class="
			box-shadow-green-3 absolute bottom-28 left-0 rounded-r-lg bg-green-300 p-3
			md:bottom-auto md:left-96 md:top-0 md:rounded-b-lg md:rounded-tr-none
		"
		on:click={() => (showAddRouteModal = true)}
	>
		<i class="fa-solid fa-plus fa-xl" />
	</button>

	<div class="pointer-events-none left-[28rem] top-0 flex-row gap-5 md:absolute md:flex">
		<button
			class="
				box-shadow-black-3
				pointer-events-auto absolute bottom-28 right-0 h-12 w-14 rounded-l-lg bg-slate-50 p-3 pt-4
				md:static md:rounded-b-lg md:rounded-tl-none md:pt-3
			"
			class:hidden={!showActiveTrip}
			on:click={openActiveTrip}
		>
			<div class="relative">
				<i class="fa-solid fa-person-biking fa-xl absolute left-0 top-0" />
				<i class="fa-solid fa-circle fa-2xs absolute bottom-2 left-7 text-red-400" />
			</div>
		</button>

		<div
			class="
				box-shadow-black-3
				width-magic pointer-events-auto absolute left-0 top-4 h-12 w-64 rounded-r-lg bg-slate-50 p-3 pr-5
				md:static md:flex md:flex-row md:rounded-b-lg md:rounded-tr-none
			"
			bind:this={mySearchBox}
		>
			<div class="fa-solid fa-magnifying-glass fa-lg mr-3 mt-auto leading-none" />
		</div>
	</div>
</MapLayout>

<Modal bind:showModal={showAddRouteModal}>
	<h2 slot="title">Maršruto kūrimas</h2>

	<Form class="my-2 flex flex-col gap-4" on:submit={addRoute}>
		<FormTextInput required bind:value={addRouteOptions.title} name="title" label="Vardas" />
		<Button variant="green">Sukurti</Button>
	</Form>
</Modal>

<style>
	@media (min-width: 768px) {
		/* Constrain search box width so it does not overlap with the top right controls from map */
		/* Too lazy to create a better solution */
		/* TODO: Create a better solution that does not involve magic numbers */
		.width-magic {
			max-width: calc(100vw - 36rem);
		}
	}
</style>
