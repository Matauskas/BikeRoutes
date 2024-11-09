<script lang="ts">
	//@ts-nocheck

	import * as api from '$lib/api';
	import { onMount } from 'svelte';
	import Button from '$lib/components/Button.svelte';

	let map, routeOnMap;
	let isLoading = false; // For loading state management
	let center = [23.922041, 54.906227]; // the center (Kaunas)

	let markers; // array to put markers coordinates
	let arr = markers || []; // showed an error so i googled it and it said to do this (no clue why)
	let amount = 0; // index to the array

	let allPoints = '';
	let allPopups: tt.Popup[] = new Array(50);

	const apiKey = '';

	// Predefined start and destination locations
	//let startInput = 'Vilnius, Lithuania';
	//let destinationInput = 'Kaunas, Lithuania';
	let buttonClicked = false; // Flag to track whether the button has been clicked
	let mode = 'normal'; // State variable to track current mode

	onMount(async () => {
		const tt = await import('@tomtom-international/web-sdk-maps');
		const { services } = await import('@tomtom-international/web-sdk-services');
		const { default: SearchBox } = await import('@tomtom-international/web-sdk-plugin-searchbox');

		console.log('Map and CSS modules loaded successfully.');
		map = tt.map({
			key: apiKey,
			container: 'map-container',
			dragPan: true,
			center: center, // center is Kaunas
			zoom: 7 // Adjusted for better visibility of the route
		});

		map.addControl(new tt.FullscreenControl()); // adds full screen icon in the map
		map.addControl(new tt.NavigationControl()); // adds another icon in the map
		map.addControl(new tt.GeolocateControl());
		let options = {
			idleTimePress: 100,
			minNumberOfCharacters: 0,
			searchOptions: {
				key: '5zOoRjf2ubXpghwAWI0VqykIEWORa1mo',
				language: 'lt-LT'
			},
			autocompleteOptions: {
				key: '5zOoRjf2ubXpghwAWI0VqykIEWORa1mo',
				language: 'lt-LT'
			},
			noResultsMessage: 'No results found.'
		};
		var ttSearchBox = new SearchBox(services, options);
		var searchMarkersManager = new SearchMarkersManager(map);
		ttSearchBox.on('tomtom.searchbox.resultsfound', handleResultsFound);
		ttSearchBox.on('tomtom.searchbox.resultselected', handleResultSelection);
		ttSearchBox.on('tomtom.searchbox.resultfocused', handleResultSelection);
		ttSearchBox.on('tomtom.searchbox.resultscleared', handleResultClearing);
		map.addControl(ttSearchBox, 'top-left');
		//////////////////////////////////////////////////////////////////////////////////////////
		// Start of searchbox functions
		//////////////////////////////////////////////////////////////////////////////////////////
		function handleResultsFound(event) {
			var results = event.data.results.fuzzySearch.results;

			if (results.length === 0) {
				searchMarkersManager.clear();
			}
			searchMarkersManager.draw(results);
			fitToViewport(results);
		}

		function handleResultSelection(event) {
			var result = event.data.result;
			if (result.type === 'category' || result.type === 'brand') {
				return;
			}
			searchMarkersManager.draw([result]);
			fitToViewport(result);
		}

		function fitToViewport(markerData) {
			if (!markerData || (markerData instanceof Array && !markerData.length)) {
				return;
			}
			var bounds = new tt.LngLatBounds();
			if (markerData instanceof Array) {
				markerData.forEach(function (marker) {
					bounds.extend(getBounds(marker));
				});
			} else {
				bounds.extend(getBounds(markerData));
			}
			map.fitBounds(bounds, { padding: 100, linear: true });
		}

		function getBounds(data) {
			var btmRight;
			var topLeft;
			if (data.viewport) {
				btmRight = [data.viewport.btmRightPoint.lng, data.viewport.btmRightPoint.lat];
				topLeft = [data.viewport.topLeftPoint.lng, data.viewport.topLeftPoint.lat];
			}
			return [btmRight, topLeft];
		}

		function handleResultClearing() {
			searchMarkersManager.clear();
		}
		function SearchMarkersManager(map, options) {
			this.map = map;
			this._options = options || {};
			this._poiList = undefined;
			this.markers = {};
		}

		SearchMarkersManager.prototype.draw = function (poiList) {
			this._poiList = poiList;
			this.clear();
			this._poiList.forEach(function (poi) {
				var markerId = poi.id;
				var poiOpts = {
					name: poi.poi ? poi.poi.name : undefined,
					address: poi.address ? poi.address.freeformAddress : '',
					distance: poi.dist,
					classification: poi.poi ? poi.poi.classifications[0].code : undefined,
					position: poi.position,
					entryPoints: poi.entryPoints
				};
				var marker = new SearchMarker(poiOpts, this._options);
				marker.addTo(this.map);
				this.markers[markerId] = marker;
			}, this);
		};

		SearchMarkersManager.prototype.clear = function () {
			for (var markerId in this.markers) {
				var marker = this.markers[markerId];
				marker.remove();
			}
			this.markers = {};
			this._lastClickedMarker = null;
		};
		function SearchMarker(poiData, options) {
			this.poiData = poiData;
			this.options = options || {};
			this.marker = new tt.Marker({
				element: this.createMarker(),
				anchor: 'bottom'
			});
			var lon = this.poiData.position.lng || this.poiData.position.lon;
			this.marker.setLngLat([lon, this.poiData.position.lat]);
		}

		SearchMarker.prototype.addTo = function (map) {
			this.marker.addTo(map);
			this._map = map;
			return this;
		};

		SearchMarker.prototype.createMarker = function () {
			var elem = document.createElement('div');
			elem.className = 'tt-icon-marker-black tt-search-marker';
			if (this.options.markerClassName) {
				elem.className += ' ' + this.options.markerClassName;
			}
			var innerElem = document.createElement('div');
			innerElem.setAttribute(
				'style',
				'background: white; width: 10px; height: 10px; border-radius: 50%; border: 3px solid black;'
			);

			elem.appendChild(innerElem);
			return elem;
		};

		SearchMarker.prototype.remove = function () {
			this.marker.remove();
			this._map = null;
		};
		////////////////////////////////////////////////////////////////////////////////////////////////////
		// End of search box functions
		////////////////////////////////////////////////////////////////////////////////////////////////////

		map.on('click', (event) => {
			if (mode === 'markerPlacement') {
				// function to add a marker once clicked
				//console.log(event); //used to see what data was saved in the event

				var marker = new tt.Marker({
					draggable: true
				})
					.setLngLat(event.lngLat)
					.addTo(map);
				let lng = event.lngLat.lng; //saves longtitude individually
				let lat = event.lngLat.lat; //saves lattitude individually
				arr[amount] = [lng, lat]; //puts them in a array in the correct order

				let details = document.createElement('p');
				details.innerHTML =
					'Tai yra: ' + (amount + 1) + '<br>Koordinates:<br>(' + lng + ',' + lat + ')<br>';
				let deleteButton = document.createElement('button');
				deleteButton.innerHTML = 'Ištrinti';
				deleteButton.onclick = function () {
					marker.remove(); // Ištrina tašką iš žemėlapio
					popup.remove(); // Ištrina balioną iš žemėlapio
				};
				details.appendChild(deleteButton);
				let popup = new tt.Popup({
					offset: 25,
					anchor: 'bottom'
				});
				popup.setDOMContent(details);
				popup.setMaxWidth('500px');
				allPopups[amount++] = popup;
				marker.setPopup(popup);
				marker.togglePopup();

				if (amount >= 2) {
					allPoints += ':' + lng + ',' + lat;
					//console.log(allPoints);
				} else {
					allPoints += lng + ',' + lat;
					//console.log(allPoints);
				}
				//console.log(arr); //used to check if it was successfully inside
				marker.on('dragend', () => {
					onDragEnd(marker);
				});
				marker.on('dra');
			}
		});

		// Wait for the map to load before calculating the route
		map.on('load', () => {
			console.log('Map loaded successfully.');
		});
	});

	function onDragEnd(marker: tt.Marker) {
		var lngLat = marker.getLngLat();
		let popupBefore = marker.getPopup();
		let index = FindIndexOfPopup(popupBefore);

		ReplaceSearchCoordinates(index, lngLat);

		let details = document.createElement('p');
		details.innerHTML =
			'Tai yra: ' + (index + 1) + '<br>Koordinates:<br>(' + lngLat.lng + ',' + lngLat.lat + ')';
		let deleteButton = document.createElement('button');
		deleteButton.innerHTML = 'Ištrinti';
		deleteButton.onclick = function () {
			marker.remove(); // Ištrina tašką iš žemėlapio
			popup.remove(); // Ištrina balioną iš žemėlapio
		};
		details.appendChild(deleteButton);
		let popup = new tt.Popup({
			offset: 25,
			anchor: 'bottom'
		});

		popup.setDOMContent(details);
		popup.setMaxWidth('500px');
		allPopups[index] = popup;
		marker.setPopup(popup);
		marker.togglePopup();
	}

	function ReplaceSearchCoordinates(index: number, lnglat: tt.LngLat) {
		let singles = allPoints.split(':');
		let object = lnglat.lng + ',' + lnglat.lat;
		singles[index] = object;
		let newString = '';
		for (let i = 0; i < singles.length; i++) {
			if (i == 0) newString += singles[i];
			else newString += ':' + singles[i];
		}
		allPoints = newString;
	}

	function FindIndexOfPopup(popup: tt.Popup) {
		let index = -1;
		for (let i = 0; i < allPopups.length; i++) {
			if (allPopups[i] == popup) index = i;
		}
		return index;
	}

	async function findRoute() {
		let routeData;
		const { services } = await import('@tomtom-international/web-sdk-services');
		try {
			routeData = await services.calculateRoute({
				key: apiKey,
				locations: allPoints, // string format that contains all necessarry points to find
				travelMode: 'bicycle'
			});
		} catch {
			console.error('Error calculating route:', error);
			alert('Failed to calculate route. Please try again.');
		}

		const geojson = routeData.toGeoJson();
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
			layout: {},
			paint: {
				'line-color': '#4a90e2',
				'line-width': 5
			}
		});
		routeOnMap = 'route';
	}

	async function saveRoute() {
		let successMessage = '';
		let errorMessage = '';
		const points = [];

		for (let i = 0; i < arr.length; i++) {
			const longtitude = arr[i][0];
			const latitude = arr[i][1];
			points.push({ longtitude, latitude });

			// console.log(`Processing point ${i + 1}: Latitude ${longtitude}, Longitude ${latitude}`);
		}

		const response = await api.saveRoute({ points });

		if (!response.ok) {
			errorMessage = 'Nepavyko išsaugoti mašruto, bandykite dar kartą vėliau';
		} else {
			successMessage = 'Sėkmingai sukurtas mašrutas';
		}

		if (errorMessage) {
			// If there was an error during sending points, display the error message
			console.error(errorMessage);
			return;
		}

		console.log(successMessage);
	}

	const handleCreateRoute = async () => {
		buttonClicked = true;
		setLoadingState(true);
		try {
			await findRoute();
			await saveRoute();
		} catch (error) {
			console.error('Error occurred:', error);
		} finally {
			setLoadingState(false);
			buttonClicked = false;
		}
	};

	function toggleMode() {
		mode = mode === 'normal' ? 'markerPlacement' : 'normal';
		if (mode === 'markerPlacement') {
			console.log('Marker Placement Mode activated');
		} else {
			console.log('Normal Mode activated');
		}
	}

	// Function to toggle loading state
	function setLoadingState(state) {
		isLoading = state;
	}
</script>

<div id="mode-info">
	<p class="grow align-bottom text-4xl font-bold">
		{#if mode === 'markerPlacement'}
			Maršruto kūrimas
		{:else if mode === 'normal'}
			Žemėlapio peržiūra
		{/if}
	</p>
</div>

<div id="map-container">
	<div id="marker"></div>
</div>

<div id="buttons">
	{#if amount >= 2}
		<button on:click={handleCreateRoute} disabled={isLoading}>
			{#if isLoading}
				<span>Loading...</span>
			{:else}
				<span><Button>Sukurti maršrutą</Button></span>
			{/if}
		</button>
	{/if}
	<div>
		<Button on:click={toggleMode}>Pakeisti žemėlapio režimą</Button>
	</div>
</div>

<style>
	#map-container {
		height: 400px;
		width: 100%;
	}
	#marker {
		background-size: cover;
	}
	#mode-info {
		text-align: center;
		margin-bottom: 10px;
	}
</style>
