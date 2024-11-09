<script lang="ts">
	import { listUserRoutes, loggedInUserId, saveRouteData } from '$lib/store';
	import { goto } from '$app/navigation';

	const apiKey = 'GwdctTYBnXKj1P4RGPRABBIlMDwdCpMu';
	const maxRetries = 5;

	async function reverseGeocodeWithRetry(coordinate: [any, any], retries: number = 0) {
		const reversedCoordinate = coordinate.join(',');
		const language = 'en-US';
		const url = `https://api.tomtom.com/search/2/reverseGeocode/${reversedCoordinate}.json?key=${apiKey}&language=${language}`;

		try {
			const response = await fetch(url);
			const data = await response.json();

			if (data.addresses && data.addresses.length > 0) {
				return data.addresses[0].address.freeformAddress;
			} else {
				throw new Error('Adresas nerastas');
			}
		} catch (error) {
			if (retries < maxRetries) {
				console.log(`Retrying (${retries + 1}/${maxRetries})...`);
				return reverseGeocodeWithRetry(coordinate, retries + 1);
			} else {
				throw new Error('Nepavyksta gauti adreso');
			}
		}
	}

	function getLast(array: any[]) {
		return array[array.length - 1];
	}
</script>

{#await listUserRoutes() then routes}
	{#each routes as route, i}
		{#if route.ownerId == $loggedInUserId}
			<button
				class="route-card cursor-pointer border-2 border-slate-700"
				style="display: block; width: 100%; padding: 1rem; margin-bottom: 1rem;"
				on:click={() => {
					saveRouteData.set({
						route: route
					});
					goto('../display');
				}}
			>
				<div class="route-info">
					<div class="route-number">Maršrutas Nr. {route.id}</div>

					<div class="coordinates">
						Pradžia:
						{#if route.points && route.points[0]}
							{#await reverseGeocodeWithRetry( [route.points[0].latitude, route.points[0].longtitude] ) then startAddress}
								{startAddress}
							{:catch error}
								{error.message}
							{/await}
						{:else}
							<span>No starting point</span>
						{/if}
					</div>

					<div class="coordinates">
						Pabaiga:
						{#if route.points && route.points.length > 0}
							{#await reverseGeocodeWithRetry( [getLast(route.points).latitude, getLast(route.points).longtitude] ) then endAddress}
								{endAddress}
							{:catch error}
								{error.message}
							{/await}
						{:else}
							<span>No ending point</span>
						{/if}
					</div>
				</div>
			</button>
		{/if}
	{/each}
{:catch error}
	<p>{error.message}</p>
{/await}

<style>
	.route-card {
		border-radius: 8px;
		box-shadow: 0px 2px 4px rgba(0, 0, 0, 0.1);
		transition: box-shadow 0.3s ease;
		background-color: #b7b5b54a;
		margin-bottom: 1rem;
	}

	.route-card:hover {
		box-shadow: 0px 4px 8px rgba(0, 0, 0, 0.2);
	}

	.route-info {
		padding: 1rem;
		border-bottom-left-radius: 8px;
		border-bottom-right-radius: 8px;
		display: flex;
		flex-direction: column;
		align-items: center; /* Center route name horizontally */
	}

	.route-number {
		font-size: 1.2rem;
		font-weight: bold;
		color: #333;
		margin-bottom: 0.5rem;
		text-align: center; /* Center text */
	}

	.coordinates {
		font-size: 0.9rem;
		color: #666;
		margin-bottom: 0.5rem;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
		text-align: left; /* Align text to the left */
		width: 100%; /* Ensure full width */
	}
</style>
