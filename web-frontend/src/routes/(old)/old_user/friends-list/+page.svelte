<script lang="ts">
	// TODO: This file is a god damn mess, a *little* bit of refactoring would be good.

	import * as api from '$lib/api';
	import {
		friends,
		users,
		type User,
		type Id,
		removeFriend as removeFriend,
		getUser,
		refreshFriends,
		listUserRoutes,
		saveRouteData
	} from '$lib/store';
	import { page } from '$app/stores';
	import Breadcrumbs from '$lib/Breadcrumbs.svelte';
	import Button from '$lib/components/Button.svelte';
	import Form from '$lib/Form.svelte';
	import FormTextInput from '$lib/FormTextInput.svelte';
	import Separator from '$lib/Separator.svelte';
	import ResultMessage from '$lib/ResultMessage.svelte';
	import { writable } from 'svelte/store';
	import { goto } from '$app/navigation';

	let friendUsername = '';
	let successMessage = '';
	let errorMessage = '';
	let friendRoutesForId: any[] = [];
	const maxRetries = 5;
	const apiKey = 'GwdctTYBnXKj1P4RGPRABBIlMDwdCpMu';

	async function showFriendRoutes(friendId: Id) {
		try {
			const friendRoutes = await listUserRoutes();
			friendRoutesForId = friendRoutes.filter((route) => route.ownerId === friendId);
			if (friendRoutesForId.length > 0) {
				console.log(`Draugo ${friendId} sukurti maršrutai:`);
				friendRoutesForId.forEach((route) => {
					console.log(route);
				});
			} else {
				console.log(`Draugas ${friendId} neturi sukurtų maršrutų.`);
			}
		} catch (error) {
			console.error(`Klaida gaunant draugo ${friendId} maršrutus:`, error);
		}
	}

	async function sendFriendRequest() {
		successMessage = '';
		errorMessage = '';

		// TODO: Show spinner
		const result = await api.sendFriendRequest(friendUsername);
		if (!result.ok) {
			if (api.containsError(result, 'already_sent')) {
				errorMessage = 'Tam vartotojiui užklausa jau išsiųsta';
			} else if (api.containsError(result, 'cant_send_to_yourself')) {
				errorMessage = 'Negalima sau siūsti užklausos';
			} else if (api.containsError(result, 'not_found', 'username')) {
				errorMessage = 'Toks vartotojas neegzistuoja';
			} else if (api.containsError(result, 'already_friends')) {
				errorMessage = 'Jau esate draugai su tuo vartotoju';
			}

			if (!errorMessage) {
				errorMessage = 'Nepavyko išsiūsti užklausos';
			}
			return;
		}

		successMessage = `Išsiūstas pakvietimas į "${friendUsername}"`;
	}

	async function onRemoveFriendClick(friendId: Id) {
		// TODO: Show spinner
		await removeFriend(friendId);

		// TODO: There is a bug, that if the other friend has already removed you, this function will fail.
		// Ideally, it should not fail and just update to not show that friend.
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

	let requestRows = writable<User[]>([]);
	// TODO: show spinner
	api.listIncomingFriendRequests().then(async (result) => {
		if (!result.ok) return; // TODO: show error message

		for (const username of result.body) {
			const user = await getUser(username);
			if (user) {
				$requestRows.push(user);
			}
		}

		$requestRows = $requestRows; // Trigger re-render
	});

	async function onAcceptFriendRequest(username: string) {
		// TODO: Disable button until request finishes
		const result = await api.sendFriendRequest(username);
		if (!result.ok) {
			// TODO: Show error message
			return;
		}

		$requestRows = $requestRows.filter((user) => user.username !== username);

		// TODO: THIS SHOULD REAAALY NOT BE NEEDED.
		// Expose proper function in `store.ts` like 'sendFriendRequest', which would update the friends list.
		// Intead of doing this... For now I'm just too lazy.
		await refreshFriends();
		await updateFriendRows();
	}

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

<Breadcrumbs class="mt-8" url={$page.url} routeId={$page.route.id} />

<Separator class="mt-2" />

<Form class="mt-5 flex flex-row gap-8" on:submit={sendFriendRequest}>
	<FormTextInput
		required
		bind:value={friendUsername}
		class="flex-grow"
		label="Draugo vartotojo vardas"
	/>
	<div class="mt-6">
		<Button>Siųsti pakvietimą</Button>
	</div>
</Form>

<ResultMessage {successMessage} bind:errorMessage />

<div class="mb-4 mt-8 border-b border-slate-400 text-2xl font-semibold">Draugai</div>
{#if $friends.length === 0 && $requestRows.length === 0}
	<div>
		Neturi draugų <i class="fa-regular fa-face-sad-tear align-middle" />
	</div>
{:else}
	<div class="flex flex-col">
		{#each $requestRows as user}
			<div class="flex flex-row rounded border border-slate-700 px-6 py-3">
				<div class="my-auto">
					{user.firstName}
					{user.lastName}
				</div>
				<div class="flex-grow" />
				<Button
					size="sm"
					variant="primary"
					class="mr-auto h-8 w-8"
					on:click={() => onAcceptFriendRequest(user.username)}
				>
					<i class="fa-solid fa-check" />
				</Button>
			</div>
		{/each}

		{#each $friendRows as friend}
			<div class="display: block; clear: both; flex-row rounded border border-slate-700 px-6 py-3">
				<div class="my-auto">
					{friend.firstName}
					{friend.lastName}
				</div>
				<div class="flex-grow" />
				<Button on:click={() => showFriendRoutes(friend.id)} class="w-8; h-8; pt-0; mb-0"
					>Maršrutai</Button
				>
				{#each friendRoutesForId as route}
					{#if friend.id == route.ownerId}
						<button
							class="route-card cursor-pointer border-2 border-slate-700"
							style="display: block; width: 100%; padding: 1rem; margin-bottom: 1rem;"
							on:click={() => {
								saveRouteData.set({
									route: route
								});
								goto('../../display');
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
				<Button
					size="sm"
					variant="red"
					class="mr-auto h-11 w-11"
					on:click={() => onRemoveFriendClick(friend.id)}
				>
					<i class="fa-solid fa-minus" />
				</Button>
			</div>
		{/each}
	</div>
{/if}

<style>
	.route-card {
		display: block;
		clear: both;
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
